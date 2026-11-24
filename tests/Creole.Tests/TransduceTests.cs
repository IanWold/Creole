using static Creole.Grammar<string>;

namespace Creole.Tests;

public class TransduceTests
{
    private static readonly Layer<char, string> _words = Layer.Tokens(CharGrammar.Letter.AtLeastOnce().Text(), skip: CharGrammar.Whitespace.SkipMany());

    private static readonly Layer<char, string> _statements = Layer.Tokens(
        CharGrammar.Longest(CharGrammar.Letter.AtLeastOnce().Text(), CharGrammar.Digit.AtLeastOnce().Text(), CharGrammar.String("="), CharGrammar.String("\n")),
        skip: CharGrammar.Char(' ').SkipMany()
    );

    private static readonly Layer<string, string> _semicolons = Layer.Transduce(
        initialState: 0,
        step: OneOf(Token("\n").Select<Emit<string>>(static _ => Emit.Synthetic(";")), Any.Select(static token => Emit.One(token))),
        atEnd: static _ => Emit.Synthetic("<end>")
    );

    private static SourceSpan Span(long start, long end) => new(SourceId.Unnamed, start, end);

    [Fact]
    public void AFailedStepEndsTheParseWithTheLayersError() =>
        Assert.Equal(
            "error[layout] at 0..2: unexpected cd; expected ab",
            LayerRun.Error(_words.Then(Layer.Transduce(0, Token("ab").Select<Emit<string>>(static token => token))), "cd").ToString()
        );

    [Fact]
    public void ALexerComposedWithALayoutPassIsOneLayer()
    {
        var lexer = _statements.Then(_semicolons);
        var statement = Satisfy(static token => char.IsLetter(token[0])).Before(Token("=")).Before(Any).Before(Token(";"));
        var program = statement.Many().Before(Token("<end>")).Before(End);
        var tokens = LayerRun.Open(lexer, "a = 1\nb = 2\n", out _);
        var cursor = new Cursor<string>(tokens);
        IReadOnlyList<string> names = [];

        Assert.True(PlanCache.Get(program).TryParse(ref cursor, ref names));
        Assert.Equal(["a", "b"], names);
        Assert.Equal(
            [
                ("a", Span(0, 1)),
                ("=", Span(2, 3)),
                ("1", Span(4, 5)),
                (";", Span(5, 5)),
                ("b", Span(6, 7)),
                ("=", Span(8, 9)),
                ("2", Span(10, 11)),
                (";", Span(11, 11)),
                ("<end>", Span(12, 12)),
            ],
            LayerRun.Items(lexer, "a = 1\nb = 2\n").Select(static entry => (entry.Item, entry.Slot.Span))
        );
    }

    [Fact]
    public void AStepThatConsumesNothingThrows() =>
        Assert.Throws<GrammarException>(() => LayerRun.Items(_words.Then(Layer.Transduce(0, Return(default(Emit<string>)))), "ab"));

    [Fact]
    public void AStepThatLeavesAStateOfAnotherTypeThrows() =>
        Assert.Equal(
            "A step of the layout layer left its state of type String, but its initialState is of type Int32.",
            Assert.Throws<GrammarException>(() => LayerRun.Values(_words.Then(Layer.Transduce(0, Any.Then(SetState("x")).As(Emit.One("a")))), "ab")).Message
        );

    [Fact]
    public void EachOutputsSlotRecordsTheStateItsStepStartedWith()
    {
        var layer = _words.Then(Layer.Transduce(0, Any.Scan<int, Emit<string>>(static (word, count, _) => ([word, Emit.Synthetic("!")], count + 1)), atEnd: static _ => "$"));

        Assert.Equal([0, 0, 1, 1, 2], LayerRun.Items(layer, "ab cd").Select(static entry => entry.Slot.StateBefore));
    }

    [Fact]
    public void EachStepReadsAndChangesTheStateAndAtEndGetsTheLast() =>
        Assert.Equal(
            ["ab", "cd", "2 words"],
            LayerRun.Values(_words.Then(Layer.Transduce(0, Any.Scan<int, Emit<string>>(static (word, count, _) => (word, count + 1)), atEnd: static count => Emit.Synthetic($"{count} words"))), "ab cd")
        );

    [Fact]
    public void NullArgumentsThrow()
    {
        Assert.Throws<GrammarException>(static () => Layer.Transduce<string, string, int>(0, null!));
        Assert.Throws<GrammarException>(static () => Layer.Transduce(0, Any.Select(static token => Emit.One(token)), name: ""));
    }

    [Fact]
    public void OutputsTakeTheirSpansStepStartsAndGroupIndices()
    {
        var step = OneOf(
            Token("ab").Select<Emit<string>>(static _ => Emit.Synthetic("x")),
            Token("cd").Select<Emit<string>>(static token => [Emit.Synthetic("<"), token, Emit.Synthetic(">")]),
            Token("ef").Then(Token("gh")).Select<Emit<string>>(static _ => ["ef+gh", "again"]),
            Token("ij").Select(static _ => default(Emit<string>)),
            Any.Select<Emit<string>>(static token => [token, Emit.Synthetic("!")])
        );

        var layer = _words.Then(Layer.Transduce(0, step, atEnd: static _ => "$"));

        Assert.Equal(
            [
                ("x", new StreamSlot(Span(0, 0), 0, 0, 0)),
                ("<", new StreamSlot(Span(4, 4), 1, 0, 0)),
                ("cd", new StreamSlot(Span(4, 6), 1, 0, 1)),
                (">", new StreamSlot(Span(8, 8), 1, 0, 2)),
                ("ef+gh", new StreamSlot(Span(8, 13), 2, 0, 0)),
                ("again", new StreamSlot(Span(8, 13), 2, 0, 1)),
                ("kl", new StreamSlot(Span(17, 19), 5, 0, 0)),
                ("!", new StreamSlot(Span(19, 19), 5, 0, 1)),
                ("$", new StreamSlot(Span(19, 19), 6, 0, 0)),
            ],
            LayerRun.Items(layer, "ab  cd  ef gh ij kl")
        );
    }

    [Fact]
    public void ReadingTheStateAsAnotherTypeNamesTheLayer() =>
        Assert.Equal(
            "The indents layer's state is of type Int32, but its grammar reads it as String.",
            Assert.Throws<GrammarException>(() => LayerRun.Values(_words.Then(Layer.Transduce(0, Any.Then(GetState<string>()).Select(static word => Emit.One(word)), name: "indents")), "ab")).Message
        );

    [Fact]
    public void TheLayerNameNamesItsErrors() =>
        Assert.Equal("indents", LayerRun.Error(_words.Then(Layer.Transduce(0, Token("ab").Select(static token => Emit.One(token)), name: "indents")), "cd").Layer);

    [Fact]
    public void WithoutAtEndNothingIsAddedAtTheEnd() =>
        Assert.Equal(["ab", "cd"], LayerRun.Values(_words.Then(Layer.Transduce(0, Any.Select(static token => Emit.One(token)))), "ab cd"));
}
