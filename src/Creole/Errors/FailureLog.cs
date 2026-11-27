namespace Creole;

/// <summary>
/// One layer's furthest failure while it parses: where it is, what was expected there, any message, and the context it was recorded in.
/// </summary>
/// <remarks>
/// <para>
/// A failure before the furthest one is ignored, and one past it replaces it.
/// One at the same position merges into it: its expectation is added unless already there, and the first message recorded there is kept.
/// The first failure at a position keeps its context stack.
/// </para>
/// <para>
/// A rejected filter and a chained non-associative operator are reported behind the cursor, at the start of the input they name, and the span covers the longest of them.
/// A chained operator's message is built only when the error is.
/// </para>
/// <para>
/// The log records positions, not items. An error reads the items and spans it needs when it is built, from input that still holds them.
/// A stream buffer may release input before that, so it first calls <see cref="Preserve"/>, which copies the item and span at each position an error may still read:
/// the furthest failure's position and the end of the input it names, the same for each saved failure, the start of each context frame, each start that <see cref="HoldStart"/> holds, and the same again for the failure each diagnostic reports.
/// A buffer releases only consumed items, which a mode change never discards, so no copy is of an item lexed under an abandoned mode.
/// </para>
/// <para>
/// A mode change at a position forgets what the furthest failure says about the items there and after it, which it discarded (<see cref="LowerAt"/>).
/// </para>
/// <para>
/// <c>Lookahead</c>, <c>Not</c> and <c>Longest</c> take a <see cref="Save"/> and may put the failure back as it was with <see cref="Restore"/>.
/// While one is open, the expectations it saved are protected, so a further failure starts a new range of the array instead of overwriting them.
/// <c>Named</c> takes a <see cref="Note"/> and relabels with <see cref="Relabel"/>.
/// </para>
/// <para>
/// Context frames live in an array with links to their parents, so a failure keeps its whole stack as one index.
/// A frame is dropped when its context ends unless the furthest failure needs it.
/// Frames that neither the current stack nor the furthest failure needs are compacted away, so the array grows with the depth of nesting rather than the length of the input.
/// While a save is open, the frames that existed when it was taken stay where they are, because the saved failure may point into them.
/// </para>
/// <para>
/// Recording stores references and indexes in reusable arrays and allocates nothing once the arrays are large enough.
/// A <see cref="ParseError"/> is built only for the failure that is reported, by <see cref="ToError"/>.
/// </para>
/// <para>
/// The log also holds the diagnostics the layer raised, with what the layer can give back when one is withdrawn.
/// </para>
/// </remarks>
internal sealed partial class FailureLog<T>
{
    private readonly record struct HeldStart(long Position, ItemCopy? Copy);

    private readonly record struct ItemCopy(T Item, SourceSpan Span);

    /// <summary>
    /// The parts of a saved failure that a stream buffer may need to copy before it releases input, and where its restore is lowered.
    /// </summary>
    /// <param name="Position">The saved failure's position.</param>
    /// <param name="End">The end of the input a failure reported behind the cursor named there, or -1.</param>
    /// <param name="At">The item at <paramref name="Position"/>, once a stream buffer copied it.</param>
    /// <param name="EndSpan">The span of the item before <paramref name="End"/>, once a stream buffer copied it.</param>
    /// <param name="Change">The position of the first change of mode made after the save, or -1 if none has been made.</param>
    /// <param name="ChangeAt">The item at <paramref name="Change"/>, once a stream buffer copied it.</param>
    private readonly record struct SavedFailure(long Position, long End, ItemCopy? At, SourceSpan? EndSpan, long Change, ItemCopy? ChangeAt);

    /// <summary>
    /// The context of a failure that a mode change lowered: none yet, because the next failure recorded at its position supplies it.
    /// </summary>
    private const int Bare = -2;

    private ItemCopy? _at;
    private ChainMessage? _chain;
    private int _context = -1;
    private int _count;
    private int _current = -1;
    private long _end = -1;
    private SourceSpan? _endSpan;
    private Expected[] _expected = new Expected[8];
    private int _frameCount;
    private int _frameFloor;
    private ContextFrame[] _frames = new ContextFrame[8];
    private int _generation;
    private int _heldCount;
    private HeldStart[] _heldStarts = new HeldStart[8];
    private string? _message;
    private int _protected;
    private int _records;
    private bool _rejected;
    private int[] _remap = [];
    private int _saveCount;
    private SavedFailure[] _saves = new SavedFailure[8];
    private int _start;
    private int _unchanged;

    /// <summary>
    /// The number of context frames the log holds, for tests of its memory bound.
    /// </summary>
    internal int FrameCount => _frameCount;

    /// <summary>
    /// The position of the furthest failure, or -1 if none has been recorded.
    /// </summary>
    public long Position { get; private set; } = -1;

    private void Add(Expected expected)
    {
        for (var i = _start; i < _count; i++)
        {
            if (ReferenceEquals(_expected[i], expected))
            {
                return;
            }
        }

        if (_count == _expected.Length)
        {
            Array.Resize(ref _expected, _count * 2);
        }

        _expected[_count++] = expected;
    }

    private void Compact()
    {
        if (_remap.Length < _frames.Length)
        {
            _remap = new int[_frames.Length];
        }

        Array.Fill(_remap, -1, _frameFloor, _frameCount - _frameFloor);

        for (var i = _current; i >= _frameFloor; i = _frames[i].Parent)
        {
            _remap[i] = 0;
        }

        for (var i = _context; i >= _frameFloor && _remap[i] < 0; i = _frames[i].Parent)
        {
            _remap[i] = 0;
        }

        var kept = _frameFloor;

        for (var i = _frameFloor; i < _frameCount; i++)
        {
            if (_remap[i] < 0)
            {
                continue;
            }

            var frame = _frames[i];

            _frames[kept] = frame with
            {
                Parent = frame.Parent < _frameFloor
                    ? frame.Parent
                    : _remap[frame.Parent],
            };
            _remap[i] = kept++;
        }

        _frameCount = kept;
        _current = _current < _frameFloor
            ? _current
            : _remap[_current];
        _context = _context < _frameFloor
            ? _context
            : _remap[_context];
    }

    private List<ContextLine> ContextLines(int innermost, long position, Func<long, long, SourceSpan> spanOf)
    {
        var context = new List<ContextLine>();

        for (var i = innermost; i >= 0; i = _frames[i].Parent)
        {
            var frame = _frames[i];

            if (frame.Start < position)
            {
                context.Add(new ContextLine(frame.Text, frame.StartSpan ?? spanOf(frame.Start, frame.Start + 1)));
            }
        }

        return context;
    }

    /// <summary>
    /// The furthest failure as it is now, with its context stack copied out of the frame array.
    /// </summary>
    private FailureCopy Copy() =>
        new(Position, _expected[_start.._count], _message, _chain, _rejected, _end, _at, _endSpan, Stack(_context), _context == Bare);

    /// <summary>
    /// Lowers the furthest failure at <paramref name="index"/>, as <see cref="LowerAt"/> does, giving a failure lowered there <paramref name="at"/> as the item there, a copy a stream buffer took before releasing it.
    /// </summary>
    private void Lower(long index, ItemCopy? at)
    {
        if (Position > index)
        {
            Position = index;
            _start = _protected;
            _count = _protected;
            _message = null;
            _chain = null;
            _rejected = false;
            _end = -1;
            _endSpan = null;
            _at = at;
            _context = Bare;
            _generation++;
            return;
        }

        if (_end > index)
        {
            _end = -1;
            _endSpan = null;
            _chain = null;

            if (_rejected)
            {
                _message = null;
                _rejected = false;
            }
        }
    }

    private bool Reaches(long position)
    {
        if (position < Position)
        {
            return false;
        }

        if (position > Position)
        {
            Position = position;
            _start = _protected;
            _count = _protected;
            _message = null;
            _chain = null;
            _rejected = false;
            _end = -1;
            _endSpan = null;
            _at = null;
            _context = _current;
        }
        else if (_context == Bare)
        {
            _context = _current;
        }

        if (_at is null
            && _heldCount > 0
            && _heldStarts[_heldCount - 1] is { Copy: { } copy } held
            && held.Position == position
        )
        {
            _at = copy;
        }

        _records++;
        return true;
    }

    /// <summary>
    /// Copies the context stack from <paramref name="innermost"/> out of the frame array, innermost first.
    /// </summary>
    /// <remarks>
    /// The copies keep their <see cref="ContextFrame.Parent"/> indexes, which mean nothing outside the array.
    /// </remarks>
    private ContextFrame[] Stack(int innermost)
    {
        var depth = 0;

        for (var i = innermost; i >= 0; i = _frames[i].Parent)
        {
            depth++;
        }

        var stack = new ContextFrame[depth];

        for (int i = innermost, j = 0; i >= 0; i = _frames[i].Parent, j++)
        {
            stack[j] = _frames[i];
        }

        return stack;
    }

    /// <summary>
    /// Records a change of mode at <paramref name="index"/>: forgets what the furthest failure says about the items from there on, as <see cref="LowerAt"/> does, and gives the position to each open save that no change has followed yet.
    /// </summary>
    /// <remarks>
    /// A save's restore is lowered at the first change made after it. A stream buffer copies the item there before it releases it, because the cursor may have moved far past it by the time the save is restored.
    /// The saves no change has followed are those taken since the last change, at the top of the stack of saves.
    /// </remarks>
    public void ChangeAt(long index)
    {
        LowerAt(index);

        for (var i = _unchanged; i < _saveCount; i++)
        {
            _saves[i] = _saves[i] with { Change = index };
        }

        _unchanged = _saveCount;
    }

    /// <summary>
    /// Forgets every failure, context and diagnostic, so the log can serve another parse.
    /// </summary>
    public void Clear()
    {
        Position = -1;
        _start = 0;
        _count = 0;
        _message = null;
        _chain = null;
        _rejected = false;
        _end = -1;
        _endSpan = null;
        _at = null;
        _context = -1;
        _current = -1;
        _frameCount = 0;
        _frameFloor = 0;
        _heldCount = 0;
        _records = 0;
        _protected = 0;
        _saveCount = 0;
        _unchanged = 0;
        _generation = 0;
        _diagnostics.Clear();
        StepStart = 0;
    }

    /// <summary>
    /// The context lines in force now, for an error at <paramref name="position"/>: innermost first, leaving out contexts that started there.
    /// </summary>
    /// <param name="position">Where the error is.</param>
    /// <param name="spanOf">The source span of the items from a start position up to an end position.</param>
    public List<ContextLine> ContextAt(long position, Func<long, long, SourceSpan> spanOf) =>
        ContextLines(_current, position, spanOf);

    /// <summary>
    /// Stops holding the start that the matching <see cref="HoldStart"/> held.
    /// </summary>
    public void DropStart() => _heldCount--;

    /// <summary>
    /// Forgets the furthest failure, leaving none, as each item of a <c>ParseMany</c> starts.
    /// </summary>
    /// <remarks>
    /// Diagnostics, and saves held by enclosing constructs, are untouched.
    /// </remarks>
    public void ForgetFailure()
    {
        Position = -1;
        _start = _protected;
        _count = _protected;
        _message = null;
        _chain = null;
        _rejected = false;
        _end = -1;
        _endSpan = null;
        _at = null;
        _context = -1;
        _generation++;
    }

    /// <summary>
    /// Enters a context with <paramref name="text"/> whose parser starts at <paramref name="start"/>.
    /// </summary>
    public void Enter(string text, long start)
    {
        if (_frameCount == _frames.Length)
        {
            Compact();
        }

        if (_frameCount == _frames.Length)
        {
            Array.Resize(ref _frames, _frameCount * 2);
        }

        _frames[_frameCount] = new ContextFrame(text, start, _current);
        _current = _frameCount++;
    }

    /// <summary>
    /// Holds <paramref name="position"/>, where a filter, a chained operator or a custom scanner starts, until the matching <see cref="DropStart"/>.
    /// </summary>
    /// <remarks>
    /// A failure recorded there may come after a stream buffer has released the item, and <see cref="Preserve"/> copies it first.
    /// A failure recorded at the innermost held start takes that copy.
    /// </remarks>
    public void HoldStart(long position)
    {
        if (_heldCount == _heldStarts.Length)
        {
            Array.Resize(ref _heldStarts, _heldCount * 2);
        }

        _heldStarts[_heldCount++] = new HeldStart(position, null);
    }

    /// <summary>
    /// Ends a <see cref="Save"/> and keeps the furthest failure as it is now.
    /// </summary>
    public void Keep(in FailureMark mark)
    {
        _saveCount--;
        _unchanged = Math.Min(_unchanged, _saveCount);
        _protected = mark.Protected;
        _frameFloor = mark.FrameFloor;
    }

    /// <summary>
    /// Leaves the innermost context.
    /// </summary>
    public void Leave()
    {
        var leaving = _current;

        _current = _frames[leaving].Parent;

        if (_context < leaving)
        {
            _frameCount = leaving;
        }
    }

    /// <summary>
    /// Forgets what the furthest failure says about the items at <paramref name="index"/> and after it, because a mode change there discarded them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A failure past <paramref name="index"/> becomes a failure there that expects nothing and has no message and no context.
    /// It replaces the furthest failure as a further failure does, so neither a later failure there nor <c>Named</c> brings back what it expected.
    /// The next failure recorded there supplies the context, as the first failure at a position does.
    /// Lowering records nothing, so it never makes <c>Named</c> relabel on its own.
    /// </para>
    /// <para>
    /// A failure at or before <paramref name="index"/> whose span reaches past it, because a rejected filter or a chained operator named input there, loses that end, and the message the rejection or the operator gave it.
    /// A message recorded with <c>Fail</c> stays.
    /// Any other failure is unchanged.
    /// </para>
    /// </remarks>
    public void LowerAt(long index) => Lower(index, at: null);

    /// <summary>
    /// The furthest failure as it is now, for <see cref="Relabel"/>. Unlike <see cref="Save"/>, it protects nothing and needs no ending.
    /// </summary>
    public FailureMark Note() =>
        new(Position, _start, _count, _message, _chain, _rejected, _end, _context, _records, _protected, _frameFloor, _generation);

    /// <summary>
    /// Copies the item and span at each position an error may still read from <paramref name="start"/> up to <paramref name="end"/>, which <paramref name="buffer"/> is about to release.
    /// </summary>
    public void Preserve(StreamBuffer<T> buffer, long start, long end)
    {
        bool Within(long position) => position >= start && position < end;

        ItemCopy CopyAt(long position) => new(buffer.ItemAt(position), buffer.SpanOf(position, position + 1));

        if (_at is null && Within(Position))
        {
            _at = CopyAt(Position);
        }

        if (_endSpan is null && Within(_end - 1))
        {
            _endSpan = buffer.SpanOf(_end - 1, _end);
        }

        for (var i = 0; i < _saveCount; i++)
        {
            var saved = _saves[i];

            saved = saved with
            {
                At = saved.At is null && Within(saved.Position)
                    ? CopyAt(saved.Position)
                    : saved.At,
                EndSpan = saved.EndSpan is null && Within(saved.End - 1)
                    ? buffer.SpanOf(saved.End - 1, saved.End)
                    : saved.EndSpan,
                ChangeAt = saved.ChangeAt is null && Within(saved.Change)
                    ? CopyAt(saved.Change)
                    : saved.ChangeAt,
            };

            _saves[i] = saved;
        }

        for (var i = 0; i < _heldCount; i++)
        {
            if (_heldStarts[i] is { Copy: null } held && Within(held.Position))
            {
                _heldStarts[i] = held with { Copy = CopyAt(held.Position) };
            }
        }

        for (var i = 0; i < _frameCount; i++)
        {
            if (_frames[i] is { StartSpan: null } frame && Within(frame.Start))
            {
                _frames[i] = frame with { StartSpan = buffer.SpanOf(frame.Start, frame.Start + 1) };
            }
        }

        foreach (var diagnostic in _diagnostics)
        {
            diagnostic.Failure.Preserve(buffer, start, end);
        }
    }

    /// <summary>
    /// Records a failure at <paramref name="position"/> that expected <paramref name="expected"/>, or nothing in particular when it is null.
    /// </summary>
    public void Record(long position, Expected? expected)
    {
        if (Reaches(position) && expected is not null)
        {
            Add(expected);
        }
    }

    /// <summary>
    /// Records, as one failure at <paramref name="position"/>, the failures of several parsers that expected <paramref name="expected"/> in turn.
    /// </summary>
    public void Record(long position, Expected[] expected)
    {
        if (!Reaches(position))
        {
            return;
        }

        foreach (var expectation in expected)
        {
            Add(expectation);
        }
    }

    /// <summary>
    /// Records a failure at <paramref name="position"/> that reports <paramref name="message"/>, as <c>Fail(message)</c> does.
    /// </summary>
    public void Record(long position, string message)
    {
        if (Reaches(position) && _message is null && _chain is null)
        {
            _message = message;
        }
    }

    /// <summary>
    /// Records a failure reported behind the cursor at <paramref name="position"/>, naming the input up to <paramref name="end"/>: a rejected filter, with <paramref name="message"/> if it has one, or a chained operator, with <paramref name="chain"/>.
    /// </summary>
    /// <remarks>
    /// <paramref name="position"/> must be the start the innermost <see cref="HoldStart"/> holds, and <paramref name="end"/> the cursor's position.
    /// </remarks>
    /// <param name="position">Where the input the failure names starts.</param>
    /// <param name="end">Where that input ends.</param>
    /// <param name="message">The filter's message, or null.</param>
    /// <param name="chain">The chained operator's message, or null.</param>
    public void Reject(long position, long end, string? message, ChainMessage? chain)
    {
        if (!Reaches(position))
        {
            return;
        }

        if (_message is null && _chain is null)
        {
            _message = message;
            _chain = chain;
            _rejected = message is not null;
        }

        if (end > _end)
        {
            _end = end;
            _endSpan = null;
        }
    }

    /// <summary>
    /// Replaces the expectations recorded at <paramref name="start"/> since <paramref name="note"/> with <paramref name="label"/>, as <c>Named</c> does when its parser consumed nothing.
    /// </summary>
    /// <remarks>
    /// Nothing changes unless the furthest failure is at <paramref name="start"/> and a failure recorded since the note is still part of it.
    /// A failure that expected nothing in particular counts. Expectations recorded at <paramref name="start"/> before the note stay, ahead of the label, unless the furthest failure was taken out of the log since the note.
    /// A message is never removed.
    /// </remarks>
    public void Relabel(in FailureMark note, long start, Expected label)
    {
        if (Position != start || _records == note.Records)
        {
            return;
        }

        _count = note.Position == start && note.Generation == _generation
            ? note.Count
            : _start;
        Add(label);
        _records++;
    }

    /// <summary>
    /// Puts the furthest failure back as it was at the innermost open <see cref="Save"/>, which stays open.
    /// </summary>
    /// <remarks>
    /// If a change of mode was made after the save, the failure put back forgets what it says about the items from that change on, as <see cref="LowerAt"/> does.
    /// </remarks>
    public void Reset(in FailureMark mark)
    {
        var saved = _saves[_saveCount - 1];

        Position = mark.Position;
        _start = mark.Start;
        _count = mark.Count;
        _message = mark.Message;
        _chain = mark.Chain;
        _rejected = mark.Rejected;
        _end = mark.End;
        _endSpan = saved.EndSpan;
        _at = saved.At;
        _context = mark.Context;
        _records = mark.Records;
        _generation = mark.Generation;

        if (saved.Change >= 0)
        {
            Lower(saved.Change, saved.ChangeAt);
        }
    }

    /// <summary>
    /// Ends a <see cref="Save"/> and puts the furthest failure back as it was then.
    /// </summary>
    public void Restore(in FailureMark mark)
    {
        Reset(in mark);
        Keep(in mark);
    }

    /// <summary>
    /// Saves the furthest failure so that <see cref="Restore"/> can put it back, protecting its expectations and context frames until <see cref="Restore"/> or <see cref="Keep"/> ends the save.
    /// </summary>
    public FailureMark Save()
    {
        var mark = Note();

        if (_saveCount == _saves.Length)
        {
            Array.Resize(ref _saves, _saveCount * 2);
        }

        _saves[_saveCount++] = new SavedFailure(Position, _end, _at, _endSpan, Change: -1, ChangeAt: null);
        _protected = _count;
        _frameFloor = _frameCount;

        return mark;
    }

    /// <summary>
    /// The error for the furthest failure, or null if none was recorded.
    /// </summary>
    /// <param name="cursor">A cursor over the input the failures were recorded in, which still holds every item the log needs and has not copied.</param>
    /// <param name="layer">The name of the layer, such as <c>parser</c>.</param>
    /// <param name="spanOf">The source span of the items from a start position up to an end position; for an empty range, a zero-width span there.</param>
    /// <remarks>
    /// The span covers the item at the position, or is zero-width at the end of the input, widened to cover the input a failure reported behind the cursor named.
    /// What was found is the message if one was recorded, otherwise that item or the end of input.
    /// An item or span copied before a stream buffer released it is used rather than read.
    /// Expected items that show the same are listed once, in the order first recorded.
    /// Context lines come from the stack of the first failure at the position, innermost first, leaving out contexts that started there.
    /// </remarks>
    public ParseError? ToError(ref readonly Cursor<T> cursor, string layer, Func<long, long, SourceSpan> spanOf) =>
        Position < 0
            ? null
            : Copy().ToError(in cursor, layer, spanOf);
}
