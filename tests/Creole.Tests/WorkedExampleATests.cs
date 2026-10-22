using System.Globalization;
using System.Text;

namespace Creole.Tests;

file enum J
{
    LBrace,
    RBrace,
    LBracket,
    RBracket,
    Colon,
    Comma,
    String,
    Number,
    True,
    False,
    Null
}

file readonly record struct JTok(J Kind, ReadOnlyMemory<char> Text) : IKinded<J>;

file sealed record JsonString(string Value);

file sealed record JsonNumber(double Value);

file sealed record JsonBool(bool Value)
{
    public static JsonBool False { get; } = new(false);

    public static JsonBool True { get; } = new(true);
}

file sealed record JsonNull
{
    public static JsonNull Instance { get; } = new();
}

file sealed record JsonArray(IReadOnlyList<JsonValue> Items);

file sealed record JsonObject(IReadOnlyList<KeyValuePair<string, JsonValue>> Members);

file union JsonValue(JsonString, JsonNumber, JsonBool, JsonNull, JsonArray, JsonObject);

file sealed partial class JsonLexer : CharGrammar
{
    private static Parser<char, JTok> Tok<T>(Parser<char, T> p, J kind) =>
        p.Slice().Select(text => new JTok(kind, text));

    private static readonly Parser<char, Unit> _stringBody =
        OneOf(Seq(Char('\\'), Any).Ignore(), NoneOf("\"\\").SkipAtLeastOnce()).SkipMany();

    private static readonly Parser<char, Unit> _number =
        Seq(Char('-').Optional(),
            Digit.SkipAtLeastOnce(),
            Seq(Char('.'), Digit.SkipAtLeastOnce()).Optional()).Ignore();

    public static readonly Parser<char, JTok> Item = Longest(
        Tok(Char('{'), J.LBrace), Tok(Char('}'), J.RBrace),
        Tok(Char('['), J.LBracket), Tok(Char(']'), J.RBracket),
        Tok(Char(':'), J.Colon), Tok(Char(','), J.Comma),
        Tok(Seq(Char('"'), _stringBody, Char('"')), J.String),
        Tok(_number, J.Number),
        Tok(String("true"), J.True), Tok(String("false"), J.False), Tok(String("null"), J.Null));

    public static readonly Parser<char, Unit> Skip = Whitespace.SkipMany();
}

file sealed partial class JsonGrammar : TokenGrammar<JTok, J>
{
    public static readonly Parser<JTok, JsonValue> Value = Rec(() => OneOf(
        Kind(J.String).Select<JsonValue>(static t => new JsonString(Unescape(t.Text))),
        Kind(J.Number).Select<JsonValue>(static t => new JsonNumber(
            double.Parse(t.Text.Span, CultureInfo.InvariantCulture))),
        Kind(J.True).As<JsonValue>(JsonBool.True),
        Kind(J.False).As<JsonValue>(JsonBool.False),
        Kind(J.Null).As<JsonValue>(JsonNull.Instance),
        _arrayValue!,
        _objectValue!)).Named("value");

    private static readonly Parser<JTok, JsonValue> _arrayValue =
        Value.SepBy(Kind(J.Comma))
             .Between(Kind(J.LBracket), Kind(J.RBracket))
             .Select<JsonValue>(static items => new JsonArray(items));

    private static readonly Parser<JTok, JsonValue> _objectValue =
        Seq(Kind(J.String), Kind(J.Colon), Value)
            .Select(static (k, _, v) => KeyValuePair.Create(Unescape(k.Text), v))
            .SepBy(Kind(J.Comma))
            .Between(Kind(J.LBrace), Kind(J.RBrace))
            .Select<JsonValue>(static members => new JsonObject(members));

    public static readonly Parser<JTok, JsonValue> Document =
        Grammar.Deterministic(Value.Before(End));

    private static string Unescape(ReadOnlyMemory<char> text)
    {
        var body = text.Span[1..^1];
        var result = new StringBuilder(body.Length);

        for (var i = 0; i < body.Length; i++)
        {
            result.Append(body[i] == '\\' ? body[++i] : body[i]);
        }

        return result.ToString();
    }
}

file static class JsonExample
{
    public static readonly Parser<char, IReadOnlyList<JTok>> Tokens =
        JsonLexer.Skip.Then(JsonLexer.Item.Before(JsonLexer.Skip).Many()).Before(Grammar<char>.End);

    public static string? Parse(string json)
    {
        var chars = Cursor<char>.FromMemory(json.AsMemory());
        var (lexed, tokens) = Execution.Run(Tokens, ref chars);

        if (!lexed)
        {
            return null;
        }

        var cursor = new Cursor<JTok>([.. tokens]);
        var (parsed, value) = Execution.Run(JsonGrammar.Document, ref cursor);

        return parsed ? Render(value) : null;
    }

    public static string Render(JsonValue value) => value switch
    {
        JsonString s => $"\"{s.Value}\"",
        JsonNumber n => n.Value.ToString(CultureInfo.InvariantCulture),
        JsonBool b => b.Value ? "true" : "false",
        JsonNull => "null",
        JsonArray a => $"[{string.Join(",", a.Items.Select(Render))}]",
        JsonObject o => $"{{{string.Join(",", o.Members.Select(m => $"\"{m.Key}\":{Render(m.Value)}"))}}}",
    };
}

public class WorkedExampleATests
{
    [Fact]
    public void TheLexerReadsTokensAndSkipsWhitespace()
    {
        var cursor = Cursor<char>.FromMemory(" [true, -1.5, \"a\\\"b\"] ".AsMemory());

        var (ok, tokens) = Execution.Run(JsonExample.Tokens, ref cursor);

        Assert.True(ok);
        Assert.Equal(
            [(J.LBracket, "["), (J.True, "true"), (J.Comma, ","), (J.Number, "-1.5"), (J.Comma, ","), (J.String, "\"a\\\"b\""), (J.RBracket, "]")],
            tokens.Select(t => (t.Kind, t.Text.ToString())));
    }

    [Fact]
    public void TheParserReadsANestedDocument() =>
        Assert.Equal(
            "{\"name\":\"Creole\",\"tags\":[\"parser\",\"dotnet\"],\"version\":0.1,\"stable\":false,\"next\":null,\"empty\":{}}",
            JsonExample.Parse("""{ "name": "Creole", "tags": ["parser", "dotnet"], "version": 0.1, "stable": false, "next": null, "empty": {} }"""));

    [Fact]
    public void TheParserReadsDeeplyNestedArrays() =>
        Assert.Equal(new string('[', 200) + new string(']', 200), JsonExample.Parse(new string('[', 200) + new string(']', 200)));

    [Theory]
    [InlineData("[1,]")]
    [InlineData("{\"a\" 1}")]
    [InlineData("[1] 2")]
    [InlineData("")]
    public void TheParserRejectsInvalidDocuments(string json) =>
        Assert.Null(JsonExample.Parse(json));

    [Theory]
    [InlineData("""{ "a": [1, true, null, "x", {}], "b": false }""")]
    [InlineData("[1,]")]
    [InlineData("{\"a\" 1}")]
    [InlineData("[[[]]")]
    public void TheValueRuleDispatchesOnTokenKinds(string json)
    {
        var chars = Cursor<char>.FromMemory(json.AsMemory());
        var (_, tokens) = Execution.Run(JsonExample.Tokens, ref chars);
        var (without, with) = Rewrites.Compare(JsonGrammar.Document, LoweringOptions.Dispatch, [.. tokens]);

        Assert.IsType<DispatchPlan<JTok, JsonValue>>(new PlanBuilder(LoweringOptions.Dispatch).Lower(JsonGrammar.Value));
        Assert.Equal(without, with);
    }
}
