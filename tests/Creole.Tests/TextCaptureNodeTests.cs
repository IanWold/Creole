using static Creole.Grammar<char>;

namespace Creole.Tests;

public class TextCaptureNodeTests
{
    private static readonly Parser<char, int> _int = Return(1);

    private static string NullMessage(string expression) =>
        $"'{expression}' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => {expression}).";

    [Fact]
    public void NullReceiverNamesTheExpression()
    {
        Parser<char, int> identifier = null!;

        Assert.Equal(NullMessage("identifier"), Assert.Throws<GrammarException>(() => identifier.Slice()).Message);
        Assert.Equal(NullMessage("identifier"), Assert.Throws<GrammarException>(() => identifier.Text()).Message);
    }

    [Fact]
    public void SliceBuildsOneSliceNode()
    {
        INode node = Assert.IsType<SliceNode<int>>(_int.Slice());

        Assert.Equal(NodeKind.Slice, node.Kind);
        Assert.Equal([_int], node.Children);
    }

    [Fact]
    public void TextBuildsOneTextNode()
    {
        INode node = Assert.IsType<TextNode<int>>(_int.Text());

        Assert.Equal(NodeKind.Text, node.Kind);
        Assert.Equal([_int], node.Children);
    }
}
