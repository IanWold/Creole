namespace Creole.Tests;

file enum Punctuation
{
    Comma,
    Text,
}

file readonly record struct Described(Punctuation Kind, string Text) : IKinded<Punctuation>
{
    static string IKinded<Punctuation>.Describe(Punctuation kind) =>
        kind == Punctuation.Comma
        ? "','"
        : "text";

    string? IKinded<Punctuation>.Detail =>
        Kind == Punctuation.Text
        ? $"\"{Text}\""
        : null;
}

file readonly record struct Plain(Punctuation Kind) : IKinded<Punctuation>;

file sealed record Word(string Value);

file sealed record Mark(char Value);

file union UnionToken(Word, Mark) : IKinded<Punctuation>
{
    public readonly Punctuation Kind =>
        Value is Mark
        ? Punctuation.Comma
        : Punctuation.Text;

    static string IKinded<Punctuation>.Describe(Punctuation kind) =>
        kind == Punctuation.Comma
        ? "mark"
        : "word";

    readonly string? IKinded<Punctuation>.Detail =>
        Value is Word word
        ? word.Value
        : null;
}

file static class Display
{
    public static string? Detail<TToken>(TToken token) where TToken : IKinded<Punctuation> =>
        token.Detail;

    public static string Kind<TToken>(Punctuation kind) where TToken : IKinded<Punctuation> =>
        TToken.Describe(kind);
}

public class TokenDisplayTests
{
    [Fact]
    public void ATokenTypeDescribesItsKinds()
    {
        Assert.Equal("','", Display.Kind<Described>(Punctuation.Comma));
        Assert.Equal("text", Display.Kind<Described>(Punctuation.Text));
    }

    [Fact]
    public void ATokenAddsItsDetail()
    {
        Assert.Equal("\"host\"", Display.Detail(new Described(Punctuation.Text, "host")));
        Assert.Null(Display.Detail(new Described(Punctuation.Comma, ",")));
    }

    [Fact]
    public void ATokenThatOverridesNothingShowsKindNamesAndNoDetail()
    {
        Assert.Equal("Comma", Display.Kind<Plain>(Punctuation.Comma));
        Assert.Null(Display.Detail(new Plain(Punctuation.Text)));
    }

    [Fact]
    public void AUnionTokenCanDescribeItself()
    {
        Assert.Equal("mark", Display.Kind<UnionToken>(Punctuation.Comma));
        Assert.Equal("ok", Display.Detail(new UnionToken(new Word("ok"))));
        Assert.Null(Display.Detail(new UnionToken(new Mark(','))));
    }

    [Fact]
    public void TheInterfaceIsStillATypeArgument()
    {
        List<IKinded<Punctuation>> tokens = [new Plain(Punctuation.Comma), new Described(Punctuation.Text, "x")];

        Assert.Equal([null, "\"x\""], tokens.Select(static token => token.Detail));
    }
}
