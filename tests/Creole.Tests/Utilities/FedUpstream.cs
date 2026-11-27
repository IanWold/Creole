namespace Creole.Tests;

/// <summary>
/// A root over an array that it receives a part at a time: each step adds every item received and not yet added, as far as the buffer has room, and when none is left it starves until <see cref="Receive"/> gives it more, or ends once it has received them all.
/// </summary>
/// <remarks>
/// Item <c>i</c> has the span <c>[i, i + 1)</c>, as a byte or a character of a root does.
/// </remarks>
/// <param name="items">The whole input.</param>
/// <param name="received">How many items it has received at the start.</param>
internal class FedUpstream<T>(T[] items, int received) : IUpstream<T>
{
    private readonly T[] _items = items;
    private int _next;
    private int _received = Math.Min(received, items.Length);

    public SourceSpan InputEnd => new(SourceId.Unnamed, _next, _next);

    public SourceSpan InputStart => new(SourceId.Unnamed, 0, 0);

    public bool Starved => _next == _received && _received < _items.Length;

    /// <summary>
    /// Receives up to <paramref name="count"/> more items.
    /// </summary>
    /// <returns>Whether any items were left to receive.</returns>
    public bool Receive(int count)
    {
        var before = _received;

        _received = (int)Math.Min(_items.Length, (long)_received + count);

        return _received > before;
    }

    public bool TryStep(StreamBuffer<T> buffer)
    {
        if (_next == _received)
        {
            return false;
        }

        var stepStart = _next;
        var count = Math.Min(_received - _next, buffer.Room);

        for (var group = 0; group < count; group++)
        {
            buffer.Add(_items[_next], new StreamSlot(new SourceSpan(SourceId.Unnamed, _next, _next + 1), stepStart, StateBefore: null, GroupIndex: group));
            _next++;
        }

        return true;
    }
}
