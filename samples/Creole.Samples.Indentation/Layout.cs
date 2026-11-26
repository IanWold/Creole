namespace Creole.Samples.Indentation;

/// <summary>
/// The second layer of the indentation-sensitive language: it turns the indentation that newlines record into INDENT and DEDENT tokens, as Python's tokenizer does.
/// </summary>
public sealed class Layout : TokenGrammar<Token, TokenKind>
{
    /// <summary>
    /// The layout pass. Its state is the indentation of each open block, which starts at the top level.
    /// </summary>
    /// <remarks>
    /// Each newline is passed on, followed by the INDENT or DEDENTs <see cref="IndentLevels.Adjust"/> gives for it.
    /// Every other token is passed on as it is.
    /// At the end of the input, a DEDENT closes each block still open.
    /// INDENT and DEDENT are synthetic, so each is zero-width at the start of the first token on the next line.
    /// </remarks>
    public static readonly Layer<Token, Token> Pass = Layer.Transduce(
        initialState: IndentLevels.Root,
        step: OneOf(
            Kind(TokenKind.Newline).Scan<IndentLevels, Emit<Token>>(static (newline, levels, _) => levels.Adjust(newline)),
            Any.Select(static token => Emit.One(token))
        ),
        atEnd: static levels => levels.CloseAll()
    );
}
