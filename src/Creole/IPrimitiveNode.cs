namespace Creole;

/// <summary>
/// A leaf node that reads input directly, seen without its type arguments: what analysis needs to know about it.
/// </summary>
/// <remarks>
/// Analysis walks the tree through <see cref="INode"/> and cannot cast to a generic node type such as <c>KindNode&lt;TToken, TKind&gt;</c>.
/// Primitive nodes implement this interface so that analysis reads their FIRST set without the type arguments.
/// </remarks>
internal interface IPrimitiveNode : INode
{
    /// <summary>
    /// The items the node can start with, and whether it can start at the end of input.
    /// An opaque node reports <see cref="FirstSet.AnyItem"/>.
    /// </summary>
    FirstSet First { get; }

    /// <summary>
    /// Whether the node can succeed without consuming input.
    /// </summary>
    bool Nullable { get; }

    /// <summary>
    /// Whether the node runs user code that analysis cannot see into, such as the predicate of <c>Satisfy</c>.
    /// </summary>
    bool Opaque { get; }
}
