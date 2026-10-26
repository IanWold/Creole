using System.Globalization;
using System.Runtime.InteropServices;
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

file readonly record struct JTok(J Kind, ReadOnlyMemory<char> Text) : IKinded<J>
{
    static string IKinded<J>.Describe(J kind) => kind switch
    {
        J.LBrace => "'{'",
        J.RBrace => "'}'",
        J.LBracket => "'['",
        J.RBracket => "']'",
        J.Colon => "':'",
        J.Comma => "','",
        J.String => "string",
        J.Number => "number",
        J.True => "true",
        J.False => "false",
        J.Null => "null",
        _ => kind.ToString(),
    };

    string? IKinded<J>.Detail =>
        Kind is J.String or J.Number
        ? Text.ToString()
        : null;
}

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
            .Select<JsonValue>(static members => new JsonObject(members))
            .Context("in object");

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

file static class HandLexer
{
    private static readonly Dictionary<char, J> _punctuation = new()
    {
        ['{'] = J.LBrace,
        ['}'] = J.RBrace,
        ['['] = J.LBracket,
        [']'] = J.RBracket,
        [':'] = J.Colon,
        [','] = J.Comma,
    };

    private static readonly Dictionary<string, J> _words = new()
    {
        ["true"] = J.True,
        ["false"] = J.False,
        ["null"] = J.Null,
    };

    public static JTok[] Lex(string json)
    {
        var tokens = new List<JTok>();
        var i = 0;

        while (i < json.Length)
        {
            var start = i;
            var c = json[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (_punctuation.TryGetValue(c, out var kind))
            {
                i++;
            }
            else if (c == '"')
            {
                i++;

                while (json[i] != '"')
                {
                    i += json[i] == '\\'
                        ? 2
                        : 1;
                }

                i++;
                kind = J.String;
            }
            else if (c == '-' || char.IsAsciiDigit(c))
            {
                i++;

                while (i < json.Length && (char.IsAsciiDigit(json[i]) || json[i] == '.'))
                {
                    i++;
                }

                kind = J.Number;
            }
            else
            {
                while (i < json.Length && char.IsAsciiLetter(json[i]))
                {
                    i++;
                }

                kind = _words[json[start..i]];
            }

            tokens.Add(new JTok(kind, json.AsMemory(start, i - start)));
        }

        return [.. tokens];
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

    public static ParseError? Error(string json, SourceId source)
    {
        var chars = Cursor<char>.FromMemory(json.AsMemory());
        var (lexed, lexedTokens) = Execution.Run(Tokens, ref chars);

        Assert.True(lexed);

        JTok[] tokens = [.. lexedTokens];
        var log = new FailureLog<JTok>();
        var cursor = new Cursor<JTok>(tokens) { Failures = log };
        JsonValue value = default!;

        Assert.False(PlanCache.Get(JsonGrammar.Document).TryParse(ref cursor, ref value));

        return log.ToError(in cursor, "parser", (start, end) => Span(json, source, tokens, start, end));
    }

    private static int Offset(JTok token) =>
        MemoryMarshal.TryGetString(token.Text, out _, out var start, out _)
        ? start
        : throw new InvalidOperationException("A token's text is not a slice of the input.");

    private static SourceSpan Span(string json, SourceId source, JTok[] tokens, long start, long end)
    {
        if (start == end)
        {
            var at = start < tokens.Length
                ? Offset(tokens[start])
                : json.Length;

            return new SourceSpan(source, at, at);
        }

        var last = tokens[end - 1];

        return new SourceSpan(source, Offset(tokens[start]), Offset(last) + last.Text.Length);
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
    public void AMalformedDocumentReportsTheTokenError()
    {
        var result = JsonGrammar.Document.Parse(HandLexer.Lex("""{ "port": 8080 "host": "x" }"""));
        var error = Assert.Single(Assert.IsType<Failed>(result.Value).Diagnostics);

        Assert.Equal(new SourceSpan(SourceId.Unnamed, 4, 5), error.Span);
        Assert.Equal(new UnexpectedItem("string \"host\""), error.Unexpected.Value);
        Assert.Equal([new ExpectedItem("','"), new ExpectedItem("'}'")], error.Expected);
        Assert.Equal([new ContextLine("in object", new SourceSpan(SourceId.Unnamed, 0, 1))], error.Context);
        Assert.Equal("error[parser] at 4..5: unexpected string \"host\"; expected ',' or '}'; in object starting at 0", result.Diagnostics.Render());
    }

    [Fact]
    public void TheGrammarParsesTokensFromAHandWrittenLexer()
    {
        var tokens = HandLexer.Lex("""{ "name": "Cre\"ole", "tags": ["parser", "dotnet"], "version": -0.1, "stable": false, "next": null, "empty": {} }""");
        var summary = JsonGrammar.Document.Parse(tokens) switch
        {
            Parsed<JsonValue> parsed => JsonExample.Render(parsed.Value),
            Recovered<JsonValue> recovered => $"recovered: {recovered.Diagnostics.Render()}",
            Failed failed => failed.Diagnostics.Render(),
        };

        Assert.Equal("{\"name\":\"Cre\"ole\",\"tags\":[\"parser\",\"dotnet\"],\"version\":-0.1,\"stable\":false,\"next\":null,\"empty\":{}}", summary);
    }

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
        var named = Assert.IsType<NamedPlan<JTok, JsonValue>>(new PlanBuilder(LoweringOptions.Dispatch).Lower(JsonGrammar.Value));
        var value = Assert.IsType<RecPlan<JTok, JsonValue>>(named.Item);

        Assert.IsType<DispatchPlan<JTok, JsonValue>>(value.Target);
        Assert.Equal(without, with);
    }

    [Fact]
    public void AnErrorRendersAsTheWhitepaperShowsIt()
    {
        var registry = new SourceRegistry();
        var config = registry.Add("config.json");
        var text = "[\n{\n  \"port\": 8080 \"host\": \"x\"\n}\n]";
        var error = JsonExample.Error(text, config);

        Assert.NotNull(error);
        Assert.Equal(
            """
            error[parser]: expected ',' or '}'
             --> config.json:3:16
              |
            3 |   "port": 8080 "host": "x"
              |                ^^^^^^ unexpected string "host"
              = note: in object starting at 2:1
            """
                .ReplaceLineEndings("\n"),
            new Diagnostics([error]).Render(new SourceText(config, "config.json", text))
        );
    }
}
