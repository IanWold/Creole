namespace Creole;

/// <summary>
/// The untyped view of a <c>Match</c> node, for analysis and grammar export.
/// </summary>
internal interface IMatchNode : INode
{
    /// <summary>
    /// The cases, in the order they are tried.
    /// </summary>
    IReadOnlyList<MatchCaseInfo> Cases { get; }

    INode Scrutinee { get; }

    /// <summary>
    /// The union type the scrutinee produces. Analysis compares its case types with the cases' to report CRL0105.
    /// </summary>
    Type UnionType { get; }
}
