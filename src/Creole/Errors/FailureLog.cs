namespace Creole;

/// <summary>
/// One layer's furthest failure while it parses: where it is, what was expected there, and any message.
/// </summary>
/// <remarks>
/// <para>
/// A failure before the furthest one is ignored, and one past it replaces it.
/// One at the same position merges into it: its expectation is added unless already there, and the first message recorded there is kept.
/// </para>
/// <para>
/// Recording stores references in a reusable array and allocates nothing once the array is large enough.
/// A <see cref="ParseError"/> is built only for the failure that is reported, by <see cref="ToError"/>.
/// </para>
/// </remarks>
internal sealed class FailureLog<T>
{
    private int _count;
    private Expected[] _expected = new Expected[8];
    private string? _message;

    /// <summary>
    /// The position of the furthest failure, or -1 if none has been recorded.
    /// </summary>
    public long Position { get; private set; } = -1;

    private void Add(Expected expected)
    {
        for (var i = 0; i < _count; i++)
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

    private bool Reaches(long position)
    {
        if (position < Position)
        {
            return false;
        }

        if (position > Position)
        {
            Position = position;
            _count = 0;
            _message = null;
        }

        return true;
    }

    /// <summary>
    /// Forgets every failure, so the log can serve another parse.
    /// </summary>
    public void Clear()
    {
        Position = -1;
        _count = 0;
        _message = null;
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
    /// Records a failure at <paramref name="position"/> that reports <paramref name="message"/>, as <c>Fail(message)</c> does.
    /// </summary>
    public void Record(long position, string message)
    {
        if (Reaches(position))
        {
            _message ??= message;
        }
    }

    /// <summary>
    /// The error for the furthest failure, or null if none was recorded.
    /// </summary>
    /// <param name="cursor">A cursor over the input the failures were recorded in, which still holds the item at <see cref="Position"/>.</param>
    /// <param name="layer">The name of the layer, such as <c>parser</c>.</param>
    /// <param name="spanOf">The source span of the items from a start position up to an end position; for an empty range, a zero-width span there.</param>
    /// <remarks>
    /// The span covers the item at the position, or is zero-width at the end of the input.
    /// What was found is the message if one was recorded, otherwise that item or the end of input.
    /// Expected items that show the same are listed once, in the order first recorded.
    /// </remarks>
    public ParseError? ToError(ref readonly Cursor<T> cursor, string layer, Func<long, long, SourceSpan> spanOf)
    {
        if (Position < 0)
        {
            return null;
        }

        var atItem = cursor.TryPeekAt(Position, out var item);
        var span = spanOf(Position, atItem ? Position + 1 : Position);

        Unexpected unexpected = _message is not null
            ? new CustomMessage(_message)
            : atItem
                ? new UnexpectedItem(Display<T>.Item(item))
                : new EndOfInput();

        var expected = new List<Expectation>(_count);

        for (var i = 0; i < _count; i++)
        {
            var expectation = _expected[i].Build();

            if (!expected.Exists(e => Equals(e.Value, expectation.Value)))
            {
                expected.Add(expectation);
            }
        }

        return new ParseError(span, layer, unexpected, expected, []);
    }
}
