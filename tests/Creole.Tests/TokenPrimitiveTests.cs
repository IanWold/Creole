using System.Reflection;

namespace Creole.Tests;

file enum J { String, Number, Comma }

file readonly record struct JTok(J Kind, string Text) : IKinded<J>;

file sealed record Ident(string Name);

file sealed record Number(int Value);

file union Tok(Ident, Number) : IKinded<J>
{
    public J Kind => this switch
    {
        Ident => J.String,
        Number => J.Number,
    };
}

file class JsonTokens : TokenGrammar<JTok, J>
{
    public static readonly Parser<JTok, JTok> Comma = Kind(J.Comma);
    public static readonly Parser<JTok, JTok> String = Kind(J.String);
}

file class UnionTokens : TokenGrammar<Tok, J>
{
    public static readonly Parser<Tok, Ident> Name = Case<Ident>();
    public static readonly Parser<Tok, Number> Value = Case<Number>();
    public static readonly Parser<Tok, Tok> NumberKind = Kind(J.Number);
}

public class TokenPrimitiveTests
{
    private const string CaseSnippet = """
        using Creole;

        public enum K { Ident, Num }
        public sealed record Ident(string Name);
        public sealed record Num(int Value);

        public union Tok(Ident, Num) : IKinded<K>
        {
            public K Kind => this switch { Ident => K.Ident, Num => K.Num };
        }

        public sealed record V1Header(int Id);
        public sealed record V2Header(int Id);
        public union Header(V1Header, V2Header);

        public sealed class Tokens : TokenGrammar<Tok, K>
        {
            public static readonly Parser<Tok, Ident> Name = Case<Ident>();
            public static readonly Parser<Tok, int> Body = Return(0);
        #if MATCH_CASE
            public static readonly object Branch = Case<V1Header>(Body, static (h, body) => h.Id + body);
        #endif
        }
        """;

    private static IPrimitiveNode Primitive<TIn, TOut>(Parser<TIn, TOut> parser, NodeKind kind)
    {
        INode node = parser;

        Assert.Equal(kind, node.Kind);
        Assert.Empty(node.Children);

        return Assert.IsType<IPrimitiveNode>(node, exactMatch: false);
    }

    [Fact]
    public void CaseFirstSetsAreTheirCaseTypes()
    {
        var name = Primitive(UnionTokens.Name, NodeKind.Case);
        var value = Primitive(UnionTokens.Value, NodeKind.Case);

        Assert.Equal(FirstSet.OfCase(typeof(Ident)), name.First);
        Assert.False(name.Nullable);
        Assert.False(name.Opaque);
        Assert.False(name.First.Overlaps(value.First));
    }

    [Fact]
    public void CaseMayOverlapAKindBecauseAnalysisCannotRelateThem() =>
        Assert.True(Primitive(UnionTokens.Name, NodeKind.Case).First.Overlaps(Primitive(UnionTokens.NumberKind, NodeKind.Kind).First));

    [Fact]
    public void CaseOverloadsOnTokenGrammarAndGrammarBothResolve()
    {
        Assert.Empty(SnippetCompiler.Compile("CaseResolution", CaseSnippet));

        var grammarHasCase = typeof(Grammar<>)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Any(method => method.Name == "Case");

        if (!grammarHasCase)
        {
            Assert.Skip("Grammar<TIn>.Case<A>(parser, func) arrives with CORE-05.");
        }

        Assert.Empty(SnippetCompiler.Compile("CaseResolution", "#define MATCH_CASE\n" + CaseSnippet));
    }

    [Fact]
    public void CaseRejectsATokenTypeThatIsNotAUnion() =>
        Assert.Equal(
            $"Case<String>() needs a union token type, but {typeof(JTok).Name} is not a union. Use Kind(k) to match tokens by kind.",
            Assert.Throws<GrammarException>(() => TokenGrammar<JTok, J>.Case<string>()).Message
        );

    [Fact]
    public void KindHoldsItsKindAndStartsWithIt()
    {
        var node = Assert.IsType<KindNode<JTok, J>>(JsonTokens.Comma);
        var comma = Primitive(JsonTokens.Comma, NodeKind.Kind);

        Assert.Equal(J.Comma, node.TokenKind);
        Assert.Equal(FirstSet.OfItem(J.Comma), comma.First);
        Assert.False(comma.Nullable);
        Assert.False(comma.Opaque);
    }

    [Fact]
    public void KindsAreDisjointFromEachOther() =>
        Assert.False(Primitive(JsonTokens.Comma, NodeKind.Kind).First.Overlaps(Primitive(JsonTokens.String, NodeKind.Kind).First));
}
