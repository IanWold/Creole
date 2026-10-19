namespace Creole.Tests;

internal abstract record Shape
{
    public record AtLeastOnce(Shape Item) : Shape;

    public record Character(char Value) : Shape;

    public record Many(Shape Item) : Shape;

    public record Repeat(Shape Item, int Count) : Shape;

    public record RepeatRange(Shape Item, int Min, int Max) : Shape;

    public record Return(string Value) : Shape;

    public record SepBy(Shape Item, Shape Separator) : Shape;

    public record SepBy1(Shape Item, Shape Separator) : Shape;

    public record SepEndBy(Shape Item, Shape Separator) : Shape;

    public record SkipAtLeastOnce(Shape Item) : Shape;

    public record SkipMany(Shape Item) : Shape;

    public record TwoCharacters(char First, char Second) : Shape;

    public record Until(Shape Item, Shape Terminator) : Shape;
}

internal abstract record Outcome
{
    public record FailConsumed : Outcome;

    public record FailEmpty : Outcome;

    public record Ok(string Value, int Consumed) : Outcome;

    public record Throws : Outcome;
}

internal record RepetitionCase(string Name, Shape Shape, string Input, Outcome Expected);

internal static class RepetitionTable
{
    private static readonly Shape _comma = new Shape.Character(',');
    private static readonly Outcome _failConsumed = new Outcome.FailConsumed();
    private static readonly Outcome _failEmpty = new Outcome.FailEmpty();
    private static readonly Shape _letterA = new Shape.Character('a');
    private static readonly Shape _lettersAB = new Shape.TwoCharacters('a', 'b');
    private static readonly Shape _returnX = new Shape.Return("x");
    private static readonly Shape _semicolon = new Shape.Character(';');
    private static readonly Outcome _throws = new Outcome.Throws();

    public static readonly IReadOnlyList<RepetitionCase> Cases =
    [
        new("Many: empty input", new Shape.Many(_letterA), "", Ok("[]", 0)),
        new("Many: stops at a non-match", new Shape.Many(_letterA), "aaab", Ok("[a,a,a]", 3)),
        new("Many: stops when an item fails without consuming", new Shape.Many(_lettersAB), "abx", Ok("[ab]", 2)),
        new("Many: an item failing after consuming fails the whole", new Shape.Many(_lettersAB), "ababac", _failConsumed),
        new("Many: of a parser that consumes nothing", new Shape.Many(_returnX), "a", _throws),
        new("Many: of Many", new Shape.Many(new Shape.Many(_letterA)), "", _throws),
        new("Many: of AtLeastOnce", new Shape.Many(new Shape.AtLeastOnce(_letterA)), "aa", Ok("[[a,a]]", 2)),

        new("AtLeastOnce: empty input", new Shape.AtLeastOnce(_letterA), "", _failEmpty),
        new("AtLeastOnce: stops at a non-match", new Shape.AtLeastOnce(_letterA), "aab", Ok("[a,a]", 2)),
        new("AtLeastOnce: first item fails after consuming", new Shape.AtLeastOnce(_lettersAB), "ac", _failConsumed),
        new("AtLeastOnce: a later item fails after consuming", new Shape.AtLeastOnce(_lettersAB), "aba", _failConsumed),

        new("Repeat: reads exactly n", new Shape.Repeat(_letterA, 3), "aaaa", Ok("[a,a,a]", 3)),
        new("Repeat: too few after consuming", new Shape.Repeat(_letterA, 3), "aab", _failConsumed),
        new("Repeat: too few on empty input", new Shape.Repeat(_letterA, 2), "", _failEmpty),
        new("Repeat: zero", new Shape.Repeat(_letterA, 0), "a", Ok("[]", 0)),
        new("Repeat: of a parser that consumes nothing", new Shape.Repeat(_returnX, 3), "", Ok("[x,x,x]", 0)),

        new("RepeatRange: stops at max", new Shape.RepeatRange(_letterA, 1, 3), "aaaa", Ok("[a,a,a]", 3)),
        new("RepeatRange: between min and max", new Shape.RepeatRange(_letterA, 1, 3), "ab", Ok("[a]", 1)),
        new("RepeatRange: below min after consuming", new Shape.RepeatRange(_letterA, 2, 3), "ab", _failConsumed),
        new("RepeatRange: below min on empty input", new Shape.RepeatRange(_letterA, 1, 3), "", _failEmpty),
        new("RepeatRange: an optional item failing after consuming fails the whole", new Shape.RepeatRange(_lettersAB, 0, 2), "aba", _failConsumed),

        new("SepBy: empty input", new Shape.SepBy(_letterA, _comma), "", Ok("[]", 0)),
        new("SepBy: no first item", new Shape.SepBy(_letterA, _comma), "b", Ok("[]", 0)),
        new("SepBy: several items", new Shape.SepBy(_letterA, _comma), "a,a,a", Ok("[a,a,a]", 5)),
        new("SepBy: stops at a non-separator", new Shape.SepBy(_letterA, _comma), "a;", Ok("[a]", 1)),
        new("SepBy: a trailing separator fails", new Shape.SepBy(_letterA, _comma), "a,", _failConsumed),
        new("SepBy: an item failing after consuming fails the whole", new Shape.SepBy(_lettersAB, _comma), "ab,ac", _failConsumed),
        new("SepBy: a separator that consumes nothing", new Shape.SepBy(_letterA, _returnX), "aa", Ok("[a,a]", 2)),
        new("SepBy: item and separator that consume nothing", new Shape.SepBy(_returnX, _returnX), "", _throws),

        new("SepBy1: empty input", new Shape.SepBy1(_letterA, _comma), "", _failEmpty),
        new("SepBy1: several items", new Shape.SepBy1(_letterA, _comma), "a,a", Ok("[a,a]", 3)),
        new("SepBy1: a trailing separator fails", new Shape.SepBy1(_letterA, _comma), "a,a,", _failConsumed),

        new("SepEndBy: empty input", new Shape.SepEndBy(_letterA, _comma), "", Ok("[]", 0)),
        new("SepEndBy: a lone separator", new Shape.SepEndBy(_letterA, _comma), ",", Ok("[]", 0)),
        new("SepEndBy: no trailing separator", new Shape.SepEndBy(_letterA, _comma), "a,a", Ok("[a,a]", 3)),
        new("SepEndBy: a trailing separator", new Shape.SepEndBy(_letterA, _comma), "a,a,", Ok("[a,a]", 4)),
        new("SepEndBy: two separators", new Shape.SepEndBy(_letterA, _comma), "a,,", Ok("[a]", 2)),
        new("SepEndBy: an item failing after consuming fails the whole", new Shape.SepEndBy(_lettersAB, _comma), "ab,a", _failConsumed),

        new("Until: terminator only", new Shape.Until(_letterA, _semicolon), ";", Ok("[]", 1)),
        new("Until: items then terminator", new Shape.Until(_letterA, _semicolon), "aa;", Ok("[a,a]", 3)),
        new("Until: empty input", new Shape.Until(_letterA, _semicolon), "", _failEmpty),
        new("Until: neither matches", new Shape.Until(_letterA, _semicolon), "b", _failEmpty),
        new("Until: no terminator after items", new Shape.Until(_letterA, _semicolon), "aa", _failConsumed),
        new("Until: the terminator is tried first", new Shape.Until(_letterA, _letterA), "aa", Ok("[]", 1)),
        new("Until: a terminator failing after consuming fails the whole", new Shape.Until(_letterA, _lettersAB), "aab", _failConsumed),
        new("Until: of a parser that consumes nothing", new Shape.Until(_returnX, _semicolon), "a", _throws),

        new("SkipMany: empty input", new Shape.SkipMany(_letterA), "", Ok("()", 0)),
        new("SkipMany: stops at a non-match", new Shape.SkipMany(_letterA), "aab", Ok("()", 2)),
        new("SkipMany: an item failing after consuming fails the whole", new Shape.SkipMany(_lettersAB), "aba", _failConsumed),
        new("SkipMany: of a parser that consumes nothing", new Shape.SkipMany(_returnX), "", _throws),

        new("SkipAtLeastOnce: empty input", new Shape.SkipAtLeastOnce(_letterA), "", _failEmpty),
        new("SkipAtLeastOnce: several items", new Shape.SkipAtLeastOnce(_letterA), "aa", Ok("()", 2)),
    ];

    private static Outcome.Ok Ok(string value, int consumed) => new Outcome.Ok(value, consumed);
}
