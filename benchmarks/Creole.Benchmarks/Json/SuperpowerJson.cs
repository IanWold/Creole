using System.Globalization;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using Superpower.Tokenizers;
using SuperpowerParse = Superpower.Parse;

namespace Creole.Benchmarks.Json;

public static class SuperpowerJson
{
    private enum JsonToken
    {
        LBrace,
        RBrace,
        LBracket,
        RBracket,
        Colon,
        Comma,
        String,
        Number,
        True,
        False,
        Null,
    }

    private static readonly TextParser<Superpower.Model.Unit> _digits = Character.Digit.IgnoreThen(Character.Digit.IgnoreMany());

    private static readonly TextParser<Superpower.Model.Unit> _numberToken =
        Character.EqualTo('-').OptionalOrDefault()
        .IgnoreThen(_digits)
        .IgnoreThen(Character.EqualTo('.').IgnoreThen(_digits).OptionalOrDefault())
        .IgnoreThen(Character.In('e', 'E').IgnoreThen(Character.In('+', '-').OptionalOrDefault()).IgnoreThen(_digits).OptionalOrDefault());

    private static readonly TextParser<Superpower.Model.Unit> _stringToken =
        Character.EqualTo('"')
        .IgnoreThen(Character.EqualTo('\\').IgnoreThen(Character.AnyChar).Or(Character.ExceptIn('"', '\\')).IgnoreMany())
        .IgnoreThen(Character.EqualTo('"').Value(Superpower.Model.Unit.Value));

    private static readonly TextParser<char> _escape =
        Character.EqualTo('\\').IgnoreThen(
            Character.EqualTo('"')
            .Or(Character.EqualTo('\\'))
            .Or(Character.EqualTo('/'))
            .Or(Character.EqualTo('b').Value('\b'))
            .Or(Character.EqualTo('f').Value('\f'))
            .Or(Character.EqualTo('n').Value('\n'))
            .Or(Character.EqualTo('r').Value('\r'))
            .Or(Character.EqualTo('t').Value('\t'))
            .Or(Character.EqualTo('u')
                .IgnoreThen(Span.Length(4))
                .Select(static hex => (char)int.Parse(hex.ToStringValue(), NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            )
        );

    private static readonly TextParser<string> _stringText =
        Character.EqualTo('"')
        .IgnoreThen(_escape.Or(Character.ExceptIn('"', '\\')).Many())
        .Then(static chars => Character.EqualTo('"').Value(new string(chars)));

    private static readonly Tokenizer<JsonToken> _tokenizer =
        new TokenizerBuilder<JsonToken>()
        .Ignore(Span.WhiteSpace)
        .Match(Character.EqualTo('{'), JsonToken.LBrace)
        .Match(Character.EqualTo('}'), JsonToken.RBrace)
        .Match(Character.EqualTo('['), JsonToken.LBracket)
        .Match(Character.EqualTo(']'), JsonToken.RBracket)
        .Match(Character.EqualTo(':'), JsonToken.Colon)
        .Match(Character.EqualTo(','), JsonToken.Comma)
        .Match(_stringToken, JsonToken.String)
        .Match(_numberToken, JsonToken.Number)
        .Match(Span.EqualTo("true"), JsonToken.True, requireDelimiters: true)
        .Match(Span.EqualTo("false"), JsonToken.False, requireDelimiters: true)
        .Match(Span.EqualTo("null"), JsonToken.Null, requireDelimiters: true)
        .Build();

    private static readonly TokenListParser<JsonToken, string> _text =
        Token.EqualTo(JsonToken.String).Apply(_stringText);

    private static readonly TokenListParser<JsonToken, JsonValue> _number =
        Token.EqualTo(JsonToken.Number).Select<JsonToken, Token<JsonToken>, JsonValue>(static token =>
            new JsonNumber(double.Parse(token.Span.Source.AsSpan(token.Span.Position.Absolute, token.Span.Length), CultureInfo.InvariantCulture))
        );

    private static readonly TokenListParser<JsonToken, JsonValue> _value =
        SuperpowerParse.Ref(() => _object!)
        .Or(SuperpowerParse.Ref(() => _array!))
        .Or(_text.Select(static s => (JsonValue)new JsonString(s)))
        .Or(_number)
        .Or(Token.EqualTo(JsonToken.True).Value((JsonValue)JsonBool.True))
        .Or(Token.EqualTo(JsonToken.False).Value((JsonValue)JsonBool.False))
        .Or(Token.EqualTo(JsonToken.Null).Value((JsonValue)JsonNull.Instance));

    private static readonly TokenListParser<JsonToken, JsonValue> _array =
        from _ in Token.EqualTo(JsonToken.LBracket)
        from items in _value.ManyDelimitedBy(Token.EqualTo(JsonToken.Comma), end: Token.EqualTo(JsonToken.RBracket))
        select (JsonValue)new JsonArray(items);

    private static readonly TokenListParser<JsonToken, KeyValuePair<string, JsonValue>> _member =
        from name in _text
        from _ in Token.EqualTo(JsonToken.Colon)
        from value in _value
        select new KeyValuePair<string, JsonValue>(name, value);

    private static readonly TokenListParser<JsonToken, JsonValue> _object =
        from _ in Token.EqualTo(JsonToken.LBrace)
        from members in _member.ManyDelimitedBy(Token.EqualTo(JsonToken.Comma), end: Token.EqualTo(JsonToken.RBrace))
        select (JsonValue)new JsonObject(members);

    private static readonly TokenListParser<JsonToken, JsonValue> _document = _value.AtEnd();

    public static JsonValue Parse(string text) =>
        _document.Parse(_tokenizer.Tokenize(text));
}
