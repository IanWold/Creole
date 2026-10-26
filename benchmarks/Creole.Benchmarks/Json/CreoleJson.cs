using System.Globalization;

namespace Creole.Benchmarks;

public sealed class CreoleJson : CharGrammar
{
    private static readonly Parser<char, Unit> _space = Whitespace.SkipMany();

    private static readonly Parser<char, char> _hexDigit = Range('0', '9').Or(Range('a', 'f')).Or(Range('A', 'F'));

    private static readonly Parser<char, char> _escape = Char('\\').Then(OneOf(
        Char('"'),
        Char('\\'),
        Char('/'),
        Char('b').As('\b'),
        Char('f').As('\f'),
        Char('n').As('\n'),
        Char('r').As('\r'),
        Char('t').As('\t'),
        Char('u').Then(_hexDigit.Repeat(4).Slice()).Select(static hex => (char)int.Parse(hex.Span, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
    ));

    private static readonly Parser<char, string> _text = Token(
        NoneOf('"', '\\').SkipAtLeastOnce().Text()
            .Or(_escape.Select(static c => c.ToString()))
            .Many()
            .Select(static parts => string.Concat(parts))
            .Between(Char('"'), Char('"'))
    );

    private static readonly Parser<char, Unit> _digits = Digit.SkipAtLeastOnce();

    private static readonly Parser<char, double> _number = Token(
        Seq(
            Char('-').Optional(),
            _digits,
            Char('.').Then(_digits).Optional(),
            Seq(Char('e').Or(Char('E')), Char('+').Or(Char('-')).Optional(), _digits).Optional()
        )
        .Slice()
        .Select(static text => double.Parse(text.Span, CultureInfo.InvariantCulture))
    );

    private static readonly Parser<char, JsonValue> _value = Rec(() => OneOf(
        _object!,
        _array!,
        _text.Select<JsonValue>(static s => new JsonString(s)),
        _number.Select<JsonValue>(static n => new JsonNumber(n)),
        Token(String("true")).As<JsonValue>(JsonBool.True),
        Token(String("false")).As<JsonValue>(JsonBool.False),
        Token(String("null")).As<JsonValue>(JsonNull.Instance)
    ));

    private static readonly Parser<char, JsonValue> _array =
        _value.SepBy(Token(Char(',')))
            .Between(Token(Char('[')), Token(Char(']')))
            .Select<JsonValue>(static items => new JsonArray(items));

    private static readonly Parser<char, JsonValue> _object =
        Seq(_text, Token(Char(':')), _value)
            .Select(static (name, _, value) => KeyValuePair.Create(name, value))
            .SepBy(Token(Char(',')))
            .Between(Token(Char('{')), Token(Char('}')))
            .Select<JsonValue>(static members => new JsonObject(members));

    public static readonly Parser<char, JsonValue> Document = _space.Then(_value).Before(End);

    private static Parser<char, T> Token<T>(Parser<char, T> parser) =>
        parser.Before(_space);

    public static JsonValue Parse(string text) => Document.Parse(text) switch
    {
        Parsed<JsonValue> parsed => parsed.Value,
        Recovered<JsonValue> recovered => throw new FormatException(recovered.Diagnostics.Render()),
        Failed failed => throw new FormatException(failed.Diagnostics.Render()),
    };
}
