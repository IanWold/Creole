using System.ComponentModel;

namespace Creole;

/// <summary>
/// Makes the outputs of a layer's step: <c>Emit.One(token)</c>, <c>Emit.Synthetic(indent)</c>.
/// </summary>
public static class Emit
{
    /// <summary>
    /// Makes an emit of <paramref name="outputs"/>. A collection expression calls this; write <c>[a, b]</c> instead of calling it.
    /// </summary>
    /// <param name="outputs">The outputs, in order.</param>
    /// <returns>An emit holding a copy of <paramref name="outputs"/>.</returns>
    /// <remarks>
    /// It is public because collection expressions build a type only through a public method. It is hidden from IntelliSense.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Emit<T> Create<T>(ReadOnlySpan<Emitted<T>> outputs) => outputs.Length switch
    {
        0 => default,
        1 => new Emit<T>(outputs[0]),
        _ => new Emit<T>(outputs.ToArray()),
    };

    /// <summary>
    /// Makes an emit of one derived output.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>An emit holding <paramref name="item"/> as a derived output.</returns>
    /// <remarks>
    /// An item converts to an emit without this method, as in <c>Emit&lt;Tok&gt; e = token;</c>.
    /// Use it where the conversion does not apply, which is when the item's type is an interface or <see cref="object"/>, or where a lambda must return an emit rather than an item.
    /// </remarks>
    public static Emit<T> One<T>(T item) =>
        new(new Emitted<T>(item, IsSynthetic: false));

    /// <summary>
    /// Makes a synthetic output: one that stands for no input, such as an INDENT or DEDENT.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>A synthetic output of <paramref name="item"/>, which converts to an <see cref="Emit{T}"/> or goes in a collection expression with other outputs.</returns>
    public static Emitted<T> Synthetic<T>(T item) =>
        new(item, IsSynthetic: true);
}
