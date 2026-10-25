using CsCheck;

namespace Creole.Tests;

public class DiagnosticsTests
{
    private static ParseError ErrorAt(SourceId source, long start, long end, string layer = "parser") =>
        new(new SourceSpan(source, start, end), layer, new EndOfInput(), [], []);

    [Fact]
    public void CollectionsWithEqualErrorsAreEqual()
    {
        var a = new Diagnostics([ErrorAt(SourceId.Unnamed, 1, 2), ErrorAt(SourceId.Unnamed, 4, 5)]);
        var b = new Diagnostics([ErrorAt(SourceId.Unnamed, 4, 5), ErrorAt(SourceId.Unnamed, 1, 2)]);
        var c = new Diagnostics([ErrorAt(SourceId.Unnamed, 1, 2)]);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, c);
        Assert.Equal(new Failed(a), new Failed(b));
    }

    [Fact]
    public void CountAndIndexerFollowSpanOrder()
    {
        var late = ErrorAt(SourceId.Unnamed, 9, 10);
        var early = ErrorAt(SourceId.Unnamed, 2, 3);
        var diagnostics = new Diagnostics([late, early]);

        Assert.Equal(2, diagnostics.Count);
        Assert.Same(early, diagnostics[0]);
        Assert.Same(late, diagnostics[1]);
        Assert.Equal([early, late], diagnostics);
    }

    [Fact]
    public void EmptyHasNoErrors()
    {
        Assert.Empty(Diagnostics.Empty);
        Assert.Equal("", Diagnostics.Empty.Render());
    }

    [Fact]
    public void ErrorsAreOrderedWhateverOrderTheyArriveIn()
    {
        var registry = new SourceRegistry();
        SourceId[] sources = [SourceId.Unnamed, registry.Add("a.json"), registry.Add("b.json")];
        string[] layers = ["lexer", "layout", "parser"];

        var gen =
            from source in Gen.Int[0, sources.Length - 1]
            from start in Gen.Long[0, 8]
            from length in Gen.Long[0, 2]
            from layer in Gen.Int[0, layers.Length - 1]
            select (source, start, end: start + length, layer);

        gen.List[0, 20].Sample(spans =>
        {
            var arrived = spans
                .Select((span, i) => new ParseError(new SourceSpan(sources[span.source], span.start, span.end), layers[span.layer], new EndOfInput(), [], [new ContextLine($"{i}", default)]))
                .ToList();

            var expected = arrived
                .Select((error, i) => (error, i))
                .OrderBy(entry => entry.error.Span.Source.Value)
                .ThenBy(entry => entry.error.Span.Start)
                .ThenBy(entry => entry.error.Span.End)
                .ThenBy(entry => entry.i)
                .Select(entry => entry.error);

            return new Diagnostics(arrived).SequenceEqual(expected);
        });
    }

    [Fact]
    public void NullsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => new Diagnostics(null!));
        Assert.Throws<ArgumentException>(() => new Diagnostics([ErrorAt(SourceId.Unnamed, 0, 1), null!]));
    }

    [Fact]
    public void RenderUsesCharacterOffsets()
    {
        var error = new ParseError(new SourceSpan(SourceId.Unnamed, 10, 14, 8, 9), "decoder", new UnexpectedItem("byte 0xFF"), [], []);

        Assert.Equal("error[decoder] at 8..9: unexpected byte 0xFF", error.ToString());
        Assert.Equal(error.ToString(), new Diagnostics([error]).Render());
    }

    [Fact]
    public void RenderWritesOneLinePerErrorInSpanOrder()
    {
        var diagnostics = new Diagnostics([
            new ParseError(new SourceSpan(SourceId.Unnamed, 15, 21), "parser", new UnexpectedItem("string \"host\""), [new ExpectedItem("','"), new ExpectedItem("'}'")], [new ContextLine("in object", new SourceSpan(SourceId.Unnamed, 2, 3))]),
            new ParseError(new SourceSpan(SourceId.Unnamed, 30, 30), "parser", new EndOfInput(), [new Label("value"), new ExpectedItem("'['"), new EndOfInput()], [new ContextLine("in array", new SourceSpan(SourceId.Unnamed, 25, 26)), new ContextLine("in object", new SourceSpan(SourceId.Unnamed, 0, 1))]),
            new ParseError(new SourceSpan(SourceId.Unnamed, 4, 7), "lexer", new CustomMessage("number too large"), [], []),
        ]);

        Assert.Equal(
            """
                error[lexer] at 4..7: number too large
                error[parser] at 15..21: unexpected string "host"; expected ',' or '}'; in object starting at 2
                error[parser] at 30..30: unexpected end of input; expected value, '[' or end of input; in array starting at 25; in object starting at 0
                """
                .ReplaceLineEndings("\n"),
            diagnostics.Render()
        );
    }
}
