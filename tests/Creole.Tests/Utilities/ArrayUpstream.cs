namespace Creole.Tests;

/// <summary>
/// An upstream over an array, which adds up to <paramref name="itemsPerStep"/> items each step and counts how often it produced each one.
/// </summary>
/// <remarks>
/// Item <c>i</c> has the span <c>[i, i + 1)</c>, as a character of a root layer does.
/// </remarks>
internal sealed class ArrayUpstream<T>(T[] items, int itemsPerStep = 1) : IUpstream<T>
{
    private readonly T[] _items = items;
    private readonly int _itemsPerStep = itemsPerStep;
    private int _next;

    /// <summary>
    /// How many times each item has been produced.
    /// </summary>
    public int[] Produced { get; } = new int[items.Length];

    /// <summary>
    /// How many steps have run.
    /// </summary>
    public int Steps { get; private set; }

    public bool TryStep(StreamBuffer<T> buffer)
    {
        if (_next == _items.Length)
        {
            return false;
        }

        var stepStart = _next;

        for (var group = 0; group < _itemsPerStep && _next < _items.Length; group++)
        {
            buffer.Add(_items[_next], new StreamSlot(new SourceSpan(SourceId.Unnamed, _next, _next + 1), stepStart, null, group));
            Produced[_next]++;
            _next++;
        }

        Steps++;
        return true;
    }
}
