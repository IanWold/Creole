using Creole.Nodes;

namespace Creole.Tests;

public class GrammarBaseTests
{
    private enum K { Number, Comma }

    private readonly record struct Tok(K Kind) : IKinded<K>;

    private sealed class Bytes : Grammar<byte>
    {
        public static readonly Parser<byte, int> Zero = Return(0);
        public static readonly Parser<byte, int> Unsupported = Fail<int>("unsupported version");
    }

    private sealed class Tokens : TokenGrammar<Tok, K>
    {
        public static readonly Parser<Tok, string> Empty = Return("");
        public static readonly Parser<Tok, string> Missing = Fail<string>("missing value");
    }

    private sealed class Chars : CharGrammar
    {
        public static readonly Parser<char, Unit> Nothing = Return(new Unit());
        public static readonly Parser<char, Unit> Never = Fail<Unit>("never");
        public static readonly Parser<char, Unit> Checked = Grammar.Deterministic(Nothing);
    }

    [Fact]
    public void GrammarFixesTheInputType()
    {
        Assert.Equal(0, Assert.IsType<ReturnNode<byte, int>>(Bytes.Zero).Value);
        Assert.Equal("unsupported version", Assert.IsType<FailNode<byte, int>>(Bytes.Unsupported).Message);
    }

    [Fact]
    public void TokenGrammarFixesTheTokenType()
    {
        Assert.Equal("", Assert.IsType<ReturnNode<Tok, string>>(Tokens.Empty).Value);
        Assert.Equal("missing value", Assert.IsType<FailNode<Tok, string>>(Tokens.Missing).Message);
    }

    [Fact]
    public void CharGrammarFixesCharAsTheInputType()
    {
        Assert.IsType<ReturnNode<char, Unit>>(Chars.Nothing);
        Assert.Equal("never", Assert.IsType<FailNode<char, Unit>>(Chars.Never).Message);
    }

    [Fact]
    public void DeterministicReturnsItsParser() =>
        Assert.Same(Chars.Nothing, Chars.Checked);

    [Fact]
    public void DeterministicRejectsANullParserNamingTheExpression()
    {
        Parser<char, int> document = null!;

        var exception = Assert.Throws<GrammarException>(() => Grammar.Deterministic(document));

        Assert.Equal(
            "'document' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => document).",
            exception.Message
        );
    }

    [Fact]
    public void FailRejectsANullMessage() =>
        Assert.Throws<GrammarException>(() => Grammar<char>.Fail<int>(null!));
}
