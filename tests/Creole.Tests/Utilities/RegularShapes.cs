using CsCheck;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

/// <summary>
/// Random Regular parsers over the characters a, b and c, with a description of each, for differential tests against the interpreter.
/// </summary>
internal static class RegularShapes
{
    /// <summary>
    /// A parser and how to read it.
    /// </summary>
    public sealed record Shape(Parser<char, Unit> Parser, string Text);

    private static bool ChoiceInsideAtomic(RegularTerm term, bool inside) => term switch
    {
        RegularTerm.Atomic atomic => ChoiceInsideAtomic(atomic.Item, inside: true),
        RegularTerm.Choice or RegularTerm.Many when inside => true,
        RegularTerm.Sequence sequence => ChoiceInsideAtomic(sequence.First, inside) || ChoiceInsideAtomic(sequence.Second, inside),
        RegularTerm.Choice choice => ChoiceInsideAtomic(choice.First, inside) || ChoiceInsideAtomic(choice.Second, inside),
        RegularTerm.Many many => ChoiceInsideAtomic(many.Item, inside),
        _ => false,
    };

    public static Gen<Shape> Of(int depth, int atomicWeight = 1)
    {
        var leaf = Gen.OneOfConst(
            new Shape(Char('a').Ignore(), "a"),
            new Shape(Char('b').Ignore(), "b"),
            new Shape(Char('c').Ignore(), "c"),
            new Shape(new CharClassNode(CharSet.Of('a', 'b'), null).Ignore(), "[ab]"),
            new Shape(new CharClassNode(CharSet.Of('b', 'c'), null).Ignore(), "[bc]"),
            new Shape(new CharClassNode(CharSet.Of('a', 'c'), null).Ignore(), "[ac]"),
            new Shape(String("ab").Ignore(), "\"ab\""),
            new Shape(String("ba").Ignore(), "\"ba\""),
            new Shape(String("cbc").Ignore(), "\"cbc\"")
        );

        if (depth <= 1)
        {
            return leaf;
        }

        var smaller = Of(depth - 1, atomicWeight);

        var unary = Gen.Select(Gen.Int[0, 3 + atomicWeight], smaller, static (which, s) => which switch
        {
            0 => new Shape(s.Parser.Optional().Ignore(), $"Optional({s.Text})"),
            1 => new Shape(s.Parser.SkipMany(), $"Many({s.Text})"),
            2 => new Shape(s.Parser.SkipAtLeastOnce(), $"AtLeastOnce({s.Text})"),
            3 => new Shape(s.Parser.Many().Ignore(), $"Many({s.Text})"),
            _ => new Shape(Atomic(s.Parser), $"Atomic({s.Text})"),
        });

        var binary = Gen.Select(Gen.Int[0, 1], smaller, smaller, static (which, first, second) => which == 0
            ? new Shape(Seq(first.Parser, second.Parser).Ignore(), $"Seq({first.Text}, {second.Text})")
            : new Shape(first.Parser.Or(second.Parser), $"Or({first.Text}, {second.Text})"));

        return Gen.Frequency((1, leaf), (2, unary), (3, binary));
    }

    public static Gen<Shape> Regular(int depth) =>
        Of(depth).Where(static shape => Creole.Regular.Of(shape.Parser) is not null);

    public static Gen<Shape> WithChoiceInsideAtomic(int depth) =>
        Of(depth, atomicWeight: 4).Where(static shape => Creole.Regular.Of(shape.Parser) is { } term && ChoiceInsideAtomic(term, inside: false));
}
