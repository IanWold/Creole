using static Creole.Grammar<char>;

namespace Creole.Tests;

public class SpanNodeTests
{
    private static readonly Parser<char, int> _int = Return(1);

    private static string NullMessage(string expression) =>
        $"'{expression}' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => {expression}).";

    [Fact]
    public void AdjacentBuildsOneAdjacentNode()
    {
        var second = Return('x');
        INode node = Assert.IsType<AdjacentNode<char, int, char>>(Adjacent(_int, second));

        Assert.Equal(NodeKind.Adjacent, node.Kind);
        Assert.Equal([_int, second], node.Children);
    }

    [Fact]
    public void LeadingTriviaIsOneSharedLeafNode()
    {
        INode node = Assert.IsType<LeadingTriviaNode<char>>(LeadingTrivia);

        Assert.Equal(NodeKind.LeadingTrivia, node.Kind);
        Assert.Empty(node.Children);
        Assert.Same(LeadingTrivia, CharGrammar.LeadingTrivia);
    }

    [Fact]
    public void NullArgumentsNameTheirExpressions()
    {
        Parser<char, int> item = null!;
        Func<int, SourceSpan, string> build = null!;

        Assert.Equal(NullMessage("item"), Assert.Throws<GrammarException>(() => Adjacent(item, _int)).Message);
        Assert.Equal(NullMessage("item"), Assert.Throws<GrammarException>(() => Adjacent(_int, item)).Message);
        Assert.Equal(NullMessage("item"), Assert.Throws<GrammarException>(() => item.Spanned()).Message);
        Assert.Equal(NullMessage("item"), Assert.Throws<GrammarException>(() => item.WithSpan(static (_, _) => "")).Message);
        Assert.Equal("'build' is null.", Assert.Throws<GrammarException>(() => _int.WithSpan(build)).Message);
    }

    [Fact]
    public void PositionIsOneSharedLeafNode()
    {
        INode node = Assert.IsType<PositionNode<char>>(Position);

        Assert.Equal(NodeKind.Position, node.Kind);
        Assert.Empty(node.Children);
        Assert.Same(Position, CharGrammar.Position);
    }

    [Fact]
    public void SpannedBuildsOneSpannedNode()
    {
        INode node = Assert.IsType<SpannedNode<char, int>>(_int.Spanned());

        Assert.Equal(NodeKind.Spanned, node.Kind);
        Assert.Equal([_int], node.Children);
    }

    [Fact]
    public void WithSpanBuildsOneWithSpanNode()
    {
        Func<int, SourceSpan, string> build = static (value, span) => $"{value}@{span.Start}";
        var node = Assert.IsType<WithSpanNode<char, int, string>>(_int.WithSpan(build));

        Assert.Equal(NodeKind.WithSpan, ((INode)node).Kind);
        Assert.Equal([_int], ((INode)node).Children);
        Assert.Same(build, node.Selector);
    }
}
