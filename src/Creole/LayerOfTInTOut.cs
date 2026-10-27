using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// A layer: turns a stream of <typeparamref name="TIn"/> items into a stream of <typeparamref name="TOut"/> items, such as characters into tokens.
/// </summary>
/// <typeparam name="TIn">The type of item the layer reads.</typeparam>
/// <typeparam name="TOut">The type of item the layer produces.</typeparam>
/// <remarks>
/// <para>
/// A layer is data, as a parser is: building one runs nothing.
/// Layers are immutable, so a layer can be shared between threads and used in any number of chains.
/// Make layers with the methods of <see cref="Layer"/>, and chain them with <see cref="Then{TNext}"/>.
/// </para>
/// <para>
/// A layer runs in steps, each reading some of its input and producing zero or more items.
/// It runs a step only when the layer above it needs another item, so it never reads further ahead than that.
/// Every item it produces carries a span in the original source, so an error found at any layer points at the source.
/// </para>
/// <para>
/// Only Creole defines layer types.
/// </para>
/// </remarks>
public abstract class Layer<TIn, TOut>
{
    internal Layer() { }

    /// <summary>
    /// Starts a run of this layer over <paramref name="input"/>.
    /// </summary>
    /// <param name="input">The buffer holding this layer's input, which pulls from the layer below.</param>
    /// <param name="options">The settings for the parse, whose depth limit applies to each step.</param>
    /// <param name="recordFailures">Whether the run keeps a failure log, so that a failed step reports what it expected.</param>
    /// <returns>The upstream that produces this layer's items into the buffer above it.</returns>
    internal abstract IUpstream<TOut> Open(StreamBuffer<TIn> input, ParseOptions options, bool recordFailures);

    /// <summary>
    /// A layer that runs this layer and then <paramref name="next"/> over its items.
    /// </summary>
    /// <typeparam name="TNext">The type of item <paramref name="next"/> produces.</typeparam>
    /// <param name="next">The layer to run over this layer's items.</param>
    /// <param name="nextExpression">Filled in by the compiler with the expression for <paramref name="next"/>.</param>
    /// <returns>A layer from this layer's input to <paramref name="next"/>'s items.</returns>
    /// <exception cref="GrammarException"><paramref name="next"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Chaining is associative: <c>a.Then(b).Then(c)</c> and <c>a.Then(b.Then(c))</c> produce the same items with the same spans.
    /// <see cref="Layer.Identity{T}"/> is its unit: chaining a layer with it returns that layer.
    /// </para>
    /// <para>
    /// Between the two layers sits a buffer of this layer's items, so <paramref name="next"/> can rewind over them without this layer running again.
    /// </para>
    /// </remarks>
    public Layer<TIn, TNext> Then<TNext>(Layer<TOut, TNext> next, [CallerArgumentExpression(nameof(next))] string nextExpression = "")
    {
        Guard.NotNull(next, nextExpression);

        return this is IdentityLayer<TIn>
            ? (Layer<TIn, TNext>)(object)next
            : next is IdentityLayer<TOut>
                ? (Layer<TIn, TNext>)(object)this
                : new ComposedLayer<TIn, TOut, TNext>(this, next);
    }
}
