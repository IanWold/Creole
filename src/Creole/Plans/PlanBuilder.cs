namespace Creole;

/// <summary>
/// Lowers a grammar tree to an execution plan. One builder lowers one grammar, then is discarded.
/// </summary>
/// <remarks>
/// <para>
/// Each node lowers itself through <see cref="Parser{TIn, TOut}.Lower"/>, which forwards the node's typed fields to a method here, so lowering logic stays in the engine (ADR-0001).
/// A node reached more than once in the same grammar is lowered once and its plan shared.
/// A node with compiled code registered in <see cref="Staging"/> uses that code instead, wherever it appears.
/// </para>
/// <para>
/// A grammar can be cyclic through <c>Rec</c>.
/// When lowering reaches a node that is still being lowered, it returns a <see cref="ForwardPlan{TIn, TOut}"/>, filled in when that node's plan is built.
/// A <c>Rec</c> lowers to its target's plan, so the forward plan on the edge that closes a cycle is the only indirection a cycle costs.
/// </para>
/// <para>
/// Lowering also applies the rewrites in <see cref="LoweringOptions"/> that the builder was given, all of them by default.
/// An <c>Or</c> dispatches when at least <paramref name="dispatchThreshold"/> of its alternatives have keys; tests lower with a threshold of one so that every such <c>Or</c> dispatches.
/// </para>
/// </remarks>
internal sealed partial class PlanBuilder(LoweringOptions options = LoweringOptions.All, int dispatchThreshold = PlanBuilder.DispatchThreshold)
{
    private readonly int _dispatchThreshold = dispatchThreshold;
    private readonly Dictionary<INode, object?> _inProgress = new(ReferenceEqualityComparer.Instance);
    private readonly LoweringOptions _options = options;
    private readonly List<(INode Node, string? Name)> _path = [];
    private readonly Dictionary<INode, object> _plans = [with(ReferenceEqualityComparer.Instance)];

    private ForwardPlan<TIn, TOut> Forward<TIn, TOut>(Parser<TIn, TOut> parser, ForwardPlan<TIn, TOut>? forward)
    {
        var start = _path.FindIndex(entry => ReferenceEquals(entry.Node, parser));

        if (_path.Skip(start).All(entry => entry.Node.Kind == NodeKind.Rec))
        {
            var names = RulePath(start) is { } path ? $"{path} → {_path[start].Name ?? "…"}" : "an unnamed rule";
            throw new GrammarException($"The rules {names} refer to each other through Rec without parsing anything, so parsing them would never terminate.");
        }

        if (forward is null)
        {
            forward = new ForwardPlan<TIn, TOut>();
            _inProgress[parser] = forward;
        }

        return forward;
    }

    private string? RulePath(int start)
    {
        var names = _path.Skip(start).Select(entry => entry.Name).OfType<string>().ToArray();

        return names.Length == 0 ? null : string.Join(" → ", names);
    }

    public static Plan<TIn, TOut> Fail<TIn, TOut>() => new FailPlan<TIn, TOut>();

    /// <summary>
    /// Returns the plan for <paramref name="parser"/>, lowering it if this builder has not seen it yet.
    /// Compiled code registered for the node takes precedence over lowering it.
    /// </summary>
    /// <exception cref="GrammarException">The grammar has a cycle of <c>Rec</c> nodes that never reaches a parser.</exception>
    public Plan<TIn, TOut> Lower<TIn, TOut>(Parser<TIn, TOut> parser)
    {
        if (_plans.TryGetValue(parser, out var existing))
        {
            return (Plan<TIn, TOut>)existing;
        }

        if (_inProgress.TryGetValue(parser, out var pending))
        {
            return Forward(parser, pending as ForwardPlan<TIn, TOut>);
        }

        _inProgress.Add(parser, null);
        _path.Add((parser, (parser as RecNode<TIn, TOut>)?.Name));

        var plan = Staging.TryGetPlan(parser, out var staged)
            ? staged
            : parser.Lower(this);

        _path.RemoveAt(_path.Count - 1);
        _inProgress.Remove(parser, out pending);

        if (pending is ForwardPlan<TIn, TOut> forward)
        {
            forward.Target = plan;
        }

        _plans.Add(parser, plan);

        return plan;
    }

    /// <summary>
    /// Lowers a <c>Rec</c> to its target's plan.
    /// </summary>
    /// <exception cref="GrammarException">The reference returned null, or building its target failed. The message gains the rule path.</exception>
    public Plan<TIn, TOut> Rec<TIn, TOut>(RecNode<TIn, TOut> rec)
    {
        Parser<TIn, TOut> target;

        try
        {
            target = rec.Target;
        }
        catch (GrammarException exception) when (RulePath(0) is { } path)
        {
            throw new GrammarException($"{exception.Message} Rule path: {path}.", exception);
        }

        return Lower(target);
    }

    public static Plan<TIn, TOut> Return<TIn, TOut>(TOut value) => new ReturnPlan<TIn, TOut>(value);
}
