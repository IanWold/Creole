using Parlot.Fluent;

namespace Creole.Benchmarks;

public static class ParlotJson
{
    public static readonly Parser<JsonValue> Interpreted = Build();

    private static Parser<JsonValue> Build()
    {
        var lBrace = Terms.Char('{');
        var rBrace = Terms.Char('}');
        var lBracket = Terms.Char('[');
        var rBracket = Terms.Char(']');
        var colon = Terms.Char(':');
        var comma = Terms.Char(',');

        var json = Deferred<JsonValue>();

        var text = Terms.String(StringLiteralQuotes.Double).Then(static s => s.ToString());
        var jsonString = text.Then<JsonValue>(static s => new JsonString(s));
        var number = Terms.Number<double>(NumberOptions.Float).Then<JsonValue>(static n => new JsonNumber(n));
        var literal = Terms.Text("true").Then<JsonValue>(JsonBool.True)
            .Or(Terms.Text("false").Then<JsonValue>(JsonBool.False))
            .Or(Terms.Text("null").Then<JsonValue>(JsonNull.Instance));

        var array = Between(lBracket, Separated(comma, json, min: 0), rBracket)
            .Then<JsonValue>(static items => new JsonArray(items));

        var member = text.AndSkip(colon).And(json)
            .Then(static m => new KeyValuePair<string, JsonValue>(m.Item1, m.Item2));
        var obj = Between(lBrace, Separated(comma, member, min: 0), rBrace)
            .Then<JsonValue>(static members => new JsonObject(members));

        json.Parser = obj.Or(array).Or(jsonString).Or(number).Or(literal);

        return json.AndSkip(SkipWhiteSpace(Always())).Eof();
    }

    public static JsonValue Parse(Parser<JsonValue> parser, string text) =>
        parser.TryParse(text, out var value) && value is not null
        ? value
        : throw new FormatException("Parlot failed to parse JSON.");
}
