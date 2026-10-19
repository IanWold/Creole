namespace Creole;

/// <summary>
/// Runs <see cref="Item"/> and produces the characters it consumed as a <see cref="string"/>, discarding its result.
/// Backs <c>Text</c>.
/// </summary>
internal sealed class TextNode<T>(Parser<char, T> item) : Parser<char, string>
{
    internal override IReadOnlyList<INode> Children => [Item];

    internal override NodeKind Kind => NodeKind.Text;

    public Parser<char, T> Item { get; } = item;

    internal override Plan<char, string> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<char, string>(Kind);
}
