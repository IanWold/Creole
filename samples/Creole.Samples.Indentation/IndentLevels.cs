namespace Creole.Samples.Indentation;

/// <summary>
/// The indentation of each block open at a point in the input, innermost first: the state of the layout pass.
/// </summary>
/// <remarks>
/// It is immutable, so backtracking puts it back by keeping a reference to it.
/// </remarks>
/// <param name="Width">The number of spaces that indent the innermost open block, or 0 at the top level.</param>
/// <param name="Enclosing">The blocks around the innermost one, or <see langword="null"/> at the top level.</param>
public sealed record IndentLevels(int Width, IndentLevels? Enclosing)
{
    private static readonly Token _dedent = new(TokenKind.Dedent, ReadOnlyMemory<char>.Empty);

    private static readonly Token _inconsistentDedent = new(TokenKind.Error, ReadOnlyMemory<char>.Empty);

    private static readonly Token _indent = new(TokenKind.Indent, ReadOnlyMemory<char>.Empty);

    /// <summary>
    /// The top level, where no block is open.
    /// </summary>
    public static IndentLevels Root { get; } = new(0, null);

    /// <summary>
    /// The tokens a newline stands for, and the blocks open on the line after it.
    /// </summary>
    /// <param name="newline">A newline token, whose width is the next line's indentation.</param>
    /// <returns>
    /// The newline, then a synthetic INDENT if the next line is indented further, or a synthetic DEDENT for each block it ends if it is indented less.
    /// If it is indented less, but not as far back as a block that is open, an <see cref="TokenKind.Error"/> token follows the DEDENTs.
    /// </returns>
    public (Emit<Token> Tokens, IndentLevels Levels) Adjust(Token newline)
    {
        if (newline.Width == Width)
        {
            return (newline, this);
        }

        if (newline.Width > Width)
        {
            return ([newline, Emit.Synthetic(_indent)], new IndentLevels(newline.Width, this));
        }

        List<Emitted<Token>> tokens = [newline];
        var levels = this;

        while (newline.Width < levels.Width)
        {
            tokens.Add(Emit.Synthetic(_dedent));
            levels = levels.Enclosing!;
        }

        if (newline.Width != levels.Width)
        {
            tokens.Add(Emit.Synthetic(_inconsistentDedent));
        }

        return ([.. tokens], levels);
    }

    /// <summary>
    /// A DEDENT for each block still open at the end of the input.
    /// </summary>
    public Emit<Token> CloseAll()
    {
        List<Emitted<Token>> dedents = [];

        for (var levels = this; levels.Enclosing is not null; levels = levels.Enclosing)
        {
            dedents.Add(Emit.Synthetic(_dedent));
        }

        return [.. dedents];
    }
}
