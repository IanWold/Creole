using System.Globalization;
using Pidgin;
using static Pidgin.Parser;

namespace Creole.Benchmarks;

public static class PidginJson
{
    public static readonly Pidgin.Parser<char, JsonValue> Parser = Build();

    private static Pidgin.Parser<char, JsonValue> Build()
    {
        static Pidgin.Parser<char, T> Tok<T>(Pidgin.Parser<char, T> parser) => parser.Before(SkipWhitespaces);
        static Pidgin.Parser<char, char> Sym(char c) => Tok(Char(c));

        var escape = Char('\\').Then(OneOf(
            Char('"'),
            Char('\\'),
            Char('/'),
            Char('b').ThenReturn('\b'),
            Char('f').ThenReturn('\f'),
            Char('n').ThenReturn('\n'),
            Char('r').ThenReturn('\r'),
            Char('t').ThenReturn('\t'),
            Char('u').Then(Pidgin.Parser<char>.Token(char.IsAsciiHexDigit).Repeat(4).Select(static hex => (char)int.Parse(new string([.. hex]), NumberStyles.HexNumber, CultureInfo.InvariantCulture)))
        ));

        var text = Tok(AnyCharExcept('"', '\\').AtLeastOnceString().Or(escape.Select(static c => c.ToString()))
            .Many()
            .Select(static parts => string.Concat(parts))
            .Between(Char('"')));

        var digits = Digit.SkipAtLeastOnce();
        var number = Tok(
            Char('-')
            .Optional()
            .Then(digits)
            .Then(Char('.').Then(digits).Optional())
            .Then(OneOf(Char('e'), Char('E')).Then(OneOf(Char('+'), Char('-')).Optional()).Then(digits).Optional())
            .Slice(static (span, _) => double.Parse(span, CultureInfo.InvariantCulture))
        );

        Pidgin.Parser<char, JsonValue> json = null!;
        var value = Rec(() => json);

        var array = value.Separated(Sym(','))
            .Between(Sym('['), Sym(']'))
            .Select<JsonValue>(static items => new JsonArray(Lists.AsReadOnlyList(items)));

        var member = Map(static (name, v) => new KeyValuePair<string, JsonValue>(name, v), text.Before(Sym(':')), value);
        var obj = member.Separated(Sym(','))
            .Between(Sym('{'), Sym('}'))
            .Select<JsonValue>(static members => new JsonObject(Lists.AsReadOnlyList(members)));

        json = OneOf(
            obj,
            array,
            text.Select<JsonValue>(static s => new JsonString(s)),
            number.Select<JsonValue>(static n => new JsonNumber(n)),
            Tok(String("true")).ThenReturn<JsonValue>(JsonBool.True),
            Tok(String("false")).ThenReturn<JsonValue>(JsonBool.False),
            Tok(String("null")).ThenReturn<JsonValue>(JsonNull.Instance)
        );

        return SkipWhitespaces.Then(json).Before(Pidgin.Parser<char>.End);
    }

    public static JsonValue Parse(string text) => Parser.ParseOrThrow(text);
}
