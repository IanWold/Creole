namespace Creole.Samples.Indentation;

/// <summary>
/// The third layer of the indentation-sensitive language: the parser, which reads statements and blocks from the tokens of the layout pass.
/// </summary>
/// <remarks>
/// A block is a colon, a newline, an INDENT, one or more statements, and a DEDENT.
/// Expressions are kept as text with every operation in parentheses, such as <c>(a + (b * 2))</c>.
/// </remarks>
public sealed class MiniGrammar : TokenGrammar<Token, TokenKind>
{
    private static readonly Parser<Token, string> _expression = Rec(() => Expression.Build(
        OneOf(
            Kind(TokenKind.Name).Select(static name => name.Text.ToString()),
            Kind(TokenKind.Number).Select(static number => number.Text.ToString()),
            _expression!.Between(Kind(TokenKind.LeftParenthesis), Kind(TokenKind.RightParenthesis))
        ),
        static operators =>
        [
            [
                operators.InfixL(Kind(TokenKind.Star), static (left, _, right) => $"({left} * {right})"),
                operators.InfixL(Kind(TokenKind.Slash), static (left, _, right) => $"({left} / {right})"),
            ],
            [
                operators.InfixL(Kind(TokenKind.Plus), static (left, _, right) => $"({left} + {right})"),
                operators.InfixL(Kind(TokenKind.Minus), static (left, _, right) => $"({left} - {right})"),
            ],
            [operators.InfixN(Kind(TokenKind.Less), static (left, _, right) => $"({left} < {right})")],
        ]
    )).Named("expression");

    private static readonly Parser<Token, Statement> _assignment =
        Seq(Kind(TokenKind.Name), Kind(TokenKind.Assign), _expression, Kind(TokenKind.Newline))
            .Select(static (name, _, value, _) => (Statement)new Assignment(name.Text.ToString(), value));

    /// <summary>
    /// One statement: an <c>if</c>, a <c>while</c> or an assignment.
    /// </summary>
    public static readonly Parser<Token, Statement> Statement = Rec(() => OneOf(_if!, _while!, _assignment)).Named("statement");

    private static readonly Parser<Token, Block> _suite =
        Seq(Kind(TokenKind.Colon), Kind(TokenKind.Newline), Kind(TokenKind.Indent), Statement.AtLeastOnce(), Kind(TokenKind.Dedent))
            .Select(static (_, _, _, body, _) => new Block(body));

    private static readonly Parser<Token, Statement> _if =
        Seq(Kind(TokenKind.If), _expression, _suite)
            .Select(static (_, condition, body) => (Statement)new IfStatement(condition, body));

    private static readonly Parser<Token, Statement> _while =
        Seq(Kind(TokenKind.While), _expression, _suite)
            .Select(static (_, condition, body) => (Statement)new WhileStatement(condition, body));

    /// <summary>
    /// A whole program: statements to the end of the input.
    /// </summary>
    public static readonly Parser<Token, Script> Root =
        Statement.Many().Before(End).Select(static statements => new Script(statements));
}
