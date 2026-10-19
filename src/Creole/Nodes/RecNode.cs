namespace Creole;

/// <summary>
/// A deferred reference to <see cref="Target"/>, so a rule can refer to a rule declared after it, or to itself. Backs <c>Rec</c>.
/// </summary>
/// <remarks>
/// The reference is called the first time <see cref="Target"/> is read, never when the node is built, and its result is kept, so the target is one node however often it is read.
/// A null result is not kept: reading <see cref="Target"/> throws, and a later read calls the reference again.
/// </remarks>
/// <param name="reference">Returns the target. It may return null until the grammar class has been initialized.</param>
/// <param name="referenceExpression">The caller's expression for <paramref name="reference"/>, used in error messages.</param>
/// <param name="name">The rule's name (ADR-0009), or null if it has none.</param>
internal sealed class RecNode<TIn, TOut>(Func<Parser<TIn, TOut>?> reference, string referenceExpression, string? name) : Parser<TIn, TOut>
{
    private readonly Lazy<Parser<TIn, TOut>> _target = new(
        () => reference() ?? throw new GrammarException($"'{referenceExpression}' returned null. A Rec must refer to a rule that is initialized by the time the grammar is used."),
        LazyThreadSafetyMode.PublicationOnly
    );

    /// <summary>
    /// The target, which may be an ancestor of this node, so the graph can contain cycles.
    /// </summary>
    internal override IReadOnlyList<INode> Children => [Target];

    internal override NodeKind Kind => NodeKind.Rec;

    /// <summary>
    /// The rule's name, or null if it has none. See <see cref="RuleName"/>.
    /// </summary>
    public string? Name { get; } = name;

    /// <summary>
    /// The caller's expression for the reference, such as <c>() =&gt; Value</c>.
    /// </summary>
    public string ReferenceExpression { get; } = referenceExpression;

    /// <summary>
    /// The node the reference returns, resolved on first read.
    /// </summary>
    /// <exception cref="GrammarException">The reference returned null.</exception>
    public Parser<TIn, TOut> Target => _target.Value;

    internal override Plan<TIn, TOut> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, TOut>(Kind);
}
