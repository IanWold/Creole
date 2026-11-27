using System.Text;
using Creole.Samples.Indentation;
using static Creole.CharGrammar;
using WordGrammar = Creole.Grammar<string>;

namespace Creole.Tests;

file enum Mode
{
    Words,
    Digits,
}

file enum K
{
    Word,
    Number,
}

file readonly record struct Lexeme(K Kind, string Text) : IKinded<K>;

/// <summary>
/// Lexers for restarts: one whose tokens are words in word mode and numbers in digit mode, one that numbers its tokens in its state, and one that raises a diagnostic for each <c>!</c>.
/// </summary>
file sealed class Lexers : CharGrammar
{
    private static readonly Parser<char, Lexeme> _number = Digit.AtLeastOnce().Text().Select(static text => new Lexeme(K.Number, text));

    private static readonly Parser<char, Lexeme> _word = Letter.AtLeastOnce().Text().Select(static text => new Lexeme(K.Word, text));

    public static readonly Layer<char, string> Counted = Layer.Tokens(
        0,
        from text in Letter.AtLeastOnce().Text()
        from count in GetState<int>()
        from _ in SetState(count + 1)
        select $"{text}#{count}",
        Whitespace.SkipMany()
    );

    public static readonly ModeChannel<Mode> Modes = new(Mode.Words);

    public static readonly Layer<char, string> Raising = Layer.Tokens(Letter.AtLeastOnce().Text().Or(Char('!').Then(new RaiseNode("a bang"))), Whitespace.SkipMany());

    public static readonly Layer<char, Lexeme> Steered = Layer.Modal(Modes, static mode => mode == Mode.Words ? _word : _number, Whitespace.SkipMany());
}

/// <summary>
/// A word, then a number read in digit mode, and back to word mode.
/// </summary>
file sealed class Pairs : TokenGrammar<Lexeme, K>
{
    public static readonly Parser<Lexeme, string> WordThenNumber =
        Seq(Kind(K.Word), SetMode(Lexers.Modes, Mode.Digits), Kind(K.Number), SetMode(Lexers.Modes, Mode.Words))
            .Select(static (word, _, number, _) => $"{word.Text}{number.Text}");
}

/// <summary>
/// The restart protocol, run synchronously over a root that receives its input a few items at a time: an item that starves goes back to its bookmark in every layer and runs again, and gives the result the same item gives with all its input in memory (ADR-0030 points 7 and 10).
/// </summary>
public class RestartTests
{
    private const string BrokenProgram = "x = 1\nwhile x < 10\n    x = x + 1\n";

    private const string Program = "x = 1\nwhile x < 10:\n    x = x + 1\n\n    if x < 5:\n      y = x * (2 - x)\nz = 0\n";

    private static readonly Parser<string, string> _pair = WordGrammar.Any.Then(WordGrammar.Any);

    private static readonly Layer<char, string> _words = Layer.Tokens(Letter.AtLeastOnce().Text(), skip: Whitespace.SkipMany());

    private static readonly Pipeline<byte, string> _aFromBytes = Pipeline.From(Layer.Utf8().Then(_words)).Then(WordGrammar.Token("a"));

    private static readonly Pipeline<byte, string> _countedFromBytes = Pipeline.From(Layer.Utf8().Then(Lexers.Counted)).Then(_pair);

    private static readonly Pipeline<byte, Script> _exampleBFromBytes = Pipeline.From(Layer.Utf8().Then(MiniLexer.Lexer).Then(Layout.Pass)).Then(MiniGrammar.Root);

    private static readonly Pipeline<byte, string> _filteredFromBytes =
        Pipeline.From(Layer.Utf8().Then(Layer.Select<char, char>(static c => char.ToLowerInvariant(c))).Then(_words).Then(Layer.Where<string>(static word => word != "the"))).Then(_pair);

    private static readonly Pipeline<char, string> _lookingAhead =
        Pipeline.From(_words).Then(WordGrammar.Lookahead(WordGrammar.Token("x").Or(WordGrammar.Any.Then(WordGrammar.Any))).Then(WordGrammar.Token("y")).Context("a record"));

    private static readonly Pipeline<byte, string> _longRecordsFromBytes =
        Pipeline.From(Layer.Utf8().Then(_words)).Then(WordGrammar.Token("x").SkipMany().Then(WordGrammar.Token("end")));

    private static readonly Pipeline<byte, string> _lookingAheadFromBytes =
        Pipeline.From(Layer.Utf8().Then(_words)).Then(WordGrammar.Lookahead(WordGrammar.Token("x").Or(WordGrammar.Any.Then(WordGrammar.Any))).Then(WordGrammar.Token("y")).Context("a record"));

    private static readonly Pipeline<char, string> _raisingLookingAhead = Pipeline.From(Lexers.Raising).Then(WordGrammar.Any.Before(WordGrammar.Lookahead(WordGrammar.Any.Then(WordGrammar.Any)).Optional()));

    private static readonly Pipeline<byte, string> _steeredByALayerFromBytes = Pipeline.From(Layer.Utf8().Then(Lexers.Steered).Then(Layer.Tokens(Pairs.WordThenNumber, name: "pairs"))).Then(WordGrammar.Any);

    private static readonly Pipeline<byte, string> _steeredFromBytes = Pipeline.From(Layer.Utf8().Then(Lexers.Steered)).Then(Pairs.WordThenNumber);

    private static readonly Pipeline<byte, string> _wordFromBytes = Pipeline.From(Layer.Utf8().Then(_words)).Then(WordGrammar.Any);

    public static TheoryData<int> Steps => [1, 2, 3, 5, 8, 64, int.MaxValue];

    private static byte[] Bytes(string text) =>
        Encoding.UTF8.GetBytes(text);

    private static int LexerDiagnostics<TIn, TOut>(Pipeline<TIn, TOut> pipeline, TIn[] input, int? step)
    {
        var logs = new List<object>();
        var root = new FedUpstream<TIn>(input, step ?? input.Length);

        using var items = step is not null
            ? pipeline.OpenItems(new StreamBuffer<TIn>(root), ParseOptions.Default, static () => null, restarts: true, logs)
            : pipeline.OpenItems(new MemorySource<TIn>(input), ParseOptions.Default, static () => null, restarts: false, logs);

        _ = Run(items, () => root.Receive(step ?? 0));

        return ((FailureLog<TIn>)logs[0]).DiagnosticCount;
    }

    private static List<object?> Outcomes<T>(List<ParseResult<T>> results) =>
        [
            .. results.Select(static object? (result) => result switch
            {
                Parsed<T> parsed => parsed.Value?.ToString(),
                Recovered<T> recovered => new object?[] { recovered.Value?.ToString(), recovered.Diagnostics.ToArray() },
                Failed failed => failed.Diagnostics.ToArray(),
            }),
        ];

    private static (List<object?> Outcomes, int Restarts) Run<TOut>(PipelineItems<TOut> items, Func<bool> receive)
    {
        var results = new List<ParseResult<TOut>>();
        var restarts = 0;

        for (var outcome = items.Next(out var result); outcome != ItemOutcome.End; outcome = items.Next(out result))
        {
            if (outcome == ItemOutcome.Starved)
            {
                Assert.True(receive(), "An item starved with all its input received.");
                restarts++;
                continue;
            }

            results.Add(result);

            if (result is Failed)
            {
                break;
            }
        }

        return (Outcomes(results), restarts);
    }

    private static void SameAsInMemory<TIn, TOut>(Pipeline<TIn, TOut> pipeline, TIn[] input, int step)
    {
        var root = new FedUpstream<TIn>(input, step);

        using var restarted = pipeline.OpenItems(new StreamBuffer<TIn>(root), ParseOptions.Default, static () => null, restarts: true);
        using var inMemory = pipeline.OpenItems(new MemorySource<TIn>(input), ParseOptions.Default, static () => null, restarts: false);

        var (outcomes, restarts) = Run(restarted, () => root.Receive(step));

        Assert.Equal(Run(inMemory, static () => false).Outcomes, outcomes);
        Assert.True(step > 1 || restarts > 0, "No item restarted with one item received at a time.");
    }

    [Theory]
    [MemberData(nameof(Steps))]
    public void AGrammarErrorInAMiddleRecordEndsTheRunAsInMemory(int step) =>
        SameAsInMemory(_aFromBytes, Bytes("a a b a"), step);

    [Theory]
    [MemberData(nameof(Steps))]
    public void ALayerThatSteersTheLayerBelowGivesTheItemsItGivesInMemory(int step) =>
        SameAsInMemory(_steeredByALayerFromBytes, Bytes("ab12 cd34  ef56 gh7"), step);

    [Theory]
    [MemberData(nameof(Steps))]
    public void ALexerErrorEndsTheRunAsInMemory(int step) =>
        SameAsInMemory(_wordFromBytes, Bytes("ab cd 1 ef"), step);

    [Fact]
    public void AnAttemptThatStarvesInsideALookaheadAndAContextLeavesNoFailureSaveOrFrame()
    {
        var logs = new List<object>();
        var root = new FedUpstream<char>("ab cd ef".ToCharArray(), received: 3);

        using var items = _lookingAhead.OpenItems(new StreamBuffer<char>(root), ParseOptions.Default, static () => null, restarts: true, logs);

        Assert.Equal(ItemOutcome.Starved, items.Next(out _));

        var lexer = (FailureLog<char>)logs[0];
        var parser = (FailureLog<string>)logs[1];

        Assert.Equal([(-1L, 0, 0), (-1L, 0, 0)], [(lexer.Position, lexer.FrameCount, lexer.SaveCount), (parser.Position, parser.FrameCount, parser.SaveCount)]);
    }

    [Theory]
    [MemberData(nameof(Steps))]
    public void AParserThatSteersItsLexerGivesTheItemsItGivesInMemory(int step) =>
        SameAsInMemory(_steeredFromBytes, Bytes("ab12 cd34  ef56 gh7"), step);

    [Theory]
    [MemberData(nameof(Steps))]
    public void AParserThatSteersItsLexerIntoAnErrorEndsTheRunAsInMemory(int step) =>
        SameAsInMemory(_steeredFromBytes, Bytes("ab12 cd ef56"), step);

    [Theory]
    [MemberData(nameof(Steps))]
    public void ARestartKeepsTheDiagnosticsRaisedBeforeTheBookmarkAndWithdrawsTheRest(int step) =>
        Assert.Equal(LexerDiagnostics(_raisingLookingAhead, "ab ! cd ef ! gh".ToCharArray(), step: null), LexerDiagnostics(_raisingLookingAhead, "ab ! cd ef ! gh".ToCharArray(), step));

    [Theory]
    [MemberData(nameof(Steps))]
    public void ExampleBGivesTheScriptItGivesInMemory(int step) =>
        SameAsInMemory(_exampleBFromBytes, Bytes(Program), step);

    [Theory]
    [MemberData(nameof(Steps))]
    public void ExampleBWithAnErrorEndsTheRunAsInMemory(int step) =>
        SameAsInMemory(_exampleBFromBytes, Bytes(BrokenProgram), step);

    [Theory]
    [MemberData(nameof(Steps))]
    public void InvalidUtf8InAMiddleRecordEndsTheRunAsInMemory(int step) =>
        SameAsInMemory<byte, string>(_wordFromBytes, [.. Bytes("ab cd "), 0xFF, .. Bytes(" ef")], step);

    [Theory]
    [MemberData(nameof(Steps))]
    public void LayerStateCarriesOverRestartsAsInMemory(int step) =>
        SameAsInMemory(_countedFromBytes, Bytes("ab cd ef gh ij kl"), step);

    [Theory]
    [InlineData(1_000)]
    [InlineData(4_096)]
    public void ARecordLongerThanManySegmentsKeepsItsStartThroughEveryRestart(int step) =>
        SameAsInMemory(_longRecordsFromBytes, Bytes(string.Concat(Enumerable.Repeat(string.Concat(Enumerable.Repeat("x ", 3_000)) + "end ", 2))), step);

    [Theory]
    [MemberData(nameof(Steps))]
    public void MultiByteCharactersAfterAByteOrderMarkGiveTheItemsTheyGiveInMemory(int step) =>
        SameAsInMemory<byte, string>(_wordFromBytes, [.. Encoding.UTF8.Preamble, .. Bytes("alpha béta gamma δέλτα ε ζ")], step);

    [Theory]
    [MemberData(nameof(Steps))]
    public void SelectAndWhereLayersGiveTheItemsTheyGiveInMemory(int step) =>
        SameAsInMemory(_filteredFromBytes, Bytes("The cat AND the Hat sat on THE mat"), step);

    [Theory]
    [MemberData(nameof(Steps))]
    public void TheRetryOfAnAttemptThatStarvedInsideALookaheadAndAContextFailsAsInMemory(int step) =>
        SameAsInMemory(_lookingAheadFromBytes, Bytes("ab cd ef"), step);
}
