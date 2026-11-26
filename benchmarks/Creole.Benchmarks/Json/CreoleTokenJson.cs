using static Creole.Grammar<Creole.Benchmarks.JsonToken>;
using static Creole.TokenGrammar<Creole.Benchmarks.JsonToken, Creole.Benchmarks.JsonKind>;

namespace Creole.Benchmarks;

/// <summary>
/// Worked Example A's parser over <see cref="JsonToken"/>s, building no values: its <c>Value</c> chooses among seven alternatives by token kind.
/// </summary>
public static class CreoleTokenJson
{
    /// <summary>
    /// One JSON value, which a JSON-lines stream parses one at a time.
    /// </summary>
    public static readonly Parser<JsonToken, Unit> Value = Rec(() => OneOf(
        Kind(JsonKind.StringLiteral).Ignore(),
        Kind(JsonKind.Number).Ignore(),
        Kind(JsonKind.True).Ignore(),
        Kind(JsonKind.False).Ignore(),
        Kind(JsonKind.Null).Ignore(),
        _valueArray!,
        _valueObject!
    ));

    private static readonly Parser<JsonToken, Unit> _valueArray =
        Value.SepBy(Kind(JsonKind.Comma)).Between(Kind(JsonKind.LBracket), Kind(JsonKind.RBracket)).Ignore();

    private static readonly Parser<JsonToken, Unit> _valueObject =
        Seq(Kind(JsonKind.StringLiteral), Kind(JsonKind.Colon), Value)
        .SepBy(Kind(JsonKind.Comma))
        .Between(Kind(JsonKind.LBrace), Kind(JsonKind.RBrace))
        .Ignore();

    public static readonly Parser<JsonToken, Unit> Document = Value.Before(End);
}
