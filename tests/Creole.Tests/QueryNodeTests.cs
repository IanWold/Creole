using System.Linq.Expressions;
using System.Reflection;

using static Creole.Grammar<char>;

namespace Creole.Tests;

file enum J {
    String,
    Colon,
    Number
}

file readonly record struct JTok(J Kind, string Text) : IKinded<J>;

file sealed class JsonGrammar : TokenGrammar<JTok, J>
{
    public static readonly Parser<JTok, string> Value = Kind(J.Number).Select(static t => t.Text);

    public static readonly Parser<JTok, KeyValuePair<string, string>> Member =
        from key in Kind(J.String)
        from colon in Kind(J.Colon)
        from value in Value
        select KeyValuePair.Create(key.Text, value);
}

file sealed class BlobGrammar : Grammar<byte>
{
    public static readonly Parser<byte, int> UInt16 = Seq(Any, Any).Select(static (hi, lo) => hi << 8 | lo);

    public static readonly Parser<byte, byte[]> Blob =
        from length in UInt16
        from bytes in Any.Repeat(length)
        select bytes.ToArray();
}

public class QueryNodeTests
{
    private static readonly Parser<char, int> _int = Return(1);
    private static readonly Parser<char, string> _string = Return("x");

    private static IEnumerable<INode> Descendants(INode node) =>
        node.Children.SelectMany(Descendants).Prepend(node);

    private static string NullMessage(string expression) =>
        $"'{expression}' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => {expression}).";

    [Fact]
    public void BindBuildsABindNode()
    {
        var node = Assert.IsType<BindNode<char, int, string, string>>(_int.Bind(n => _string));

        Assert.Equal(NodeKind.Bind, ((INode)node).Kind);
        Assert.Equal([_int], ((INode)node).Children);
        Assert.Same(_string, node.Next(2));
        Assert.Equal("y", node.Selector(2, "y"));
    }

    [Fact]
    public void BlobQueryBuildsABind()
    {
        var node = Assert.IsType<BindNode<byte, int, IReadOnlyList<byte>, byte[]>>(BlobGrammar.Blob);

        Assert.Equal(NodeKind.Bind, ((INode)node).Kind);
        Assert.Equal([BlobGrammar.UInt16], ((INode)node).Children);
        Assert.Equal(NodeKind.Repeat, ((INode)node.Next(3)).Kind);
        Assert.NotSame(node.Next(2), node.Next(3));
        Assert.Equal([1, 2], node.Selector(2, [1, 2]));
    }

    [Fact]
    public void ClauseThatUsesItsParameterIsCompiledNotEvaluated()
    {
        var calls = 0;
        Func<int, Parser<char, string>> counted = _ =>
        {
            calls++;
            return _string;
        };

        var node = Assert.IsType<BindNode<char, int, string, string>>(from a in _int from b in counted(a) select b + a);

        Assert.Equal(0, calls);
        Assert.Same(_string, node.Next(2));
        Assert.Equal(1, calls);
        Assert.Equal("y2", node.Selector(2, "y"));
    }

    [Fact]
    public void ClauseThatDoesNotUseItsParameterIsEvaluatedOnce()
    {
        var calls = 0;
        Func<Parser<char, string>> counted = () =>
        {
            calls++;
            return _string;
        };

        var node = Assert.IsType<SequenceNode<char, int, string, string>>(from a in _int from b in counted() select b + a);

        Assert.Equal(1, calls);
        Assert.Equal([_int, _string], ((INode)node).Children);
        Assert.Equal("y2", node.Selector(2, "y"));
    }

    [Fact]
    public void ClauseThatThrowsWhenEvaluatedThrowsUnwrapped()
    {
        Parser<char, int> later = null!;

        var exception = Assert.Throws<GrammarException>(() => from a in _int from b in _string.Before(later) select b + a);

        Assert.Equal(NullMessage("later"), exception.Message);
    }

    [Fact]
    public void LaterClauseThatUsesAnEarlierVariableIsABindOverTheSequenceBeforeIt()
    {
        INode node = from a in _int from b in _string from c in _string.Repeat(a) select b + c.Count;

        Assert.Equal(NodeKind.Bind, node.Kind);
        var first = Assert.Single(node.Children);
        Assert.Equal(NodeKind.Sequence, first.Kind);
        Assert.Equal([_int, _string], first.Children);
    }

    [Fact]
    public void LetIsAMap()
    {
        INode node = from a in _int let b = a * 2 select b;

        Assert.Equal(NodeKind.Map, node.Kind);
        var inner = Assert.Single(node.Children);
        Assert.Equal(NodeKind.Map, inner.Kind);
        Assert.Equal([_int], inner.Children);
    }

    [Fact]
    public void MemberQueryBuildsASequence()
    {
        INode member = JsonGrammar.Member;

        Assert.Equal(NodeKind.Sequence, member.Kind);
        Assert.Equal(2, member.Children.Count);
        Assert.Equal(NodeKind.Sequence, member.Children[0].Kind);
        Assert.Equal([NodeKind.Kind, NodeKind.Kind], member.Children[0].Children.Select(child => child.Kind));
        Assert.Same(JsonGrammar.Value, member.Children[1]);
        Assert.DoesNotContain(NodeKind.Bind, Descendants(member).Select(node => node.Kind));
    }

    [Fact]
    public void NoNodeInABuiltQueryHoldsAnExpression()
    {
        INode query =
            from a in _int
            from b in _string
            where b.Length == a
            let c = b + a
            from d in _string.Repeat(a)
            select c + d.Count;

        var fields = Descendants(query)
            .SelectMany(node => node.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Select(field => field.GetValue(node)));

        Assert.Contains(NodeKind.Bind, Descendants(query).Select(node => node.Kind));
        Assert.Contains(NodeKind.Sequence, Descendants(query).Select(node => node.Kind));
        Assert.DoesNotContain(fields, value => value is System.Linq.Expressions.Expression);
    }

    [Fact]
    public void NullArgumentsNameTheExpression()
    {
        Func<int, Parser<char, string>> choose = null!;
        Expression<Func<int, Parser<char, string>>> clause = null!;
        Func<int, string, string> combine = null!;
        Func<int, bool> keep = null!;

        Assert.Equal("'choose' is null.", Assert.Throws<GrammarException>(() => _int.Bind(choose)).Message);
        Assert.Equal("'clause' is null.", Assert.Throws<GrammarException>(() => _int.SelectMany(clause, (a, b) => b)).Message);
        Assert.Equal("'combine' is null.", Assert.Throws<GrammarException>(() => _int.SelectMany(a => _string, combine)).Message);
        Assert.Equal("'keep' is null.", Assert.Throws<GrammarException>(() => _int.Where(keep)).Message);
    }

    [Fact]
    public void NullParserFromAClauseNamesTheClause()
    {
        Parser<char, string> later = null!;

        var exception = Assert.Throws<GrammarException>(() => from a in _int from b in later select b + a);

        Assert.Equal(NullMessage("later"), exception.Message);
    }

    [Fact]
    public void WhereAfterTwoClausesFiltersTheSequence()
    {
        INode node = from a in _int from b in _string where b.Length == a select b + a;

        Assert.Equal(NodeKind.Map, node.Kind);
        var filter = Assert.Single(node.Children);
        Assert.Equal(NodeKind.Filter, filter.Kind);
        var sequence = Assert.Single(filter.Children);
        Assert.Equal(NodeKind.Sequence, sequence.Kind);
        Assert.Equal([_int, _string], sequence.Children);
    }

    [Fact]
    public void WhereBuildsAFilter()
    {
        var node = Assert.IsType<FilterNode<char, int>>(from a in _int where a > 0 select a);

        Assert.Equal(NodeKind.Filter, ((INode)node).Kind);
        Assert.Equal([_int], ((INode)node).Children);
        Assert.True(node.Predicate(1));
        Assert.False(node.Predicate(0));
    }
}
