using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

public class StateNodeTests
{
    private static readonly Parser<char, char> _countA = Char('a').Scan<int, char>(static (a, count, _) => (a, count + 1));

    [Fact]
    public void AGrammarThatChangesStateTakesFullMarks()
    {
        Assert.IsType<OrPlan<char, char, FullMark>>(new PlanBuilder().Lower(SetState(1).Then(Char('a')).Or(Char('b'))));
        Assert.IsType<OrPlan<char, char, FullMark>>(new PlanBuilder().Lower(_countA.Or(Char('b'))));
        Assert.IsType<OrPlan<char, char, FullMark>>(new PlanBuilder().Lower(Char('b').Or(Rec(() => _countA))));
    }

    [Fact]
    public void AGrammarThatOnlyReadsStateTakesLightMarks() =>
        Assert.IsType<OrPlan<char, char, LightMark>>(new PlanBuilder().Lower(GetState<int>().Then(Char('a')).Or(Char('b'))));

    [Fact]
    public void ANullFunctionNamesItsExpression()
    {
        Func<char, int, SourceSpan, (char, int)> scan = null!;

        Assert.Equal("'scan' is null.", Assert.Throws<GrammarException>(() => Char('a').Scan(scan)).Message);
    }

    [Fact]
    public void GetStateIsOneSharedLeafNodeForEachStateType()
    {
        INode node = Assert.IsType<GetStateNode<char, int>>(GetState<int>());

        Assert.Equal(NodeKind.GetState, node.Kind);
        Assert.Empty(node.Children);
        Assert.Same(GetState<int>(), CharGrammar.GetState<int>());
        Assert.NotSame((INode)GetState<int>(), GetState<long>());
    }

    [Fact]
    public void ScanBuildsOneScanNodeOverItsSource()
    {
        var source = Char('a');
        INode node = Assert.IsType<ScanNode<char, char, int, char>>(source.Scan<int, char>(static (c, count, _) => (c, count)));

        Assert.Equal(NodeKind.Scan, node.Kind);
        Assert.Equal([source], node.Children);
    }

    [Fact]
    public void ScanTakesItsSourcesKeysForDispatchAndSetStateHasNone()
    {
        Assert.IsType<DispatchPlan<char, char, FullMark>>(new PlanBuilder(dispatchThreshold: 2).Lower(OneOf(_countA, Char('b'))));
        Assert.IsType<OrPlan<char, char, FullMark>>(new PlanBuilder(dispatchThreshold: 2).Lower(OneOf(SetState(1).Then(Char('a')), Char('b'))));
    }

    [Fact]
    public void SetStateBuildsALeafNodeThatHoldsItsState()
    {
        var node = Assert.IsType<SetStateNode<char, string>>(SetState("x"));

        Assert.Equal(NodeKind.SetState, ((INode)node).Kind);
        Assert.Empty(((INode)node).Children);
        Assert.Equal("x", node.State);
    }
}
