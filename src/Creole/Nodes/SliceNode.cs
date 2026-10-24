namespace Creole;

/// <summary>
/// Runs <see cref="Item"/> and produces the characters it consumed as a <see cref="ReadOnlyMemory{T}"/>, discarding its result.
/// Backs <c>Slice</c>.
/// </summary>
internal sealed class SliceNode<T>(Parser<char, T> item) : Parser<char, ReadOnlyMemory<char>>
{
    internal override IReadOnlyList<INode> Children => [Item];

    internal override NodeKind Kind => NodeKind.Slice;

    public Parser<char, T> Item { get; } = item;

    internal override Plan<char, ReadOnlyMemory<char>> Lower(PlanBuilder builder) =>
        builder.Slice(Item);

    internal override MatchValue<ReadOnlyMemory<char>> ValueOfMatch() =>
        static (ref cursor, start) => cursor.Since(start);
}
