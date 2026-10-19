namespace Creole.Tests;

internal abstract record Pattern
{
    public record Empty : Pattern;

    public record Letters : Pattern;

    public record Literal(string Text) : Pattern;
}

internal record LongestToken(string Label, Pattern Pattern);

internal record LongestCase(string Name, IReadOnlyList<LongestToken> Alternatives, string Input, Outcome Expected);

/// <summary>
/// The semantics of <c>Longest</c>: the longest match wins, a tie goes to the earliest, and alternatives run atomically.
/// Each row runs <c>Longest</c> of its alternatives, in order, on its input; <see cref="Outcome.Ok"/> holds the winner's label and how much input it consumed.
/// INT-03 runs the interpreter against every row.
/// </summary>
internal static class LongestTable
{
    private static readonly LongestToken _empty = new("empty", new Pattern.Empty());
    private static readonly Outcome _failEmpty = new Outcome.FailEmpty();
    private static readonly LongestToken _identifier = new("identifier", new Pattern.Letters());
    private static readonly LongestToken _if = new("if", new Pattern.Literal("if"));
    private static readonly LongestToken _lessThan = new("<", new Pattern.Literal("<"));
    private static readonly LongestToken _lessThanOrEqual = new("<=", new Pattern.Literal("<="));
    private static readonly LongestToken _spaceship = new("<=>", new Pattern.Literal("<=>"));

    public static readonly IReadOnlyList<LongestCase> Cases =
    [
        new("<= beats <", [_lessThan, _lessThanOrEqual], "<=", Ok("<=", 2)),
        new("<= beats < whatever the order", [_lessThanOrEqual, _lessThan], "<=", Ok("<=", 2)),
        new("< wins when <= does not match", [_lessThan, _lessThanOrEqual], "<a", Ok("<", 1)),

        new("iffy beats if", [_if, _identifier], "iffy", Ok("identifier", 4)),
        new("a tie goes to the earliest", [_if, _identifier], "if", Ok("if", 2)),
        new("a tie goes to the earliest whatever it is", [_identifier, _if], "if", Ok("identifier", 2)),

        new("an alternative failing after consuming does not fail the whole", [_lessThan, _spaceship], "<=x", Ok("<", 1)),
        new("every alternative fails", [_lessThan, _lessThanOrEqual], "x", _failEmpty),
        new("every alternative fails, one after consuming", [_lessThanOrEqual, _spaceship], "<x", _failEmpty),

        new("an empty match beats a failure", [_lessThan, _empty], "x", Ok("empty", 0)),
        new("a longer match beats an empty one", [_empty, _lessThan], "<", Ok("<", 1)),
    ];

    public static Parser<char, string> ToParser(LongestCase row) =>
        row.Alternatives.Select(ToParser).ToArray() switch
        {
            [var a, var b] => Grammar<char>.Longest(a, b),
            [var a, var b, var c] => Grammar<char>.Longest(a, b, c),
            var alternatives => throw new NotSupportedException($"No Longest overload in the table helper for {alternatives.Length} alternatives."),
        };

    private static Outcome.Ok Ok(string value, int consumed) => new(value, consumed);

    private static Parser<char, string> ToParser(LongestToken token) =>
        token.Pattern switch
        {
            Pattern.Empty => Grammar<char>.Return(token.Label),
            Pattern.Letters => CharGrammar.Letter.SkipAtLeastOnce().As(token.Label),
            Pattern.Literal literal => CharGrammar.String(literal.Text).As(token.Label),
            _ => throw new NotSupportedException(token.Pattern.ToString()),
        };
}
