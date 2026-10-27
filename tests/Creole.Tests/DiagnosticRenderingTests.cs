using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CsCheck;

namespace Creole.Tests;

public class DiagnosticRenderingTests
{
    private const string Acute = "\u0301";
    private const string Esc = "\u001B";
    private const string EscPicture = "\u241B";
    private const string ThumbsUp = "\U0001F44D\U0001F3FD";

    private static ParseError Error(long start, long end, Unexpected unexpected, Expectation[]? expected = null, ContextLine[]? context = null, string layer = "parser", SourceId source = default) =>
        new(new SourceSpan(source, start, end), layer, unexpected, expected ?? [], context ?? []);

    private static string Lines(string text) =>
        text.ReplaceLineEndings("\n");

    private static string Render(string text, params ParseError[] errors) =>
        new Diagnostics(errors).Render(new SourceText(text));

    [Fact]
    public void RendersTheWhitepaperExample()
    {
        var registry = new SourceRegistry();
        var id = registry.Add("config.json");
        var text = Lines("""
            [
            {
              "port": 8080 "host": "x"
            }
            ]
            """
        );
        var start = text.IndexOf("\"host\"", StringComparison.Ordinal);
        var error = Error(start, start + 6, new UnexpectedItem("string \"host\""), [new ExpectedItem("','"), new ExpectedItem("'}'")], [new ContextLine("in object", new SourceSpan(id, 2, 3))], source: id);

        Assert.Equal(
            Lines("""
                error[parser]: expected ',' or '}'
                 --> config.json:3:16
                  |
                3 |   "port": 8080 "host": "x"
                  |                ^^^^^^ unexpected string "host"
                  = note: in object starting at 2:1
                """
            ),
            new Diagnostics([error]).Render(new SourceText(id, registry.GetName(id), text))
        );
    }

    [Fact]
    public void TabsAreExpandedToStopsOfFour()
    {
        var text = "if x {\n\tlet\ty = 1\n}";
        var y = text.IndexOf('y');

        Assert.Equal(
            Lines("""
                error[parser]: unexpected identifier y
                 --> 2:6
                  |
                2 |     let y = 1
                  |         ^
                """
            ),
            Render(text, Error(y, y + 1, new UnexpectedItem("identifier y")))
        );
    }

    [Fact]
    public void WideCharactersTakeTwoColumns()
    {
        var text = "name = \"中文\" + 1";
        var plus = text.IndexOf('+');

        Assert.Equal(
            Lines("""
                error[parser]: expected ';'
                 --> 1:13
                  |
                1 | name = "中文" + 1
                  |               ^ unexpected '+'
                """
            ),
            Render(text, Error(plus, plus + 1, new UnexpectedItem("'+'"), [new ExpectedItem("';'")]))
        );
    }

    [Fact]
    public void EmojiTakeTwoColumnsWhateverTheirLength()
    {
        var text = $"x = {ThumbsUp} + 1";
        var plus = text.IndexOf('+');

        Assert.Equal(
            Lines($$"""
                error[parser]: expected expression
                 --> 1:5
                  |
                1 | x = {{ThumbsUp}} + 1
                  |     ^^ unexpected emoji

                error[parser]: expected end of input
                 --> 1:8
                  |
                1 | x = {{ThumbsUp}} + 1
                  |        ^ unexpected '+'
                """
            ),
            Render(
                text,
                Error(4, 4 + ThumbsUp.Length, new UnexpectedItem("emoji"), [new Label("expression")]),
                Error(plus, plus + 1, new UnexpectedItem("'+'"), [new EndOfInput()])
            )
        );
    }

    [Fact]
    public void CombiningMarksTakeNoColumnButCountInTheLocation()
    {
        var text = $"cafe{Acute} := 1";
        var assign = text.IndexOf(':');

        Assert.Equal(
            Lines($$"""
                error[lexer]: combining mark outside a string
                 --> 1:5
                  |
                1 | cafe{{Acute}} := 1
                  |    ^

                error[parser]: expected '='
                 --> 1:7
                  |
                1 | cafe{{Acute}} := 1
                  |      ^^ unexpected ':='
                """
            ),
            Render(
                text,
                Error(assign, assign + 2, new UnexpectedItem("':='"), [new ExpectedItem("'='")]),
                Error(4, 5, new CustomMessage("combining mark outside a string"), layer: "lexer")
            )
        );
    }

    [Fact]
    public void CrlfLineEndingsAreNotShown()
    {
        var text = "{\r\n  \"a\": 1,\r\n  \"b\" 2\r\n}\r\n";
        var lineBreak = text.IndexOf(",\r\n", StringComparison.Ordinal) + 1;
        var two = text.IndexOf('2');
        var rendered = Render(
            text,
            Error(lineBreak, lineBreak + 2, new UnexpectedItem("line break"), [new Label("member")]),
            Error(two, two + 1, new UnexpectedItem("number 2"), [new ExpectedItem("':'")]),
            Error(text.Length, text.Length, new EndOfInput(), [new ExpectedItem("']'")])
        );

        Assert.Equal(
            Lines("""
                error[parser]: expected member
                 --> 2:10
                  |
                2 |   "a": 1,
                  |          ^ unexpected line break

                error[parser]: expected ':'
                 --> 3:7
                  |
                3 |   "b" 2
                  |       ^ unexpected number 2

                error[parser]: expected ']'
                 --> 5:1
                  |
                5 |
                  | ^ unexpected end of input
                """
            ),
            rendered
        );

        Assert.DoesNotContain('\r', rendered);
    }

    [Fact]
    public void CrLineEndingsAreNotShown() =>
        Assert.Equal(
            Lines("""
                error[lexer]: unexpected character '#'
                 --> 3:1
                  |
                3 | #
                  | ^
                """
            ),
            Render("a\rb\r#", Error(4, 5, new UnexpectedItem("character '#'"), layer: "lexer"))
        );

    [Fact]
    public void AnEmptySpanAtTheEndOfALineGetsOneCaretAfterIt() =>
        Assert.Equal(
            Lines("""
                error[parser]: expected ',' or ']'
                 --> 1:6
                  |
                1 | [1, 2
                  |      ^ unexpected end of input
                """
            ),
            Render("[1, 2", Error(5, 5, new EndOfInput(), [new ExpectedItem("','"), new ExpectedItem("']'")]))
        );

    [Fact]
    public void ASpanOverSeveralLinesIsShownOnItsFirstLine()
    {
        var text = "f(\n  a,\n  b)";

        Assert.Equal(
            Lines("""
                error[parser]: arguments must be named
                 --> 2:3
                  |
                2 |   a,
                  |   ^^
                """
            ),
            Render(text, Error(text.IndexOf('a'), text.IndexOf('b') + 1, new CustomMessage("arguments must be named")))
        );
    }

    [Fact]
    public void TheGutterIsAsWideAsTheLineNumber()
    {
        var text = string.Join('\n', Enumerable.Range(1, 10).Select(i => $"line {i}")) + " #";

        Assert.Equal(
            Lines("""
                error[lexer]: unexpected character '#'
                  --> 10:9
                   |
                10 | line 10 #
                   |         ^
                   = note: in block starting at 10:1
                   = note: in file starting at 1:1
                """
            ),
            Render(text, Error(text.Length - 1, text.Length, new UnexpectedItem("character '#'"), context: [new ContextLine("in block", new SourceSpan(default, text.LastIndexOf('\n') + 1, text.LastIndexOf('\n') + 2)), new ContextLine("in file", new SourceSpan(default, 0, 1))], layer: "lexer"))
        );
    }

    [Fact]
    public void ErrorsAreGroupedBySourceInRegistryOrder()
    {
        var registry = new SourceRegistry();
        var a = registry.Add("a.json");
        var b = registry.Add("b.json");

        var diagnostics = new Diagnostics([
            Error(2, 3, new UnexpectedItem("number 2"), [new EndOfInput()], source: b),
            Error(0, 1, new UnexpectedItem("'?'"), layer: "input"),
            Error(2, 3, new UnexpectedItem("identifier y"), [new EndOfInput()], source: a),
        ]);

        Assert.Equal(
            Lines("""
                error[input]: unexpected '?'
                 --> 1:1
                  |
                1 | ?
                  | ^

                error[parser]: expected end of input
                 --> a.json:1:3
                  |
                1 | x y
                  |   ^ unexpected identifier y

                error[parser]: expected end of input
                 --> b.json:1:3
                  |
                1 | 1 2
                  |   ^ unexpected number 2
                """
            ),
            diagnostics.Render(new SourceText(b, "b.json", "1 2"), new SourceText(a, "a.json", "x y"), new SourceText("?"))
        );
    }

    [Fact]
    public void ACustomMessageLabelsTheCaretsWhenSomethingWasExpected() =>
        Assert.Equal(
            Lines("""
                error[parser]: expected name
                 --> 1:5
                  |
                1 | let 9x = 1
                  |     ^^ names cannot start with a digit
                """
            ),
            Render("let 9x = 1", Error(4, 6, new CustomMessage("names cannot start with a digit"), [new Label("name")]))
        );

    [Fact]
    public void ControlCharactersInTheSourceAreShownAsPictures() =>
        Assert.Equal(
            Lines($$"""
                error[lexer]: unexpected control character U+001B
                 --> 1:5
                  |
                1 | say {{EscPicture}}[31mhi
                  |     ^
                """
            ),
            Render($"say {Esc}[31mhi", Error(4, 5, new UnexpectedItem("control character U+001B"), layer: "lexer"))
        );

    [Fact]
    public void NoErrorsRenderAsAnEmptyString()
    {
        Assert.Equal("", Diagnostics.Empty.Render(new SourceText("abc")));
        Assert.Equal("", Diagnostics.Empty.Render([]));
    }

    [Fact]
    public void AnErrorInASourceWithNoTextThrows()
    {
        var registry = new SourceRegistry();
        var diagnostics = new Diagnostics([Error(0, 1, new EndOfInput(), source: registry.Add("a.json"))]);

        Assert.Throws<ArgumentException>("sources", () => diagnostics.Render(new SourceText("abc")));
    }

    [Fact]
    public void ASpanPastTheEndOfItsTextThrows()
    {
        var diagnostics = new Diagnostics([Error(1, 5, new EndOfInput())]);

        Assert.Throws<ArgumentException>("sources", () => diagnostics.Render(new SourceText("abc")));
    }

    [Fact]
    public void AContextLineInASourceWithNoTextThrows()
    {
        var registry = new SourceRegistry();
        var diagnostics = new Diagnostics([Error(0, 1, new EndOfInput(), context: [new ContextLine("in a", new SourceSpan(registry.Add("a.json"), 0, 1))])]);

        Assert.Throws<ArgumentException>("sources", () => diagnostics.Render(new SourceText("abc")));
    }

    [Fact]
    public void AContextLineStartingPastTheEndOfItsTextThrows()
    {
        var diagnostics = new Diagnostics([Error(0, 1, new EndOfInput(), context: [new ContextLine("in a", new SourceSpan(default, 4, 5))])]);

        Assert.Throws<ArgumentException>("sources", () => diagnostics.Render(new SourceText("abc")));
    }

    [Fact]
    public void AContextLineShowsWhereItStartedInItsOwnSource()
    {
        var registry = new SourceRegistry();
        var host = registry.Add("page.html");
        var script = registry.Add("script.js");
        var error = Error(1, 2, new EndOfInput(), [new Label("value")], [new ContextLine("in script tag", new SourceSpan(host, 7, 8))], source: script);

        Assert.Equal(
            Lines("""
                error[parser]: expected value
                 --> script.js:1:2
                  |
                1 | x;
                  |  ^ unexpected end of input
                  = note: in script tag starting at 2:1
                """
            ),
            new Diagnostics([error]).Render(new SourceText(host, "page.html", "<html>\n<script>"), new SourceText(script, "script.js", "x;"))
        );
    }

    [Theory]
    [InlineData(4, new[] { 6, 5 }, 3, new[] { 1, 0 })]
    [InlineData(5, new[] { 6, 5, 4 }, 2, new[] { 1, 0 })]
    [InlineData(1, new[] { 6 }, 6, new int[0])]
    [InlineData(0, new int[0], 7, new int[0])]
    public void NotesPastMaxNotesKeepTheInnermostAndOutermost(int maxNotes, int[] innermost, int omitted, int[] outermost)
    {
        var text = "[[[[[[[x";
        var context = Enumerable.Range(0, 7).Reverse().Select(start => new ContextLine("in list", new SourceSpan(default, start, start + 1))).ToArray();
        var error = Error(7, 8, new UnexpectedItem("'x'"), [new ExpectedItem("'['")], context);

        static string Note(int start) =>
            $"  = note: in list starting at 1:{start + 1}";

        Assert.Equal(
            [.. innermost.Select(Note), $"  = note: … {omitted} more", .. outermost.Select(Note)],
            new Diagnostics([error]).Render(new RenderOptions { MaxNotes = maxNotes }, new SourceText(text)).Split('\n')[5..]
        );
    }

    [Theory]
    [InlineData(7)]
    [InlineData(int.MaxValue)]
    public void NotesUpToMaxNotesAreAllShown(int maxNotes)
    {
        var text = "[[[[[[[x";
        var context = Enumerable.Range(0, 7).Reverse().Select(start => new ContextLine("in list", new SourceSpan(default, start, start + 1))).ToArray();
        var error = Error(7, 8, new UnexpectedItem("'x'"), [new ExpectedItem("'['")], context);

        Assert.Equal(
            [.. Enumerable.Range(0, 7).Reverse().Select(start => $"  = note: in list starting at 1:{start + 1}")],
            new Diagnostics([error]).Render(new RenderOptions { MaxNotes = maxNotes }, new SourceText(text)).Split('\n')[5..]
        );
    }

    [Fact]
    public void NullOptionsThrow()
    {
        var diagnostics = new Diagnostics([Error(0, 1, new EndOfInput())]);

        Assert.Throws<ArgumentNullException>("options", () => diagnostics.Render((RenderOptions)null!));
        Assert.Throws<ArgumentNullException>("options", () => diagnostics.Render((RenderOptions)null!, new SourceText("a")));
    }

    [Fact]
    public void BadSourceListsThrow()
    {
        var diagnostics = new Diagnostics([Error(0, 1, new EndOfInput())]);

        Assert.Throws<ArgumentException>("sources", () => diagnostics.Render(new SourceText("a"), new SourceText("b")));
        Assert.Throws<ArgumentException>("sources", () => diagnostics.Render([null!]));
    }

    [Fact]
    public void AHeldSourceRendersAsIfItWereGiven()
    {
        var source = new SourceText("[\n1 2]", "a.json");
        var diagnostics = new Diagnostics([Error(4, 5, new UnexpectedItem("'2'"), [new ExpectedItem("','")])], source);

        Assert.Equal(
            Lines("""
                error[parser]: expected ','
                 --> a.json:2:3
                  |
                2 | 1 2]
                  |   ^ unexpected '2'
                """
            ),
            diagnostics.Render()
        );

        Assert.Equal(diagnostics.Render(source), diagnostics.Render());
    }

    [Fact]
    public void ErrorsRenderOneLineEachWhenTheHeldSourcesDoNotCoverThem()
    {
        var registry = new SourceRegistry();
        var other = registry.Add("b.json");
        var held = new SourceText("abc", "a.json");
        Diagnostics[] uncovered =
        [
            new([Error(0, 1, new EndOfInput()), Error(0, 1, new EndOfInput(), source: other)], held),
            new([Error(0, 1, new EndOfInput(), context: [new ContextLine("in b", new SourceSpan(other, 0, 1))])], held),
            new([Error(2, 5, new EndOfInput())], held),
            new([Error(0, 1, new EndOfInput())]),
        ];

        Assert.All(uncovered, diagnostics => Assert.Equal(string.Join('\n', diagnostics), diagnostics.Render()));
    }

    [Fact]
    public void ACopyOfACollectionHoldsNoSources()
    {
        var held = new Diagnostics([Error(1, 2, new EndOfInput())], new SourceText("abc"));
        var copy = new Diagnostics(held);

        Assert.Equal("error[parser] at 1..2: unexpected end of input", copy.Render());
    }

    [Fact]
    public void GivenSourcesReplaceHeldOnes()
    {
        var diagnostics = new Diagnostics([Error(1, 2, new EndOfInput())], new SourceText("abc", "held.json"));

        Assert.Contains(" --> given.json:1:2", diagnostics.Render(new SourceText("xyz", "given.json")), StringComparison.Ordinal);
        Assert.Throws<ArgumentException>("sources", () => diagnostics.Render(new SourceText("x")));
    }

    [Fact]
    public void TextOverMemoryRendersAsTheSameString()
    {
        var text = "[\n  1 2\n]";
        var buffer = $"xx{text}yy".ToCharArray();
        var error = Error(6, 7, new UnexpectedItem("'2'"), [new ExpectedItem("','")], [new ContextLine("in array", new SourceSpan(SourceId.Unnamed, 0, 1))]);
        var diagnostics = new Diagnostics([error]);

        Assert.Equal(
            diagnostics.Render(new SourceText(text, "a.json")),
            diagnostics.Render(new SourceText(SourceId.Unnamed, "a.json", buffer.AsMemory(2, text.Length)))
        );
    }

    [Fact]
    public void SourceTextHoldsItsTextWithoutCopying()
    {
        var text = "abc";
        var memory = "xabcx".ToCharArray().AsMemory(1, 3);

        Assert.True(MemoryMarshal.TryGetString(new SourceText(text).Text, out var held, out var start, out var length));
        Assert.Same(text, held);
        Assert.Equal((0, 3), (start, length));
        Assert.True(new SourceText(SourceId.Unnamed, null, memory).Text.Equals(memory));
    }

    [Fact]
    public void SourceTextBuildsItsLineMapOnce()
    {
        var source = new SourceText("a\nb", "x.txt");

        Assert.Equal(SourceId.Unnamed, source.Id);
        Assert.Equal("x.txt", source.Name);
        Assert.Same(source.LineMap, source.LineMap);
        Assert.Equal(2, source.LineMap.LineCount);
        Assert.Throws<ArgumentNullException>(() => new SourceText(null!));
        Assert.Throws<ArgumentNullException>(() => new SourceText(SourceId.Unnamed, null, (string)null!));
    }

    [Fact]
    public void AnySpanRendersInTheSameShape()
    {
        var gen =
            from parts in Gen.OneOf("a", " ", "\t", "\r", "\n", "\r\n", "\u4E2D", "e\u0301", "\U0001F44D", "\u200D", "\u001B", "\uD800").Array[0, 30]
            let text = string.Concat(parts)
            from start in Gen.Int[0, text.Length]
            from end in Gen.Int[start, text.Length]
            select (text, start, end);

        gen.Sample(sample =>
        {
            var (text, start, end) = sample;
            var rendered = Render(text, Error(start, end, new EndOfInput(), [new Label("value")], [new ContextLine("in test", new SourceSpan(default, start, start))]));
            var lines = rendered.Split('\n');
            var position = LineMap.Create(text).GetPosition(start);
            var number = position.Line.ToString(CultureInfo.InvariantCulture);
            var gutter = new string(' ', number.Length);

            return !rendered.Contains('\r')
                && !rendered.Contains('\u240A')
                && !rendered.Contains('\u240D')
                && lines.Length == 6
                && lines[0] == "error[parser]: expected value"
                && lines[1] == $"{gutter}--> {position}"
                && lines[2] == $"{gutter} |"
                && (lines[3] == $"{number} |" || lines[3].StartsWith($"{number} | ", StringComparison.Ordinal))
                && Regex.IsMatch(lines[4], $@"^{gutter} \| *\^+ unexpected end of input$")
                && lines[5] == $"{gutter} = note: in test starting at {position}";
        });
    }
}
