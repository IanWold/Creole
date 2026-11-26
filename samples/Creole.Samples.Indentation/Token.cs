namespace Creole.Samples.Indentation;

/// <summary>
/// A token of the indentation-sensitive language.
/// </summary>
/// <param name="Kind">The kind of token.</param>
/// <param name="Text">The source text the lexer read it from. It is empty for the tokens the layout pass makes.</param>
/// <param name="Width">For a newline, the number of spaces that indent the next line that is not blank, or 0 at the end of the input. 0 for every other token.</param>
public readonly record struct Token(TokenKind Kind, ReadOnlyMemory<char> Text, int Width = 0) : IKinded<TokenKind>
{
    static string IKinded<TokenKind>.Describe(TokenKind kind) => kind switch
    {
        TokenKind.Name => "name",
        TokenKind.Number => "number",
        TokenKind.If => "'if'",
        TokenKind.While => "'while'",
        TokenKind.Assign => "'='",
        TokenKind.Plus => "'+'",
        TokenKind.Minus => "'-'",
        TokenKind.Star => "'*'",
        TokenKind.Slash => "'/'",
        TokenKind.Less => "'<'",
        TokenKind.LeftParenthesis => "'('",
        TokenKind.RightParenthesis => "')'",
        TokenKind.Colon => "':'",
        TokenKind.Newline => "newline",
        TokenKind.Indent => "indent",
        TokenKind.Dedent => "dedent",
        TokenKind.Error => "inconsistent dedent",
        _ => kind.ToString(),
    };

    string? IKinded<TokenKind>.Detail =>
        Kind is TokenKind.Name or TokenKind.Number
        ? Text.ToString()
        : null;
}
