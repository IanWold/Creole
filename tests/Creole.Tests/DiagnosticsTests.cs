using CsCheck;

namespace Creole.Tests;

public class DiagnosticsTests
{
    private static ParseError ErrorAt(SourceId source, long start, long end, string layer = "parser") =>
        new(new SourceSpan(source, start, end), layer, new EndOfInput(), [], []);

    [Fact]
    public void EmptyHasNoErrors()
    {
        Assert.Empty(Diagnostics.Empty);
        Assert.Equal("", Diagnostics.Empty.Render());
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
    public void ErrorsAreOrderedWhateverOrderTheyArriveIn()
    {
        var registry = new SourceRegistry();
        SourceId[] sources = [SourceId.Unnamed, registry.Add("a.json"), registry.Add("b.json")];

        var gen =
            from source in Gen.Int[0, sources.Length - 1]
            from start in Gen.Long[0, 8]
            from length in Gen.Long[0, 2]
            select (source, start, end: start + length);

        gen.List[0, 20].Sample(spans =>
        {
            var arrived = spans.Select((span, i) => ErrorAt(sources[span.source], span.start, span.end, i.ToString("D2", System.Globalization.CultureInfo.InvariantCulture))).ToList();
            var expected = arrived
                .OrderBy(error => error.Span.Source.Value)
                .ThenBy(error => error.Span.Start)
                .ThenBy(error => error.Span.End)
                .ThenBy(error => error.Layer, StringComparer.Ordinal);

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
    public void RenderWritesOneLinePerErrorInSpanOrder()
    {
        var diagnostics = new Diagnostics([
            new ParseError(new SourceSpan(SourceId.Unnamed, 15, 21), "parser", new UnexpectedItem("string \"host\""), [new ExpectedItem("','"), new ExpectedItem("'}'")], ["in object starting at 2:1"]),
            new ParseError(new SourceSpan(SourceId.Unnamed, 30, 30), "parser", new EndOfInput(), [new Label("value"), new ExpectedItem("'['"), new EndOfInput()], ["in array", "in object starting at 1:1"]),
            new ParseError(new SourceSpan(SourceId.Unnamed, 4, 7), "lexer", new CustomMessage("number too large"), [], []),
        ]);

        Assert.Equal(
            """
                error[lexer] at 4..7: number too large
                error[parser] at 15..21: unexpected string "host"; expected ',' or '}'; in object starting at 2:1
                error[parser] at 30..30: unexpected end of input; expected value, '[' or end of input; in array; in object starting at 1:1
                """
                .ReplaceLineEndings("\n"),
            diagnostics.Render()
        );
    }

    [Fact]
    public void RenderUsesCharacterOffsets()
    {
        var error = new ParseError(new SourceSpan(SourceId.Unnamed, 10, 14, 8, 9), "decoder", new UnexpectedItem("byte 0xFF"), [], []);

        Assert.Equal("error[decoder] at 8..9: unexpected byte 0xFF", error.ToString());
        Assert.Equal(error.ToString(), new Diagnostics([error]).Render());
    }
}
