using static Creole.Grammar<char>;

namespace Creole.Tests;

public class SequencingNodeTests
{
    private static readonly Parser<char, bool> _bool = Return(true);
    private static readonly Parser<char, int> _int = Return(1);
    private static readonly Parser<char, int>[] _operands = [.. Enumerable.Range(1, 8).Select(Return)];
    private static readonly Parser<char, int> _p1 = _operands[0];
    private static readonly Parser<char, int> _p2 = _operands[1];
    private static readonly Parser<char, int> _p3 = _operands[2];
    private static readonly Parser<char, int> _p4 = _operands[3];
    private static readonly Parser<char, int> _p5 = _operands[4];
    private static readonly Parser<char, int> _p6 = _operands[5];
    private static readonly Parser<char, int> _p7 = _operands[6];
    private static readonly Parser<char, int> _p8 = _operands[7];
    private static readonly Parser<char, string> _string = Return("x");

    private static void AssertSequence(object parser, int arity, Delegate selector)
    {
        var node = Assert.IsAssignableFrom<INode>(parser);

        Assert.Equal(NodeKind.Sequence, node.Kind);
        Assert.Equal(_operands[..arity], node.Children);
        Assert.Same(selector, parser.GetType().GetProperty("Selector")!.GetValue(parser));
    }

    private static string NullMessage(string expression) =>
        $"'{expression}' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => {expression}).";

    [Fact]
    public void AsMapsToAFixedValue()
    {
        var node = Assert.IsType<MapNode<char, int, string>>(_int.As("one"));

        Assert.Equal([_int], ((INode)node).Children);
        Assert.Equal("one", node.Selector(7));
    }

    [Fact]
    public void BeforeIsASequenceKeepingTheFirstResult()
    {
        var node = Assert.IsType<SequenceNode<char, int, string, int>>(_int.Before(_string));

        Assert.Equal(NodeKind.Sequence, ((INode)node).Kind);
        Assert.Equal([_int, _string], ((INode)node).Children);
        Assert.Equal(5, node.Selector(5, "y"));
    }

    [Fact]
    public void BetweenIsASequenceOfOpenThisAndClose()
    {
        var node = Assert.IsType<SequenceNode<char, string, int, bool, int>>(_int.Between(_string, _bool));

        Assert.Equal([_string, _int, _bool], ((INode)node).Children);
        Assert.Equal(5, node.Selector("(", 5, false));
    }

    [Fact]
    public void IgnoreMapsToUnit()
    {
        var node = Assert.IsType<MapNode<char, int, Unit>>(_int.Ignore());

        Assert.Equal([_int], ((INode)node).Children);
        Assert.Equal(default, node.Selector(7));
    }

    [Fact]
    public void NaryResultTypeCannotBeGivenExplicitly()
    {
        var errors = SnippetCompiler.Compile("Outsider", """
            sealed class G : Creole.CharGrammar
            {
                static readonly Creole.Parser<char, object> P = Seq(Return(1), Return(2)).Select<object>((a, b) => a + b);
            }
            """
        );

        Assert.Equal(["CS1593"], errors);
    }

    [Fact]
    public void NarySelectAfterAnotherSelectorDoesNotReplaceIt()
    {
        var swapped = Seq(_p1, _p2).Select((a, b) => (b, a));
        var node = Assert.IsType<MapNode<char, (int, int), int>>(swapped.Select((first, _) => first));

        Assert.Equal([swapped], ((INode)node).Children);
    }

    [Fact]
    public void NarySelectOnATupleNotBuiltBySeqIsAMap()
    {
        var pair = Return((1, "x"));
        var node = Assert.IsType<MapNode<char, (int, string), string>>(pair.Select((n, s) => s + n));

        Assert.Equal([pair], ((INode)node).Children);
        Assert.Equal("y2", node.Selector((2, "y")));
    }

    [Fact]
    public void NullOperandsNameTheExpression()
    {
        Parser<char, int> later = null!;

        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => Seq(later, _int)).Message);
        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => Seq(_p1, _p2, _p3, _p4, _p5, _p6, _p7, later)).Message);
        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => _int.Before(later)).Message);
        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => _int.Then(later)).Message);
        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => _int.Between(later, _int)).Message);
        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => _int.Between(_int, later)).Message);
    }

    [Fact]
    public void NullReceiverOfNarySelectNamesTheExpression()
    {
        Parser<char, (int, int)> pair = null!;

        Assert.Equal(NullMessage("pair"), Assert.Throws<GrammarException>(() => pair.Select((a, b) => a + b)).Message);
    }

    [Fact]
    public void NullSelectorNamesTheExpression()
    {
        Func<int, int> unary = null!;
        Func<int, int, int> binary = null!;

        Assert.Equal("'unary' is null.", Assert.Throws<GrammarException>(() => _int.Select(unary)).Message);
        Assert.Equal("'binary' is null.", Assert.Throws<GrammarException>(() => Seq(_p1, _p2).Select(binary)).Message);
    }

    [Fact]
    public void SelectCanNameAUnionResultType()
    {
        var node = Assert.IsType<MapNode<char, int, Shape>>(_int.Select<Shape>(static n => new Circle(n)));

        Assert.Equal(new Circle(3), node.Selector(3).Value);
    }

    [Fact]
    public void SelectIsAMapOverItsSource()
    {
        var node = Assert.IsType<MapNode<char, int, string>>(_int.Select(n => $"#{n}"));

        Assert.Equal(NodeKind.Map, ((INode)node).Kind);
        Assert.Equal([_int], ((INode)node).Children);
        Assert.Equal("#4", node.Selector(4));
    }

    [Fact]
    public void SeqAloneProducesATuple()
    {
        var node = Assert.IsType<SequenceNode<char, int, string, bool, (int, string, bool)>>(Seq(_int, _string, _bool));

        Assert.Equal([_int, _string, _bool], ((INode)node).Children);
        Assert.Equal((2, "y", false), node.Selector(2, "y", false));
    }

    [Fact]
    public void SeqThenSelectBuildsOneSequenceNode()
    {
        Func<int, string, bool, string> combine = static (a, b, c) => $"{a}{b}{c}";

        var node = Assert.IsType<SequenceNode<char, int, string, bool, string>>(Seq(_int, _string, _bool).Select(combine));

        Assert.Equal(NodeKind.Sequence, ((INode)node).Kind);
        Assert.Equal([_int, _string, _bool], ((INode)node).Children);
        Assert.Same(combine, node.Selector);
    }

    [Fact]
    public void SeqThenSelectBuildsOneSequenceNodeAtEveryArity()
    {
        Func<int, int, int> f2 = static (a, b) => a + b;
        Func<int, int, int, int> f3 = static (a, b, c) => a + b + c;
        Func<int, int, int, int, int> f4 = static (a, b, c, d) => a + b + c + d;
        Func<int, int, int, int, int, int> f5 = static (a, b, c, d, e) => a + b + c + d + e;
        Func<int, int, int, int, int, int, int> f6 = static (a, b, c, d, e, f) => a + b + c + d + e + f;
        Func<int, int, int, int, int, int, int, int> f7 = static (a, b, c, d, e, f, g) => a + b + c + d + e + f + g;
        Func<int, int, int, int, int, int, int, int, int> f8 = static (a, b, c, d, e, f, g, h) => a + b + c + d + e + f + g + h;

        AssertSequence(Seq(_p1, _p2).Select(f2), 2, f2);
        AssertSequence(Seq(_p1, _p2, _p3).Select(f3), 3, f3);
        AssertSequence(Seq(_p1, _p2, _p3, _p4).Select(f4), 4, f4);
        AssertSequence(Seq(_p1, _p2, _p3, _p4, _p5).Select(f5), 5, f5);
        AssertSequence(Seq(_p1, _p2, _p3, _p4, _p5, _p6).Select(f6), 6, f6);
        AssertSequence(Seq(_p1, _p2, _p3, _p4, _p5, _p6, _p7).Select(f7), 7, f7);
        AssertSequence(Seq(_p1, _p2, _p3, _p4, _p5, _p6, _p7, _p8).Select(f8), 8, f8);
    }

    [Fact]
    public void ThenIsASequenceKeepingTheSecondResult()
    {
        var node = Assert.IsType<SequenceNode<char, int, string, string>>(_int.Then(_string));

        Assert.Equal([_int, _string], ((INode)node).Children);
        Assert.Equal("y", node.Selector(5, "y"));
    }
}

file sealed record Circle(int Radius);

file sealed record Square(int Side);

file union Shape(Circle, Square);
