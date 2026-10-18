using System.Globalization;
using System.Text;

namespace Creole.Benchmarks.Json;

public abstract class JsonValue
{
    public string ToCanonicalString()
    {
        var builder = new StringBuilder();
        Write(builder, this);
        return builder.ToString();
    }

    private static void Write(StringBuilder builder, JsonValue value)
    {
        switch (value)
        {
            case JsonNull:
                builder.Append("null");
                break;

            case JsonBool b:
                builder.Append(b.Value ? "true" : "false");
                break;

            case JsonNumber n:
                builder.Append(n.Value.ToString("R", CultureInfo.InvariantCulture));
                break;

            case JsonString s:
                WriteString(builder, s.Value);
                break;

            case JsonArray a:
                builder.Append('[');

                for (var i = 0; i < a.Items.Count; i++)
                {
                    builder.Append(i == 0 ? "" : ",");
                    Write(builder, a.Items[i]);
                }

                builder.Append(']');
                break;

            case JsonObject o:
                builder.Append('{');

                for (var i = 0; i < o.Members.Count; i++)
                {
                    builder.Append(i == 0 ? "" : ",");
                    WriteString(builder, o.Members[i].Key);

                    builder.Append(':');
                    Write(builder, o.Members[i].Value);
                }

                builder.Append('}');
                break;

            default:
                throw new InvalidOperationException($"Unknown JSON value {value.GetType()}.");
        }
    }

    private static void WriteString(StringBuilder builder, string value)
    {
        builder.Append('"');

        foreach (var c in value)
        {
            switch (c)
            {
                case '"' or '\\':
                case < ' ':
                case > '~':
                    builder.Append(CultureInfo.InvariantCulture, $"\\u{(int)c:x4}");
                    break;

                default:
                    builder.Append(c);
                    break;
            }
        }

        builder.Append('"');
    }
}

public sealed class JsonNull : JsonValue
{
    public static readonly JsonNull Instance = new();

    private JsonNull() { }
}

public sealed class JsonBool : JsonValue
{
    public static readonly JsonBool True = new(true);
    public static readonly JsonBool False = new(false);

    private JsonBool(bool value) => Value = value;

    public bool Value { get; }
}

public sealed class JsonNumber(double value) : JsonValue
{
    public double Value { get; } = value;
}

public sealed class JsonString(string value) : JsonValue
{
    public string Value { get; } = value;
}

public sealed class JsonArray(IReadOnlyList<JsonValue> items) : JsonValue
{
    public IReadOnlyList<JsonValue> Items { get; } = items;
}

public sealed class JsonObject(IReadOnlyList<KeyValuePair<string, JsonValue>> members) : JsonValue
{
    public IReadOnlyList<KeyValuePair<string, JsonValue>> Members { get; } = members;
}

internal static class Lists
{
    public static IReadOnlyList<T> AsReadOnlyList<T>(IEnumerable<T> items) => items as IReadOnlyList<T> ?? [.. items];
}
