namespace Creole;

internal sealed partial class PlanBuilder
{
    /// <summary>
    /// The fewest alternatives with keys for which an <c>Or</c> dispatches.
    /// </summary>
    /// <remarks>
    /// A dispatch costs about as much as trying two or three alternatives that fail at once.
    /// INT-05 measured choices whose keyed alternatives come first and never match: dispatch was 1.21× slower with one, 1.10× slower with two, 1.03× faster with three and 1.17× faster with four.
    /// </remarks>
    public const int DispatchThreshold = 4;

    /// <summary>
    /// A dispatch plan for an <c>Or</c> when at least the builder's dispatch threshold of its alternatives have keys and they are all read the same way; otherwise null.
    /// </summary>
    private DispatchPlan<TIn, T>? Dispatch<TIn, T>(IReadOnlyList<Parser<TIn, T>> alternatives, Plan<TIn, T>[] plans)
    {
        var keyed = alternatives.Select(KeyedPrimitive).ToArray();
        var primitives = keyed.OfType<IPrimitiveNode>().ToArray();

        return primitives.Length >= _dispatchThreshold && KeyReader<TIn>(primitives) is { } read
            ? new DispatchPlan<TIn, T>(plans, [.. keyed.Select(primitive => primitive?.First.Keys)], read)
            : null;
    }

    /// <summary>
    /// The primitive whose keys an alternative takes, or null when it has no keys (ADR-0016 point 1).
    /// </summary>
    /// <remarks>
    /// The walk follows the leading position through nodes that consume nothing, pass no cut and run no user code before their first item does.
    /// It ends at a primitive that fails without consuming whenever the next item is outside its FIRST keys.
    /// Any other node ends it with no keys, so the alternative is always tried.
    /// </remarks>
    private static IPrimitiveNode? KeyedPrimitive(INode node) => node.Kind switch
    {
        NodeKind.Atomic or NodeKind.Bind or NodeKind.Context or NodeKind.Filter or NodeKind.Map or NodeKind.Named or NodeKind.Sequence or NodeKind.Slice or NodeKind.Text =>
            KeyedPrimitive(node.Children[0]),
        NodeKind.Any or NodeKind.Case or NodeKind.CharClass or NodeKind.Custom or NodeKind.End or NodeKind.Kind or NodeKind.Satisfy or NodeKind.String or NodeKind.Token =>
            node is IPrimitiveNode { Nullable: false, Opaque: false, First: { MatchesAnyItem: false, MatchesEnd: false, Cases.Count: 0, Values.Count: 0 } } primitive
            ? primitive
            : null,
        NodeKind.Cut or NodeKind.Expression or NodeKind.Fail or NodeKind.Longest or NodeKind.Lookahead
            or NodeKind.Match or NodeKind.Not or NodeKind.Optional or NodeKind.Or or NodeKind.Rec
            or NodeKind.Repeat or NodeKind.Return or NodeKind.Separated or NodeKind.SkipRepeat
            or NodeKind.Switch or NodeKind.Until =>
            null,
    };

    /// <summary>
    /// How to read the key of an item, so that it compares with the keys of <paramref name="primitives"/>; null when they are not all read the same way.
    /// </summary>
    private static Func<TIn, long>? KeyReader<TIn>(IEnumerable<IPrimitiveNode> primitives)
    {
        if (typeof(TIn) == typeof(char))
        {
            return (Func<TIn, long>)(object)(Func<char, long>)(static c => c);
        }

        if (typeof(TIn).IsEnum)
        {
            return static item => FirstSet.EnumKey(item);
        }

        var kinds = primitives.Select(primitive => primitive as IKindNode<TIn>).ToArray();

        return kinds.All(kind => kind is not null) && kinds.Select(kind => kind!.KindType).Distinct().Count() == 1
            ? kinds[0]!.ReadKey
            : null;
    }
}
