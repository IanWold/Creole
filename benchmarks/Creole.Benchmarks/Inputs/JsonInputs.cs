using System.Globalization;
using System.Text;

namespace Creole.Benchmarks;

public static class JsonInputs
{
    public const string Large = "large";
    public const string Medium = "medium";
    public const string Small = "small";

    private static readonly Dictionary<string, Lazy<string>> _documents = new()
    {
        [Small] = new(() => Generate(seed: 1, targetBytes: 1_000)),
        [Medium] = new(() => Generate(seed: 2, targetBytes: 100_000)),
        [Large] = new(() => Generate(seed: 3, targetBytes: 1_000_000)),
    };

    private static readonly string[] _escapes = [@"\""", @"\\", @"\n", @"\t", @"\r", @"\u00e9", @"\u4e2d"];

    private static readonly string[] _words =
    [
        "alpha", "bravo", "charlie", "delta", "echo", "foxtrot", "golf", "hotel", "india", "juliett",
        "kilo", "lima", "mike", "november", "oscar", "papa", "quebec", "romeo", "sierra", "tango",
        "café", "naïve", "über", "日本語", "Ελληνικά",
    ];

    public static readonly IReadOnlyList<string> Names = [Small, Medium, Large];

    private static string Generate(int seed, int targetBytes)
    {
        var random = new Random(seed);
        var builder = new StringBuilder();

        builder.Append("[\n");

        for (var id = 0; builder.Length < targetBytes; id++)
        {
            builder.Append(id == 0 ? "" : ",\n");
            builder.Append("  ");
            WriteRecord(builder, random, id, depth: 1);
        }

        builder.Append("\n]\n");

        return builder.ToString();
    }

    private static void WriteNumber(StringBuilder builder, Random random)
    {
        var integer = random.Next(-1_000_000, 1_000_000);
        builder.Append(integer.ToString(CultureInfo.InvariantCulture));

        switch (random.Next(4))
        {
            case 1:
                builder.Append('.').Append(random.Next(0, 1000).ToString("D3", CultureInfo.InvariantCulture));
                break;

            case 2:
                builder.Append('.').Append(random.Next(0, 100).ToString(CultureInfo.InvariantCulture));
                builder.Append(random.Next(100) < 50 ? "e" : "E").Append(random.Next(100) < 50 ? "-" : "+");
                builder.Append(random.Next(0, 20).ToString(CultureInfo.InvariantCulture));
                break;

            default:
                break;
        }
    }

    private static void WriteRecord(StringBuilder builder, Random random, int id, int depth)
    {
        var indent = new string(' ', (depth + 1) * 2);
        var closing = new string(' ', depth * 2);

        builder.Append("{\n");
        builder.Append(indent).Append(CultureInfo.InvariantCulture, $"\"id\": {id},\n");
        builder.Append(indent).Append("\"name\": ");
        WriteString(builder, random);
        builder.Append(",\n").Append(indent).Append("\"active\": ").Append(random.Next(100) < 50 ? "true" : "false");
        builder.Append(",\n").Append(indent).Append("\"score\": ");

        WriteNumber(builder, random);

        builder.Append(",\n").Append(indent).Append("\"note\": ");

        if (random.Next(100) < 30)
        {
            builder.Append("null");
        }
        else
        {
            WriteString(builder, random);
        }

        builder.Append(",\n").Append(indent).Append("\"tags\": [");

        var tags = random.Next(0, 5);
        for (var i = 0; i < tags; i++)
        {
            builder.Append(i == 0 ? "" : ", ");
            WriteString(builder, random);
        }

        builder.Append("],\n").Append(indent).Append("\"values\": [");

        var values = random.Next(0, 6);
        for (var i = 0; i < values; i++)
        {
            builder.Append(i == 0 ? "" : ", ");
            WriteNumber(builder, random);
        }

        builder.Append(']');

        if (depth < 3 && random.Next(100) < 40)
        {
            builder.Append(",\n").Append(indent).Append("\"child\": ");
            WriteRecord(builder, random, id, depth + 1);
        }

        builder.Append('\n').Append(closing).Append('}');
    }

    private static void WriteString(StringBuilder builder, Random random)
    {
        builder.Append('"');

        var words = random.Next(1, 5);
        for (var i = 0; i < words; i++)
        {
            builder.Append(i == 0 ? "" : " ");
            builder.Append(_words[random.Next(_words.Length)]);

            if (random.Next(100) < 10)
            {
                builder.Append(_escapes[random.Next(_escapes.Length)]);
            }
        }

        builder.Append('"');
    }

    public static string Get(string name) =>
        _documents[name].Value;

    public static int Utf8Length(string name) =>
        Encoding.UTF8.GetByteCount(Get(name));
}
