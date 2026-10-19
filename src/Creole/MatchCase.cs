namespace Creole;

/// <summary>
/// One case of a <c>Match</c>: the union case it applies to, the branch to run and the function that combines them.
/// </summary>
/// <typeparam name="TIn">The input item type of the grammar.</typeparam>
/// <typeparam name="TOut">The type the <c>Match</c> produces.</typeparam>
/// <remarks>
/// Create cases with <c>Case</c>, a member of <see cref="Grammar{TIn}"/>, and pass them to <c>Match</c>.
/// A case has no public members; it is only passed to <c>Match</c>.
/// </remarks>
public abstract class MatchCase<TIn, TOut>
{
    /// <summary>
    /// The branch parser.
    /// </summary>
    internal abstract INode BranchNode { get; }

    /// <summary>
    /// The union case type this case applies to.
    /// </summary>
    internal abstract Type CaseType { get; }

    internal MatchCase() { }
}
