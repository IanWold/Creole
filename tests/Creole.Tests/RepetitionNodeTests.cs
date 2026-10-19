namespace Creole.Tests;

public class RepetitionNodeTests
{
    private static readonly Parser<char, string> _comma = Grammar<char>.Return(",");
    private static readonly Parser<char, int> _item = Grammar<char>.Return(1);

    public static TheoryData<string> Combinators => new()
    {
        nameof(Repetition.Many),
        nameof(Repetition.AtLeastOnce),
        "Repeat(3)",
        "Repeat(2, 5)",
        nameof(Repetition.SepBy),
        nameof(Repetition.SepBy1),
        nameof(Repetition.SepEndBy),
        nameof(Repetition.Until),
        nameof(Repetition.SkipMany),
        nameof(Repetition.SkipAtLeastOnce),
    };

    public static TheoryData<string, int, int?> RepeatCases => new()
    {
        { nameof(Repetition.Many), 0, null },
        { nameof(Repetition.AtLeastOnce), 1, null },
        { "Repeat(3)", 3, 3 },
        { "Repeat(2, 5)", 2, 5 },
        { "Repeat(0, 0)", 0, 0 },
    };

    private static Parser<char, IReadOnlyList<int>> Build(string combinator) => combinator switch
    {
        nameof(Repetition.Many) => _item.Many(),
        nameof(Repetition.AtLeastOnce) => _item.AtLeastOnce(),
        "Repeat(3)" => _item.Repeat(3),
        "Repeat(2, 5)" => _item.Repeat(2, 5),
        "Repeat(0, 0)" => _item.Repeat(0, 0),
        _ => throw new ArgumentOutOfRangeException(nameof(combinator)),
    };

    private static object Call(string combinator, Parser<char, int> rule) => combinator switch
    {
        nameof(Repetition.Many) => rule.Many(),
        nameof(Repetition.AtLeastOnce) => rule.AtLeastOnce(),
        "Repeat(3)" => rule.Repeat(3),
        "Repeat(2, 5)" => rule.Repeat(2, 5),
        nameof(Repetition.SepBy) => rule.SepBy(_comma),
        nameof(Repetition.SepBy1) => rule.SepBy1(_comma),
        nameof(Repetition.SepEndBy) => rule.SepEndBy(_comma),
        nameof(Repetition.Until) => rule.Until(_comma),
        nameof(Repetition.SkipMany) => rule.SkipMany(),
        nameof(Repetition.SkipAtLeastOnce) => rule.SkipAtLeastOnce(),
        _ => throw new ArgumentOutOfRangeException(nameof(combinator)),
    };

    private static string NullMessage(string expression) =>
        $"'{expression}' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => {expression}).";

    [Theory]
    [MemberData(nameof(Combinators))]
    public void NullReceiverNamesTheExpression(string combinator) =>
        Assert.Equal(NullMessage("rule"), Assert.Throws<GrammarException>(() => Call(combinator, null!)).Message);

    [Fact]
    public void NullSeparatorNamesTheExpression()
    {
        Parser<char, string> comma = null!;

        Assert.Equal(NullMessage("comma"), Assert.Throws<GrammarException>(() => _item.SepBy(comma)).Message);
        Assert.Equal(NullMessage("comma"), Assert.Throws<GrammarException>(() => _item.SepBy1(comma)).Message);
        Assert.Equal(NullMessage("comma"), Assert.Throws<GrammarException>(() => _item.SepEndBy(comma)).Message);
    }

    [Fact]
    public void NullTerminatorNamesTheExpression()
    {
        Parser<char, string> close = null!;

        Assert.Equal(NullMessage("close"), Assert.Throws<GrammarException>(() => _item.Until(close)).Message);
    }

    [Theory]
    [MemberData(nameof(RepeatCases))]
    public void RepeatCombinatorsBuildOneRepeatNode(string combinator, int min, int? max)
    {
        var node = Assert.IsType<RepeatNode<char, int>>(Build(combinator));

        Assert.Equal(NodeKind.Repeat, ((INode)node).Kind);
        Assert.Equal([_item], ((INode)node).Children);
        Assert.Equal(min, node.Min);
        Assert.Equal(max, node.Max);
    }

    [Fact]
    public void RepeatRejectsAMaximumBelowTheMinimum() =>
        Assert.Equal(
            "Repeat maximum 2 is less than the minimum 3.",
            Assert.Throws<GrammarException>(() => _item.Repeat(3, 2)).Message
        );

    [Fact]
    public void RepeatRejectsANegativeCount()
    {
        Assert.Equal("Repeat count -1 is negative.", Assert.Throws<GrammarException>(() => _item.Repeat(-1)).Message);
        Assert.Equal("Repeat count -1 is negative.", Assert.Throws<GrammarException>(() => _item.Repeat(-1, 2)).Message);
    }

    [Theory]
    [InlineData(nameof(Repetition.SepBy), 0, false)]
    [InlineData(nameof(Repetition.SepBy1), 1, false)]
    [InlineData(nameof(Repetition.SepEndBy), 0, true)]
    public void SeparatorCombinatorsBuildOneSeparatedNode(string combinator, int min, bool allowTrailing)
    {
        var parser = combinator switch
        {
            nameof(Repetition.SepBy) => _item.SepBy(_comma),
            nameof(Repetition.SepBy1) => _item.SepBy1(_comma),
            _ => _item.SepEndBy(_comma),
        };
        var node = Assert.IsType<SeparatedNode<char, int, string>>(parser);

        Assert.Equal(NodeKind.Separated, ((INode)node).Kind);
        Assert.Equal([_item, _comma], ((INode)node).Children);
        Assert.Equal(min, node.Min);
        Assert.Equal(allowTrailing, node.AllowTrailing);
    }

    [Theory]
    [InlineData(nameof(Repetition.SkipMany), 0)]
    [InlineData(nameof(Repetition.SkipAtLeastOnce), 1)]
    public void SkipCombinatorsBuildOneSkipRepeatNode(string combinator, int min)
    {
        var parser = combinator == nameof(Repetition.SkipMany) ? _item.SkipMany() : _item.SkipAtLeastOnce();
        var node = Assert.IsType<SkipRepeatNode<char, int>>(parser);

        Assert.Equal(NodeKind.SkipRepeat, ((INode)node).Kind);
        Assert.Equal([_item], ((INode)node).Children);
        Assert.Equal(min, node.Min);
        Assert.Null(node.Max);
    }

    [Fact]
    public void UntilBuildsOneUntilNodeWithItemThenTerminator()
    {
        INode node = Assert.IsType<UntilNode<char, int, string>>(_item.Until(_comma));

        Assert.Equal(NodeKind.Until, node.Kind);
        Assert.Equal([_item, _comma], node.Children);
    }
}
