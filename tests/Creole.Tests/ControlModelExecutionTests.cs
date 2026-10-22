using CsCheck;

namespace Creole.Tests;

/// <summary>
/// The interpreter against <see cref="ControlModel"/>, the reference model of commit and cut semantics.
/// </summary>
/// <remarks>
/// Each run compares the outcome (value and consumption, or a failure with or without consuming) and whether a cut is in force afterwards.
/// Together they cover the four outcomes of § Results in every context (see <see cref="ControlModel"/>).
/// </remarks>
public class ControlModelExecutionTests
{
    private const int Depth = 3;
    private const int InputLength = 3;

    private static readonly Gen<ControlShape> _shapes = Shapes(5).Where(shape => !ControlModel.RepeatsNullable(shape));

    private static string? Difference(ControlShape shape, string input)
    {
        var expected = ControlModel.InOrder.Run(shape, input);
        var actual = Run(shape, input);

        return actual == expected
            ? null
            : $"{ControlModel.Describe(shape)} on \"{input}\": model {expected}, interpreter {actual}";
    }

    private static (Outcome Outcome, bool CutInForce) Run(ControlShape shape, string input)
    {
        var cursor = new Cursor<char>(input);

        try
        {
            var plan = new PlanBuilder(LoweringOptions.All, dispatchThreshold: 1).Lower(ControlTable.Build(shape));
            var value = "";
            var ok = plan.TryParse(ref cursor, ref value);

            Outcome outcome = ok ? new Outcome.Ok(value, (int)cursor.Position)
                : cursor.Position > 0 ? new Outcome.FailConsumed()
                : new Outcome.FailEmpty();

            return (outcome, cursor.Cut);
        }
        catch (GrammarException)
        {
            return (new Outcome.Throws(), false);
        }
    }

    private static Gen<ControlShape> Shapes(int depth)
    {
        var leaf = Gen.OneOfConst<ControlShape>(new ControlShape.Chars("a"), new ControlShape.Chars("b"), new ControlShape.Chars("ab"), new ControlShape.Cut());

        if (depth <= 1)
        {
            return leaf;
        }

        var smaller = Shapes(depth - 1);

        var unary = Gen.Select(Gen.Int[0, 5], smaller, (which, s) => which switch
        {
            0 => (ControlShape)new ControlShape.Atomic(s),
            1 => new ControlShape.Lookahead(s),
            2 => new ControlShape.Not(s),
            3 => new ControlShape.Optional(s),
            4 => new ControlShape.Many(s),
            _ => new ControlShape.AtLeastOnce(s),
        });

        var binary = Gen.Select(Gen.Int[0, 4], smaller, smaller, (which, first, second) => which switch
        {
            0 => (ControlShape)new ControlShape.Seq(first, second),
            1 => new ControlShape.Or(first, second),
            2 => new ControlShape.Longest(first, second),
            3 => new ControlShape.SepBy(first, second),
            _ => new ControlShape.Until(first, second),
        });

        return Gen.Frequency((1, leaf), (2, unary), (3, binary));
    }

    [Fact]
    public void TheInterpreterAgreesWithTheModelOnEveryShapeToDepthThree()
    {
        var inputs = ControlModel.AllInputs(InputLength).ToArray();

        var difference = ControlModel.AllShapes(Depth)
            .SelectMany(shape => inputs.Select(input => Difference(shape, input)))
            .FirstOrDefault(d => d is not null);

        Assert.Null(difference);
    }

    [Fact]
    public void TheInterpreterAgreesWithTheModelOnRandomDeeperShapes() =>
        Gen.Select(_shapes, Gen.Char["ab"].Array[0, 6]).Sample(
            (shape, input) => Difference(shape, new string(input)) is null,
            iter: 20_000,
            print: t => $"{ControlModel.Describe(t.Item1)} on \"{new string(t.Item2)}\""
        );
}
