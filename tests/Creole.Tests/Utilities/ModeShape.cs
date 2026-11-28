namespace Creole.Tests;

/// <summary>
/// The modes of <see cref="ModeShapes.Lexer"/>: one character to a token, or a run of the same character to a token.
/// </summary>
internal enum RunMode
{
    Single,
    Runs,
}

/// <summary>
/// The kinds of token <see cref="ModeShapes.Lexer"/> produces: a run of <c>a</c> or a run of <c>b</c>, of one character in <see cref="RunMode.Single"/>.
/// </summary>
internal enum RunKind
{
    A,
    B,
}

internal readonly record struct RunToken(RunKind Kind, string Text) : IKinded<RunKind>;

/// <summary>
/// The item parsers of <see cref="ModeShapes.Lexer"/>.
/// </summary>
file class RunLexer : CharGrammar
{
    public static readonly Parser<char, RunToken> Runs = OneOf(
        Char('a').AtLeastOnce().Text().Select(static text => new RunToken(RunKind.A, text)),
        Char('b').AtLeastOnce().Text().Select(static text => new RunToken(RunKind.B, text))
    );

    public static readonly Parser<char, RunToken> Single = OneOf(
        Char('a').As(new RunToken(RunKind.A, "a")),
        Char('b').As(new RunToken(RunKind.B, "b"))
    );
}

/// <summary>
/// A grammar over <see cref="RunToken"/> that changes the mode of <see cref="ModeShapes.Modes"/>, for <see cref="ModeModel"/>. Every shape produces a string.
/// </summary>
internal abstract record ModeShape
{
    /// <summary>
    /// Produces its item's value.
    /// </summary>
    public record Atomic(ModeShape Item) : ModeShape;

    /// <summary>
    /// Produces its item's value, without consuming.
    /// </summary>
    public record Lookahead(ModeShape Item) : ModeShape;

    /// <summary>
    /// Produces the value of the alternative that consumed the most tokens, the first on a tie.
    /// </summary>
    public record Longest(ModeShape First, ModeShape Second) : ModeShape;

    /// <summary>
    /// Produces its items' values joined. Its item never succeeds without consuming.
    /// </summary>
    public record Many(ModeShape Item) : ModeShape;

    /// <summary>
    /// Produces the empty string.
    /// </summary>
    public record Not(ModeShape Item) : ModeShape;

    /// <summary>
    /// Produces its item's value, or the empty string.
    /// </summary>
    public record Optional(ModeShape Item) : ModeShape;

    /// <summary>
    /// Produces the value of the alternative that succeeded.
    /// </summary>
    public record Or(ModeShape First, ModeShape Second) : ModeShape;

    /// <summary>
    /// Pushes <paramref name="Mode"/>, runs its item and pops the mode, as <c>PushMode(m).Then(item).Before(PopMode())</c> does. Produces its item's value.
    /// </summary>
    public record Push(RunMode Mode, ModeShape Item) : ModeShape;

    /// <summary>
    /// Produces its parts' values joined.
    /// </summary>
    public record Seq(ModeShape First, ModeShape Second) : ModeShape;

    /// <summary>
    /// Replaces the mode on top of the stack with <paramref name="Mode"/>. Produces the empty string.
    /// </summary>
    public record Set(RunMode Mode) : ModeShape;

    /// <summary>
    /// One token of <paramref name="Kind"/>. Produces its text in brackets, so that <c>[aa]</c>, read in <see cref="RunMode.Runs"/>, differs from <c>[a][a]</c>.
    /// </summary>
    public record Tok(RunKind Kind) : ModeShape;
}

/// <summary>
/// Builds <see cref="ModeShape"/> grammars as Creole parsers over the tokens of <see cref="Lexer"/>, which they steer.
/// </summary>
internal static class ModeShapes
{
    public static readonly ModeChannel<RunMode> Modes = new(RunMode.Single);

    /// <summary>
    /// A lexer over <c>a</c> and <c>b</c> that reads one character to a token in <see cref="RunMode.Single"/>, its initial mode, and a run of the same character to a token in <see cref="RunMode.Runs"/>.
    /// </summary>
    /// <remarks>
    /// So a token read in the wrong mode shows in the parser's value and in where the tokens after it start.
    /// </remarks>
    public static readonly Layer<char, RunToken> Lexer = Layer.Modal(
        Modes,
        static mode =>
            mode == RunMode.Runs
                ? RunLexer.Runs
                : RunLexer.Single
    );

    public static Parser<RunToken, string> Build(ModeShape shape) => shape switch
    {
        ModeShape.Atomic atomic => Grammar<RunToken>.Atomic(Build(atomic.Item)),
        ModeShape.Lookahead lookahead => Grammar<RunToken>.Lookahead(Build(lookahead.Item)),
        ModeShape.Longest longest => Grammar<RunToken>.Longest(Build(longest.First), Build(longest.Second)),
        ModeShape.Many many => Build(many.Item).Many().Select(string.Concat),
        ModeShape.Not not => Grammar<RunToken>.Not(Build(not.Item)).As(""),
        ModeShape.Optional optional => Build(optional.Item).Optional().Select(static value => value.GetValueOrDefault("")),
        ModeShape.Or or => Build(or.First).Or(Build(or.Second)),
        ModeShape.Push push => Grammar<RunToken>.PushMode(Modes, push.Mode).Then(Build(push.Item)).Before(Grammar<RunToken>.PopMode(Modes)),
        ModeShape.Seq seq => Grammar<RunToken>.Seq(Build(seq.First), Build(seq.Second)).Select(static (first, second) => first + second),
        ModeShape.Set set => Grammar<RunToken>.SetMode(Modes, set.Mode).As(""),
        ModeShape.Tok tok => TokenGrammar<RunToken, RunKind>.Kind(tok.Kind).Select(static token => $"[{token.Text}]"),
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
    };

    public static string Describe(ModeShape shape) => shape switch
    {
        ModeShape.Atomic atomic => $"Atomic({Describe(atomic.Item)})",
        ModeShape.Lookahead lookahead => $"Lookahead({Describe(lookahead.Item)})",
        ModeShape.Longest longest => $"Longest({Describe(longest.First)}, {Describe(longest.Second)})",
        ModeShape.Many many => $"Many({Describe(many.Item)})",
        ModeShape.Not not => $"Not({Describe(not.Item)})",
        ModeShape.Optional optional => $"Optional({Describe(optional.Item)})",
        ModeShape.Or or => $"Or({Describe(or.First)}, {Describe(or.Second)})",
        ModeShape.Push push => $"Push({push.Mode}, {Describe(push.Item)})",
        ModeShape.Seq seq => $"Seq({Describe(seq.First)}, {Describe(seq.Second)})",
        ModeShape.Set set => $"Set({set.Mode})",
        ModeShape.Tok tok => tok.Kind.ToString(),
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
    };

    /// <summary>
    /// Whether <paramref name="shape"/> can succeed without consuming a token, so that it may not be the item of <see cref="ModeShape.Many"/>.
    /// </summary>
    public static bool Nullable(ModeShape shape) => shape switch
    {
        ModeShape.Atomic atomic => Nullable(atomic.Item),
        ModeShape.Longest longest => Nullable(longest.First) || Nullable(longest.Second),
        ModeShape.Or or => Nullable(or.First) || Nullable(or.Second),
        ModeShape.Push push => Nullable(push.Item),
        ModeShape.Seq seq => Nullable(seq.First) && Nullable(seq.Second),
        ModeShape.Tok => false,
        _ => true,
    };
}
