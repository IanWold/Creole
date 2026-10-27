using System.Globalization;
using System.Runtime.InteropServices;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file sealed record JsonString(string Value);

file sealed record JsonNumber(double Value);

file sealed record JsonBool(bool Value);

file sealed record JsonNull;

file sealed record JsonArray(IReadOnlyList<JsonValue> Items);

file sealed record JsonObject(IReadOnlyList<KeyValuePair<string, JsonValue>> Members);

file union JsonValue(JsonString, JsonNumber, JsonBool, JsonNull, JsonArray, JsonObject);

file sealed class CharJson : CharGrammar
{
    private static readonly Parser<char, Unit> _space = Whitespace.SkipMany();

    private static Parser<char, T> Token<T>(Parser<char, T> parser) =>
        parser.Before(_space);

    private static readonly Parser<char, char> _escape =
        Char('\\').Then(OneOf(Char('"'), Char('\\'), Char('/'), Char('n').As('\n'), Char('t').As('\t')));

    private static readonly Parser<char, string> _string =
        Token(Char('"').Then(NoneOf("\"\\").Or(_escape).Many()).Before(Char('"')))
            .Select(static chars => new string([.. chars]));

    private static readonly Parser<char, double> _number =
        Token(Seq(Char('-').Optional(), Digit.SkipAtLeastOnce(), Seq(Char('.'), Digit.SkipAtLeastOnce()).Optional()).Slice())
            .Select(static text => double.Parse(text.Span, CultureInfo.InvariantCulture));

    public static readonly Parser<char, JsonValue> Value = Rec(() => OneOf(
        _string.Select<JsonValue>(static text => new JsonString(text)),
        _number.Select<JsonValue>(static number => new JsonNumber(number)),
        Token(String("true")).As<JsonValue>(new JsonBool(true)),
        Token(String("false")).As<JsonValue>(new JsonBool(false)),
        Token(String("null")).As<JsonValue>(new JsonNull()),
        _array!,
        _object!)).Named("value");

    private static readonly Parser<char, JsonValue> _array =
        Value.SepBy(Token(Char(',')))
            .Between(Token(Char('[')), Token(Char(']')))
            .Select<JsonValue>(static items => new JsonArray(items));

    private static readonly Parser<char, JsonValue> _object =
        Seq(_string, Token(Char(':')), Value)
            .Select(static (key, _, value) => KeyValuePair.Create(key, value))
            .SepBy(Token(Char(',')))
            .Between(Token(Char('{')), Token(Char('}')))
            .Select<JsonValue>(static members => new JsonObject(members))
            .Context("in object");

    public static readonly Parser<char, JsonValue> Document =
        _space.Then(Value).Before(End);

    public static string Outcome(ParseResult<JsonValue> result) => result switch
    {
        Parsed<JsonValue> parsed => Show(parsed.Value),
        Recovered<JsonValue> recovered => recovered.Diagnostics.Render(),
        Failed failed => failed.Diagnostics.Render(),
    };

    public static string Show(JsonValue value) => value switch
    {
        JsonString text => $"\"{text.Value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
        JsonNumber number => number.Value.ToString(CultureInfo.InvariantCulture),
        JsonBool boolean => boolean.Value
            ? "true"
            : "false",
        JsonNull => "null",
        JsonArray array => $"[{string.Join(",", array.Items.Select(Show))}]",
        JsonObject obj => $"{{{string.Join(",", obj.Members.Select(member => $"\"{member.Key}\":{Show(member.Value)}"))}}}",
    };
}

file sealed class Nesting : CharGrammar
{
    public static readonly Parser<char, int> Brackets =
        Char('[').Then(Rec(() => Brackets)).Before(Char(']')).Select(static depth => depth + 1)
        .Or(Char('x').As(0));

    public static readonly Parser<char, int> Lists =
        Char('[').Then(Rec(() => Lists)).Before(Char(']')).Select(static depth => depth + 1).Context("in list")
        .Or(Char('x').As(0));
}

file enum Bracket
{
    Open,
    Close,
    X,
    Other,
}

file readonly record struct BracketToken(Bracket Kind) : IKinded<Bracket>
{
    static string IKinded<Bracket>.Describe(Bracket kind) => kind switch
    {
        Bracket.Open => "'['",
        Bracket.Close => "']'",
        Bracket.X => "'x'",
        _ => kind.ToString(),
    };
}

file sealed class TokenNesting : TokenGrammar<BracketToken, Bracket>
{
    public static readonly Parser<BracketToken, int> Brackets =
        Kind(Bracket.Open).Then(Rec(() => Brackets)).Before(Kind(Bracket.Close)).Select(static depth => depth + 1)
        .Or(Kind(Bracket.X).As(0));

    public static readonly Parser<BracketToken, int> Lists =
        Kind(Bracket.Open).Then(Rec(() => Lists)).Before(Kind(Bracket.Close)).Select(static depth => depth + 1).Context("in list")
        .Or(Kind(Bracket.X).As(0));

    public static BracketToken[] Lex(string text) =>
    [
        .. text.Select(static c => new BracketToken(c switch
        {
            '[' => Bracket.Open,
            ']' => Bracket.Close,
            'x' => Bracket.X,
            _ => Bracket.Other,
        })),
    ];
}

public class ParsingTests
{
    private static string Nested(int levels) =>
        $"{new string('[', levels)}x{new string(']', levels)}";

    private static ParseError OnlyError<T>(ParseResult<T> result) =>
        Assert.Single(Assert.IsType<Failed>(result.Value).Diagnostics);

    private static string Outcome(ParseResult<int> result) => result switch
    {
        Parsed<int> parsed => parsed.Value.ToString(CultureInfo.InvariantCulture),
        Recovered<int> recovered => recovered.Diagnostics.Render(),
        Failed failed => failed.Diagnostics.Render(),
    };

    private static string? ShapeDisagreement(ControlShape shape, string input)
    {
        try
        {
            return Execution.Disagreement(ControlTable.Build(shape), input.AsMemory(), ParseOptions.Default) is { } disagreement
                ? $"{ControlModel.Describe(shape)} {disagreement}"
                : null;
        }
        catch (GrammarException)
        {
            return null;
        }
    }

    [Fact]
    public void ACharacterLevelJsonDocumentParses()
    {
        var text = """ { "a": [1, -2.5, true, false, null], "b": "x\"y", "c": {} } """;

        var summary = CharJson.Document.Parse(text) switch
        {
            Parsed<JsonValue> parsed => CharJson.Show(parsed.Value),
            Recovered<JsonValue> recovered => $"recovered: {recovered.Diagnostics.Render()}",
            Failed failed => failed.Diagnostics.Render(),
        };

        Assert.Equal("""{"a":[1,-2.5,true,false,null],"b":"x\"y","c":{}}""", summary);
    }

    [Fact]
    public void ACharacterParserGivenMemoryOrAnArrayRendersAgainstTheText()
    {
        var parser = Char('a');
        var expected = parser.Parse("b").Diagnostics.Render();

        Assert.Contains(" --> 1:1", expected, StringComparison.Ordinal);
        Assert.Equal(expected, parser.Parse("b".AsMemory()).Diagnostics.Render());
        Assert.Equal(expected, parser.Parse("b".AsMemory(), options: ParseOptions.Default).Diagnostics.Render());
        Assert.Equal(expected, parser.Parse("b".AsMemory(), null).Diagnostics.Render());
        Assert.Equal(expected, parser.Parse("b".ToCharArray()).Diagnostics.Render());
    }

    [Fact]
    public void ADepthErrorAtTheEndOfInputIsZeroWidth()
    {
        var error = OnlyError(Nesting.Brackets.Parse("[[[[", options: new ParseOptions { MaxDepth = 3 }));

        Assert.Equal(new SourceSpan(SourceId.Unnamed, 4, 4), error.Span);
    }

    [Fact]
    public void ADepthErrorKeepsTheContextInForce()
    {
        var error = OnlyError(Nesting.Lists.Parse(Nested(4), options: new ParseOptions { MaxDepth = 3 }));

        Assert.Equal(
            [
                new ContextLine("in list", new SourceSpan(SourceId.Unnamed, 3, 4)),
                new ContextLine("in list", new SourceSpan(SourceId.Unnamed, 2, 3)),
                new ContextLine("in list", new SourceSpan(SourceId.Unnamed, 1, 2)),
                new ContextLine("in list", new SourceSpan(SourceId.Unnamed, 0, 1)),
            ],
            error.Context
        );
    }

    [Fact]
    public void ADeepDepthErrorRendersItsInnermostAndOutermostNotes()
    {
        var result = Nesting.Lists.Parse(new string('[', 1500));

        Assert.Equal(1001, OnlyError(result).Context.Count);
        Assert.Equal(
            [
                "error[parser]: nesting exceeds ParseOptions.MaxDepth of 1000",
                " --> 1:1002",
                "  |",
                $"1 | {new string('[', 1500)}",
                $"  | {new string(' ', 1001)}^",
                "  = note: in list starting at 1:1001",
                "  = note: in list starting at 1:1000",
                "  = note: in list starting at 1:999",
                "  = note: in list starting at 1:998",
                "  = note: in list starting at 1:997",
                "  = note: … 991 more",
                "  = note: in list starting at 1:5",
                "  = note: in list starting at 1:4",
                "  = note: in list starting at 1:3",
                "  = note: in list starting at 1:2",
                "  = note: in list starting at 1:1",
            ],
            result.Diagnostics.Render().Split('\n')
        );
    }

    [Fact]
    public void AFailedParseRendersAgainstItsInput()
    {
        var text = "{\n  \"port\": 8080 \"host\": \"x\"\n}";
        var result = CharJson.Document.Parse(text, source: "config.json");

        Assert.Equal(
            """
            error[parser]: expected whitespace, ',' or '}'
             --> config.json:2:16
              |
            2 |   "port": 8080 "host": "x"
              |                ^ unexpected '"'
              = note: in object starting at 1:1
            """
                .ReplaceLineEndings("\n"),
            result.Diagnostics.Render()
        );
        Assert.Equal(result.Diagnostics.Render(new SourceText(text, "config.json")), result.Diagnostics.Render());
    }

    [Theory]
    [InlineData("")]
    [InlineData("tru")]
    [InlineData("[1, 2")]
    [InlineData("[1,,2]")]
    [InlineData("{\"a\" 1}")]
    [InlineData("{\"a\": [1, 2}")]
    [InlineData("{\n  \"port\": 8080 \"host\": \"x\"\n}")]
    public void AFailedJsonParseReportsWhatOneRunWithTheLogReports(string input) =>
        Assert.Null(Execution.Disagreement(CharJson.Document, input.AsMemory(), ParseOptions.Default));

    [Fact]
    public void AFailedParseReportsWhatOneRunWithTheLogReports()
    {
        var shallow = new ParseOptions { MaxDepth = 3 };

        Assert.Null(Execution.Disagreement(Digit.SkipAtLeastOnce().Slice().Where(static digits => digits.Length < 3, "number too long"), "1234".AsMemory(), ParseOptions.Default));
        Assert.Null(Execution.Disagreement(Longest(String("<"), String("<="), String("<<")).Before(End), "<=x".AsMemory(), ParseOptions.Default));
        Assert.Null(Execution.Disagreement(Nesting.Lists, Nested(4).AsMemory(), shallow));
        Assert.Null(Execution.Disagreement(TokenNesting.Lists, TokenNesting.Lex(Nested(4)), shallow));
        Assert.Null(Execution.Disagreement(TokenNesting.Lists.Before(TokenGrammar<BracketToken, Bracket>.End), TokenNesting.Lex("[[x]"), ParseOptions.Default));
    }

    [Fact]
    public void ANestedParseKeepsItsOwnFailures()
    {
        var inner = Char('x');
        var outer = Char('a').Then(Char('a').Optional()).Select(_ => inner.Parse("y").Value is Failed).Then(Char('b'));

        _ = Char('z').Parse("z");

        Assert.Equal([new ExpectedItem("'a'"), new ExpectedItem("'b'")], OnlyError(outer.Parse("ac")).Expected);
    }

    [Fact]
    public void ANestedTokenParseKeepsItsOwnFailures()
    {
        var inner = TokenNesting.Brackets;
        var outer = TokenGrammar<BracketToken, Bracket>.Kind(Bracket.Open)
            .Then(TokenGrammar<BracketToken, Bracket>.Kind(Bracket.Open).Optional())
            .Select(_ => inner.Parse(TokenNesting.Lex("y")).Value is Failed)
            .Then(TokenGrammar<BracketToken, Bracket>.Kind(Bracket.Close));

        _ = TokenNesting.Brackets.Parse(TokenNesting.Lex("x"));

        Assert.Equal([new ExpectedItem("'['"), new ExpectedItem("']'")], OnlyError(outer.Parse(TokenNesting.Lex("[y"))).Expected);
    }

    [Fact]
    public void AnInputWithoutASourceRendersWithoutAName()
    {
        var result = Char('a').Parse("b");

        Assert.Contains(" --> 1:1", result.Diagnostics.Render(), StringComparison.Ordinal);
    }

    [Fact]
    public void AParsedResultHoldsNoErrors()
    {
        var result = Char('a').Parse("a");

        Assert.Equal('a', Assert.IsType<Parsed<char>>(result.Value).Value);
        Assert.Same(Diagnostics.Empty, result.Diagnostics);
    }

    [Fact]
    public void AParseNeedNotReachTheEndOfInput() =>
        Assert.Equal('a', Assert.IsType<Parsed<char>>(Char('a').Parse("ab").Value).Value);

    [Fact]
    public void ATokenDepthErrorAtTheEndOfInputIsZeroWidth()
    {
        var error = OnlyError(TokenNesting.Brackets.Parse(TokenNesting.Lex("[[[["), options: new ParseOptions { MaxDepth = 3 }));

        Assert.Equal(new SourceSpan(SourceId.Unnamed, 4, 4), error.Span);
    }

    [Fact]
    public void ATokenDepthErrorKeepsTheContextInForce()
    {
        var error = OnlyError(TokenNesting.Lists.Parse(TokenNesting.Lex(Nested(4)), options: new ParseOptions { MaxDepth = 3 }));

        Assert.Equal(
            [
                new ContextLine("in list", new SourceSpan(SourceId.Unnamed, 3, 4)),
                new ContextLine("in list", new SourceSpan(SourceId.Unnamed, 2, 3)),
                new ContextLine("in list", new SourceSpan(SourceId.Unnamed, 1, 2)),
                new ContextLine("in list", new SourceSpan(SourceId.Unnamed, 0, 1)),
            ],
            error.Context
        );
    }

    [Fact]
    public void ADeepTokenDepthErrorRendersItsInnermostAndOutermostNotesOnOneLine()
    {
        var result = TokenNesting.Lists.Parse(TokenNesting.Lex(new string('[', 1500)));
        var error = OnlyError(result);
        var expected =
            "error[parser] at 1001..1002: nesting exceeds ParseOptions.MaxDepth of 1000"
            + "; in list starting at 1000; in list starting at 999; in list starting at 998; in list starting at 997; in list starting at 996"
            + "; … 991 more"
            + "; in list starting at 4; in list starting at 3; in list starting at 2; in list starting at 1; in list starting at 0";

        Assert.Equal(1001, error.Context.Count);
        Assert.Equal(expected, result.Diagnostics.Render());
        Assert.Equal(expected, error.ToString());
    }

    [Fact]
    public void ATokenParseCountsTokensAndRendersOneLinePerError()
    {
        var parser = TokenGrammar<BracketToken, Bracket>.Kind(Bracket.Open).Then(TokenGrammar<BracketToken, Bracket>.Kind(Bracket.Close));
        var result = parser.Parse(TokenNesting.Lex("[y"));
        var error = OnlyError(result);

        Assert.Equal(new SourceSpan(SourceId.Unnamed, 1, 2), error.Span);
        Assert.Equal("parser", error.Layer);
        Assert.Equal(new UnexpectedItem("Other"), error.Unexpected.Value);
        Assert.Equal([new ExpectedItem("']'")], error.Expected);
        Assert.Equal("error[parser] at 1..2: unexpected Other; expected ']'", result.Diagnostics.Render());
    }

    [Fact]
    public void ATokenParseReadsOnlyTheMemoryItIsGiven()
    {
        var tokens = TokenNesting.Lex("y[x]y");

        Assert.Equal(1, Assert.IsType<Parsed<int>>(TokenNesting.Brackets.Parse(tokens.AsMemory(1, 3)).Value).Value);
        Assert.Equal(new SourceSpan(SourceId.Unnamed, 2, 2), OnlyError(TokenNesting.Brackets.Parse(tokens.AsMemory(1, 2))).Span);
    }

    [Fact]
    public void ATooDeepParseFailsWithOneDepthError()
    {
        var result = Nesting.Brackets.Parse(Nested(4), options: new ParseOptions { MaxDepth = 3 });
        var error = OnlyError(result);

        Assert.Equal(new SourceSpan(SourceId.Unnamed, 4, 5), error.Span);
        Assert.Equal("parser", error.Layer);
        Assert.Equal(new CustomMessage("nesting exceeds ParseOptions.MaxDepth of 3"), error.Unexpected.Value);
        Assert.Empty(error.Expected);
        Assert.Empty(error.Context);
        Assert.Equal("error[parser] at 4..5: nesting exceeds ParseOptions.MaxDepth of 3", error.ToString());
    }

    [Fact]
    public void ATooDeepTokenParseFailsWithOneDepthError()
    {
        var result = TokenNesting.Brackets.Parse(TokenNesting.Lex(Nested(4)), options: new ParseOptions { MaxDepth = 3 });
        var error = OnlyError(result);

        Assert.Equal(new SourceSpan(SourceId.Unnamed, 4, 5), error.Span);
        Assert.Equal("parser", error.Layer);
        Assert.Equal(new CustomMessage("nesting exceeds ParseOptions.MaxDepth of 3"), error.Unexpected.Value);
        Assert.Empty(error.Expected);
        Assert.Empty(error.Context);
        Assert.Equal("error[parser] at 4..5: nesting exceeds ParseOptions.MaxDepth of 3", result.Diagnostics.Render());
    }

    [Fact]
    public void ErrorsPointIntoTheInputAsCharacterOffsets()
    {
        var error = OnlyError(Char('a').Then(Char('b')).Parse("ax"));

        Assert.Equal(new SourceSpan(SourceId.Unnamed, 1, 2), error.Span);
        Assert.Equal("parser", error.Layer);
        Assert.Equal([new ExpectedItem("'b'")], error.Expected);
    }

    [Fact]
    public void EveryControlShapeToDepthThreeFailsAsOneRunWithTheLogDoes()
    {
        var inputs = ControlModel.AllInputs(3).ToArray();
        var disagreement = ControlModel.AllShapes(3)
            .SelectMany(shape => inputs.Select(input => ShapeDisagreement(shape, input)))
            .FirstOrDefault(static difference => difference is not null);

        Assert.Null(disagreement);
    }

    [Fact]
    public void MaxDepthDefaultsToAThousandAndMustBePositive()
    {
        Assert.Equal(1000, ParseOptions.Default.MaxDepth);
        Assert.Equal(1000, new ParseOptions().MaxDepth);
        Assert.Equal(5, (ParseOptions.Default with { MaxDepth = 5 }).MaxDepth);
        Assert.Throws<ArgumentOutOfRangeException>("MaxDepth", () => new ParseOptions { MaxDepth = 0 });
        Assert.Throws<ArgumentOutOfRangeException>("MaxDepth", () => new ParseOptions { MaxDepth = -1 });
    }

    [Fact]
    public void MemoryParsesAsTheSameStringDoes()
    {
        var text = """{"a": [1, 2 3]}""";
        var buffer = $"xx{text}yy".ToCharArray();
        var memory = buffer.AsMemory(2, text.Length);

        Assert.Equal(CharJson.Document.Parse(text), CharJson.Document.Parse(memory));
        Assert.Equal(CharJson.Document.Parse(text).Diagnostics.Render(), CharJson.Document.Parse(memory).Diagnostics.Render());
        Assert.Equal(
            CharJson.Show(Assert.IsType<Parsed<JsonValue>>(CharJson.Document.Parse("[1, 2]").Value).Value),
            CharJson.Show(Assert.IsType<Parsed<JsonValue>>(CharJson.Document.Parse("_[1, 2]_".AsMemory(1, 6)).Value).Value)
        );
    }

    [Fact]
    public void NullArgumentsThrow()
    {
        Assert.Throws<GrammarException>(() => ((Parser<char, char>)null!).Parse("a"));
        Assert.Throws<GrammarException>(() => ((Parser<char, char>)null!).Parse("a".AsMemory()));
        Assert.Throws<ArgumentNullException>("input", () => Char('a').Parse((string)null!));
        Assert.Throws<GrammarException>(() => ((Parser<BracketToken, int>)null!).Parse(TokenNesting.Lex("x")));
    }

    [Fact]
    public void ParsesOnManyThreadsAgreeWithParsesOnOne()
    {
        string[] texts = [.. Enumerable.Range(0, 200).Select(static i =>
            i % 2 == 0
                ? $"[{i}, {{\"k\": [{i}, true]}}]"
                : $"[{i}, {{\"k\" {i}}}]")];
        var expected = texts.Select(static text => CharJson.Outcome(CharJson.Document.Parse(text))).ToArray();
        var mismatches = 0;

        Parallel.For(0, texts.Length * 20, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            if (CharJson.Outcome(CharJson.Document.Parse(texts[i % texts.Length])) != expected[i % texts.Length])
            {
                Interlocked.Increment(ref mismatches);
            }
        });

        Assert.Contains(expected, outcome => outcome.StartsWith("error[parser]", StringComparison.Ordinal));
        Assert.Equal(0, mismatches);
    }

    [Fact]
    public void RunningShortOfStackFailsATokenParseWithADepthError()
    {
        var deep = TokenNesting.Lex(new string('[', 100_000));
        ParseResult<int> result = default;

        var thread = new Thread(
            () => result = TokenNesting.Brackets.Parse(deep, options: new ParseOptions { MaxDepth = int.MaxValue }),
            256 * 1024
        );

        thread.Start();
        thread.Join();

        var error = OnlyError(result);

        Assert.Equal(new CustomMessage("nesting is too deep for the thread's stack"), error.Unexpected.Value);
        Assert.Empty(error.Expected);
        Assert.InRange(error.Span.Start, 21, 99_999);
    }

    [Fact]
    public void RunningShortOfStackFailsWithADepthError()
    {
        var deep = new string('[', 100_000);
        ParseResult<int> result = default;

        var thread = new Thread(
            () => result = Nesting.Brackets.Parse(deep, options: new ParseOptions { MaxDepth = int.MaxValue }),
            256 * 1024
        );

        thread.Start();
        thread.Join();

        var error = OnlyError(result);
        Assert.Equal(new CustomMessage("nesting is too deep for the thread's stack"), error.Unexpected.Value);
        Assert.Empty(error.Expected);
        Assert.InRange(error.Span.Start, 21, 99_999);
    }

    [Fact]
    public void SlicesAreViewsOfTheInput()
    {
        var text = "abc";
        var memory = "xabcx".ToCharArray().AsMemory(1, 3);
        var letters = Letter.SkipMany().Slice();

        Assert.True(MemoryMarshal.TryGetString(Assert.IsType<Parsed<ReadOnlyMemory<char>>>(letters.Parse(text).Value).Value, out var held, out _, out _));
        Assert.Same(text, held);
        Assert.True(Assert.IsType<Parsed<ReadOnlyMemory<char>>>(letters.Parse(memory).Value).Value.Equals(memory));
    }

    [Fact]
    public void TheDefaultLimitAllowsAThousandLevels()
    {
        Assert.Equal(1000, Assert.IsType<Parsed<int>>(Nesting.Brackets.Parse(Nested(1000)).Value).Value);
        Assert.Equal("nesting exceeds ParseOptions.MaxDepth of 1000", Assert.IsType<CustomMessage>(OnlyError(Nesting.Brackets.Parse(Nested(1001))).Unexpected.Value).Text);
    }

    [Fact]
    public void TheDefaultLimitAllowsAThousandLevelsOfTokens()
    {
        Assert.Equal(1000, Assert.IsType<Parsed<int>>(TokenNesting.Brackets.Parse(TokenNesting.Lex(Nested(1000))).Value).Value);
        Assert.Equal("nesting exceeds ParseOptions.MaxDepth of 1000", Assert.IsType<CustomMessage>(OnlyError(TokenNesting.Brackets.Parse(TokenNesting.Lex(Nested(1001)))).Unexpected.Value).Text);
    }

    [Fact]
    public void TokenParsesOnManyThreadsAgreeWithParsesOnOne()
    {
        BracketToken[][] inputs = [..
            Enumerable.Range(0, 200).Select(static i =>
                TokenNesting.Lex(
                    i % 2 == 0
                    ? Nested(i % 30)
                    : $"{new string('[', i % 30)}y"
                )
            )
        ];

        var expected = inputs.Select(static tokens => Outcome(TokenNesting.Brackets.Parse(tokens))).ToArray();
        var mismatches = 0;

        Parallel.For(0, inputs.Length * 20, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i =>
        {
            if (Outcome(TokenNesting.Brackets.Parse(inputs[i % inputs.Length])) != expected[i % inputs.Length])
            {
                Interlocked.Increment(ref mismatches);
            }
        });

        Assert.Contains(expected, outcome => outcome.StartsWith("error[parser]", StringComparison.Ordinal));
        Assert.Equal(0, mismatches);
    }
}
