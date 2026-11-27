using static Creole.CharGrammar;

namespace Creole.Tests;

file enum Mode
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
/// Lexers that read <c>/</c> as the start of a regex in operand mode and as a slash in operator mode.
/// </summary>
file sealed class Lexers : CharGrammar
{
    public static readonly ModeChannel<Mode> Modes = new(Mode.Operand);

    private static readonly Parser<char, Lexeme> _name = Letter.AtLeastOnce().Text().Select(static text => new Lexeme(K.Name, text));
    private static readonly Parser<char, Lexeme> _regex = Char('/').Then(NoneOf('/').SkipMany()).Then(Char('/')).Text().Select(static text => new Lexeme(K.Regex, text));
    private static readonly Parser<char, Lexeme> _slash = Char('/').As(new Lexeme(K.Slash, "/"));

    /// <summary>
    /// Counts in its state the tokens it has produced, and adds each token's number to its text.
    /// </summary>
    public static readonly Layer<char, Lexeme> Counting = Layer.Modal(
        0,
        Modes,
        static mode =>
            Numbered(
                mode == Mode.Operand
                    ? _name.Or(_regex)
                    : _name.Or(_slash)
            ),
        Whitespace.SkipMany()
    );

    /// <summary>
    /// Raises a diagnostic for each slash it reads in operator mode.
    /// </summary>
    public static readonly Layer<char, Lexeme> Raising = Layer.Modal(
        Modes,
        static mode =>
            mode == Mode.Operand
                ? _name.Or(_regex)
                : _name.Or(_slash.Before(new RaiseNode("a slash"))),
        Whitespace.SkipMany()
    );

    /// <summary>
    /// Adds each token it produces to <paramref name="lexed"/>.
    /// </summary>
    public static Layer<char, Lexeme> Recording(List<string> lexed) =>
        Layer.Modal(
            Modes,
            mode =>
                (mode == Mode.Operand ? _name.Or(_regex) : _name.Or(_slash))
                .Select(lexeme =>
                {
                    lexed.Add(lexeme.Text);
                    return lexeme;
                }),
            Whitespace.SkipMany()
        );

    /// <summary>
    /// In operator mode, first tries <c>//!</c>, which fails after reading <c>//</c> if no <c>!</c> follows.
    /// </summary>
    public static readonly Layer<char, Lexeme> Searching = Layer.Modal(
        Modes,
        static mode =>
            mode == Mode.Operand
                ? _name.Or(_regex)
                : _name.Or(Atomic(Char('/').Then(Char('/')).Then(Char('!'))).As(new Lexeme(K.Slash, "//!"))).Or(_slash),
        Whitespace.SkipMany()
    );

    private static Parser<char, Lexeme> Numbered(Parser<char, Lexeme> token) =>
        from lexeme in token
        from count in GetState<int>()
        from _ in SetState(count + 1)
        select lexeme with { Text = $"{lexeme.Text}#{count}" };
}

/// <summary>
/// Parsers that change the mode of <see cref="Lexers.Modes"/> after looking at a token in the old mode.
/// </summary>
file sealed class Parsers : TokenGrammar<Lexeme, K>
{
    private static readonly ModeChannel<Mode> _modes = Lexers.Modes;

    /// <summary>
    /// Reads a name, a slash and a name in operator mode, looks at the next token in operator mode, and then changes to operand mode to read it as a regex.
    /// </summary>
    public static readonly Parser<Lexeme, string> DivisionThenRegex =
        Seq(Kind(K.Name), SetMode(_modes, Mode.Operator), Kind(K.Slash), Kind(K.Name), Kind(K.Regex).Or(SetMode(_modes, Mode.Operand).Then(Kind(K.Regex))))
            .Select(static (left, _, slash, right, regex) => $"{left.Text} {slash.Text} {right.Text} {regex.Text}");

    /// <summary>
    /// Sets operand mode, which is the mode the channel starts in, reads a name, and looks at the next token.
    /// </summary>
    public static readonly Parser<Lexeme, string> NameLookingAhead =
        SetMode(_modes, Mode.Operand).Then(Kind(K.Name)).Before(Lookahead(Kind(K.Regex)).Optional()).Select(static name => name.Text);

    /// <summary>
    /// Reads a regex, or a name that leaves operator mode after looking at a slash in it.
    /// </summary>
    public static readonly Parser<Lexeme, string> RegexOrName =
        Kind(K.Regex).Or(Kind(K.Name).Before(SetMode(_modes, Mode.Operator)).Before(Lookahead(Kind(K.Slash)))).Select(static lexeme => lexeme.Text);

    /// <summary>
    /// Reads a name, looks at the next token in operator mode, then reads it as a regex in operand mode, and then a name.
    /// </summary>
    public static readonly Parser<Lexeme, string> RegexThenName =
        Seq(Kind(K.Name), SetMode(_modes, Mode.Operator), Kind(K.Regex).Or(SetMode(_modes, Mode.Operand).Then(Kind(K.Regex))), Kind(K.Name))
            .Select(static (left, _, regex, right) => $"{left.Text} {regex.Text} {right.Text}");
}

/// <summary>
/// What a change of mode discards and produces again in the <c>Layer.Modal</c> that reads the channel: its items from the cursor on, the diagnostics of the steps that produced them, and its failures from those steps' start.
/// </summary>
public class ModeInvalidationTests
{
    private static string Value<T>(ParseResult<T> result) =>
        result.Value switch
        {
            Parsed<T> parsed => $"{parsed.Value}",
            Failed failed => failed.Diagnostics.Render(),
            _ => "",
        };

    [Fact]
    public void AChangeRunsTheStepAgainFromTheStateItStartedWith() =>
        Assert.Equal("a#0 /#1 b#2 /c/#3", Value(Pipeline.From(Lexers.Counting).Then(Parsers.DivisionThenRegex).Parse("a / b /c/")));

    [Fact]
    public void AChangeWithdrawsTheDiagnosticsOfTheStepsItRunsAgainAndNoOthers()
    {
        var run = new PipelineRun(ParseOptions.Default, recordFailures: true) { Logs = [] };

        Assert.Equal((true, "a / b /c/", null), LayerRun.Parse(Lexers.Raising, Parsers.DivisionThenRegex, "a / b /c/", run: run));
        Assert.Equal(1, ((FailureLog<char>)run.Logs!.Single()).DiagnosticCount);
    }

    [Fact]
    public void AChangeMakesTheLexerForgetItsFailuresFromTheStartOfTheStepItRunsAgain()
    {
        var expected = new ParseError(new SourceSpan(SourceId.Unnamed, 4, 5), "lexer", new UnexpectedItem("'@'"), [new Label("whitespace"), new Label("letter"), new ExpectedItem("'/'")], []);

        Assert.Equal(expected, Assert.IsType<Failed>(Pipeline.From(Lexers.Searching).Then(Parsers.RegexThenName).Parse("a //@").Value).Diagnostics.Single());
        Assert.Equal((false, "", expected), LayerRun.Parse(Lexers.Searching, Parsers.RegexThenName, "a //@", segmentSize: 1) with { Value = "" });
    }

    [Fact]
    public void AParseManyItemStartingInTheModeTheLastLeftReadsNothingAgain()
    {
        var lexed = new List<string>();

        Assert.Equal(["a", "b", "c"], Pipeline.From(Lexers.Recording(lexed)).Then(Parsers.NameLookingAhead).ParseMany("a b c").Select(Value));
        Assert.Equal(["a", "b", "c"], lexed);
    }

    [Fact]
    public void EachParseManyItemReadsAgainWhatTheLastItemReadAheadInAnotherMode() =>
        Assert.Equal(["a", "/b/"], Pipeline.From(Lexers.Raising).Then(Parsers.RegexOrName).ParseMany("a /b/").Select(Value));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    [InlineData(4, 3)]
    public void ATokensLayerTagsEachDiagnosticWithTheStartOfItsStep(long withdrawnFrom, int left)
    {
        var run = new PipelineRun(ParseOptions.Default, recordFailures: true) { Logs = [] };

        LayerRun.Items(Layer.Tokens(Letter.Before(new RaiseNode("a letter")), Whitespace.SkipMany()), "a b c", run: run);

        var log = (FailureLog<char>)run.Logs!.Single();

        log.WithdrawFromStep(withdrawnFrom);

        Assert.Equal(left, log.DiagnosticCount);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 3)]
    public void ATransduceLayerTagsEachDiagnosticWithTheStartOfItsStep(long withdrawnFrom, int left)
    {
        var run = new PipelineRun(ParseOptions.Default, recordFailures: true) { Logs = [] };

        LayerRun.Items(Layer.Transduce(0, Letter.Before(new RaiseNode("a letter")).Select(static letter => (Emit<char>)letter)), "abc", run: run);

        var log = (FailureLog<char>)run.Logs!.Single();

        log.WithdrawFromStep(withdrawnFrom);

        Assert.Equal(left, log.DiagnosticCount);
    }
}
