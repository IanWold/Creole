using System.Globalization;

namespace Creole.Benchmarks;

public sealed class CreoleSlicedJson : CharGrammar
{
    private static readonly Parser<char, Unit> _space = Whitespace.SkipMany();

    private static readonly Parser<char, char> _hexDigit = Range('0', '9').Or(Range('a', 'f')).Or(Range('A', 'F'));

    private static readonly Parser<char, Unit> _escape = Char('\\').Then(OneOf(
        Char('"').Ignore(),
        Char('\\').Ignore(),
        Char('/').Ignore(),
        Char('b').Ignore(),
        Char('f').Ignore(),
        Char('n').Ignore(),
        Char('r').Ignore(),
        Char('t').Ignore(),
        Char('u').Then(Seq(_hexDigit, _hexDigit, _hexDigit, _hexDigit)).Ignore()
    ));

    private static readonly Parser<char, string> _text = Token(
        NoneOf('"', '\\').SkipAtLeastOnce()
            .Or(_escape)
            .SkipMany()
            .Slice()
            .Select(static text => Unescape(text.Span))
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

    private static string Unescape(ReadOnlySpan<char> text)
    {
        var backslash = text.IndexOf('\\');

        if (backslash < 0)
        {
            return new string(text);
        }

        Span<char> buffer = text.Length <= 256
            ? stackalloc char[text.Length]
            : new char[text.Length];

        var length = 0;
        var rest = text;

        while (backslash >= 0)
        {
            rest[..backslash].CopyTo(buffer[length..]);
            length += backslash;

            var escaped = rest[backslash + 1];

            if (escaped == 'u')
            {
                buffer[length++] = (char)int.Parse(rest.Slice(backslash + 2, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                rest = rest[(backslash + 6)..];
            }
            else
            {
                buffer[length++] = escaped switch
                {
                    'b' => '\b',
                    'f' => '\f',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => escaped,
                };
                rest = rest[(backslash + 2)..];
            }

            backslash = rest.IndexOf('\\');
        }

        rest.CopyTo(buffer[length..]);
        length += rest.Length;

        return new string(buffer[..length]);
    }

    public static JsonValue Parse(string text) => Document.Parse(text) switch
    {
        Parsed<JsonValue> parsed => parsed.Value,
        Recovered<JsonValue> recovered => throw new FormatException(recovered.Diagnostics.Render()),
        Failed failed => throw new FormatException(failed.Diagnostics.Render()),
    };
}
