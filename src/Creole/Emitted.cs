namespace Creole;

/// <summary>
/// One output of a layer's step, as an <see cref="Emit{T}"/> holds it: the item, and whether it is synthetic.
/// </summary>
/// <typeparam name="T">The type of item the layer produces.</typeparam>
/// <remarks>
/// <para>
/// A derived output came from the input its step consumed, and its span covers that input.
/// A synthetic output, such as an INDENT or DEDENT, stands for no input, and its span is zero-width.
/// </para>
/// <para>
/// An item converts to a derived output, and <see cref="Emit.Synthetic{T}(T)"/> makes a synthetic one, so a collection expression can mix them: <c>[newline, Emit.Synthetic(indent)]</c>.
/// </para>
/// </remarks>
/// <param name="Item">The item.</param>
/// <param name="IsSynthetic">Whether the item is synthetic rather than derived from input.</param>
public readonly record struct Emitted<T>(T Item, bool IsSynthetic)
{
    /// <summary>
    /// Makes a derived output of <paramref name="item"/>.
    /// </summary>
    /// <param name="item">The item.</param>
    public static implicit operator Emitted<T>(T item) =>
        new(item, IsSynthetic: false);
}
