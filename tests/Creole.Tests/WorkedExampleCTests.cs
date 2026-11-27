using static Creole.CharGrammar;

namespace Creole.Tests;

file enum LexMode
{
    Operand,
    Operator,
}

file enum K
{
    Name,
    Number,
    If,
    LParen,
    RParen,
    Dot,
    Semi,
    Slash,
    SlashEquals,
    Regex,
}

file readonly record struct Tok(K Kind, string Text) : IKinded<K>;

/// <summary>
/// The lexer of the whitepaper's Example C: in operand mode <c>/</c> starts a regex, and in operator mode it divides.
/// </summary>
file class ScriptLexer : CharGrammar
{
    public static readonly ModeChannel<LexMode> Modes = new(LexMode.Operand);

    private static readonly Parser<char, Tok> _common = OneOf(
        Letter.AtLeastOnce().Text().Select(static text => new Tok(text == "if" ? K.If : K.Name, text)),
        Digit.AtLeastOnce().Text().Select(static text => new Tok(K.Number, text)),
        Char('(').As(new Tok(K.LParen, "(")),
        Char(')').As(new Tok(K.RParen, ")")),
        Char('.').As(new Tok(K.Dot, ".")),
        Char(';').As(new Tok(K.Semi, ";"))
    );

    private static readonly Parser<char, Tok> _regex = Char('/').Then(NoneOf('/').SkipMany()).Then(Char('/')).Text().Select(static text => new Tok(K.Regex, text));
    private static readonly Parser<char, Tok> _slash = Char('/').As(new Tok(K.Slash, "/"));
    private static readonly Parser<char, Tok> _slashEquals = String("/=").As(new Tok(K.SlashEquals, "/="));

    public static readonly Layer<char, Tok> Lexer = Modal(static (_, token) => token);

    /// <summary>
    /// The same lexer, adding each token it produces to <paramref name="lexed"/> with the mode it read it in.
    /// </summary>
    public static Layer<char, Tok> Recording(List<string> lexed) =>
        Modal((mode, token) =>
        {
            lexed.Add($"{mode} {token.Text}");
            return token;
        });

    private static Layer<char, Tok> Modal(Func<LexMode, Tok, Tok> lexed) =>
        Layer.Modal(
            Modes,
            mode =>
                (mode switch
                {
                    LexMode.Operand => Longest(_common, _regex),
                    _ => Longest(_common, _slash, _slashEquals),
                }).Select(token => lexed(mode, token)),
            Whitespace.SkipMany()
        );
}

/// <summary>
/// The parser of the whitepaper's Example C, with <c>Primary</c>, <c>Exp</c> and <c>Statement</c> filled in.
/// </summary>
/// <remarks>
/// A primary is a name, a number, a regex or a parenthesized expression, and an operand is a primary followed by any number of calls such as <c>.test(s)</c>.
/// The calls come after the operand sets operator mode, so the parser looks for a <c>.</c> in the mode that reads what follows an operand.
/// Read in operand mode, the <c>/ c;</c> of <c>a / b / c;</c> would be an unterminated regex, which ends the parse: re-reading can replace only a token that was read.
/// Each value is the source it parsed, with single spaces.
/// </remarks>
file class ScriptGrammar : TokenGrammar<Tok, K>
{
    private static readonly ModeChannel<LexMode> _modes = ScriptLexer.Modes;
    private static readonly Parser<Tok, string> _expression = Rec(() => Exp);
    private static readonly Parser<Tok, string> _statement = Rec(() => Statement);
    private static readonly Parser<Tok, string> _parenthesized = _expression.Between(Kind(K.LParen), Kind(K.RParen));

    private static readonly Parser<Tok, string> _call =
        Seq(Kind(K.Dot), Kind(K.Name), _parenthesized).Select(static (_, name, argument) => $".{name.Text}({argument})");

    public static readonly Parser<Tok, string> Primary = OneOf(
        Kind(K.Name).Select(static name => name.Text),
        Kind(K.Number).Select(static number => number.Text),
        Kind(K.Regex).Select(static regex => regex.Text),
        _parenthesized.Select(static inner => $"({inner})")
    );

    public static readonly Parser<Tok, string> Operand =
        Seq(Primary.Between(SetMode(_modes, LexMode.Operand), SetMode(_modes, LexMode.Operator)), _call.Many())
            .Select(static (primary, calls) => primary + string.Concat(calls));

    public static readonly Parser<Tok, string> Exp =
        Expression.Build(Operand, static op => [[op.InfixL(Kind(K.Slash), static (left, _, right) => $"{left} / {right}")]]);

    public static readonly Parser<Tok, string> IfStmt =
        Seq(Kind(K.If), Exp.Between(Kind(K.LParen), Kind(K.RParen)), SetMode(_modes, LexMode.Operand), _statement)
            .Select(static (_, condition, _, body) => $"if ({condition}) {body}");

    public static readonly Parser<Tok, string> Statement =
        IfStmt.Or(Exp.Before(Kind(K.Semi)).Select(static expression => $"{expression};"));

    public static readonly Parser<Tok, string> Program =
        Statement.Many().Before(End).Select(static statements => string.Join(" ", statements));
}

/// <summary>
/// A parser that consumes nothing and reports how many entries the mode log of the layer running it holds.
/// </summary>
file class LogSize(Action<int> seen) : Parser<Tok, Unit>
{
    private readonly Action<int> _seen = seen;

    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.Return;

    internal override Plan<Tok, Unit> Lower(PlanBuilder builder) => new LogSizePlan(_seen);
}

/// <summary>
/// The plan of <see cref="LogSize"/>.
/// </summary>
file class LogSizePlan(Action<int> seen) : Plan<Tok, Unit>
{
    private readonly Action<int> _seen = seen;

    public override bool TryParse(ref Cursor<Tok> cursor, ref Unit result)
    {
        _seen(cursor.Modes?.Count ?? 0);
        return true;
    }
}

/// <summary>
/// Grammars over Example C's tokens whose errors show what a change of mode does to them. Each first reads the statement <c>x;</c>, which leaves the lexer in operator mode.
/// </summary>
file class ErrorCases : TokenGrammar<Tok, K>
{
    private static readonly ModeChannel<LexMode> _modes = ScriptLexer.Modes;

    /// <summary>
    /// After <c>x;</c>, a <c>Context</c> around an operand followed by a number.
    /// </summary>
    public static readonly Parser<Tok, string> OperandInContext =
        ScriptGrammar.Statement.Then(ScriptGrammar.IfStmt.Or(ScriptGrammar.Operand.Before(Kind(K.Number)).Context("in operand")));

    /// <summary>
    /// A filter that rejects a whole document of statements, after an <c>Atomic</c> that reads every name and <c>;</c> in it and fails at its end.
    /// </summary>
    public static readonly Parser<Tok, string> RejectedDocument =
        Atomic(OneOf(Kind(K.Name), Kind(K.Semi)).Many().Then(Kind(K.If))).Optional()
            .Then(ScriptGrammar.Statement.Many().Where(static _ => false))
            .Select(static _ => "");

    /// <summary>
    /// After <c>x;</c>, a look at the next token in operator mode, which records no failure, and then a filter that rejects every operand.
    /// </summary>
    public static readonly Parser<Tok, string> RejectedOperand =
        ScriptGrammar.Statement.Then(Lookahead(Any)).Then(ScriptGrammar.Operand.Where(static _ => false));

    /// <summary>
    /// After <c>x;</c>, an <c>Atomic</c> that reads a slash, a name and a slash in operator mode and fails at the next token, then a change to operand mode where it started, and a regex and a number.
    /// </summary>
    public static readonly Parser<Tok, string> ReadAheadThenChange =
        ScriptGrammar.Statement.Then(
            Atomic(Seq(Kind(K.Slash), Kind(K.Name), Kind(K.Slash), Kind(K.Number)).Select(static (_, _, _, _) => ""))
                .Or(Seq(SetMode(_modes, LexMode.Operand), Kind(K.Regex), Kind(K.Number)).Select(static (_, _, _) => ""))
        );

    /// <summary>
    /// After <c>x;</c>, an <c>Atomic</c> around a filter that rejects a slash and a name read in operator mode, then a change to operand mode where it started, and a number.
    /// </summary>
    public static readonly Parser<Tok, string> RejectThenChange =
        ScriptGrammar.Statement.Then(
            Atomic(Seq(Kind(K.Slash), Kind(K.Name)).Where(static _ => false, "rejected").Select(static _ => ""))
                .Or(SetMode(_modes, LexMode.Operand).Then(Kind(K.Number)).Select(static _ => ""))
        );
}

/// <summary>
/// Runs Example C's grammars as a pipeline over text in memory, and again over buffers of one item to a segment that overwrite what they release and discard, and checks that both runs agree.
/// </summary>
file static class Script
{
    public static ParseError Error<T>(Parser<Tok, T> parser, string text) =>
        Error(parser, text, out _);

    /// <summary>
    /// The error of <paramref name="parser"/> on <paramref name="text"/>, which both runs must agree on, and the first token the streamed run still held when it ended.
    /// </summary>
    public static ParseError Error<T>(Parser<Tok, T> parser, string text, out long held)
    {
        var failed = Assert.IsType<Failed>(Pipeline.From(ScriptLexer.Lexer).Then(parser).Parse(text).Value);
        var start = 0L;
        var streamed = LayerRun.Parse(ScriptLexer.Lexer, parser, text, segmentSize: 1, released: position => start = position);

        Assert.False(streamed.Ok);
        Assert.Equal(failed.Diagnostics.Single(), streamed.Error);

        held = start;

        return streamed.Error!;
    }

    public static string Found(K kind, string text) =>
        Display<Tok>.Item(new Tok(kind, text));

    /// <summary>
    /// The most entries the parser's mode log holds after any statement of <paramref name="statements"/>, parsed over buffers of 16 items to a segment.
    /// </summary>
    public static int PeakLogSize(int statements)
    {
        var peak = 0;
        var parser = ScriptGrammar.Statement.Before(new LogSize(size => peak = Math.Max(peak, size))).SkipMany();
        var text = string.Concat(Enumerable.Repeat("x; /re/.test(s); ", statements / 2));

        Assert.True(LayerRun.Parse(ScriptLexer.Lexer, parser, text, segmentSize: 16).Ok);

        return peak;
    }

    public static SourceSpan SpanOf(long start, long end) =>
        new(SourceId.Unnamed, start, end);

    public static string Value(Parser<Tok, string> parser, string text)
    {
        var parsed = Assert.IsType<Parsed<string>>(Pipeline.From(ScriptLexer.Lexer).Then(parser).Parse(text).Value);
        var streamed = LayerRun.Parse(ScriptLexer.Lexer, parser, text, segmentSize: 1);

        Assert.Equal((true, parsed.Value, null), streamed);

        return parsed.Value;
    }
}

/// <summary>
/// The whitepaper's Example C, a parser that steers its lexer: <c>/</c> divides after an operand and starts a regex elsewhere, and a token the lexer read ahead in the wrong mode is read again.
/// </summary>
public class WorkedExampleCTests
{
    [Fact]
    public void AChangeAfterReadingAheadForgetsTheFailurePastIt()
    {
        var error = Script.Error(ErrorCases.ReadAheadThenChange, "x; /a/ b");

        Assert.Equal(Script.SpanOf(7, 8), error.Span);
        Assert.Equal(new UnexpectedItem(Script.Found(K.Name, "b")), error.Unexpected);
        Assert.Single(error.Expected);
    }

    [Fact]
    public void AContextAroundAnOperandReportsTheItemLexedInTheNewMode()
    {
        var error = Script.Error(ErrorCases.OperandInContext, "x; /re/.test(s) z", out var held);

        Assert.True(held > 2);
        Assert.Equal(Script.SpanOf(16, 17), error.Span);
        Assert.Equal(new UnexpectedItem(Script.Found(K.Name, "z")), error.Unexpected);
        Assert.Equal([new ContextLine("in operand", Script.SpanOf(3, 7))], error.Context);
    }

    [Fact]
    public void AFilterAroundADocumentReportsTheFirstChangeInItAfterTheBufferReleasedThatItem()
    {
        var error = Script.Error(ErrorCases.RejectedDocument, string.Concat(Enumerable.Repeat("x; ", 100)));

        Assert.Equal(Script.SpanOf(1, 2), error.Span);
        Assert.Equal(new UnexpectedItem(Script.Found(K.Semi, ";")), error.Unexpected);
        Assert.Empty(error.Expected);
    }

    [Fact]
    public void AFilterAroundAnOperandReportsTheItemLexedInTheNewMode()
    {
        var error = Script.Error(ErrorCases.RejectedOperand, "x; /re/.test(s)", out var held);

        Assert.True(held > 2);
        Assert.Equal(Script.SpanOf(3, 15), error.Span);
        Assert.Equal(new UnexpectedItem(Script.Found(K.Regex, "/re/")), error.Unexpected);
    }

    [Fact]
    public void ARejectionWhoseSpanCrossedALaterChangeNeitherWidensTheErrorNorGivesItsMessage()
    {
        var error = Script.Error(ErrorCases.RejectThenChange, "x; /a/");

        Assert.Equal(Script.SpanOf(3, 6), error.Span);
        Assert.Equal(new UnexpectedItem(Script.Found(K.Regex, "/a/")), error.Unexpected);
        Assert.Single(error.Expected);
    }

    [Fact]
    public void EachTokenReadInTheWrongModeIsReadAgainInTheNewOne()
    {
        var lexed = new List<string>();

        Assert.Equal("x; /re/.test(s);", Assert.IsType<Parsed<string>>(Pipeline.From(ScriptLexer.Recording(lexed)).Then(ScriptGrammar.Program).Parse("x; /re/.test(s);").Value).Value);
        Assert.Equal(
            ["Operand x", "Operator ;", "Operator /", "Operand /re/", "Operator .", "Operator test", "Operator (", "Operand s", "Operator )", "Operator ;"],
            lexed
        );
    }

    [Fact]
    public void TheModeLogHoldsNoMoreEntriesOverALongerDocument()
    {
        var shorter = Script.PeakLogSize(1_000);

        Assert.InRange(shorter, 1, 64);
        Assert.Equal(shorter, Script.PeakLogSize(100_000));
    }

    [Theory]
    [InlineData("(a) / 2;")]
    [InlineData("if (ok) /x/.test(s);")]
    [InlineData("x; /re/.test(s);")]
    [InlineData("a / b / c;")]
    [InlineData("if (a / b) /c/;")]
    public void TheLexerReadsSlashAsTheParserExpectsIt(string text) =>
        Assert.Equal(text, Script.Value(ScriptGrammar.Program, text));
}
