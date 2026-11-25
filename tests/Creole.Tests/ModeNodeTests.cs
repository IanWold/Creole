using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file enum TokKind
{
    Word,
}

file readonly record struct Tok(TokKind Kind) : IKinded<TokKind>;

/// <summary>
/// The nodes that change a mode channel's stack, and how analysis and lowering treat them (§ 3. Mode channels).
/// </summary>
public class ModeNodeTests
{
    private enum LexMode
    {
        Operand,
        Operator,
    }

    private static readonly ModeChannel<LexMode> _modes = new(LexMode.Operand);

    public static TheoryData<string> ModeChanges => ["SetMode", "PushMode", "PopMode"];

    private static Parser<char, Unit> Change(string name) => name switch
    {
        "SetMode" => SetMode(_modes, LexMode.Operator),
        "PushMode" => PushMode(_modes, LexMode.Operator),
        _ => PopMode(_modes),
    };

    [Fact]
    public void AChannelHoldsItsInitialModeAndIsNamedByItsModeType()
    {
        Assert.Equal(LexMode.Operand, _modes.Initial);
        Assert.Equal("ModeChannel<LexMode>", _modes.ToString());
    }

    [Theory]
    [MemberData(nameof(ModeChanges))]
    public void AnAlternativeThatStartsWithAModeChangeHasNoDispatchKeys(string name)
    {
        Assert.IsType<DispatchPlan<char, char, LightMark>>(new PlanBuilder(dispatchThreshold: 2).Lower(OneOf(Char('a'), Char('b'))));
        Assert.IsType<OrPlan<char, char, LightMark>>(new PlanBuilder(dispatchThreshold: 2).Lower(OneOf(Change(name).Then(Char('a')), Char('b'))));
    }

    [Fact]
    public void ANullChannelNamesItsExpression()
    {
        ModeChannel<LexMode> channel = null!;

        Assert.Equal("'channel' is null.", Assert.Throws<GrammarException>(() => SetMode(channel, LexMode.Operator)).Message);
        Assert.Equal("'channel' is null.", Assert.Throws<GrammarException>(() => PushMode(channel, LexMode.Operator)).Message);
        Assert.Equal("'channel' is null.", Assert.Throws<GrammarException>(() => PopMode(channel)).Message);
    }

    [Fact]
    public void PopModeBuildsALeafNodeThatHoldsItsChannel()
    {
        var node = Assert.IsType<PopModeNode<char, LexMode>>(PopMode(_modes));

        Assert.Equal(NodeKind.PopMode, ((INode)node).Kind);
        Assert.Empty(((INode)node).Children);
        Assert.Same(_modes, node.Channel);
    }

    [Fact]
    public void PushModeBuildsALeafNodeThatHoldsItsChannelAndMode()
    {
        var node = Assert.IsType<PushModeNode<char, LexMode>>(PushMode(_modes, LexMode.Operator));

        Assert.Equal(NodeKind.PushMode, ((INode)node).Kind);
        Assert.Empty(((INode)node).Children);
        Assert.Same(_modes, node.Channel);
        Assert.Equal(LexMode.Operator, node.Mode);
    }

    [Fact]
    public void SetModeBuildsALeafNodeThatHoldsItsChannelAndMode()
    {
        var node = Assert.IsType<SetModeNode<char, LexMode>>(SetMode(_modes, LexMode.Operator));

        Assert.Equal(NodeKind.SetMode, ((INode)node).Kind);
        Assert.Empty(((INode)node).Children);
        Assert.Same(_modes, node.Channel);
        Assert.Equal(LexMode.Operator, node.Mode);
    }

    [Fact]
    public void TokenGrammarsHaveTheModeChanges() =>
        Assert.IsType<SetModeNode<Tok, LexMode>>(TokenGrammar<Tok, TokKind>.SetMode(_modes, LexMode.Operator));
}
