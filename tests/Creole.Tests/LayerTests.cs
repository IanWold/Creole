using CsCheck;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;
using StringGrammar = Creole.Grammar<string>;

namespace Creole.Tests;

public class LayerTests
{
    private static readonly Layer<char, string> _lexer = Layer.Tokens(
        Longest(Letter.AtLeastOnce().Text(), Digit.AtLeastOnce().Text(), String("<"), String("<="), String("<<")),
        skip: Whitespace.SkipMany()
    );

    private static readonly Parser<char, char> _nested = OneOf(Letter, Char('(').Then(Rec(() => _nested)).Before(Char(')')));

    private static readonly Gen<string> _token = Gen.OneOf(
        Gen.Char['a', 'z'].Array[1, 4].Select(static letters => new string(letters)),
        Gen.Char['0', '9'].Array[1, 3].Select(static digits => new string(digits)),
        Gen.OneOf("<", "<=", "<<")
    );

    private static SourceSpan Span(long start, long end) => new(SourceId.Unnamed, start, end);

    [Fact]
    public void ADepthErrorInAStepIsTheLayersError() =>
        Assert.Equal(
            "error[lexer] at 3..4: nesting exceeds ParseOptions.MaxDepth of 2",
            LayerRun.Error(Layer.Tokens(_nested), "((((a))))", ParseOptions.Default with { MaxDepth = 2 }).ToString()
        );

    [Fact]
    public void AFailedItemEndsTheParseWithTheLayersFurthestFailure() =>
        Assert.Equal(
            "error[lexer] at 3..4: unexpected '?'; expected whitespace, letter, digit, \"<\", \"<=\" or \"<<\"",
            LayerRun.Error(_lexer, "ab ?").ToString()
        );

    [Fact]
    public void AFailedSkipEndsTheParse()
    {
        var error = LayerRun.Error(Layer.Tokens(Letter, skip: Whitespace.Ignore()), " a");

        Assert.Equal("lexer", error.Layer);
        Assert.Equal(Span(2, 2), error.Span);
        Assert.Equal(new EndOfInput(), error.Unexpected.Value);
    }

    [Fact]
    public void ALayersBacktrackingIsInvisibleDownstream()
    {
        var selected = 0;
        var counted = _lexer.Then(Layer.Select<string, string>(token =>
        {
            selected++;
            return token;
        }));
        var tokens = LayerRun.Open(counted, "a <= b", out var characters);
        var parser = StringGrammar.OneOf(
            StringGrammar.Atomic(StringGrammar.Token("a").Then(StringGrammar.Token("<=")).Then(StringGrammar.Token("b")).Then(StringGrammar.Token("!"))),
            StringGrammar.Token("a").Then(StringGrammar.Token("<=")).Then(StringGrammar.Token("b"))
        );
        var cursor = new Cursor<string>(tokens);
        var value = "";

        Assert.True(PlanCache.Get(parser).TryParse(ref cursor, ref value));
        Assert.Equal("b", value);
        Assert.Equal(3, selected);
        Assert.All(characters.Produced, static count => Assert.Equal(1, count));
    }

    [Fact]
    public void AnItemThatConsumesNothingThrows() =>
        Assert.Throws<GrammarException>(() => LayerRun.Items(Layer.Tokens(Digit.Many().Text()), "a"));

    [Fact]
    public void ATokenSpanCoversItsOwnInputAndNoSkippedInput() =>
        Gen.Select(
            Gen.Char[" \t\n"].Array[0, 3],
            Gen.Select(Gen.Char[" \t\n"].Array[1, 3], _token).Array[0, 8],
            Gen.Char[" \t\n"].Array[0, 3],
            Gen.Int[1, 4]
        ).Sample(
            static (leading, tokens, trailing, segmentSize) =>
            {
                var text = new System.Text.StringBuilder().Append(leading);
                var expected = new List<(string, StreamSlot)>();

                for (var i = 0; i < tokens.Length; i++)
                {
                    var stepStart = text.Length;

                    if (i > 0)
                    {
                        text.Append(tokens[i].Item1);
                    }
                    else
                    {
                        stepStart = 0;
                    }

                    expected.Add((tokens[i].Item2, new StreamSlot(Span(text.Length, text.Length + tokens[i].Item2.Length), stepStart, null, 0)));
                    text.Append(tokens[i].Item2);
                }

                text.Append(trailing);

                return LayerRun.Items(_lexer, text.ToString(), segmentSize).SequenceEqual(expected);
            },
            iter: 500
        );

    [Fact]
    public void ATokensLayerOverOnlySkippedInputProducesNothing()
    {
        Assert.Empty(LayerRun.Items(_lexer, ""));
        Assert.Empty(LayerRun.Items(_lexer, " \t\n "));
    }

    [Fact]
    public void ATokensLayerProducesEachValueWithItsSpan() =>
        Assert.Equal(
            [
                ("ab", new StreamSlot(Span(2, 4), 0, null, 0)),
                ("12", new StreamSlot(Span(5, 7), 4, null, 0)),
                ("<=", new StreamSlot(Span(8, 10), 7, null, 0)),
                ("x", new StreamSlot(Span(10, 11), 10, null, 0)),
            ],
            LayerRun.Items(_lexer, "  ab 12\t<=x ")
        );

    [Fact]
    public void ATokensLayerWithoutSkipReadsItemsBackToBack() =>
        Assert.Equal(
            [
                ('a', new StreamSlot(Span(0, 1), 0, null, 0)),
                ('b', new StreamSlot(Span(1, 2), 1, null, 0)),
            ],
            LayerRun.Items(Layer.Tokens(Letter), "ab")
        );

    [Fact]
    public void ChainingIsAssociative()
    {
        const string text = "ab < cd << 12";

        var keep = Layer.Where<string>(static token => token != "<");
        var upper = Layer.Select<string, string>(static token => token.ToUpperInvariant());

        Assert.Equal(LayerRun.Items(_lexer.Then(keep).Then(upper), text), LayerRun.Items(_lexer.Then(keep.Then(upper)), text));
        Assert.Equal(["AB", "CD", "<<", "12"], LayerRun.Values(_lexer.Then(keep).Then(upper), text));
    }

    [Fact]
    public void IdentityIsTheUnitOfThen()
    {
        Assert.Same(_lexer, Layer.Identity<char>().Then(_lexer));
        Assert.Same(_lexer, _lexer.Then(Layer.Identity<string>()));
    }

    [Fact]
    public void IdentityOnItsOwnPassesItemsThrough() =>
        Assert.Equal(
            [
                ('a', new StreamSlot(Span(0, 1), 0, null, 0)),
                ('b', new StreamSlot(Span(1, 2), 1, null, 0)),
            ],
            LayerRun.Items(Layer.Identity<char>(), "ab")
        );

    [Fact]
    public void NullArgumentsThrow()
    {
        Assert.Throws<GrammarException>(static () => Layer.Tokens<char, char>(null!));
        Assert.Throws<GrammarException>(static () => Layer.Tokens(Letter, skip: null!));
        Assert.Throws<GrammarException>(static () => Layer.Tokens(Letter, name: ""));
        Assert.Throws<GrammarException>(static () => Layer.Where<char>(null!));
        Assert.Throws<GrammarException>(static () => Layer.Select<char, char>(null!));
        Assert.Throws<GrammarException>(static () => _lexer.Then<string>(null!));
    }

    [Fact]
    public void SelectMapsEachItemAndKeepsItsSpan() =>
        Assert.Equal(
            [
                (2, new StreamSlot(Span(0, 2), 0, null, 0)),
                (1, new StreamSlot(Span(3, 4), 1, null, 0)),
            ],
            LayerRun.Items(_lexer.Then(Layer.Select<string, int>(static token => token.Length)), "ab c")
        );

    [Fact]
    public void TheLayerNameNamesItsErrors() =>
        Assert.Equal("words", LayerRun.Error(Layer.Tokens(Letter, name: "words"), "1").Layer);

    [Fact]
    public void TheLayersFurthestFailureSpansItsSteps() =>
        Assert.Equal(
            "error[lexer] at 1..2: unexpected ';'; expected 'x' or digit",
            LayerRun.Error(Layer.Tokens(Digit.Before(Char('x').Optional())), "1;").ToString()
        );

    [Fact]
    public void WhereKeepsPassingItemsWithTheirSpans() =>
        Assert.Equal(
            [
                ("ab", new StreamSlot(Span(0, 2), 0, null, 0)),
                ("cd", new StreamSlot(Span(5, 7), 2, null, 0)),
            ],
            LayerRun.Items(_lexer.Then(Layer.Where<string>(static token => token != "<")), "ab < cd")
        );
}
