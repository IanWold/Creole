using System.Globalization;
using Creole.Samples.Indentation;
using CsCheck;

namespace Creole.Tests;

/// <summary>
/// A line of a generated program: blank lines before it, its text, and, for an <c>if</c> or a <c>while</c>, its block and how much further the block is indented.
/// </summary>
file sealed record Line(string[] Blank, string Text, int Indent, Line[] Body);

/// <summary>
/// Random programs of the indentation-sensitive language, written with random indentation and blank lines, and in the standard form <see cref="Script.ToString"/> gives.
/// </summary>
file static class Programs
{
    private static readonly Gen<string> _name = Gen.Char["abxyz"].Array[1, 3].Select(static letters => new string(letters));

    private static readonly Gen<string[]> _blank = Gen.Int[0, 3].Select(static spaces => new string(' ', spaces)).Array[0, 2];

    public static readonly Gen<(string Source, string Standard)> All =
        Gen.Select(Generate(3).Array[1, 4], _blank, static (lines, trailing) => (Source(lines, trailing), string.Concat(lines.Select(static line => Standard(line, "")))));

    private static Gen<string> Expression(int depth)
    {
        var atom = Gen.Frequency((1, _name), (1, Gen.Int[0, 999].Select(static number => number.ToString(CultureInfo.InvariantCulture))));

        if (depth == 0)
        {
            return atom;
        }

        var operand = Expression(depth - 1);

        return Gen.Frequency((2, atom), (1, Gen.Select(operand, Gen.OneOf("+", "-", "*", "/", "<"), operand, static (left, symbol, right) => $"({left} {symbol} {right})")));
    }

    private static Gen<Line> Generate(int depth)
    {
        var assignment = Gen.Select(_blank, _name, Expression(2), static (blank, name, value) => new Line(blank, $"{name} = {value}", 0, []));

        if (depth == 0)
        {
            return assignment;
        }

        var compound = Gen.Select(
            _blank,
            Gen.OneOf("if", "while"),
            Expression(2),
            Gen.Int[1, 4],
            Generate(depth - 1).Array[1, 3],
            static (blank, keyword, condition, indent, body) => new Line(blank, $"{keyword} {condition}", indent, body)
        );

        return Gen.Frequency((2, assignment), (1, compound));
    }

    private static string Source(Line[] lines, string[] trailing) =>
        Source(lines[0] with { Blank = [] }, 0)
        + string.Concat(lines.Skip(1).Select(static line => Source(line, 0)))
        + string.Concat(trailing.Select(static spaces => $"{spaces}\n"));

    private static string Source(Line line, int indentation) =>
        string.Concat(line.Blank.Select(static spaces => $"{spaces}\n"))
        + $"{new string(' ', indentation)}{line.Text}{Colon(line)}\n"
        + string.Concat(line.Body.Select(inner => Source(inner, indentation + line.Indent)));

    private static string Standard(Line line, string indentation) =>
        $"{indentation}{line.Text}{Colon(line)}\n"
        + string.Concat(line.Body.Select(inner => Standard(inner, indentation + "    ")));

    private static string Colon(Line line) =>
        line.Body.Length == 0
        ? ""
        : ":";
}

/// <summary>
/// Example B, the indentation-sensitive language in three layers, and LAY-07's criterion that its errors at a DEDENT, and its "expected statement" errors after a block, point at the dedented line.
/// </summary>
public class WorkedExampleBTests
{
    private const string Program = "x = 1\nwhile x < 10:\n    x = x + 1\n\n    if x < 5:\n      y = x * (2 - x)\nz = 0\n";

    private const string Standard = "x = 1\nwhile (x < 10):\n    x = (x + 1)\n    if (x < 5):\n        y = (x * (2 - x))\nz = 0\n";

    private static readonly Pipeline<char, Script> _mini = Pipeline.From(MiniLexer.Lexer).Then(Layout.Pass).Then(MiniGrammar.Root);

    private static readonly Layer<char, Token> _tokens = MiniLexer.Lexer.Then(Layout.Pass);

    private static List<(TokenKind Kind, long Start, long End)> Tokens(string text) =>
        [.. LayerRun.Items(_tokens, text).Select(static entry => (entry.Item.Kind, entry.Slot.Span.Start, entry.Slot.Span.End))];

    [Fact]
    public void ABlockWithNoBodyReportsTheDedentOnTheDedentedLine() =>
        Assert.Equal(
            """
            error[parser]: expected indent
             --> 3:1
              |
            3 | c = 1
              | ^ unexpected dedent
            """,
            _mini.Parse("while a:\n    if b:\nc = 1\n").Diagnostics.Render()
        );

    [Fact]
    public void AnExpectedStatementErrorAfterABlockPointsAtTheDedentedLine() =>
        Assert.Equal(
            """
            error[parser]: expected statement or end of input
             --> 3:1
              |
            3 | )
              | ^ unexpected ')'
            """,
            _mini.Parse("if a:\n    b = 1\n)\n").Diagnostics.Render()
        );

    [Fact]
    public void AnInconsistentDedentAfterABlockReportsExpectedStatementOnTheDedentedLine() =>
        Assert.Equal(
            """
            error[parser]: expected statement or end of input
             --> 3:3
              |
            3 |   c = 2
              |   ^ unexpected inconsistent dedent
            """,
            _mini.Parse("if a:\n    b = 1\n  c = 2\n").Diagnostics.Render()
        );

    [Fact]
    public void BlankLinesBelongToTheNewlineBeforeThem() =>
        Assert.Equal(
            [
                (TokenKind.If, 0, 2),
                (TokenKind.Name, 3, 4),
                (TokenKind.Colon, 4, 5),
                (TokenKind.Newline, 5, 12),
                (TokenKind.Indent, 12, 12),
                (TokenKind.Name, 12, 13),
                (TokenKind.Assign, 14, 15),
                (TokenKind.Number, 16, 17),
                (TokenKind.Newline, 17, 22),
                (TokenKind.Dedent, 22, 22),
            ],
            Tokens("if a:\n\n  \n  b = 1\n \n  ")
        );

    [Fact]
    public void BlocksStillOpenAtTheEndCloseThere() =>
        Assert.Equal(
            [
                (TokenKind.If, 0, 2),
                (TokenKind.Name, 3, 4),
                (TokenKind.Colon, 4, 5),
                (TokenKind.Newline, 5, 7),
                (TokenKind.Indent, 7, 7),
                (TokenKind.While, 7, 12),
                (TokenKind.Name, 13, 14),
                (TokenKind.Colon, 14, 15),
                (TokenKind.Newline, 15, 19),
                (TokenKind.Indent, 19, 19),
                (TokenKind.Name, 19, 20),
                (TokenKind.Dedent, 20, 20),
                (TokenKind.Dedent, 20, 20),
            ],
            Tokens("if a:\n while b:\n   c")
        );

    [Fact]
    public void IndentsAndDedentsSitAtTheFirstTokenOfTheirLine() =>
        Assert.Equal(
            [
                (TokenKind.If, 0, 2),
                (TokenKind.Name, 3, 4),
                (TokenKind.Colon, 4, 5),
                (TokenKind.Newline, 5, 8),
                (TokenKind.Indent, 8, 8),
                (TokenKind.Name, 8, 9),
                (TokenKind.Assign, 10, 11),
                (TokenKind.Number, 12, 13),
                (TokenKind.Newline, 13, 14),
                (TokenKind.Dedent, 14, 14),
                (TokenKind.Name, 14, 15),
                (TokenKind.Assign, 16, 17),
                (TokenKind.Number, 18, 19),
                (TokenKind.Newline, 19, 20),
            ],
            Tokens("if a:\n  b = 1\nc = 2\n")
        );

    [Fact]
    public void RandomProgramsParseToTheirStandardFormAtEverySegmentSize() =>
        Gen.Select(Programs.All, Gen.OneOf(1, 2, 3, 1024)).Sample(
            static (program, segmentSize) => LayerRun.Run(_tokens, MiniGrammar.Root, program.Source, segmentSize) is (true, var script, _) && script.ToString() == program.Standard,
            iter: 2_000,
            print: static sample => sample.Item1.Source
        );

    [Fact]
    public void ThePipelineParsesAProgram() =>
        Assert.Equal(Standard, Assert.IsType<Parsed<Script>>(_mini.Parse(Program).Value).Value.ToString());

    [Fact]
    public void ThePipelineParsesAProgramFromAReader() =>
        Assert.Equal(Standard, Assert.IsType<Parsed<Script>>(_mini.Parse(new StringReader(Program)).Value).Value.ToString());
}
