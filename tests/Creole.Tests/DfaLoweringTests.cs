using CsCheck;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file sealed record Alternative(Parser<char, string> Parser, string Text);

file static class Alternatives
{
    private static readonly Alternative[] _fixed =
    [
        new(Range('a', 'c').Select(static c => $"char {c}"), "Range(a, c)"),
        new(CIString("ab"), "CIString(ab)"),
        new(Atomic(Seq(Char('a'), Char('b').Optional())).Text().Named("ab?"), "Atomic(a, b?).Text().Named()"),
        new(Seq(Char('a'), Any).Select(static (a, any) => $"{a}{any}"), "Seq(a, Any).Select()"),
    ];

    public static Gen<Alternative> Of(int depth) =>
        Gen.Select(Gen.Int[0, 4], Gen.Int[0, _fixed.Length - 1], RegularShapes.Regular(depth), static (form, which, shape) => form switch
        {
            0 => new Alternative(shape.Parser.As("constant"), $"{shape.Text}.As()"),
            1 => new Alternative(shape.Parser.Text(), $"{shape.Text}.Text()"),
            2 => new Alternative(shape.Parser.Slice().Select(static slice => $"slice {slice}"), $"{shape.Text}.Slice().Select()"),
            3 => new Alternative(shape.Parser.Optional().Select(static maybe => maybe.TryGetValue(out Unit _) ? "some" : "none"), $"{shape.Text}.Optional().Select()"),
            _ => _fixed[which],
        });
}

public class DfaLoweringTests
{
    private const long MaxFallbackBytes = 140_000_000;

    private static Gen<int?> SegmentSizes =>
        Gen.Int[0, 3].Select(static size => size == 0 ? (int?)null : size);

    private static Plan<TIn, T> Lower<TIn, T>(Parser<TIn, T> parser, int dfaStateCap = PlanBuilder.DfaStateCap, long dfaWorkBudget = PlanBuilder.DfaWorkBudget) =>
        new PlanBuilder(LoweringOptions.All, dfaStateCap: dfaStateCap, dfaWorkBudget: dfaWorkBudget).Lower(parser);

    private static (bool Ok, string? Value, long Position) Run(Plan<char, string> plan, string text, int position, bool segmented)
    {
        var cursor = segmented
            ? new Cursor<char>(new SegmentedSource<char>(text.ToCharArray(), 5))
            : new Cursor<char>(text);

        cursor.Reset(position);

        var value = "";
        var ok = plan.TryParse(ref cursor, ref value);

        return (ok, ok ? value : null, cursor.Position);
    }

    [Fact]
    public void ALongestNestedDirectlyInsideALongestCompilesWithIt()
    {
        var parser = Longest(String("a"), Longest(String("ab"), String("abc")));

        Assert.IsType<DfaLongestPlan<string>>(Lower(parser));
        Assert.Equal(Rewrites.Compare(parser, LoweringOptions.None, "abcd"), Rewrites.Compare(parser, LoweringOptions.Dfa, "abcd"));
    }

    [Fact]
    public void ALongestOfRegularAlternativesCompiles() =>
        Assert.IsType<DfaLongestPlan<string>>(Lower(Longest(String("<"), String("<="), String("<<="))));

    [Fact]
    public void ALongestOverTheStateCapRunsOnTheInterpreter()
    {
        var parser = LexerSamples.Longest([.. LexerSamples.CSharp().Select(static (rule, index) => rule.Text().Select(token => $"{index}:{token}"))]);
        var interpreted = new PlanBuilder(LoweringOptions.None).Lower(parser);
        var capped = Lower(parser, dfaStateCap: 50);

        Assert.IsType<LongestPlan<char, string>>(capped);

        for (var position = 0; position < LexerSamples.CSharpText.Length; position++)
        {
            Assert.Equal(Run(interpreted, LexerSamples.CSharpText, position, segmented: false), Run(capped, LexerSamples.CSharpText, position, segmented: false));
        }
    }

    [Fact]
    public void ALongestOverTheWorkBudgetRunsOnTheInterpreter()
    {
        var parser = LexerSamples.Longest([.. LexerSamples.CSharp().Select(static (rule, index) => rule.Text().Select(token => $"{index}:{token}"))]);
        var interpreted = new PlanBuilder(LoweringOptions.None).Lower(parser);
        var budgeted = Lower(parser, dfaWorkBudget: 1_000);

        Assert.IsType<LongestPlan<char, string>>(budgeted);

        for (var position = 0; position < LexerSamples.CSharpText.Length; position++)
        {
            Assert.Equal(Run(interpreted, LexerSamples.CSharpText, position, segmented: false), Run(budgeted, LexerSamples.CSharpText, position, segmented: false));
        }
    }

    [Fact]
    public void ALongestOverTokensRunsOnTheInterpreter() =>
        Assert.IsType<LongestPlan<int, int>>(Lower(Grammar<int>.Longest(Grammar<int>.Token(1), Grammar<int>.Token(2))));

    [Fact]
    public void ALongestThatOutgrowsTheStateCapFallsBackCheaply()
    {
        var parser = LexerSamples.Longest([.. LexerSamples.Keywords(400).Select(static (rule, index) => rule.As(index))]);
        Plan<char, int>? plan = null;
        var allocated = Allocation.Fewest(() => plan = Lower(parser));

        Assert.IsType<LongestPlan<char, int>>(plan);
        Assert.InRange(allocated, 0, MaxFallbackBytes);
    }

    [Fact]
    public void ALongestWithAChoiceOutsideFragmentEPrimeRunsOnTheInterpreter()
    {
        var outsideEPrime = Atomic(Seq(new CharClassNode(CharSet.Of('a', 'c'), null), String("cbc")).SkipMany()).Or(Atomic(Char('b')).SkipMany());
        var parser = Longest(outsideEPrime.As("outside"), Char('a').As("a"));

        Assert.IsType<LongestPlan<char, string>>(Lower(parser));

        foreach (var input in new[] { "", "a", "c", "b", "acbc", "acbcb", "acb", "bbc" })
        {
            var (without, with) = Rewrites.Compare(parser, LoweringOptions.Dfa, input);
            Assert.Equal(without, with);
        }
    }

    [Fact]
    public void ALongestWithAnAlternativeThatIsNotRegularRunsOnTheInterpreter() =>
        Assert.IsType<LongestPlan<char, char>>(Lower(Longest(Char('a'), Satisfy(char.IsDigit))));

    [Theory]
    [InlineData("JSON", false)]
    [InlineData("JSON", true)]
    [InlineData("C#", false)]
    [InlineData("C#", true)]
    public void ACompiledLexerTokenizesLikeTheInterpreter(string lexer, bool segmented)
    {
        var rules = lexer == "JSON" ? LexerSamples.Json() : LexerSamples.CSharp();
        var text = lexer == "JSON" ? LexerSamples.JsonText : LexerSamples.CSharpText;
        var parser = LexerSamples.Longest([.. rules.Select(static (rule, index) => rule.Text().Select(token => $"{index}:{token}"))]);
        var interpreted = new PlanBuilder(LoweringOptions.None).Lower(parser);
        var compiled = new PlanBuilder(LoweringOptions.Dfa).Lower(parser);

        Assert.IsType<DfaLongestPlan<string>>(compiled);

        for (var position = 0; position < text.Length;)
        {
            var expected = Run(interpreted, text, position, segmented);

            Assert.Equal(expected, Run(compiled, text, position, segmented));

            position = expected.Ok ? (int)expected.Position : position + 1;
        }
    }

    [Fact]
    public void ACompiledLongestAgreesWithTheInterpreter() =>
        Gen.Select(Gen.Int[2, 3].SelectMany(static count => Alternatives.Of(5).Array[count]), Gen.Char["abcAB"].Array[0, 8], SegmentSizes, Gen.Bool).Sample(
            static (alternatives, input, segmentSize, cut) =>
            {
                var parser = LexerSamples.Longest([.. alternatives.Select(static (alternative, index) => alternative.Parser.Select(value => $"{index}: {value}"))]);
                var (without, with) = Rewrites.Compare(parser, LoweringOptions.Dfa, new string(input), segmentSize, cut);

                return new PlanBuilder(LoweringOptions.Dfa).Lower(parser) is DfaLongestPlan<string> && without == with;
            },
            iter: 2_000,
            print: static t => $"{string.Join(" | ", t.Item1.Select(static alternative => alternative.Text))} on \"{new string(t.Item2)}\", segments {t.Item3}, cut {t.Item4}"
        );

    [Theory]
    [InlineData("JSON")]
    [InlineData("C#")]
    [InlineData("300 keywords")]
    public void TheLexersAndThreeHundredKeywordsCompile(string rules)
    {
        var alternatives = rules switch
        {
            "JSON" => LexerSamples.Json(),
            "C#" => LexerSamples.CSharp(),
            _ => LexerSamples.Keywords(300),
        };

        Assert.IsType<DfaLongestPlan<int>>(Lower(LexerSamples.Longest([.. alternatives.Select(static (rule, index) => rule.As(index))])));
    }

    [Fact]
    public void WithoutTheRewriteALongestRunsOnTheInterpreter() =>
        Assert.IsType<LongestPlan<char, string>>(new PlanBuilder(LoweringOptions.None).Lower(Longest(String("<"), String("<="))));
}
