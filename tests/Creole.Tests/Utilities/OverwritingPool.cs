using System.Buffers;

namespace Creole.Tests;

/// <summary>
/// A pool that overwrites each array returned to it with <paramref name="filler"/> and hands the newest back first, so whatever still reads a released array reads the filler or another segment's items.
/// </summary>
internal sealed class OverwritingPool<T>(T filler) : ArrayPool<T>
{
    private readonly T _filler = filler;
    private readonly Stack<T[]> _returned = [];

    /// <summary>
    /// How many arrays have been returned.
    /// </summary>
    public int Returns { get; private set; }

    public override T[] Rent(int minimumLength) =>
        _returned.TryPeek(out var array) && array.Length >= minimumLength
        ? _returned.Pop()
        : new T[minimumLength];

    public override void Return(T[] array, bool clearArray = false)
    {
        array.AsSpan().Fill(_filler);
        _returned.Push(array);
        Returns++;
    }
}
