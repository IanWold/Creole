using System.Text.Json;

namespace Creole.Benchmarks;

public static class SystemTextJsonBaseline
{
    private static JsonValue ReadValue(ref Utf8JsonReader reader)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return JsonNull.Instance;

            case JsonTokenType.True:
                return JsonBool.True;

            case JsonTokenType.False:
                return JsonBool.False;

            case JsonTokenType.Number:
                return new JsonNumber(reader.GetDouble());

            case JsonTokenType.String:
                return new JsonString(reader.GetString()!);

            case JsonTokenType.StartArray:
                var items = new List<JsonValue>();

                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    items.Add(ReadValue(ref reader));
                }

                return new JsonArray(items);

            case JsonTokenType.StartObject:
                var members = new List<KeyValuePair<string, JsonValue>>();

                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    var name = reader.GetString()!;

                    reader.Read();
                    members.Add(new(name, ReadValue(ref reader)));
                }

                return new JsonObject(members);

            default:
                throw new JsonException($"Unexpected token {reader.TokenType}.");
        }
    }

    public static JsonValue Parse(byte[] utf8)
    {
        var reader = new Utf8JsonReader(utf8);
        reader.Read();
        return ReadValue(ref reader);
    }
}
