namespace Creole;

/// <summary>
/// Runs <see cref="Source"/> and transforms its result with <see cref="Selector"/>.
/// Backs the one-parameter <c>Select</c>, <c>As</c> and <c>Ignore</c>, and an N-ary <c>Select</c> over a parser not built by <c>Seq</c>.
/// </summary>
/// <remarks>
/// <c>As</c> and <c>Ignore</c> set <see cref="IgnoresSource"/>: their selector returns the same value whatever it is given.
/// </remarks>
internal sealed class MapNode<TIn, TSource, TResult>(Parser<TIn, TSource> source, Func<TSource, TResult> selector, bool ignoresSource = false) : Parser<TIn, TResult>
{
    internal override IReadOnlyList<INode> Children => [Source];

    internal override NodeKind Kind => NodeKind.Map;

    public bool IgnoresSource { get; } = ignoresSource;

    public Func<TSource, TResult> Selector { get; } = selector;

    public Parser<TIn, TSource> Source { get; } = source;

    internal override Plan<TIn, TResult> Lower(PlanBuilder builder) =>
        builder.Map(Source, Selector);

    internal override MatchValue<TResult>? ValueOfMatch()
    {
        var selector = Selector;

        if (IgnoresSource)
        {
            return (ref _, _) => selector(default!);
        }

        return Source.ValueOfMatch() is { } source
            ? (ref Cursor<char> cursor, long start) => selector(source(ref cursor, start))
            : null;
    }
}
