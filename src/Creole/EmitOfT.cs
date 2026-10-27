using System.Runtime.CompilerServices;
using System.Text;

namespace Creole;

/// <summary>
/// The outputs one step of a layer produces: none, one or several, in order, each derived from the input the step consumed or synthetic.
/// </summary>
/// <typeparam name="T">The type of item the layer produces.</typeparam>
/// <remarks>
/// <para>
/// Build an emit from its outputs.
/// <c>[]</c> and <c>default</c> hold none.
/// An item converts to an emit of one derived output: <c>Emit&lt;Tok&gt; e = token;</c>.
/// <see cref="Emit.Synthetic{T}(T)"/> makes a synthetic output, and a collection expression builds several: <c>[newline, Emit.Synthetic(indent)]</c>.
/// Spreads work, including of another emit, whose outputs keep their marks.
/// </para>
/// <para>
/// Read an emit with <see cref="Count"/> and <c>foreach</c>, which yields each output as an <see cref="Emitted{T}"/>.
/// An emit is not a union: its outputs are one list, so there are no cases to switch on.
/// </para>
/// <para>
/// An emit of zero or one output holds it inline, so building and reading one allocates nothing. Several outputs are held in one array.
/// Two emits are equal when they hold equal outputs in the same order.
/// </para>
/// </remarks>
[CollectionBuilder(typeof(Emit), nameof(Emit.Create))]
public readonly struct Emit<T> : IEquatable<Emit<T>>
{
    /// <summary>
    /// Steps through an emit's outputs in order. <c>foreach</c> uses it; there is no need to call it directly.
    /// </summary>
    /// <param name="emit">The emit to step through.</param>
    public struct Enumerator(Emit<T> emit)
    {
        private readonly Emit<T> _emit = emit;
        private int _index = -1;

        /// <summary>
        /// The output at the enumerator's position.
        /// </summary>
        public readonly Emitted<T> Current => _emit.At(_index);

        /// <summary>
        /// Moves to the next output.
        /// </summary>
        /// <returns>Whether there was a next output.</returns>
        public bool MoveNext() => ++_index < _emit.Count;
    }

    private readonly Emitted<T>[]? _many;
    private readonly Emitted<T> _one;

    /// <summary>
    /// The number of outputs.
    /// </summary>
    public int Count { get; }

    internal Emit(Emitted<T> one)
    {
        _one = one;
        Count = 1;
    }

    internal Emit(Emitted<T>[] many)
    {
        _many = many;
        Count = many.Length;
    }

    private Emitted<T> At(int index) =>
        _many is null
        ? _one
        : _many[index];

    /// <summary>
    /// Whether <paramref name="other"/> holds equal outputs in the same order.
    /// </summary>
    /// <param name="other">The emit to compare with.</param>
    /// <returns>Whether the two are equal.</returns>
    public bool Equals(Emit<T> other)
    {
        if (Count != other.Count)
        {
            return false;
        }

        for (var i = 0; i < Count; i++)
        {
            if (!EqualityComparer<Emitted<T>>.Default.Equals(At(i), other.At(i)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Whether <paramref name="obj"/> is an emit that holds equal outputs in the same order.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>Whether the two are equal.</returns>
    public override bool Equals(object? obj) =>
        obj is Emit<T> other && Equals(other);

    /// <summary>
    /// Returns an enumerator over the outputs, for <c>foreach</c>.
    /// </summary>
    /// <returns>An enumerator that yields each output in order.</returns>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// Returns a hash code computed from the outputs, so equal emits have equal hash codes.
    /// </summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var output in this)
        {
            hash.Add(output);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Returns the outputs in brackets, each synthetic one marked: <c>[newline, indent (synthetic)]</c>.
    /// </summary>
    /// <returns>A description of the emit for debugging.</returns>
    public override string ToString()
    {
        var builder = new StringBuilder("[");

        foreach (var output in this)
        {
            if (builder.Length > 1)
            {
                builder.Append(", ");
            }

            builder.Append(output.Item);

            if (output.IsSynthetic)
            {
                builder.Append(" (synthetic)");
            }
        }

        return builder.Append(']').ToString();
    }

    /// <summary>
    /// Whether two emits hold equal outputs in the same order.
    /// </summary>
    /// <param name="left">The first emit.</param>
    /// <param name="right">The second emit.</param>
    /// <returns>Whether the two are equal.</returns>
    public static bool operator ==(Emit<T> left, Emit<T> right) => left.Equals(right);

    /// <summary>
    /// Whether two emits differ in their outputs or their order.
    /// </summary>
    /// <param name="left">The first emit.</param>
    /// <param name="right">The second emit.</param>
    /// <returns>Whether the two differ.</returns>
    public static bool operator !=(Emit<T> left, Emit<T> right) => !left.Equals(right);

    /// <summary>
    /// Makes an emit of one derived output.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <remarks>
    /// C# applies no user-defined conversion from an interface or <see cref="object"/>, so for those item types use <see cref="Emit.One{T}(T)"/>.
    /// </remarks>
    public static implicit operator Emit<T>(T item) =>
        new(new Emitted<T>(item, IsSynthetic: false));

    /// <summary>
    /// Makes an emit of one output.
    /// </summary>
    /// <param name="output">The output.</param>
    public static implicit operator Emit<T>(Emitted<T> output) => new(output);
}
