namespace Creole;

/// <summary>
/// A parser that reads items of type <typeparamref name="TIn"/> and produces a <typeparamref name="TOut"/>.
/// </summary>
/// <typeparam name="TIn">The input item type, such as <see cref="char"/> or a token type.</typeparam>
/// <typeparam name="TOut">The type of value the parser produces when it succeeds.</typeparam>
/// <remarks>
/// <para>
/// A parser is a node in a grammar tree.
/// Combinators build nodes; nothing runs until an engine executes the tree.
/// Nodes are immutable and compared by reference, so the same grammar can be shared between threads and used as a cache key.
/// </para>
/// <para>
/// Only Creole defines parser types.
/// To add hand-written scanning code, wrap it in a custom primitive instead of deriving from this class.
/// </para>
/// </remarks>
public abstract partial class Parser<TIn, TOut> : INode
{
    /// <summary>
    /// The direct child nodes, in grammar order. Empty for leaves.
    /// </summary>
    internal abstract IReadOnlyList<INode> Children { get; }

    /// <summary>
    /// The kind of this node, for consumers that need only the grammar's structure.
    /// </summary>
    internal abstract NodeKind Kind { get; }

    IReadOnlyList<INode> INode.Children => Children;

    NodeKind INode.Kind => Kind;

    internal Parser() { }

    /// <summary>
    /// Lowers this node to an interpreter plan.
    /// Call <see cref="PlanBuilder.Lower{TIn, TOut}"/> instead, which shares plans for nodes reached more than once.
    /// </summary>
    internal abstract Plan<TIn, TOut> Lower(PlanBuilder builder);

    /// <summary>
    /// A function that computes this parser's value from the input it matched, or <see langword="null"/> when the value depends on how that input was parsed.
    /// A compiled automaton uses it to produce a value without running the parser again.
    /// </summary>
    internal virtual MatchValue<TOut>? ValueOfMatch() => null;
}
