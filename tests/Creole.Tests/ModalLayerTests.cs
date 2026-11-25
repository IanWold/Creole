using System.Collections.Concurrent;
using System.Collections.Immutable;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file enum LexMode
{
    Operand,
    Operator,
}

file enum K
{
    Name,
    Slash,
    Regex,
}

file readonly record struct Lexeme(K Kind, string Text) : IKinded<K>;

/// <summary>
/// The code sample in docs/concepts/mode-channels.md § Reading a channel: Layer.Modal. Keep the two in step.
/// </summary>
file sealed class ScriptLexer : CharGrammar
{
    public static readonly ModeChannel<LexMode> Modes = new(LexMode.Operand);

    private static readonly Parser<char, Lexeme> _name = Letter.AtLeastOnce().Text().Select(static text => new Lexeme(K.Name, text));
    private static readonly Parser<char, Lexeme> _regex = Char('/').Then(NoneOf('/').SkipMany()).Then(Char('/')).Text().Select(static text => new Lexeme(K.Regex, text));
    private static readonly Parser<char, Lexeme> _slash = Char('/').As(new Lexeme(K.Slash, "/"));

    public static readonly Layer<char, Lexeme> Lexer = Layer.Modal(Modes, static mode => mode switch
    {
        LexMode.Operand => _name.Or(_regex),
        _ => _name.Or(_slash),
    }, Whitespace.SkipMany());
}

/// <summary>
/// The parser of the code sample in docs/concepts/mode-channels.md § Reading a channel: Layer.Modal. Keep the two in step.
/// </summary>
file sealed class ScriptGrammar : TokenGrammar<Lexeme, K>
{
    public static readonly Parser<Lexeme, string> Division =
        Seq(Kind(K.Name), SetMode(ScriptLexer.Modes, LexMode.Operator), Kind(K.Slash), SetMode(ScriptLexer.Modes, LexMode.Operand), Kind(K.Regex))
            .Select(static (name, _, slash, _, regex) => $"{name.Text} {slash.Text} {regex.Text}");
}

/// <summary>
/// The tokens of <see cref="ScriptLexer"/>, for lexers the tests build.
/// </summary>
file static class Lexemes
{
    public static readonly Parser<char, Lexeme> Name = Letter.AtLeastOnce().Text().Select(static text => new Lexeme(K.Name, text));

    public static readonly Parser<char, Lexeme> Regex = Char('/').Then(NoneOf('/').SkipMany()).Then(Char('/')).Text().Select(static text => new Lexeme(K.Regex, text));

    public static readonly Parser<char, Lexeme> Slash = Char('/').As(new Lexeme(K.Slash, "/"));
}

/// <summary>
/// <c>Layer.Modal</c>, which lexes in the mode a layer above it sets, and the rules for who may read and write a channel (§ 3. Mode channels, ADR-0041).
/// </summary>
public class ModalLayerTests
{
    private static string Value<T>(ParseResult<T> result) =>
        result.Value switch
        {
            Parsed<T> parsed => $"{parsed.Value}",
            Failed failed => failed.Diagnostics.Render(),
            _ => "",
        };

    [Fact]
    public void ALayerBetweenTheReaderAndTheParserMayWriteTheChannel()
    {
        var relay = Layer.Tokens(TokenGrammar<Lexeme, K>.Any.Before(TokenGrammar<Lexeme, K>.SetMode(ScriptLexer.Modes, LexMode.Operator)), name: "relay");
        var parser = TokenGrammar<Lexeme, K>.Seq(TokenGrammar<Lexeme, K>.Kind(K.Name), TokenGrammar<Lexeme, K>.Kind(K.Slash)).Select(static (name, slash) => $"{name.Text} {slash.Text}");

        Assert.Equal("a /", Value(Pipeline.From(ScriptLexer.Lexer).Then(relay).Then(parser).Parse("a /")));
    }

    [Fact]
    public void AModalLayerThatChangesItsOwnChannelThrows() =>
        Assert.Equal(
            "The lexer layer changes the mode of a ModeChannel<LexMode>, but no layer below it reads that channel. Read it with Layer.Modal in a layer below, using the same ModeChannel instance.",
            Assert.Throws<GrammarException>(() => Pipeline.From(Layer.Modal(ScriptLexer.Modes, static _ => Lexemes.Name.Before(SetMode(ScriptLexer.Modes, LexMode.Operator)))).Then(TokenGrammar<Lexeme, K>.Any).Parse("a")).Message
        );

    [Fact]
    public void ASecondLayerWritingAChannelThrows()
    {
        var relay = Layer.Tokens(TokenGrammar<Lexeme, K>.Any.Before(TokenGrammar<Lexeme, K>.SetMode(ScriptLexer.Modes, LexMode.Operand)), name: "relay");
        var parser = TokenGrammar<Lexeme, K>.Any.Before(TokenGrammar<Lexeme, K>.SetMode(ScriptLexer.Modes, LexMode.Operator));

        Assert.Equal(
            "The parser layer changes the mode of a ModeChannel<LexMode>, which the relay layer already changes. A channel has one writing layer.",
            Assert.Throws<GrammarException>(() => Pipeline.From(ScriptLexer.Lexer).Then(relay).Then(parser).Parse("a")).Message
        );
    }

    [Fact]
    public void ASelectorThatReturnsNullThrowsWhenTheParseReachesItsMode()
    {
        var lexer = Layer.Modal(
            ScriptLexer.Modes,
            static mode => mode == LexMode.Operand
                ? Lexemes.Name
                : null!,
            Whitespace.SkipMany()
        );
        var parser = TokenGrammar<Lexeme, K>.Kind(K.Name).Before(TokenGrammar<Lexeme, K>.SetMode(ScriptLexer.Modes, LexMode.Operator)).Then(TokenGrammar<Lexeme, K>.Any);

        Assert.Equal("The selector of the lexer layer returned null for the mode Operator.", Assert.Throws<GrammarException>(() => Pipeline.From(lexer).Then(parser).Parse("a b")).Message);
    }

    [Fact]
    public void AStatefulModalLayerCarriesItsStateFromStepToStep()
    {
        var layer = Layer.Modal(
            0,
            ScriptLexer.Modes,
            static _ => Lexemes.Name.Scan<int, Lexeme>(static (name, count, _) => (name with { Text = $"{count}:{name.Text}" }, count + 1)),
            Whitespace.SkipMany()
        );

        Assert.Equal(["0:a", "1:b"], LayerRun.Values(layer, "a b").Select(static lexeme => lexeme.Text));
    }

    [Fact]
    public void AWriteThatNoLayerBelowReadsThrows()
    {
        var other = new ModeChannel<LexMode>(LexMode.Operand);
        var below = Layer.Tokens(Letter.Before(SetMode(ScriptLexer.Modes, LexMode.Operator)), name: "below").Then(Layer.Modal(ScriptLexer.Modes, static _ => Any.Select(static c => new Lexeme(K.Name, $"{c}"))));

        Assert.Equal(
            "The parser layer changes the mode of a ModeChannel<LexMode>, but no layer below it reads that channel. Read it with Layer.Modal in a layer below, using the same ModeChannel instance.",
            Assert.Throws<GrammarException>(() => Pipeline.From(ScriptLexer.Lexer).Then(TokenGrammar<Lexeme, K>.Any.Before(TokenGrammar<Lexeme, K>.SetMode(other, LexMode.Operator))).Parse("a")).Message
        );
        Assert.Equal(
            "The below layer changes the mode of a ModeChannel<LexMode>, but no layer below it reads that channel. Read it with Layer.Modal in a layer below, using the same ModeChannel instance.",
            Assert.Throws<GrammarException>(() => Pipeline.From(below).Then(TokenGrammar<Lexeme, K>.Any).Parse("a")).Message
        );
    }

    [Fact]
    public void EachFormBindsToItsOverload()
    {
        var skip = Whitespace.SkipMany();

        ModalLayer<char, Lexeme, LexMode>[] layers =
        [
            .. new[]
            {
                Layer.Modal(ScriptLexer.Modes, static mode => mode switch { LexMode.Operand => Lexemes.Name, _ => Lexemes.Slash }),
                Layer.Modal(ScriptLexer.Modes, static mode => mode switch { LexMode.Operand => Lexemes.Name, _ => Lexemes.Slash }, "x"),
                Layer.Modal(ScriptLexer.Modes, static mode => mode switch { LexMode.Operand => Lexemes.Name, _ => Lexemes.Slash }, name: "x"),
                Layer.Modal(ScriptLexer.Modes, static mode => mode switch { LexMode.Operand => Lexemes.Name, _ => Lexemes.Slash }, skip),
                Layer.Modal(ScriptLexer.Modes, static mode => mode switch { LexMode.Operand => Lexemes.Name, _ => Lexemes.Slash }, skip: skip),
                Layer.Modal(ScriptLexer.Modes, static mode => mode switch { LexMode.Operand => Lexemes.Name, _ => Lexemes.Slash }, skip, "x"),
                Layer.Modal(ScriptLexer.Modes, static mode => mode == LexMode.Operand ? Lexemes.Name : Lexemes.Slash, skip: skip),
                Layer.Modal(0, ScriptLexer.Modes, static mode => mode switch { LexMode.Operand => Lexemes.Name, _ => Lexemes.Slash }, skip),
                Layer.Modal(ImmutableStack<int>.Empty, ScriptLexer.Modes, static mode => mode switch { LexMode.Operand => Lexemes.Name, _ => Lexemes.Slash }, skip, name: "x"),
                Layer.Modal(initialState: 0, channel: ScriptLexer.Modes, selector: static mode => mode switch { LexMode.Operand => Lexemes.Name, _ => Lexemes.Slash }, skip: skip),
            }.Cast<ModalLayer<char, Lexeme, LexMode>>(),
        ];

        Assert.Equal(
            [
                (false, false, "lexer"),
                (false, false, "x"),
                (false, false, "x"),
                (true, false, "lexer"),
                (true, false, "lexer"),
                (true, false, "x"),
                (true, false, "lexer"),
                (true, true, "lexer"),
                (true, true, "x"),
                (true, true, "lexer"),
            ],
            layers.Select(static layer => (layer.Skip is not null, layer.InitialState is not null, layer.Name))
        );
    }

    [Fact]
    public void EachModesItemParserIsPlannedOnce()
    {
        var calls = 0;
        var items = new ConcurrentBag<CountingNode<Lexeme>>();
        var lexer = Layer.Modal<char, Lexeme, LexMode>(
            ScriptLexer.Modes,
            mode =>
            {
                Interlocked.Increment(ref calls);

                var item = new CountingNode<Lexeme>(
                    mode == LexMode.Operand
                        ? Lexemes.Name.Or(Lexemes.Regex)
                        : Lexemes.Name.Or(Lexemes.Slash)
                );

                items.Add(item);

                return item;
            },
            Whitespace.SkipMany()
        );
        var pipeline = Pipeline.From(lexer).Then(ScriptGrammar.Division);

        Parallel.For(0, 64, _ => Assert.Equal("a / /b/", Value(pipeline.Parse("a / /b/"))));

        for (var i = 0; i < 16; i++)
        {
            Assert.Equal("c / /d/", Value(pipeline.Parse("c / /d/")));
        }

        Assert.Equal(2, calls);
        Assert.Equal(2, items.Count);
        Assert.All(items, static item => Assert.Equal(1, item.LowerCount));
    }

    [Fact]
    public void EachParseManyItemStartsInTheInitialMode()
    {
        var item = TokenGrammar<Lexeme, K>.Kind(K.Regex).Select(static regex => regex.Text)
            .Or(
                TokenGrammar<Lexeme, K>.Seq(TokenGrammar<Lexeme, K>.Kind(K.Name), TokenGrammar<Lexeme, K>.SetMode(ScriptLexer.Modes, LexMode.Operator), TokenGrammar<Lexeme, K>.Kind(K.Slash))
                    .Select(static (name, _, slash) => $"{name.Text} {slash.Text}")
            );

        Assert.Equal(["a /", "/b/"], Pipeline.From(ScriptLexer.Lexer).Then(item).ParseMany("a / /b/").Select(Value));
    }

    [Fact]
    public void NullArgumentsNameTheirExpressions()
    {
        ModeChannel<LexMode> channel = null!;
        Func<LexMode, Parser<char, Lexeme>> selector = null!;
        Parser<char, Unit> skip = null!;

        Assert.Equal("'channel' is null.", Assert.Throws<GrammarException>(() => Layer.Modal(channel, static _ => Lexemes.Name)).Message);
        Assert.Equal("'selector' is null.", Assert.Throws<GrammarException>(() => Layer.Modal(ScriptLexer.Modes, selector)).Message);
        Assert.StartsWith("'skip' is null. ", Assert.Throws<GrammarException>(() => Layer.Modal(ScriptLexer.Modes, static _ => Lexemes.Name, skip)).Message);
        Assert.StartsWith("'skip' is null. ", Assert.Throws<GrammarException>(() => Layer.Modal(0, ScriptLexer.Modes, static _ => Lexemes.Name, skip)).Message);
        Assert.Equal("'name' is empty.", Assert.Throws<GrammarException>(() => Layer.Modal(ScriptLexer.Modes, static _ => Lexemes.Name, name: "")).Message);
    }

    [Fact]
    public void PushModeAndPopModeSteerTheLexer()
    {
        var parser = TokenGrammar<Lexeme, K>.Seq(
                TokenGrammar<Lexeme, K>.Kind(K.Name),
                TokenGrammar<Lexeme, K>.PushMode(ScriptLexer.Modes, LexMode.Operator),
                TokenGrammar<Lexeme, K>.Kind(K.Slash),
                TokenGrammar<Lexeme, K>.PopMode(ScriptLexer.Modes),
                TokenGrammar<Lexeme, K>.Kind(K.Regex)
            )
            .Select(static (name, _, slash, _, regex) => $"{name.Text} {slash.Text} {regex.Text}");

        Assert.Equal("a / /b/", Value(Pipeline.From(ScriptLexer.Lexer).Then(parser).Parse("a / /b/")));
    }

    [Fact]
    public void TheLexerReadsEachItemInTheModeTheParserSet()
    {
        Assert.Equal("a / /b/", Value(Pipeline.From(ScriptLexer.Lexer).Then(ScriptGrammar.Division).Parse("a / /b/")));
        Assert.Equal(["a", "/ /"], LayerRun.Values(ScriptLexer.Lexer, "a / /").Select(static lexeme => lexeme.Text));
    }

    [Fact]
    public void TwoModalLayersInAChainCannotReadOneChannel()
    {
        var first = Layer.Modal(ScriptLexer.Modes, static _ => Any, name: "first");
        var second = Layer.Modal(ScriptLexer.Modes, static _ => Any, name: "second");
        var keep = Layer.Where<char>(static _ => true);
        const string Message = "The first layer and the second layer both read a ModeChannel<LexMode>. A chain has one Layer.Modal for each channel.";

        Assert.Equal(Message, Assert.Throws<GrammarException>(() => first.Then(second)).Message);
        Assert.Equal(Message, Assert.Throws<GrammarException>(() => first.Then(keep).Then(second)).Message);
        Assert.Equal(Message, Assert.Throws<GrammarException>(() => first.Then(keep.Then(second))).Message);
        Assert.Equal(Message, Assert.Throws<GrammarException>(() => first.Then(Layer.Identity<char>()).Then(second)).Message);
    }
}
