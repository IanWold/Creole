using System.Globalization;
using System.Text;
using Creole.Samples.Indentation;

namespace Creole.MemoryGate;

/// <summary>
/// Example B as an input of the memory gate: random top-level statements of the indentation-sensitive language, read by its three layers and a root that keeps none of them.
/// </summary>
/// <remarks>
/// Each record is one top-level statement, with blocks up to three deep, random indentation, and blank lines of spaces after some lines.
/// Names use letters that cannot spell a keyword, and every operation is in parentheses, so every record parses.
/// </remarks>
public static class ExampleBInput
{
    private const string Letters = "abcdexyz";

    private static readonly string[] _operators = ["+", "-", "*", "/", "<"];

    private static readonly Pipeline<char, Unit> _pipeline =
        Pipeline.From(MiniLexer.Lexer).Then(Layout.Pass).Then(MiniGrammar.Statement.SkipMany().Before(Grammar<Token>.End));

    private static void BlankLines(Random random, StringBuilder record)
    {
        var lines = random.Next(5) == 0
            ? random.Next(1, 3)
            : 0;

        for (; lines > 0; lines--)
        {
            record.Append(' ', random.Next(4)).Append('\n');
        }
    }

    private static void Expression(Random random, StringBuilder record, int depth)
    {
        if (depth > 0 && random.Next(3) == 0)
        {
            record.Append('(');
            Expression(random, record, depth - 1);
            record.Append(' ').Append(_operators[random.Next(_operators.Length)]).Append(' ');
            Expression(random, record, depth - 1);
            record.Append(')');
        }
        else if (random.Next(2) == 0)
        {
            Name(random, record);
        }
        else
        {
            record.Append(random.Next(1_000).ToString(CultureInfo.InvariantCulture));
        }
    }

    private static void Name(Random random, StringBuilder record)
    {
        for (var letters = random.Next(1, 4); letters > 0; letters--)
        {
            record.Append(Letters[random.Next(Letters.Length)]);
        }
    }

    private static void Statement(Random random, StringBuilder record, int indentation, int depth)
    {
        record.Append(' ', indentation);

        if (depth > 0 && random.Next(3) == 0)
        {
            record.Append(random.Next(2) == 0 ? "if " : "while ");
            Expression(random, record, 2);
            record.Append(":\n");
            BlankLines(random, record);

            var inner = indentation + random.Next(1, 5);

            for (var statements = random.Next(1, 4); statements > 0; statements--)
            {
                Statement(random, record, inner, depth - 1);
            }
        }
        else
        {
            Name(random, record);
            record.Append(" = ");
            Expression(random, record, 2);
            record.Append('\n');
            BlankLines(random, record);
        }
    }

    /// <summary>
    /// Parses the text <paramref name="reader"/> reads as a whole program, in one streamed parse.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <returns>Whether the program parsed.</returns>
    public static bool Parse(TextReader reader) =>
        _pipeline.Parse(reader) is Parsed<Unit>;

    /// <summary>
    /// Writes one top-level statement to <paramref name="record"/>.
    /// </summary>
    /// <param name="random">The source of the statement's choices.</param>
    /// <param name="record">Where to write it.</param>
    public static void Write(Random random, StringBuilder record) =>
        Statement(random, record, indentation: 0, depth: 3);
}
