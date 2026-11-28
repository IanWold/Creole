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
    Number,
    Semi,
    Slash,
    Regex,
}

file readonly record struct Lexeme(K Kind, string Text) : IKinded<K>;

/// <summary>
/// A lexer that starts in operator mode, where <c>/</c> is a slash, and in operand mode reads <c>/</c> as the start of a regex that runs to the next <c>/</c>.
/// </summary>
/// <remarks>
/// A <c>/</c> with no <c>/</c> after it is a slash in operand mode too, so the lexer reads any input in either mode, and no test ends at a token the mode cannot read.
/// So <c>/a/</c> is a slash, a name and a slash in operator mode, and one regex in operand mode.
/// </remarks>
file class Lexers : CharGrammar
{
    public static readonly ModeChannel<Mode> Modes = new(Mode.Operator);

    private static readonly Parser<char, Lexeme> _common = OneOf(
        Letter.AtLeastOnce().Text().Select(static text => new Lexeme(K.Name, text)),
        Digit.AtLeastOnce().Text().Select(static text => new Lexeme(K.Number, text)),
        Char(';').As(new Lexeme(K.Semi, ";"))
    );

    private static readonly Parser<char, Lexeme> _regex = Char('/').Then(NoneOf('/').SkipMany()).Then(Char('/')).Text().Select(static text => new Lexeme(K.Regex, text));
    private static readonly Parser<char, Lexeme> _slash = Char('/').As(new Lexeme(K.Slash, "/"));

    public static readonly Layer<char, Lexeme> Lexer = Modal(static (_, lexeme) => lexeme);

    /// <summary>
    /// The same lexer, adding each token it produces to <paramref name="lexed"/> with the mode it read it in.
    /// </summary>
    public static Layer<char, Lexeme> Recording(List<string> lexed) =>
        Modal((mode, lexeme) =>
        {
            lexed.Add($"{mode} {lexeme.Text}");
            return lexeme;
        });

    private static Layer<char, Lexeme> Modal(Func<Mode, Lexeme, Lexeme> lexed) =>
        Layer.Modal(
            Modes,
            mode =>
                (mode == Mode.Operand
                    ? Longest(_common, _regex, _slash)
                    : Longest(_common, _slash)
                ).Select(lexeme => lexed(mode, lexeme)),
            Whitespace.SkipMany()
        );
}

/// <summary>
/// Parsers that change the mode of <see cref="Lexers.Modes"/> and then go back to before the change.
/// </summary>
file class Parsers : TokenGrammar<Lexeme, K>
{
    private static readonly ModeChannel<Mode> _modes = Lexers.Modes;
    private static readonly Parser<Lexeme, Unit> _operand = SetMode(_modes, Mode.Operand);
    private static readonly Parser<Lexeme, Unit> _operator = SetMode(_modes, Mode.Operator);
    private static readonly Parser<Lexeme, string> _semi = Kind(K.Semi).Select(static semi => semi.Text);

    /// <summary>
    /// A slash, a name and a slash, as operator mode reads <c>/a/</c>.
    /// </summary>
    public static readonly Parser<Lexeme, string> Division =
        Seq(Kind(K.Slash), Kind(K.Name), Kind(K.Slash)).Select(static (left, name, right) => $"{left.Text} {name.Text} {right.Text}");

    /// <summary>
    /// A change to operand mode and a number, which fails without consuming on <c>/a/</c>, read in operand mode as a regex.
    /// </summary>
    public static readonly Parser<Lexeme, string> OperandNumber = _operand.Then(Kind(K.Number)).Select(static number => number.Text);

    /// <summary>
    /// A change to operand mode and a regex.
    /// </summary>
    public static readonly Parser<Lexeme, string> OperandRegex = _operand.Then(Kind(K.Regex)).Select(static regex => regex.Text);

    /// <summary>
    /// A slash and a name, as operator mode reads the start of <c>/a/</c>.
    /// </summary>
    public static readonly Parser<Lexeme, string> SlashName =
        Seq(Kind(K.Slash), Kind(K.Name)).Select(static (slash, name) => $"{slash.Text} {name.Text}");

    /// <summary>
    /// An <c>Atomic</c> around a slash, a name and a number, which on <c>/a/</c> in operator mode fails at the second slash, expecting a number.
    /// </summary>
    public static readonly Parser<Lexeme, string> SlashNameNumber =
        Atomic(Seq(Kind(K.Slash), Kind(K.Name), Kind(K.Number))).Select(static parts => parts.Item3.Text);

    /// <summary>
    /// An <c>Atomic</c> that reads a name, changes to operand mode and reads a number, then failing after consuming the name, or a name and a <see cref="Division"/>.
    /// </summary>
    public static readonly Parser<Lexeme, string> AtomicAroundAChange =
        Atomic(Seq(Kind(K.Name), _operand, Kind(K.Number)).Select(static (name, _, number) => $"{name.Text} {number.Text}"))
            .Or(Seq(Kind(K.Name), Division).Select(static (name, division) => $"{name.Text} {division}"));

    /// <summary>
    /// After a failure at the second slash of <c>/a/</c>, a choice whose second alternative keeps operator mode and fails without consuming.
    /// </summary>
    public static readonly Parser<Lexeme, string> ChangeKeptThenUndone =
        OneOf(SlashNameNumber, _operator.Then(Kind(K.Number)).Select(static number => number.Text), _semi);

    /// <summary>
    /// After a failure at the second slash of <c>/a/</c>, a choice whose second alternative changes to operand mode and fails without consuming.
    /// </summary>
    public static readonly Parser<Lexeme, string> ChangeMadeThenUndone = OneOf(SlashNameNumber, OperandNumber, _semi);

    /// <summary>
    /// A number, or an <c>Atomic</c> that changes to operand mode, reads a regex and fails at the next token.
    /// </summary>
    public static readonly Parser<Lexeme, string> FailureAfterAChange =
        Kind(K.Number).Select(static number => number.Text)
            .Or(Atomic(Seq(_operand, Kind(K.Regex), Kind(K.Number))).Select(static parts => parts.Item3.Text));

    /// <summary>
    /// An <c>Atomic</c> around a successful <c>Longest</c> whose winner changes the mode and whose loser fails further on, and a number that fails, then a semicolon.
    /// </summary>
    public static readonly Parser<Lexeme, string> LongestThenUndo =
        Atomic(
            Seq(
                Longest(
                    Seq(Kind(K.Slash), Kind(K.Number)).Select(static (slash, number) => slash.Text + number.Text),
                    Seq(_operand, Kind(K.Regex), Kind(K.Name)).Select(static (_, regex, name) => regex.Text + name.Text)
                ),
                Kind(K.Number)
            ).Select(static (longest, _) => longest)
        ).Or(_semi);

    /// <summary>
    /// A <c>Longest</c> whose winner keeps operator mode and whose shorter alternative, tried after it, changes to operand mode.
    /// </summary>
    public static readonly Parser<Lexeme, string> LongestWinnerFirst = Longest(SlashName, OperandRegex).Before(Kind(K.Slash));

    /// <summary>
    /// A <c>Longest</c> whose first alternative changes to operand mode and loses to the second.
    /// </summary>
    public static readonly Parser<Lexeme, string> LongestWinnerSecond = Longest(OperandRegex, SlashName).Before(Kind(K.Slash));

    /// <summary>
    /// An <c>Atomic</c> that changes to operand mode and reads a regex, then a <c>Named</c> whose choice fails at the next token, then a semicolon.
    /// </summary>
    public static readonly Parser<Lexeme, string> NamedThenUndo =
        Atomic(Seq(_operand, Kind(K.Regex), OneOf(Kind(K.Number), Kind(K.Semi)).Named("tail"))).Select(static parts => parts.Item3.Text).Or(_semi);

    /// <summary>
    /// After a failure at the second slash of <c>/a/</c>, a <c>Not</c> around an <c>Atomic</c> that fails further on and then a change to operand mode, then a semicolon.
    /// </summary>
    public static readonly Parser<Lexeme, string> NotAroundAChange =
        Seq(
            SlashNameNumber.Optional(),
            Not(Seq(Atomic(Seq(Kind(K.Slash), Kind(K.Name), Kind(K.Slash), Kind(K.Number))).Optional(), _operand, Kind(K.Number))),
            _semi
        ).Select(static (_, _, semi) => semi);

    /// <summary>
    /// The code sample in docs/concepts/mode-channels.md § Going back. Keep the two in step.
    /// </summary>
    public static readonly Parser<Lexeme, Lexeme> NumberOrSlash =
        SetMode(_modes, Mode.Operand).Then(Kind(K.Number)).Or(Kind(K.Slash));

    /// <summary>
    /// A push of operand mode, then a pop that fails to read a number in operator mode, or a regex.
    /// </summary>
    public static readonly Parser<Lexeme, string> PopUndone =
        PushMode(_modes, Mode.Operand).Then(PopMode(_modes).Then(Kind(K.Number)).Or(Kind(K.Regex))).Select(static lexeme => lexeme.Text);

    /// <summary>
    /// A choice between a change to operand mode and a regex, which has no dispatch keys, and a slash.
    /// </summary>
    public static readonly Parser<Lexeme, Lexeme> RegexOrSlash = OneOf(_operand.Then(Kind(K.Regex)), Kind(K.Slash));

    /// <summary>
    /// An <c>Atomic</c> around a filter that rejects a regex read in operand mode, after a number that fails at the next token, then a semicolon.
    /// </summary>
    public static readonly Parser<Lexeme, string> RejectionThenUndo =
        Atomic(Seq(_operand, Kind(K.Regex), Kind(K.Number).Optional()).Where(static _ => false)).Select(static parts => parts.Item2.Text).Or(_semi);

    /// <summary>
    /// An <c>Atomic</c> that changes to operand mode, reads a regex, pushes operator mode and fails at the next token, or a <see cref="Division"/>.
    /// </summary>
    public static readonly Parser<Lexeme, string> TwoChangesUndone =
        Atomic(Seq(_operand, Kind(K.Regex), PushMode(_modes, Mode.Operator), Kind(K.Number))).Select(static parts => parts.Item4.Text).Or(Division);

    /// <summary>
    /// A change of operator mode to itself and a number, which fails without consuming, then a <see cref="Division"/>.
    /// </summary>
    public static readonly Parser<Lexeme, string> UndoKeepingTheMode = _operator.Then(Kind(K.Number)).Select(static number => number.Text).Or(Return("")).Then(Division);

    /// <summary>
    /// An <c>Atomic</c> around an attempt that changes to operand mode and is undone, then a slash and a number, which fails at the name after the slash, or a <see cref="Division"/>.
    /// </summary>
    public static readonly Parser<Lexeme, string> UndoneThenRewound =
        Atomic(Seq(OperandNumber.Or(Return("")), Kind(K.Slash), Kind(K.Number))).Select(static parts => parts.Item3.Text).Or(Division);

    /// <summary>
    /// <see cref="NotAroundAChange"/> without the <c>Not</c>.
    /// </summary>
    public static readonly Parser<Lexeme, string> WithoutTheNot = SlashNameNumber.Optional().Then(_semi);

    /// <summary>
    /// An attempt that changes to operand mode and then fails without consuming on <c>/a/</c>, or a <c>Lookahead</c> or <c>Not</c> around one, so that going back undoes the change.
    /// </summary>
    public static Parser<Lexeme, string> Attempt(string name) =>
        name switch
        {
            "Bind" => Return(0).Bind(static _ => OperandNumber).Or(Return("")),
            "Lookahead" => Lookahead(OperandRegex),
            "Many" => OperandNumber.Many().Select(static _ => ""),
            "Not" => Not(OperandNumber).Select(static _ => ""),
            "Optional" => OperandNumber.Optional().Select(static _ => ""),
            "Or" => OperandNumber.Or(Return("")),
            _ => PushMode(_modes, Mode.Operand).Then(Kind(K.Number)).Before(PopMode(_modes)).Select(static number => number.Text).Or(Return("")),
        };
}

/// <summary>
/// Runs the parsers in a pipeline over text in memory, and again over buffers of one item to a segment that overwrite what they release and discard, and checks that both runs agree.
/// </summary>
file static class Rollback
{
    /// <summary>
    /// The outcome of <paramref name="parser"/> on <paramref name="text"/> with every choice that has a keyed alternative dispatched, after checking that it does dispatch.
    /// </summary>
    public static (bool Ok, T Value, ParseError? Error) Dispatched<T>(Parser<Lexeme, T> parser, string text)
    {
        Assert.IsType<DispatchPlan<Lexeme, T, FullMark>>(new PlanBuilder(LoweringOptions.All, dispatchThreshold: 1).Lower(parser));

        return LayerRun.Parse(Lexers.Lexer, parser, text, builder: new PlanBuilder(LoweringOptions.All, dispatchThreshold: 1));
    }

    public static ParseError Error<T>(Parser<Lexeme, T> parser, string text)
    {
        var failed = Assert.IsType<Failed>(Pipeline.From(Lexers.Lexer).Then(parser).Parse(text).Value);
        var streamed = LayerRun.Parse(Lexers.Lexer, parser, text, segmentSize: 1);

        Assert.False(streamed.Ok);
        Assert.Equal(failed.Diagnostics.Single(), streamed.Error);

        return streamed.Error!;
    }

    public static Expectation Expects(K kind) =>
        new ExpectedItem(Display.Kind<Lexeme, K>(kind));

    public static Unexpected Found(K kind, string text) =>
        new UnexpectedItem(Display<Lexeme>.Item(new Lexeme(kind, text)));

    /// <summary>
    /// The value of <paramref name="parser"/> on <paramref name="text"/>, which both runs must agree on, and each token the run in memory lexed, with the mode it read it in.
    /// </summary>
    public static (string Value, List<string> Lexed) Run(Parser<Lexeme, string> parser, string text)
    {
        var lexed = new List<string>();
        var parsed = Assert.IsType<Parsed<string>>(Pipeline.From(Lexers.Recording(lexed)).Then(parser).Parse(text).Value);

        Assert.Equal((true, parsed.Value, null), LayerRun.Parse(Lexers.Lexer, parser, text, segmentSize: 1));

        return (parsed.Value, lexed);
    }

    public static SourceSpan SpanOf(long start, long end) =>
        new(SourceId.Unnamed, start, end);

    /// <summary>
    /// The outcome of <paramref name="parser"/> on <paramref name="text"/> with no choice dispatched.
    /// </summary>
    public static (bool Ok, T Value, ParseError? Error) Undispatched<T>(Parser<Lexeme, T> parser, string text) =>
        LayerRun.Parse(Lexers.Lexer, parser, text, builder: new PlanBuilder(LoweringOptions.All & ~LoweringOptions.Dispatch));
}

/// <summary>
/// The rollback rule of § 3. Mode channels: going back to a mark undoes every mode change made since, newest first, and an undo that changes the mode on top back discards the tokens read in the undone mode and forgets the failures past it.
/// </summary>
public class ModeRollbackTests
{
    public static TheoryData<string> Attempts => ["Bind", "Lookahead", "Many", "Not", "Optional", "Or", "PushMode"];

    [Fact]
    public void AChangeAlreadyUndoneIsNotUndoneAgainByAnEnclosingRewind()
    {
        var (value, lexed) = Rollback.Run(Parsers.UndoneThenRewound, "/a/");

        Assert.Equal("/ a /", value);
        Assert.Equal(["Operand /a/", "Operator /", "Operator a", "Operator /"], lexed);
    }

    [Fact]
    public void AFailureAfterAnUndoneChangeExpectsNothingUnlessNamedLabelsIt()
    {
        var error = Rollback.Error(Parsers.FailureAfterAChange, "/a/ x");
        var named = Rollback.Error(Parsers.FailureAfterAChange.Named("operand"), "/a/ x");

        Assert.Equal(Rollback.SpanOf(0, 1), error.Span);
        Assert.Equal(Rollback.Found(K.Slash, "/"), error.Unexpected);
        Assert.Empty(error.Expected);
        Assert.Equal(error with { Expected = [new Label("operand")] }, named);
    }

    [Fact]
    public void AGoingBackUndoesEveryChangeSinceItsMarkNewestFirst()
    {
        var (value, lexed) = Rollback.Run(Parsers.TwoChangesUndone, "/a/ x");

        Assert.Equal("/ a /", value);
        Assert.Equal(["Operand /a/", "Operator x", "Operator /", "Operator a", "Operator /"], lexed);
    }

    [Fact]
    public void ALongestAlternativeThatChangesTheModeAndLosesLeavesTheWinnersMode()
    {
        var (value, lexed) = Rollback.Run(Parsers.LongestWinnerFirst, "/a/");

        Assert.Equal("/ a", value);
        Assert.Equal(["Operator /", "Operator a", "Operand /a/", "Operator /", "Operator a", "Operator /"], lexed);
    }

    [Fact]
    public void ALongestUndoesTheChangesOfEachAlternativeBeforeTheNext()
    {
        var (value, lexed) = Rollback.Run(Parsers.LongestWinnerSecond, "/a/");

        Assert.Equal("/ a", value);
        Assert.Equal(["Operand /a/", "Operator /", "Operator a", "Operator /"], lexed);
    }

    [Fact]
    public void AnAlternativeThatChangesTheModeAndConsumesNothingLeavesTheNextOneTheOldMode()
    {
        var (value, lexed) = Rollback.Run(Parsers.NumberOrSlash.Select(static slash => slash.Text), "/x/");

        Assert.Equal("/", value);
        Assert.Equal(["Operand /x/", "Operator /"], lexed);
    }

    [Fact]
    public void AnAtomicThatRewindsUndoesTheChangeItMadeAfterConsuming()
    {
        var (value, lexed) = Rollback.Run(Parsers.AtomicAroundAChange, "x /a/");

        Assert.Equal("x / a /", value);
        Assert.Equal(["Operator x", "Operand /a/", "Operator /", "Operator a", "Operator /"], lexed);
    }

    [Theory]
    [MemberData(nameof(Attempts))]
    public void AnAttemptThatFailsWithoutConsumingUndoesItsChange(string attempt)
    {
        var (value, lexed) = Rollback.Run(Parsers.Attempt(attempt).Then(Parsers.Division), "/a/");

        Assert.Equal("/ a /", value);
        Assert.Equal(["Operand /a/", "Operator /", "Operator a", "Operator /"], lexed);
    }

    [Fact]
    public void AnUndoAfterAFilterRejectedReportsTheNextAlternative()
    {
        var error = Rollback.Error(Parsers.RejectionThenUndo, "/a/ x");

        Assert.Equal(Rollback.SpanOf(0, 1), error.Span);
        Assert.Equal(Rollback.Found(K.Slash, "/"), error.Unexpected);
        Assert.Equal([Rollback.Expects(K.Semi)], error.Expected);
    }

    [Fact]
    public void AnUndoAfterASuccessfulLongestReportsTheNextAlternative()
    {
        var error = Rollback.Error(Parsers.LongestThenUndo, "/a/ x");

        Assert.Equal(Rollback.SpanOf(0, 1), error.Span);
        Assert.Equal(Rollback.Found(K.Slash, "/"), error.Unexpected);
        Assert.Equal([Rollback.Expects(K.Semi)], error.Expected);
    }

    [Fact]
    public void AnUndoAfterNamedRelabelledReportsTheNextAlternative()
    {
        var error = Rollback.Error(Parsers.NamedThenUndo, "/a/ x");

        Assert.Equal(Rollback.SpanOf(0, 1), error.Span);
        Assert.Equal(Rollback.Found(K.Slash, "/"), error.Unexpected);
        Assert.Equal([Rollback.Expects(K.Semi)], error.Expected);
    }

    [Fact]
    public void AnUndoneChangeLeavesTheFailuresPastItForgotten()
    {
        var error = Rollback.Error(Parsers.ChangeMadeThenUndone, "/a/");

        Assert.Equal(Rollback.SpanOf(0, 1), error.Span);
        Assert.Equal(Rollback.Found(K.Slash, "/"), error.Unexpected);
        Assert.Equal([Rollback.Expects(K.Number), Rollback.Expects(K.Semi)], error.Expected);
    }

    [Fact]
    public void AnUndoThatKeepsTheModeReadsNothingAgainAndForgetsNoFailure()
    {
        var (value, lexed) = Rollback.Run(Parsers.UndoKeepingTheMode, "/a/");

        Assert.Equal("/ a /", value);
        Assert.Equal(["Operator /", "Operator a", "Operator /"], lexed);

        var error = Rollback.Error(Parsers.ChangeKeptThenUndone, "/a/");

        Assert.Equal(Rollback.SpanOf(2, 3), error.Span);
        Assert.Equal(Rollback.Found(K.Slash, "/"), error.Unexpected);
        Assert.Equal([Rollback.Expects(K.Number)], error.Expected);
    }

    [Fact]
    public void APopThatIsUndoneLeavesTheModeItPopped()
    {
        var (value, lexed) = Rollback.Run(Parsers.PopUndone, "/a/");

        Assert.Equal("/a/", value);
        Assert.Equal(["Operator /", "Operand /a/"], lexed);
    }

    [Fact]
    public void DispatchGivesTheSameErrorWhenAnAlternativeChangesTheMode()
    {
        var with = Rollback.Dispatched(Parsers.RegexOrSlash, "x");

        Assert.Equal(Rollback.Undispatched(Parsers.RegexOrSlash, "x"), with);
        Assert.False(with.Ok);
        Assert.Equal([Rollback.Expects(K.Regex), Rollback.Expects(K.Slash)], with.Error!.Expected);
    }

    [Fact]
    public void DispatchGivesTheSameResultWhenAnAlternativeChangesTheMode()
    {
        var with = Rollback.Dispatched(Parsers.RegexOrSlash, "/x/");

        Assert.Equal(Rollback.Undispatched(Parsers.RegexOrSlash, "/x/"), with);
        Assert.Equal((true, new Lexeme(K.Regex, "/x/"), null), with);
    }

    [Fact]
    public void NotAroundAChangeLeavesTheFailureAsItFoundIt()
    {
        var error = Rollback.Error(Parsers.NotAroundAChange, "/a/");

        Assert.Equal(Rollback.SpanOf(2, 3), error.Span);
        Assert.Equal(Rollback.Found(K.Slash, "/"), error.Unexpected);
        Assert.Equal([Rollback.Expects(K.Number)], error.Expected);
        Assert.Equal(Rollback.Error(Parsers.WithoutTheNot, "/a/"), error);
    }
}
