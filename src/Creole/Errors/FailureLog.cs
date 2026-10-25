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
/// A rejected filter and a chained non-associative operator are reported behind the cursor, at the start of the input they name.
/// They capture that position's item by value and the end of their span, so building the error reads nothing behind the cursor, and the span covers the longest of them.
/// A chained operator's message is built only when the error is.
/// </para>
/// <para>
/// <c>Lookahead</c>, <c>Not</c> and <c>Longest</c> take a <see cref="Save"/> and may put the failure back as it was with <see cref="Restore"/>.
/// While one is open, the expectations it saved are protected, so a further failure starts a new range of the array instead of overwriting them.
/// <c>Named</c> takes a <see cref="Note"/> and relabels with <see cref="Relabel"/>.
/// </para>
/// <para>
/// Context frames live in an array with links to their parents, so a failure keeps its whole stack as one index.
/// A frame is dropped when its context ends unless the furthest failure needs it.
/// When no saved failure is open, frames that neither the current stack nor the furthest failure needs are compacted away,
/// so the array grows with the depth of nesting rather than the length of the input.
/// </para>
/// <para>
/// Recording stores references and indexes in reusable arrays and allocates nothing once the arrays are large enough.
/// A <see cref="ParseError"/> is built only for the failure that is reported, by <see cref="ToError"/>.
/// </para>
/// </remarks>
internal sealed class FailureLog<T>
{
    private ChainMessage? _chain;
    private int _context = -1;
    private int _count;
    private int _current = -1;
    private long _end = -1;
    private Expected[] _expected = new Expected[8];
    private int _frameCount;
    private ContextFrame[] _frames = new ContextFrame[8];
    private bool _hasItem;
    private T _item = default!;
    private string? _message;
    private int _protected;
    private int _records;
    private int[] _remap = [];
    private int _restoring;
    private int _start;

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

        Array.Fill(_remap, -1, 0, _frameCount);

        for (var i = _current; i >= 0; i = _frames[i].Parent)
        {
            _remap[i] = 0;
        }

        for (var i = _context; i >= 0 && _remap[i] < 0; i = _frames[i].Parent)
        {
            _remap[i] = 0;
        }

        var kept = 0;

        for (var i = 0; i < _frameCount; i++)
        {
            if (_remap[i] < 0)
            {
                continue;
            }

            var frame = _frames[i];

            _frames[kept] = frame with
            {
                Parent = frame.Parent < 0
                    ? -1
                    : _remap[frame.Parent],
            };
            _remap[i] = kept++;
        }

        _frameCount = kept;
        _current = _current < 0
            ? -1
            : _remap[_current];
        _context = _context < 0
            ? -1
            : _remap[_context];
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
            _end = -1;
            _hasItem = false;
            _item = default!;
            _context = _current;
        }

        _records++;
        return true;
    }

    /// <summary>
    /// Forgets every failure and context, so the log can serve another parse.
    /// </summary>
    public void Clear()
    {
        Position = -1;
        _start = 0;
        _count = 0;
        _message = null;
        _chain = null;
        _end = -1;
        _hasItem = false;
        _item = default!;
        _context = -1;
        _current = -1;
        _frameCount = 0;
        _records = 0;
        _protected = 0;
        _restoring = 0;
    }

    /// <summary>
    /// Enters a context with <paramref name="text"/> whose parser starts at <paramref name="start"/>.
    /// </summary>
    public void Enter(string text, long start)
    {
        if (_frameCount == _frames.Length && _restoring == 0)
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
    /// Ends a <see cref="Save"/> and keeps the furthest failure as it is now.
    /// </summary>
    public void Keep(in FailureMark<T> mark)
    {
        _protected = mark.Protected;
        _restoring--;
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
    /// The furthest failure as it is now, for <see cref="Relabel"/>. Unlike <see cref="Save"/>, it protects nothing and needs no ending.
    /// </summary>
    public FailureMark<T> Note() =>
        new(Position, _start, _count, _message, _chain, _end, _hasItem, _item, _context, _records, _protected);

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
    /// <param name="position">Where the input the failure names starts.</param>
    /// <param name="end">Where that input ends.</param>
    /// <param name="hasItem">Whether <paramref name="item"/> holds the item at <paramref name="position"/>.</param>
    /// <param name="item">The item at <paramref name="position"/>, captured before it was consumed.</param>
    /// <param name="message">The filter's message, or null.</param>
    /// <param name="chain">The chained operator's message, or null.</param>
    public void Reject(long position, long end, bool hasItem, T item, string? message, ChainMessage? chain)
    {
        if (!Reaches(position))
        {
            return;
        }

        if (_message is null && _chain is null)
        {
            _message = message;
            _chain = chain;
        }

        if (end > _end)
        {
            _end = end;
        }

        if (hasItem && !_hasItem)
        {
            _item = item;
            _hasItem = true;
        }
    }

    /// <summary>
    /// Replaces the expectations recorded at <paramref name="start"/> since <paramref name="note"/> with <paramref name="label"/>, as <c>Named</c> does when its parser consumed nothing.
    /// </summary>
    /// <remarks>
    /// Nothing changes unless the furthest failure is at <paramref name="start"/> and a failure recorded since the note is still part of it.
    /// A failure that expected nothing in particular counts. Expectations recorded at <paramref name="start"/> before the note stay, ahead of the label.
    /// A message is never removed.
    /// </remarks>
    public void Relabel(in FailureMark<T> note, long start, Expected label)
    {
        if (Position != start || _records == note.Records)
        {
            return;
        }

        _count = note.Position == start
            ? note.Count
            : _start;
        Add(label);
        _records++;
    }

    /// <summary>
    /// Ends a <see cref="Save"/> and puts the furthest failure back as it was then.
    /// </summary>
    public void Restore(in FailureMark<T> mark)
    {
        Position = mark.Position;
        _start = mark.Start;
        _count = mark.Count;
        _message = mark.Message;
        _chain = mark.Chain;
        _end = mark.End;
        _hasItem = mark.HasItem;
        _item = mark.Item;
        _context = mark.Context;
        _records = mark.Records;
        _protected = mark.Protected;
        _restoring--;
    }

    /// <summary>
    /// Saves the furthest failure so that <see cref="Restore"/> can put it back, protecting its expectations until <see cref="Restore"/> or <see cref="Keep"/> ends the save.
    /// </summary>
    public FailureMark<T> Save()
    {
        var mark = Note();

        _protected = _count;
        _restoring++;

        return mark;
    }

    /// <summary>
    /// The error for the furthest failure, or null if none was recorded.
    /// </summary>
    /// <param name="cursor">A cursor over the input the failures were recorded in, which still holds the item at <see cref="Position"/>.</param>
    /// <param name="layer">The name of the layer, such as <c>parser</c>.</param>
    /// <param name="spanOf">The source span of the items from a start position up to an end position; for an empty range, a zero-width span there.</param>
    /// <remarks>
    /// The span covers the item at the position, or is zero-width at the end of the input, widened to cover the input a failure reported behind the cursor named.
    /// What was found is the message if one was recorded, otherwise that item or the end of input.
    /// An item captured by a failure reported behind the cursor is used rather than read.
    /// Expected items that show the same are listed once, in the order first recorded.
    /// Context lines come from the stack of the first failure at the position, innermost first, leaving out contexts that started there.
    /// </remarks>
    public ParseError? ToError(ref readonly Cursor<T> cursor, string layer, Func<long, long, SourceSpan> spanOf)
    {
        if (Position < 0)
        {
            return null;
        }

        var item = _item;
        var atItem = _hasItem || cursor.TryPeekAt(Position, out item);
        var span = spanOf(Position, Math.Max(atItem ? Position + 1 : Position, _end));
        var message = _message ?? _chain?.Text(_hasItem, _item);

        Unexpected unexpected = message is not null
            ? new CustomMessage(message)
            : atItem
                ? new UnexpectedItem(Display<T>.Item(item))
                : new EndOfInput();

        var expected = new List<Expectation>(_count - _start);

        for (var i = _start; i < _count; i++)
        {
            var expectation = _expected[i].Build();

            if (!expected.Exists(e => Equals(e.Value, expectation.Value)))
            {
                expected.Add(expectation);
            }
        }

        var context = new List<ContextLine>();

        for (var i = _context; i >= 0; i = _frames[i].Parent)
        {
            var frame = _frames[i];

            if (frame.Start < Position)
            {
                context.Add(new ContextLine(frame.Text, spanOf(frame.Start, frame.Start + 1)));
            }
        }

        return new ParseError(span, layer, unexpected, expected, context);
    }
}
