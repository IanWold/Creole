using static Creole.Grammar<string>;

namespace Creole.Tests;

public class SyntheticItemTests
{
    private static readonly Layer<string, string> _dedents = Layer.Transduce(
        initialState: 0,
        step: OneOf(Token("\n").Select<Emit<string>>(static newline => [newline, Emit.Synthetic("<dedent>")]), Any.Select(static token => Emit.One(token))),
        atEnd: static _ => Emit.Synthetic("<end>")
    );

    private static readonly Layer<char, string> _lexer = Layer.Tokens(
        CharGrammar.Longest(CharGrammar.Letter.AtLeastOnce().Text(), CharGrammar.Digit.AtLeastOnce().Text(), CharGrammar.String("="), CharGrammar.String("\n")),
        skip: CharGrammar.Char(' ').SkipMany()
    );

    private static readonly Layer<string, string> _semicolons = Layer.Transduce(
        initialState: 0,
        step: OneOf(Token("\n").Select<Emit<string>>(static _ => Emit.Synthetic(";")), Any.Select(static token => Emit.One(token)))
    );

    private static List<(string Item, SourceSpan Span)> Spans(Layer<char, string> layer, string text) =>
        [.. LayerRun.Items(layer, text).Select(static entry => (entry.Item, entry.Slot.Span))];

    private static string Render<TOut>(Layer<char, string> layer, Parser<string, TOut> parser, string text)
    {
        var tokens = LayerRun.Open(layer, text, out _);
        var failures = new FailureLog<string>();
        var cursor = new Cursor<string>(tokens) { Failures = failures };
        var value = default(TOut)!;

        Assert.False(PlanCache.Get(parser).TryParse(ref cursor, ref value));

        return new Diagnostics([ParseErrors.Failure(in cursor, failures, 0, "parser", tokens.SpanOf)]).Render(new SourceText(text));
    }

    private static SourceSpan Span(long start, long end) => new(SourceId.Unnamed, start, end);

    [Fact]
    public void ADerivedOutputOverOnlySyntheticInputIsZeroWidth() =>
        Assert.Equal(
            [
                ("a", Span(0, 1)),
                ("\n", Span(1, 2)),
                ("<block-end>", Span(4, 4)),
                ("b", Span(4, 5)),
                ("<end>", Span(5, 5)),
            ],
            Spans(
                _lexer.Then(_dedents).Then(Layer.Transduce(0, OneOf(Token("<dedent>").Select<Emit<string>>(static _ => "<block-end>"), Any.Select(static token => Emit.One(token))))),
                "a\n  b"
            )
        );

    [Fact]
    public void AnErrorAtASyntheticOutputAfterANewlineShowsOnTheNextLine()
    {
        var statement = Satisfy(static token => char.IsLetter(token[0])).Before(Token("=")).Before(Any).Before(Token("\n")).Named("statement");

        Assert.Equal(
            """
            error[parser]: expected statement
             --> 2:3
              |
            2 |   b = 2
              |   ^ unexpected <dedent>
            """
                .ReplaceLineEndings("\n"),
            Render(_lexer.Then(_dedents), statement.Then(statement), "a = 1\n  b = 2\n")
        );
    }

    [Fact]
    public void AnItemIsZeroWidthExactlyWhenItIsSyntheticThroughEveryLayer()
    {
        var items = Spans(
            _lexer.Then(_dedents).Then(Layer.Select<string, string>(static token => token.ToUpperInvariant())).Then(Layer.Where<string>(static token => token != "=")),
            "a = 1\n  b\n"
        );

        Assert.Equal(
            [
                ("A", Span(0, 1)),
                ("1", Span(4, 5)),
                ("\n", Span(5, 6)),
                ("<DEDENT>", Span(8, 8)),
                ("B", Span(8, 9)),
                ("\n", Span(9, 10)),
                ("<DEDENT>", Span(10, 10)),
                ("<END>", Span(10, 10)),
            ],
            items
        );
        Assert.All(items, static entry => Assert.Equal(entry.Item.StartsWith('<'), entry.Span.Length == 0));
    }

    [Fact]
    public void ATokenOverSyntheticItemsAtOnePositionIsZeroWidth() =>
        Assert.Equal(
            [
                ("a", Span(0, 1)),
                ("\n", Span(1, 2)),
                ("<2 dedents>", Span(4, 4)),
                ("b", Span(4, 5)),
            ],
            Spans(
                _lexer
                    .Then(Layer.Transduce(0, OneOf(Token("\n").Select<Emit<string>>(static newline => [newline, Emit.Synthetic("<dedent>"), Emit.Synthetic("<dedent>")]), Any.Select(static token => Emit.One(token)))))
                    .Then(Layer.Tokens(OneOf(Token("<dedent>").AtLeastOnce().Select(static dedents => $"<{dedents.Count} dedents>"), Any))),
                "a\n  b"
            )
        );

    [Fact]
    public void ATokenOverSyntheticItemsAtTwoPositionsCoversTheTextBetween() =>
        Assert.Equal(
            [
                ("a", Span(0, 1)),
                (";;", Span(1, 2)),
                ("b", Span(3, 4)),
            ],
            Spans(_lexer.Then(_semicolons).Then(Layer.Tokens(OneOf(Token(";").AtLeastOnce().Select(static semicolons => string.Concat(semicolons)), Any))), "a\n\nb")
        );
}
