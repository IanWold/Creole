namespace Creole;

/// <summary>
/// Decides whether a character parser is Regular, so that it can compile to a commit-aware automaton.
/// </summary>
/// <remarks>
/// <para>
/// A Regular parser is built only from the node kinds the INT-06 prototypes tested: character classes (including <c>Any</c>), <c>String</c>, <c>Seq</c>, <c>Or</c>, <c>Optional</c>,
/// <c>Many</c>, <c>AtLeastOnce</c> and their skip forms over an item that cannot match empty, and <c>Atomic</c>.
/// <c>Select</c>, <c>As</c>, <c>Slice</c>, <c>Text</c>, <c>Named</c> and <c>Context</c> may wrap any of them, because they do not change what is matched.
/// A <c>Longest</c> is compiled when each of its alternatives is Regular; a <c>Longest</c> nested inside one is not Regular, because no test covers it yet.
/// </para>
/// <para>
/// Every choice inside an <c>Atomic</c> must also be in fragment E′, so that the automaton can resolve it early.
/// Let h be the choice's first branch, l its second, and K the rest of the term up to the end of the outermost <c>Atomic</c> around it.
/// Either h·K never fails after consuming, or l·K cannot match empty and starts with different characters from h·K
/// A choice is an <c>Or</c>, an <c>Optional</c>, or the decision a repetition makes before each item.
/// </para>
/// </remarks>
internal static class Regular
{
    private static RegularTerm? Fold(IReadOnlyList<INode> nodes, Func<RegularTerm, RegularTerm, RegularTerm> combine)
    {
        if (From(nodes[^1]) is not { } folded)
        {
            return null;
        }

        for (var index = nodes.Count - 2; index >= 0; index--)
        {
            if (From(nodes[index]) is not { } term)
            {
                return null;
            }

            folded = combine(term, folded);
        }

        return folded;
    }

    private static RegularTerm? From(INode node) => node.Kind switch
    {
        NodeKind.Any => new RegularTerm.Chars(CharSet.All),
        NodeKind.CharClass => new RegularTerm.Chars(((CharClassNode)node).Set),
        NodeKind.String => Literal((StringNode)node),
        NodeKind.Sequence => Fold(node.Children, static (first, second) => new RegularTerm.Sequence(first, second)),
        NodeKind.Or => Fold(node.Children, static (first, second) => new RegularTerm.Choice(first, second)),
        NodeKind.Optional => From(node.Children[0]) is { } item ? new RegularTerm.Choice(item, RegularTerm.Empty.Instance) : null,
        NodeKind.Repeat or NodeKind.SkipRepeat => Repetition((IRepetitionNode)node),
        NodeKind.Atomic => From(node.Children[0]) is { } item ? new RegularTerm.Atomic(item) : null,
        NodeKind.Context or NodeKind.Map or NodeKind.Named or NodeKind.Slice or NodeKind.Text => From(node.Children[0]),
        NodeKind.Bind or NodeKind.Case or NodeKind.Custom or NodeKind.Cut or NodeKind.End or NodeKind.Expression
            or NodeKind.Fail or NodeKind.Filter or NodeKind.Kind or NodeKind.Longest or NodeKind.Lookahead
            or NodeKind.Match or NodeKind.Not or NodeKind.Rec or NodeKind.Return or NodeKind.Satisfy
            or NodeKind.Separated or NodeKind.Switch or NodeKind.Token or NodeKind.Until =>
            null,
    };

    private static bool InsideAtomic(RegularTerm term, RegularTerm rest) => term switch
    {
        RegularTerm.Atomic atomic => InsideAtomic(atomic.Item, rest),
        RegularTerm.Sequence sequence => InsideAtomic(sequence.First, RegularTerm.Then(sequence.Second, rest)) && InsideAtomic(sequence.Second, rest),
        RegularTerm.Choice choice => ResolvesEarly(choice.First, choice.Second, rest) && InsideAtomic(choice.First, rest) && InsideAtomic(choice.Second, rest),
        RegularTerm.Many many => ResolvesEarly(new RegularTerm.Sequence(many.Item, many), RegularTerm.Empty.Instance, rest) && InsideAtomic(many.Item, RegularTerm.Then(many, rest)),
        _ => true,
    };

    private static RegularTerm Literal(StringNode literal)
    {
        RegularTerm chars = new RegularTerm.Chars(literal.CharactersAt(literal.Text.Length - 1));

        for (var index = literal.Text.Length - 2; index >= 0; index--)
        {
            chars = new RegularTerm.Sequence(new RegularTerm.Chars(literal.CharactersAt(index)), chars);
        }

        return literal.Text.Length == 1 ? chars : new RegularTerm.Atomic(chars);
    }

    private static bool OutsideAtomic(RegularTerm term) => term switch
    {
        RegularTerm.Atomic atomic => InsideAtomic(atomic.Item, RegularTerm.Empty.Instance),
        RegularTerm.Sequence sequence => OutsideAtomic(sequence.First) && OutsideAtomic(sequence.Second),
        RegularTerm.Choice choice => OutsideAtomic(choice.First) && OutsideAtomic(choice.Second),
        RegularTerm.Many many => OutsideAtomic(many.Item),
        _ => true,
    };

    private static RegularTerm? Repetition(IRepetitionNode repetition)
    {
        if (repetition.Min > 1 || repetition.Max is not null || From(repetition.Children[0]) is not { Nullable: false } item)
        {
            return null;
        }

        var many = new RegularTerm.Many(item);

        return repetition.Min == 0 ? many : new RegularTerm.Sequence(item, many);
    }

    private static bool ResolvesEarly(RegularTerm first, RegularTerm second, RegularTerm rest)
    {
        var high = RegularTerm.Then(first, rest);
        var low = RegularTerm.Then(second, rest);

        return high.NeverFailsAfterConsuming || (!low.Nullable && !high.FirstChars.Keys.Overlaps(low.FirstChars.Keys));
    }

    /// <summary>
    /// The term for <paramref name="parser"/> if it is Regular, or <see langword="null"/>.
    /// </summary>
    public static RegularTerm? Of<T>(Parser<char, T> parser) =>
        From(parser) is { } term && OutsideAtomic(term) ? term : null;
}
