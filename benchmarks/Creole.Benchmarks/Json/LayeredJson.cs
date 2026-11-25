using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Benchmarks;

/// <summary>
/// The token JSON parser in two pipelines whose lexers differ only in kind: a <c>Layer.Tokens</c>, and a <c>Layer.Modal</c> with one mode that gives the same item parser.
/// </summary>
public static class LayeredJson
{
    private static readonly Parser<char, Unit> _number =
        Char('-').Optional()
            .Then(Digit.SkipAtLeastOnce())
            .Then(Char('.').Then(Digit.SkipAtLeastOnce()).Optional())
            .Then(OneOf(Char('e'), Char('E')).Then(OneOf(Char('+'), Char('-')).Optional()).Then(Digit.SkipAtLeastOnce()).Optional())
            .Ignore();

    private static readonly Parser<char, Unit> _string =
        Char('"').Then(OneOf(Char('\\').Then(Any).Ignore(), NoneOf('"', '\\').SkipAtLeastOnce()).SkipMany()).Before(Char('"'));

    private static readonly Parser<char, JsonToken> _token = OneOf(
        Char('{').As(new JsonToken(JsonKind.LBrace)),
        Char('}').As(new JsonToken(JsonKind.RBrace)),
        Char('[').As(new JsonToken(JsonKind.LBracket)),
        Char(']').As(new JsonToken(JsonKind.RBracket)),
        Char(':').As(new JsonToken(JsonKind.Colon)),
        Char(',').As(new JsonToken(JsonKind.Comma)),
        _string.As(new JsonToken(JsonKind.StringLiteral)),
        _number.As(new JsonToken(JsonKind.Number)),
        String("true").As(new JsonToken(JsonKind.True)),
        String("false").As(new JsonToken(JsonKind.False)),
        String("null").As(new JsonToken(JsonKind.Null))
    );

    /// <summary>
    /// The channel the modal lexer reads, which nothing changes.
    /// </summary>
    public static readonly ModeChannel<int> Modes = new(0);

    /// <summary>
    /// A one-mode <c>Layer.Modal</c> lexer over JSON text.
    /// </summary>
    public static readonly Layer<char, JsonToken> ModalLexer = Layer.Modal(Modes, static _ => _token, Whitespace.SkipMany());

    /// <summary>
    /// The same lexer as a <c>Layer.Tokens</c>.
    /// </summary>
    public static readonly Layer<char, JsonToken> TokensLexer = Layer.Tokens(_token, Whitespace.SkipMany());

    /// <summary>
    /// The modal lexer, then the token JSON parser.
    /// </summary>
    public static readonly Pipeline<char, Unit> Modal = Pipeline.From(ModalLexer).Then(CreoleTokenJson.Document);

    /// <summary>
    /// The <c>Layer.Tokens</c> lexer, then the token JSON parser.
    /// </summary>
    public static readonly Pipeline<char, Unit> Tokens = Pipeline.From(TokensLexer).Then(CreoleTokenJson.Document);
}
