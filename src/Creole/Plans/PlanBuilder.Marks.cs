namespace Creole;

internal partial class PlanBuilder
{
    private bool? _fullMarks;

    /// <summary>
    /// Whether the plans this builder makes take <see cref="FullMark"/> rather than <see cref="LightMark"/>.
    /// </summary>
    /// <remarks>
    /// The first parser the builder lowers decides, because a builder lowers one grammar.
    /// A grammar takes full marks when <see cref="LoweringOptions.LightMarks"/> is off or <see cref="CanChangeWhatFullMarksSave"/> says it needs them.
    /// </remarks>
    private bool FullMarks => _fullMarks is true;

    /// <summary>
    /// Whether a parser reachable from <paramref name="root"/>, following <c>Rec</c>, can raise diagnostics or change the layer's state.
    /// </summary>
    /// <remarks>
    /// <c>SetState</c> and <c>Scan</c> change the state.
    /// A <c>Bind</c> counts, because its continuation can return any parser.
    /// A <c>Rec</c> whose reference fails is passed over here, so that lowering reports it with its rule path.
    /// </remarks>
    private static bool CanChangeWhatFullMarksSave(INode root)
    {
        var seen = new HashSet<INode>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<INode>([root]);

        while (pending.TryPop(out var node))
        {
            if (!seen.Add(node))
            {
                continue;
            }

            if (node.Kind is NodeKind.Bind or NodeKind.SetState or NodeKind.Scan)
            {
                return true;
            }

            IReadOnlyList<INode> children;

            try
            {
                children = node.Children;
            }
            catch (GrammarException)
            {
                continue;
            }

            foreach (var child in children)
            {
                pending.Push(child);
            }
        }

        return false;
    }

    /// <summary>
    /// Decides <see cref="FullMarks"/> from <paramref name="root"/>, unless the builder has already lowered a parser.
    /// </summary>
    private void ChooseMarks(INode root) =>
        _fullMarks ??= !_options.HasFlag(LoweringOptions.LightMarks) || CanChangeWhatFullMarksSave(root);
}
