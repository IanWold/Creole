using static Creole.Grammar<char>;

namespace Creole.Tests;

public class ControlNodeTests
{
    private static readonly Parser<char, int> _int = Return(1);

    private static string NullMessage(string expression) =>
        $"'{expression}' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => {expression}).";

    [Fact]
    public void AtomicBuildsOneAtomicNode()
    {
        INode node = Assert.IsType<AtomicNode<char, int>>(Atomic(_int));

        Assert.Equal(NodeKind.Atomic, node.Kind);
        Assert.Equal([_int], node.Children);
    }

    [Fact]
    public void ContextBuildsOneContextNodeWithItsText()
    {
        var node = Assert.IsType<ContextNode<char, int>>(_int.Context("in object"));

        Assert.Equal(NodeKind.Context, ((INode)node).Kind);
        Assert.Equal([_int], ((INode)node).Children);
        Assert.Equal("in object", node.Text);
    }

    [Fact]
    public void CutIsOneSharedLeaf()
    {
        INode node = Assert.IsType<CutNode<char>>(Cut);

        Assert.Equal(NodeKind.Cut, node.Kind);
        Assert.Empty(node.Children);
        Assert.Same(Cut, Grammar<char>.Cut);
        Assert.Same(CharGrammar.Cut, Grammar<char>.Cut);
    }

    [Fact]
    public void LookaheadBuildsOneLookaheadNode()
    {
        INode node = Assert.IsType<LookaheadNode<char, int>>(Lookahead(_int));

        Assert.Equal(NodeKind.Lookahead, node.Kind);
        Assert.Equal([_int], node.Children);
    }

    [Fact]
    public void NamedBuildsOneNamedNodeWithItsLabel()
    {
        var node = Assert.IsType<NamedNode<char, int>>(_int.Named("value"));

        Assert.Equal(NodeKind.Named, ((INode)node).Kind);
        Assert.Equal([_int], ((INode)node).Children);
        Assert.Equal(new Label("value"), node.Label);
    }

    [Fact]
    public void NotBuildsOneNotNode()
    {
        INode node = Assert.IsType<NotNode<char, int>>(Not(_int));

        Assert.Equal(NodeKind.Not, node.Kind);
        Assert.Equal([_int], node.Children);
    }

    [Fact]
    public void NullParserNamesTheExpression()
    {
        Parser<char, int> later = null!;

        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => Atomic(later)).Message);
        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => Lookahead(later)).Message);
        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => Not(later)).Message);
    }

    [Fact]
    public void NullReceiverNamesTheExpression()
    {
        Parser<char, int> later = null!;

        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => later.Named("value")).Message);
        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => later.Context("in object")).Message);
    }

    [Fact]
    public void NullOrEmptyLabelOrTextIsRejected()
    {
        Assert.Equal("'label' is null.", Assert.Throws<GrammarException>(() => _int.Named(null!)).Message);
        Assert.Equal("'label' is empty.", Assert.Throws<GrammarException>(() => _int.Named("")).Message);
        Assert.Equal("'text' is null.", Assert.Throws<GrammarException>(() => _int.Context(null!)).Message);
        Assert.Equal("'text' is empty.", Assert.Throws<GrammarException>(() => _int.Context("")).Message);
    }
}
