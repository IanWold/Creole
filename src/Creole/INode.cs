namespace Creole;

/// <summary>
/// The untyped, structural view of a grammar node: its kind and its children.
/// </summary>
/// <remarks>
/// Analysis, grammar export and other consumers that need only structure walk the tree through this view and switch on <see cref="NodeKind"/>.
/// Operations that need a node's type arguments are abstract members of <see cref="Parser{TIn, TOut}"/> instead (ADR-0001).
/// </remarks>
internal interface INode
{
    IReadOnlyList<INode> Children { get; }

    NodeKind Kind { get; }
}
