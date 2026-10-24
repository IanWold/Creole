using CsCheck;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

public class CommitAutomatonTests
{
    private const int MaxStates = 10_000;

    private static readonly string[] _inputs = [.. AllInputs(6)];

    private static readonly string[] _shortInputs = [.. AllInputs(5)];

    private static List<string> AllInputs(int length)
    {
        IEnumerable<string> level = [""];
        var all = new List<string> { "" };

        for (var i = 0; i < length; i++)
        {
            level = [.. level.SelectMany(static prefix => "abc".Select(c => prefix + c))];
            all.AddRange(level);
        }

        return all;
    }

    private static CommitAutomaton Automaton(params Parser<char, Unit>[] alternatives) =>
        CommitAutomaton.Build([.. alternatives.Select(static alternative => Regular.Of(alternative)!)], MaxStates)!;

    private static CommitAutomaton.Result Interpret(Parser<char, int> parser, string input, int start = 0)
    {
        var plan = new PlanBuilder(LoweringOptions.None).Lower(parser);
        var cursor = new Cursor<char>(input);

        cursor.Reset(start);

        var value = -1;

        return plan.TryParse(ref cursor, ref value)
            ? new CommitAutomaton.Result(value, (int)(cursor.Position - start), Committed: false)
            : new CommitAutomaton.Result(-1, 0, cursor.Position > start);
    }

    [Fact]
    public void ALongestIsOneAutomatonWhoseTiesGoToTheEarliest()
    {
        var identifier = Seq(Range('a', 'z'), Range('a', 'z').SkipMany()).Ignore();
        var automaton = Automaton(String("if").Ignore(), identifier);

        Assert.Equal(new CommitAutomaton.Result(0, 2, Committed: false), automaton.Match("if"));
        Assert.Equal(new CommitAutomaton.Result(1, 4, Committed: false), automaton.Match("iffy"));
        Assert.Equal(new CommitAutomaton.Result(1, 1, Committed: false), automaton.Match("i("));
    }

    [Fact]
    public void AnAutomatonOverTheStateCapIsNotBuilt() =>
        Assert.Null(CommitAutomaton.Build([.. LexerSamples.CSharp().Select(static rule => Regular.Of(rule)!)], maxStates: 50));

    [Fact]
    public void BothLexersStaySmall()
    {
        var json = Automaton(LexerSamples.Json());
        var csharp = Automaton(LexerSamples.CSharp());

        Assert.InRange(json.States, 1, 100);
        Assert.InRange(csharp.States, 1, 1_000);
    }

    [Fact]
    public void TheParsecStyleDecimalFailsAfterConsumingOnAPointWithNoDigits()
    {
        var digits = Range('0', '9').SkipAtLeastOnce();
        var automaton = Automaton(Seq(digits, Seq(Char('.'), digits).Optional()).Ignore());

        Assert.Equal(new CommitAutomaton.Result(-1, 0, Committed: true), automaton.Match("1.x"));
        Assert.Equal(new CommitAutomaton.Result(0, 3, Committed: false), automaton.Match("1.5"));
        Assert.Equal(new CommitAutomaton.Result(0, 1, Committed: false), automaton.Match("1x"));
    }

    [Theory]
    [InlineData("JSON", LexerSamples.JsonText)]
    [InlineData("C#", LexerSamples.CSharpText)]
    public void TokenizesLikeTheInterpretersNestedLongest(string lexer, string text)
    {
        var rules = lexer == "JSON" ? LexerSamples.Json() : LexerSamples.CSharp();
        var automaton = Automaton(rules);
        var interpreter = LexerSamples.Longest([.. rules.Select(static (rule, index) => rule.As(index))]);
        var tokens = 0;

        for (var position = 0; position < text.Length; tokens++)
        {
            var expected = Interpret(interpreter, text, position);

            Assert.Equal(expected, automaton.Match(text.AsSpan(position)));

            position += expected.Alternative >= 0
                ? expected.End
                : 1;
        }

        Assert.True(tokens > 50);
    }

    [Fact]
    public void AgreesWithTheInterpreterOnRandomLongests() =>
        Gen.Int[2, 3].SelectMany(static count => RegularShapes.Regular(4).Array[count]).Sample(
            static shapes =>
            {
                var automaton = Automaton([.. shapes.Select(static shape => shape.Parser)]);
                var interpreter = LexerSamples.Longest([.. shapes.Select(static (shape, index) => shape.Parser.As(index))]);

                return _shortInputs.All(input => automaton.Match(input) == Interpret(interpreter, input));
            },
            iter: 400,
            print: static shapes => string.Join(" | ", shapes.Select(static shape => shape.Text))
        );

    [Fact]
    public void AgreesWithTheInterpreterOnRandomTermsWithAChoiceInsideAtomic() =>
        Gen.Select(RegularShapes.WithChoiceInsideAtomic(5), Gen.Char["abc"].Array[7, 14].Array[10]).Sample(
            static (shape, longer) =>
            {
                var automaton = Automaton(shape.Parser);
                var interpreter = shape.Parser.As(0);

                return _inputs.Concat(longer.Select(static chars => new string(chars))).All(input => automaton.Match(input) == Interpret(interpreter, input));
            },
            iter: 1_000,
            print: static t => t.Item1.Text
        );

    [Fact]
    public void AgreesWithTheInterpreterOnRandomTerms() =>
        Gen.Select(RegularShapes.Regular(5), Gen.Char["abc"].Array[7, 14].Array[10]).Sample(
            static (shape, longer) =>
            {
                var automaton = Automaton(shape.Parser);
                var interpreter = shape.Parser.As(0);

                return _inputs.Concat(longer.Select(static chars => new string(chars))).All(input => automaton.Match(input) == Interpret(interpreter, input));
            },
            iter: 400,
            print: static t => t.Item1.Text
        );
}
