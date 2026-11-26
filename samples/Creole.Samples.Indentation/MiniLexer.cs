namespace Creole.Samples.Indentation;

/// <summary>
/// The first layer of the indentation-sensitive language: it turns characters into tokens.
/// </summary>
/// <remarks>
/// Spaces between tokens are skipped. A newline token takes in the blank lines after it and the next line's indentation, and records its width.
/// </remarks>
public sealed class MiniLexer : CharGrammar
{
    private static readonly Parser<char, Token> _newline =
        Seq(
            Char('\n').Then(Atomic(Char(' ').SkipMany().Then(Char('\n'))).SkipMany()).Then(Char(' ').SkipMany()).Slice(),
            OneOf(End.As(true), Return(false))
        )
        .Select(static (text, atEnd) => new Token(
            TokenKind.Newline,
            text,
            atEnd
                ? 0
                : text.Length - text.Span.LastIndexOf('\n') - 1
        ));

    /// <summary>
    /// The lexer, which produces names, numbers, keywords, punctuation and newlines.
    /// </summary>
    public static readonly Layer<char, Token> Lexer = Layer.Tokens(
        item: OneOf(
            _newline,
            Longest(
                Lex(String("if"), TokenKind.If),
                Lex(String("while"), TokenKind.While),
                Lex(Letter.Then(LetterOrDigit.SkipMany()), TokenKind.Name),
                Lex(Digit.SkipAtLeastOnce(), TokenKind.Number),
                Lex(Char('='), TokenKind.Assign),
                Lex(Char('+'), TokenKind.Plus),
                Lex(Char('-'), TokenKind.Minus),
                Lex(Char('*'), TokenKind.Star),
                Lex(Char('/'), TokenKind.Slash),
                Lex(Char('<'), TokenKind.Less),
                Lex(Char('('), TokenKind.LeftParenthesis),
                Lex(Char(')'), TokenKind.RightParenthesis),
                Lex(Char(':'), TokenKind.Colon)
            )
        ),
        skip: Char(' ').SkipMany()
    );

    private static Parser<char, Token> Lex<T>(Parser<char, T> parser, TokenKind kind) =>
        parser.Slice().Select(text => new Token(kind, text));
}
