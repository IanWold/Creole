using System.Globalization;
using System.Text;
using Creole.Benchmarks;

namespace Creole.MemoryGate;

/// <summary>
/// A JSON-lines stream as an input of the memory gate: one random object per line, read by the benchmarks' token lexer and token JSON parser, one value at a time.
/// </summary>
/// <remarks>
/// Each record is one object with nested objects up to two deep, strings with escapes, numbers with fractions and exponents, and arrays.
/// The text is ASCII, so its length in characters is its length in UTF-8 bytes.
/// </remarks>
public static class JsonLinesInput
{
    private static readonly string[] _escapes = [@"\""", @"\\", @"\n", @"\t", @"é"];

    private static readonly Pipeline<char, Unit> _pipeline = Pipeline.From(LayeredJson.TokensLexer).Then(CreoleTokenJson.Value);

    private static readonly string[] _words = ["alpha", "bravo", "charlie", "delta", "echo", "foxtrot", "golf", "hotel", "india", "juliett"];

    private static void Number(Random random, StringBuilder record)
    {
        record.Append(random.Next(-1_000_000, 1_000_000).ToString(CultureInfo.InvariantCulture));

        switch (random.Next(4))
        {
            case 1:
                record.Append('.').Append(random.Next(1_000).ToString(CultureInfo.InvariantCulture));
                break;

            case 2:
                record.Append('.').Append(random.Next(100).ToString(CultureInfo.InvariantCulture));
                record.Append(random.Next(2) == 0 ? "e-" : "E+").Append(random.Next(20).ToString(CultureInfo.InvariantCulture));
                break;

            default:
                break;
        }
    }

    private static void Object(Random random, StringBuilder record, int depth)
    {
        record.Append("{\"id\":").Append(random.Next(1_000_000).ToString(CultureInfo.InvariantCulture));
        record.Append(",\"name\":");
        String(random, record);
        record.Append(",\"active\":").Append(random.Next(2) == 0 ? "true" : "false");
        record.Append(",\"score\":");
        Number(random, record);
        record.Append(",\"note\":");

        if (random.Next(10) < 3)
        {
            record.Append("null");
        }
        else
        {
            String(random, record);
        }

        record.Append(",\"tags\":[");

        for (var tag = random.Next(5); tag > 0; tag--)
        {
            String(random, record);
            record.Append(tag > 1 ? "," : "");
        }

        record.Append("],\"values\":[");

        for (var value = random.Next(6); value > 0; value--)
        {
            Number(random, record);
            record.Append(value > 1 ? "," : "");
        }

        record.Append(']');

        if (depth < 2 && random.Next(10) < 4)
        {
            record.Append(",\"child\":");
            Object(random, record, depth + 1);
        }

        record.Append('}');
    }

    private static void String(Random random, StringBuilder record)
    {
        record.Append('"');

        for (var word = random.Next(1, 5); word > 0; word--)
        {
            record.Append(_words[random.Next(_words.Length)]);

            if (random.Next(10) == 0)
            {
                record.Append(_escapes[random.Next(_escapes.Length)]);
            }

            record.Append(word > 1 ? " " : "");
        }

        record.Append('"');
    }

    /// <summary>
    /// Parses the text <paramref name="reader"/> reads as a sequence of values, one streamed item at a time.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <returns>Whether every value parsed.</returns>
    public static bool Parse(TextReader reader) =>
        _pipeline.ParseMany(reader).All(static result => result is Parsed<Unit>);

    /// <summary>
    /// Writes one object and a newline to <paramref name="record"/>.
    /// </summary>
    /// <param name="random">The source of the object's choices.</param>
    /// <param name="record">Where to write it.</param>
    public static void Write(Random random, StringBuilder record)
    {
        Object(random, record, depth: 0);
        record.Append('\n');
    }
}
