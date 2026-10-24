using CsCheck;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file sealed record Shape(Parser<char, Unit> Parser, string Text);

file static class Shapes
{
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

    public static Gen<Shape> Regular(int depth) => Of(depth).Where(static shape => Creole.Regular.Of(shape.Parser) is not null);

    public static Gen<Shape> RegularWithChoiceInsideAtomic(int depth) =>
        Of(depth, atomicWeight: 4).Where(static shape => Creole.Regular.Of(shape.Parser) is { } term && ChoiceInsideAtomic(term, inside: false));
}

public class CommitAutomatonTests
{
    private const int MaxStates = 10_000;

    private const string CSharpText = """
        /* header ** comment */ /**/ /***/ namespace Creole.Tests; // a line comment
        public static class iffy { int x = 0x1F + 12.5e-3f * 'c' - '\n' / 7.; string s = "a\"b\\";
        x <<= 2; y ??= z => q?.r; if (a <= b && c != d || !e) return @x #y $z; é 1e 1.e5 /* never closed
        """;

    private const string JsonText = """
        {"name": "Creole", "version": -1.25e+3, "tags": ["a\"b", "\u00e9\n", true, false, null],
         "nested": {"x": 0, "y": [1, 2.5, 3E-2]}, "bad": 01, "frac": 1., "exp": 1e, "neg": -, "u": "\u12G4"}
        """;

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

    private static Parser<char, int> Longest(Parser<char, int>[] p) => p.Length switch
    {
        1 => p[0],
        2 => Grammar<char>.Longest(p[0], p[1]),
        3 => Grammar<char>.Longest(p[0], p[1], p[2]),
        4 => Grammar<char>.Longest(p[0], p[1], p[2], p[3]),
        5 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4]),
        6 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5]),
        7 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6]),
        8 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7]),
        9 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8]),
        10 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9]),
        11 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10]),
        12 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11]),
        13 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11], p[12]),
        14 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11], p[12], p[13]),
        15 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11], p[12], p[13], p[14]),
        _ => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11], p[12], p[13], p[14], p[15]),
    };

    private static Parser<char, int> NestedLongest(Parser<char, Unit>[] rules)
    {
        var indexed = rules.Select(static (rule, index) => rule.As(index)).ToArray();

        Parser<char, int> Level(int from) =>
            indexed.Length - from <= 16 ? Longest(indexed[from..]) : Longest([.. indexed[from..(from + 15)], Level(from + 15)]);

        return Level(0);
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
    [InlineData("JSON", JsonText)]
    [InlineData("C#", CSharpText)]
    public void TokenizesLikeTheInterpretersNestedLongest(string lexer, string text)
    {
        var rules = lexer == "JSON" ? LexerSamples.Json() : LexerSamples.CSharp();
        var automaton = Automaton(rules);
        var interpreter = NestedLongest(rules);
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
        Gen.Int[2, 3].SelectMany(static count => Shapes.Regular(4).Array[count]).Sample(
            static shapes =>
            {
                var automaton = Automaton([.. shapes.Select(static shape => shape.Parser)]);
                var interpreter = NestedLongest([.. shapes.Select(static shape => shape.Parser)]);

                return _shortInputs.All(input => automaton.Match(input) == Interpret(interpreter, input));
            },
            iter: 400,
            print: static shapes => string.Join(" | ", shapes.Select(static shape => shape.Text))
        );

    [Fact]
    public void AgreesWithTheInterpreterOnRandomTermsWithAChoiceInsideAtomic() =>
        Gen.Select(Shapes.RegularWithChoiceInsideAtomic(5), Gen.Char["abc"].Array[7, 14].Array[10]).Sample(
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
        Gen.Select(Shapes.Regular(5), Gen.Char["abc"].Array[7, 14].Array[10]).Sample(
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
