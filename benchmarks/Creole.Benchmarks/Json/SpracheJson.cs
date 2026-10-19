using System.Globalization;
using Sprache;
using SpracheParse = Sprache.Parse;

namespace Creole.Benchmarks;

public static class SpracheJson
{
    private static readonly Parser<string> _digits = SpracheParse.Digit.AtLeastOnce().Text();

    private static readonly Parser<char> _escape =
        from _ in SpracheParse.Char('\\')
        from c in SpracheParse.Char('"')
            .Or(SpracheParse.Char('\\'))
            .Or(SpracheParse.Char('/'))
            .Or(SpracheParse.Char('b').Return('\b'))
            .Or(SpracheParse.Char('f').Return('\f'))
            .Or(SpracheParse.Char('n').Return('\n'))
            .Or(SpracheParse.Char('r').Return('\r'))
            .Or(SpracheParse.Char('t').Return('\t'))
            .Or(SpracheParse.Char('u')
                .Then(_ => SpracheParse.Chars("0123456789abcdefABCDEF").Repeat(4).Text())
                .Select(static hex => (char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            )
        select c;

    private static readonly Parser<JsonValue> _literal =
        SpracheParse.String("true").Return((JsonValue)JsonBool.True)
            .Or(SpracheParse.String("false").Return((JsonValue)JsonBool.False))
            .Or(SpracheParse.String("null").Return((JsonValue)JsonNull.Instance))
            .Token();

    private static readonly Parser<JsonValue> _number =
        (
            from sign in SpracheParse.Char('-').Once().Text().Optional()
            from integer in _digits
            from fraction in SpracheParse.Char('.').Then(_ => _digits).Select(static d => "." + d).Optional()
            from exponent in (
                from e in SpracheParse.Chars("eE")
                from s in SpracheParse.Chars("+-").Once().Text().Optional()
                from d in _digits
                select "e" + s.GetOrElse("") + d
            ).Optional()
            select (JsonValue)new JsonNumber(double.Parse(
                sign.GetOrElse("") + integer + fraction.GetOrElse("") + exponent.GetOrElse(""),
                CultureInfo.InvariantCulture
            ))
        ).Token();

    private static readonly Parser<string> _text =
        (
            from _ in SpracheParse.Char('"')
            from chars in _escape.Or(SpracheParse.CharExcept("\"\\")).Many()
            from __ in SpracheParse.Char('"')
            select new string([.. chars])
        ).Token();

    private static readonly Parser<JsonValue> _value =
        SpracheParse.Ref(() => _object!)
            .Or(SpracheParse.Ref(() => _array!))
            .Or(_text.Select(static s => (JsonValue)new JsonString(s)))
            .Or(_number)
            .Or(_literal);

    private static readonly Parser<JsonValue> _array =
        from _ in SpracheParse.Char('[').Token()
        from items in _value.DelimitedBy(SpracheParse.Char(',').Token()).Optional()
        from __ in SpracheParse.Char(']').Token()
        select (JsonValue)new JsonArray(items.IsDefined ? Lists.AsReadOnlyList(items.Get()) : []);

    private static readonly Parser<JsonValue> _document = _value.End();

    private static readonly Parser<KeyValuePair<string, JsonValue>> _member =
        from name in _text
        from _ in SpracheParse.Char(':').Token()
        from value in _value
        select new KeyValuePair<string, JsonValue>(name, value);

    private static readonly Parser<JsonValue> _object =
        from _ in SpracheParse.Char('{').Token()
        from members in _member.DelimitedBy(SpracheParse.Char(',').Token()).Optional()
        from __ in SpracheParse.Char('}').Token()
        select (JsonValue)new JsonObject(members.IsDefined ? Lists.AsReadOnlyList(members.Get()) : []);

    public static JsonValue Parse(string text) =>
        _document.Parse(text);
}
