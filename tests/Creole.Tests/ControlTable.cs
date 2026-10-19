using static Creole.Grammar<char>;

namespace Creole.Tests;

/// <summary>
/// A grammar over characters for one row of <see cref="ControlTable"/>. Every shape produces a string.
/// </summary>
internal abstract record ControlShape
{
    /// <summary>
    /// Produces its items' values joined. Fails if the item does not match at least once.
    /// </summary>
    public record AtLeastOnce(ControlShape Item) : ControlShape;

    /// <summary>
    /// Produces its item's value.
    /// </summary>
    public record Atomic(ControlShape Item) : ControlShape;

    /// <summary>
    /// The characters of <paramref name="Text"/> in order, one at a time, so it fails after consuming if a later character does not match. Produces <paramref name="Text"/>.
    /// </summary>
    public record Chars(string Text) : ControlShape;

    /// <summary>
    /// Produces its item's value.
    /// </summary>
    public record Context(ControlShape Item, string Text) : ControlShape;

    /// <summary>
    /// Produces the empty string.
    /// </summary>
    public record Cut : ControlShape;

    /// <summary>
    /// Produces its item's value.
    /// </summary>
    public record Lookahead(ControlShape Item) : ControlShape;

    /// <summary>
    /// Produces the value of the alternative that consumed the most.
    /// </summary>
    public record Longest(ControlShape First, ControlShape Second) : ControlShape;

    /// <summary>
    /// Produces its items' values joined.
    /// </summary>
    public record Many(ControlShape Item) : ControlShape;

    /// <summary>
    /// Produces its item's value.
    /// </summary>
    public record Named(ControlShape Item, string Label) : ControlShape;

    /// <summary>
    /// Produces the empty string.
    /// </summary>
    public record Not(ControlShape Item) : ControlShape;

    /// <summary>
    /// Produces its item's value, or the empty string.
    /// </summary>
    public record Optional(ControlShape Item) : ControlShape;

    /// <summary>
    /// Produces the value of whichever alternative matched.
    /// </summary>
    public record Or(ControlShape First, ControlShape Second) : ControlShape;

    /// <summary>
    /// Produces its items' values joined; the separators' values are discarded.
    /// </summary>
    public record SepBy(ControlShape Item, ControlShape Separator) : ControlShape;

    /// <summary>
    /// Produces its steps' values joined.
    /// </summary>
    public record Seq(params ControlShape[] Steps) : ControlShape;

    /// <summary>
    /// Produces its items' values joined; the terminator's value is discarded.
    /// </summary>
    public record Until(ControlShape Item, ControlShape Terminator) : ControlShape;
}

internal record ControlCase(string Name, ControlShape Shape, string Input, Outcome Expected);

/// <summary>
/// The control-node semantics as cases: a grammar, an input and the outcome every engine must give.
/// </summary>
internal static class ControlTable
{
    private static readonly ControlShape _a = new ControlShape.Chars("a");
    private static readonly ControlShape _ab = new ControlShape.Chars("ab");
    private static readonly ControlShape _ac = new ControlShape.Chars("ac");
    private static readonly ControlShape _b = new ControlShape.Chars("b");
    private static readonly ControlShape _cut = new ControlShape.Cut();
    private static readonly Outcome _failConsumed = new Outcome.FailConsumed();
    private static readonly Outcome _failEmpty = new Outcome.FailEmpty();
    private static readonly ControlShape _x = new ControlShape.Chars("x");

    public static readonly IReadOnlyList<ControlCase> Cases =
    [
        new("Atomic: success passes through", Atomic(_ab), "abc", Ok("ab", 2)),
        new("Atomic: a failure without consuming passes through", Atomic(_a), "x", _failEmpty),
        new("Atomic: a failure after consuming is rewound", Atomic(_ab), "ac", _failEmpty),
        new("Atomic: lets Or try the next alternative", Or(Atomic(_ab), _ac), "ac", Ok("ac", 2)),
        new("Or: without Atomic, an alternative failing after consuming fails the choice", Or(_ab, _ac), "ac", _failConsumed),

        new("Lookahead: success rewinds and keeps the value", Lookahead(_ab), "ab", Ok("ab", 0)),
        new("Lookahead: a failure without consuming passes through", Lookahead(_a), "x", _failEmpty),
        new("Lookahead: a failure after consuming stays committed", Lookahead(_ab), "ac", _failConsumed),
        new("Lookahead: the next parser starts where it did", Seq(Lookahead(_a), _ab), "ab", Ok("aab", 2)),
        new("Lookahead: Atomic makes a committed failure empty", Atomic(Lookahead(_ab)), "ac", _failEmpty),

        new("Not: succeeds when the parser fails without consuming", Not(_a), "x", Ok("", 0)),
        new("Not: succeeds at the end of input", Not(_a), "", Ok("", 0)),
        new("Not: succeeds and rewinds when the parser fails after consuming", Not(_ab), "ac", Ok("", 0)),
        new("Not: fails without consuming when the parser succeeds", Not(_ab), "ab", _failEmpty),
        new("Not: the next parser starts where it did", Seq(Not(_b), _a), "a", Ok("a", 1)),

        new("Cut: succeeds without consuming", _cut, "x", Ok("", 0)),
        new("Cut: succeeds at the end of input", _cut, "", Ok("", 0)),
        new("Cut: inside Atomic, a later failure stays committed", Atomic(Seq(_a, _cut, _b)), "ac", _failConsumed),
        new("Cut: inside Atomic, a failure before the cut is rewound", Atomic(Seq(_a, _b, _cut)), "ac", _failEmpty),
        new("Cut: inside Atomic, Or does not try the next alternative", Or(Atomic(Seq(_a, _cut, _b)), _ac), "ac", _failConsumed),
        new("Cut: reaches every enclosing Atomic", Atomic(Seq(_x, Atomic(Seq(_a, _cut, _b)))), "xac", _failConsumed),
        new("Cut: stays in force after the Atomic around it succeeds", Atomic(Seq(Atomic(Seq(_a, _cut)), _b)), "ac", _failConsumed),
        new("Cut: never makes a failure without consuming committed", Or(Atomic(Seq(_cut, _a)), _b), "b", Ok("b", 1)),
        new("Cut: before an Atomic does not stop it rewinding", Seq(_cut, Or(Atomic(_ab), _ac)), "ac", Ok("ac", 2)),
        new("Cut: is dropped with an alternative that fails without consuming", Atomic(Or(Seq(_cut, _x), _ab)), "ac", _failEmpty),
        new("Cut: is dropped with the last alternative that fails without consuming", Atomic(Seq(_a, Or(_b, Seq(_cut, _x)))), "ac", _failEmpty),
        new("Cut: is dropped with a mandatory item that fails without consuming", Atomic(Seq(_a, AtLeastOnce(Seq(_cut, _x)))), "ac", _failEmpty),
        new("Cut: is dropped with an Until terminator that fails without consuming", Atomic(Seq(_a, Until(_x, Seq(_cut, _b)))), "ac", _failEmpty),
        new("Cut: is dropped with an Until item that fails without consuming", Atomic(Seq(_a, Until(Seq(_cut, _x), _b))), "ac", _failEmpty),
        new("Cut: is dropped with a SepBy separator that fails without consuming", Atomic(Seq(SepBy(_a, Seq(_cut, _b)), _x)), "ac", _failEmpty),
        new("Cut: is dropped with an item after a separator that fails without consuming", Atomic(SepBy(Seq(Or(_a, _cut), _b), _a)), "aba", _failEmpty),
        new("Cut: in a nested sequence stays in force", Atomic(Seq(_a, Seq(_cut, _b))), "ac", _failConsumed),
        new("Cut: is dropped with a repetition step that fails without consuming", Atomic(Seq(Many(Seq(_cut, _x)), _ab)), "ac", _failEmpty),
        new("Cut: in a repetition step that succeeded stays in force", Atomic(Seq(Many(Seq(_a, _cut)), _b)), "aac", _failConsumed),
        new("Cut: is dropped with an Optional that fails without consuming", Atomic(Seq(Optional(Seq(_cut, _x)), _ab)), "ac", _failEmpty),
        new("Cut: after a lookahead commits once the lookahead matches", Or(Atomic(Seq(Lookahead(_a), _cut, _ab)), _ac), "ac", _failConsumed),
        new("Cut: does not escape a Lookahead that succeeds", Atomic(Seq(Lookahead(Seq(_a, _cut)), _ab)), "ac", _failEmpty),
        new("Cut: does not escape a Lookahead that fails", Atomic(Lookahead(Seq(_a, _cut, _b))), "ac", _failEmpty),
        new("Cut: stops an Atomic inside a Lookahead", Lookahead(Atomic(Seq(_a, _cut, _b))), "ac", _failConsumed),
        new("Cut: does not escape a Not whose parser fails", Atomic(Seq(Not(Seq(_a, _cut, _b)), _ab)), "ac", _failEmpty),
        new("Cut: does not escape a Not whose parser succeeds", Atomic(Seq(_a, Not(Seq(_b, _cut)))), "ab", _failEmpty),
        new("Cut: Not still succeeds when an Atomic inside it keeps a committed failure", Not(Atomic(Seq(_a, _cut, _b))), "ac", Ok("", 0)),
        new("Cut: does not escape a Longest alternative that wins", Atomic(Seq(Longest(Seq(_a, _cut), _x), _ab)), "ac", _failEmpty),
        new("Cut: does not escape a Longest alternative that fails", Atomic(Seq(Longest(Seq(_a, _cut, _b), _a), _x)), "ac", _failEmpty),
        new("Cut: does not escape a later Longest alternative", Atomic(Seq(Longest(_a, Seq(_a, _cut)), _b)), "ac", _failEmpty),
        new("Cut: does not stop Longest giving back a failed alternative", Longest(Seq(_a, _cut, _b), _a), "ac", Ok("a", 1)),

        new("Named: does not change a success", Named(_ab, "ab"), "ab", Ok("ab", 2)),
        new("Named: does not change a failure without consuming", Named(_a, "a"), "x", _failEmpty),
        new("Named: does not change a failure after consuming", Named(_ab, "ab"), "ac", _failConsumed),
        new("Context: does not change a success", Context(_ab, "in ab"), "ab", Ok("ab", 2)),
        new("Context: does not change a failure without consuming", Context(_a, "in a"), "x", _failEmpty),
        new("Context: does not change a failure after consuming", Context(_ab, "in ab"), "ac", _failConsumed),
        new("Named and Context: pass a cut through", Atomic(Context(Named(Seq(_a, _cut, _b), "ab"), "in ab")), "ac", _failConsumed),
    ];

    /// <summary>
    /// Builds the Creole grammar for <paramref name="shape"/>.
    /// </summary>
    public static Parser<char, string> Build(ControlShape shape) => shape switch
    {
        ControlShape.AtLeastOnce s => Build(s.Item).AtLeastOnce().Select(string.Concat),
        ControlShape.Atomic s => Grammar<char>.Atomic(Build(s.Item)),
        ControlShape.Chars s => s.Text.Skip(1).Aggregate(
            CharGrammar.Char(s.Text[0]).Select(c => c.ToString()),
            (text, c) => Grammar<char>.Seq(text, CharGrammar.Char(c)).Select((t, next) => t + next)
        ),
        ControlShape.Context s => Build(s.Item).Context(s.Text),
        ControlShape.Cut => Grammar<char>.Cut.As(""),
        ControlShape.Lookahead s => Grammar<char>.Lookahead(Build(s.Item)),
        ControlShape.Longest s => Grammar<char>.Longest(Build(s.First), Build(s.Second)),
        ControlShape.Many s => Build(s.Item).Many().Select(string.Concat),
        ControlShape.Named s => Build(s.Item).Named(s.Label),
        ControlShape.Not s => Grammar<char>.Not(Build(s.Item)).As(""),
        ControlShape.Optional s => Build(s.Item).Optional().Select(m => m.GetValueOrDefault("")),
        ControlShape.Or s => Build(s.First).Or(Build(s.Second)),
        ControlShape.SepBy s => Build(s.Item).SepBy(Build(s.Separator)).Select(string.Concat),
        ControlShape.Until s => Build(s.Item).Until(Build(s.Terminator)).Select(string.Concat),
        ControlShape.Seq s => s.Steps.Skip(1).Aggregate(
            Build(s.Steps[0]),
            (text, step) => Grammar<char>.Seq(text, Build(step)).Select((t, next) => t + next)
        ),
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
    };

    private static ControlShape.AtLeastOnce AtLeastOnce(ControlShape item) => new ControlShape.AtLeastOnce(item);

    private static ControlShape.Atomic Atomic(ControlShape item) => new ControlShape.Atomic(item);

    private static ControlShape.Context Context(ControlShape item, string text) => new ControlShape.Context(item, text);

    private static ControlShape.Lookahead Lookahead(ControlShape item) => new ControlShape.Lookahead(item);

    private static ControlShape.Longest Longest(ControlShape first, ControlShape second) => new ControlShape.Longest(first, second);

    private static ControlShape.Many Many(ControlShape item) => new ControlShape.Many(item);

    private static ControlShape.Named Named(ControlShape item, string label) => new ControlShape.Named(item, label);

    private static ControlShape.Not Not(ControlShape item) => new ControlShape.Not(item);

    private static Outcome.Ok Ok(string value, int consumed) => new(value, consumed);

    private static ControlShape.Optional Optional(ControlShape item) => new ControlShape.Optional(item);

    private static ControlShape.Or Or(ControlShape first, ControlShape second) => new ControlShape.Or(first, second);

    private static ControlShape.SepBy SepBy(ControlShape item, ControlShape separator) => new ControlShape.SepBy(item, separator);

    private static ControlShape.Seq Seq(params ControlShape[] steps) => new ControlShape.Seq(steps);

    private static ControlShape.Until Until(ControlShape item, ControlShape terminator) => new ControlShape.Until(item, terminator);
}
