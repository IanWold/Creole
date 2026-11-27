using System.Diagnostics;

namespace Creole;

internal sealed partial class FailureLog<T>
{
    /// <summary>
    /// A furthest failure copied out of the log: what an error is built from, and what a diagnostic built from the furthest failure keeps.
    /// </summary>
    /// <param name="position">Where the failure is.</param>
    /// <param name="expectations">What was expected there, in the order recorded.</param>
    /// <param name="message">The first message recorded there, or null.</param>
    /// <param name="chain">The chained operator's message, if that came first, or null.</param>
    /// <param name="rejected">Whether a rejected filter gave it <paramref name="message"/>.</param>
    /// <param name="end">The furthest end of the input a failure reported behind the cursor named there, or -1.</param>
    /// <param name="at">The item at the position, copied before a stream buffer released it, or null.</param>
    /// <param name="endSpan">The span of the item before <paramref name="end"/>, copied before a stream buffer released it, or null.</param>
    /// <param name="context">The context stack of the first failure there, innermost first.</param>
    /// <param name="bare">Whether a mode change lowered it, so that the next failure recorded there supplies its context.</param>
    private sealed class FailureCopy(long position, Expected[] expectations, string? message, ChainMessage? chain, bool rejected, long end, ItemCopy? at, SourceSpan? endSpan, ContextFrame[] context, bool bare)
    {
        public ItemCopy? At { get; private set; } = at;

        public bool Bare { get; } = bare;

        public ChainMessage? Chain { get; } = chain;

        public ContextFrame[] Context { get; } = context;

        public long End { get; } = end;

        public SourceSpan? EndSpan { get; private set; } = endSpan;

        public Expected[] Expectations { get; } = expectations;

        public string? Message { get; } = message;

        public long Position { get; } = position;

        public bool Rejected { get; } = rejected;

        /// <summary>
        /// Copies the item and spans this failure may still read from <paramref name="start"/> up to <paramref name="end"/>, which <paramref name="buffer"/> is about to release.
        /// </summary>
        public void Preserve(StreamBuffer<T> buffer, long start, long end)
        {
            bool Within(long position) => position >= start && position < end;

            if (At is null && Within(Position))
            {
                At = new ItemCopy(buffer.ItemAt(Position), buffer.SpanOf(Position, Position + 1));
            }

            if (EndSpan is null && Within(End - 1))
            {
                EndSpan = buffer.SpanOf(End - 1, End);
            }

            for (var i = 0; i < Context.Length; i++)
            {
                if (Context[i] is { StartSpan: null } frame && Within(frame.Start))
                {
                    Context[i] = frame with { StartSpan = buffer.SpanOf(frame.Start, frame.Start + 1) };
                }
            }
        }

        /// <summary>
        /// The error for this failure. See <see cref="FailureLog{T}.ToError"/>.
        /// </summary>
        public ParseError ToError(ref readonly Cursor<T> cursor, string layer, Func<long, long, SourceSpan> spanOf)
        {
            T item;
            bool atItem;

            if (At is { } copy)
            {
                item = copy.Item;
                atItem = true;
            }
            else
            {
                atItem = cursor.TryPeekAt(Position, out item);
            }

            var first = At?.Span ?? spanOf(Position, atItem ? Position + 1 : Position);
            var last = End > (atItem ? Position + 1 : Position)
                ? EndSpan ?? spanOf(End - 1, End)
                : first;
            var span = new SourceSpan(first.Source, first.Start, last.End, first.CharStart, last.CharEnd);
            var message = Message ?? Chain?.Text(atItem, item);

            Unexpected unexpected = message is not null
                ? new CustomMessage(message)
                : atItem
                    ? new UnexpectedItem(Display<T>.Item(item))
                    : new EndOfInput();

            var expected = new List<Expectation>(Expectations.Length);

            for (var i = 0; i < Expectations.Length; i++)
            {
                var expectation = Expectations[i].Build();

                if (!expected.Exists(e => Equals(e.Value, expectation.Value)))
                {
                    expected.Add(expectation);
                }
            }

            var lines = new List<ContextLine>();

            foreach (var frame in Context)
            {
                if (frame.Start < Position)
                {
                    lines.Add(new ContextLine(frame.Text, frame.StartSpan ?? spanOf(frame.Start, frame.Start + 1)));
                }
            }

            return new ParseError(span, layer, unexpected, expected, lines);
        }
    }

    /// <summary>
    /// A diagnostic the layer raised: the failure it reports, the start of the step that raised it, and whether withdrawing it gives that failure back to the log.
    /// </summary>
    private readonly record struct Diagnostic(FailureCopy Failure, long Step, bool GivesBack);

    private readonly List<Diagnostic> _diagnostics = [];

    /// <summary>
    /// Where the step started that raised each diagnostic held, in the order raised, for tests of what a restart keeps.
    /// </summary>
    internal IEnumerable<long> DiagnosticSteps => _diagnostics.Select(static diagnostic => diagnostic.Step);

    /// <summary>
    /// How many diagnostics the layer has raised and not withdrawn. A mark saves it, and <see cref="Withdraw"/> goes back to it.
    /// </summary>
    public int DiagnosticCount => _diagnostics.Count;

    /// <summary>
    /// Where the step the layer is running started. A diagnostic raised now records it, so <see cref="WithdrawFromStep"/> can find it.
    /// </summary>
    /// <remarks>
    /// A producing layer sets it at the start of each step. A layer that runs in one step leaves it at 0.
    /// </remarks>
    public long StepStart { get; set; }

    /// <summary>
    /// Records <paramref name="failure"/> again, as any failure is recorded, after the failures recorded since it was taken out.
    /// </summary>
    /// <remarks>
    /// A failure before the furthest one is ignored. One past it replaces it and brings back its own context stack.
    /// One at the same position adds its expectations after those recorded there since, and its message only if there is none.
    /// If a mode change lowered the furthest failure since, the failure given back supplies its context, as the next failure there does.
    /// </remarks>
    private void GiveBack(FailureCopy failure)
    {
        var suppliesContext = failure.Position > Position || (failure.Position == Position && _context == Bare);

        if (!Reaches(failure.Position))
        {
            return;
        }

        foreach (var expected in failure.Expectations)
        {
            Add(expected);
        }

        if (_message is null && _chain is null)
        {
            _message = failure.Message;
            _chain = failure.Chain;
            _rejected = failure.Rejected;
        }

        if (failure.End > _end)
        {
            _end = failure.End;
            _endSpan = failure.EndSpan;
        }

        _at ??= failure.At;

        if (suppliesContext)
        {
            _context = failure.Bare
                ? Bare
                : Restack(failure.Context);
        }
    }

    /// <summary>
    /// Adds <paramref name="stack"/>, innermost first, to the frame array as a chain of its own, and returns the index of its innermost frame, or -1 if it is empty.
    /// </summary>
    private int Restack(ContextFrame[] stack)
    {
        if (_frameCount + stack.Length > _frames.Length)
        {
            Compact();
        }

        if (_frameCount + stack.Length > _frames.Length)
        {
            Array.Resize(ref _frames, Math.Max(_frames.Length * 2, _frameCount + stack.Length));
        }

        var parent = -1;

        for (var i = stack.Length - 1; i >= 0; i--)
        {
            _frames[_frameCount] = stack[i] with { Parent = parent };
            parent = _frameCount++;
        }

        return parent;
    }

    /// <summary>
    /// Takes the furthest failure out of the log, leaving none, and returns it.
    /// </summary>
    /// <remarks>
    /// Saves held by enclosing constructs are untouched, so a construct that restores one puts back the failure it saved.
    /// </remarks>
    private FailureCopy Take()
    {
        var failure = Copy();

        ForgetFailure();

        return failure;
    }

    /// <summary>
    /// Raises a diagnostic at <paramref name="position"/> that reports <paramref name="message"/>, in the context in force now.
    /// The furthest failure is untouched, and withdrawing the diagnostic gives nothing back.
    /// </summary>
    public void Raise(long position, string message) =>
        _diagnostics.Add(new Diagnostic(new FailureCopy(position, [], message, null, rejected: false, -1, null, null, Stack(_current), bare: false), StepStart, GivesBack: false));

    /// <summary>
    /// Raises a diagnostic that reports the furthest failure, and takes that failure out of the log, so it is not reported twice.
    /// Withdrawing the diagnostic gives the failure back.
    /// </summary>
    /// <param name="position">Where to report a failure that expected nothing in particular, in the context in force now, if none has been recorded.</param>
    public void RaiseFromFurthestFailure(long position) =>
        _diagnostics.Add(
            Position < 0
                ? new Diagnostic(new FailureCopy(position, [], null, null, rejected: false, -1, null, null, Stack(_current), bare: false), StepStart, GivesBack: false)
                : new Diagnostic(Take(), StepStart, GivesBack: true)
        );

    /// <summary>
    /// The errors for the diagnostics the layer has raised and not withdrawn, in the order raised.
    /// </summary>
    /// <param name="cursor">A cursor over the input the diagnostics were raised in, which still holds every item they need and have not copied.</param>
    /// <param name="layer">The name of the layer, such as <c>parser</c>.</param>
    /// <param name="spanOf">The source span of the items from a start position up to an end position; for an empty range, a zero-width span there.</param>
    public List<ParseError> ToDiagnostics(ref readonly Cursor<T> cursor, string layer, Func<long, long, SourceSpan> spanOf)
    {
        var errors = new List<ParseError>(_diagnostics.Count);

        foreach (var diagnostic in _diagnostics)
        {
            errors.Add(diagnostic.Failure.ToError(in cursor, layer, spanOf));
        }

        return errors;
    }

    /// <summary>
    /// Withdraws the diagnostics raised after the first <paramref name="count"/>, as a rewind to a mark that saved that count does.
    /// Each one built from the furthest failure gives that failure back, in the order they were raised.
    /// </summary>
    public void Withdraw(int count)
    {
        Debug.Assert(count <= _diagnostics.Count, "A withdrawal to more diagnostics than are held.");

        if (count == _diagnostics.Count)
        {
            return;
        }

        for (var i = count; i < _diagnostics.Count; i++)
        {
            if (_diagnostics[i] is { GivesBack: true } diagnostic)
            {
                GiveBack(diagnostic.Failure);
            }
        }

        _diagnostics.RemoveRange(count, _diagnostics.Count - count);
    }

    /// <summary>
    /// Withdraws every diagnostic raised by a step that started at or after <paramref name="stepStart"/>, because those steps are run again or their outputs discarded.
    /// </summary>
    /// <remarks>
    /// Nothing is given back: a step that runs again records its own failures.
    /// </remarks>
    public void WithdrawFromStep(long stepStart) =>
        _diagnostics.RemoveAll(diagnostic => diagnostic.Step >= stepStart);
}
