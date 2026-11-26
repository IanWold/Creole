namespace Creole.Samples.Indentation;

/// <summary>
/// The kinds of token in the indentation-sensitive language.
/// </summary>
public enum TokenKind
{
    /// <summary>
    /// A name, such as <c>count</c>.
    /// </summary>
    Name,

    /// <summary>
    /// A whole number, such as <c>42</c>.
    /// </summary>
    Number,

    /// <summary>
    /// The keyword <c>if</c>.
    /// </summary>
    If,

    /// <summary>
    /// The keyword <c>while</c>.
    /// </summary>
    While,

    /// <summary>
    /// <c>=</c>.
    /// </summary>
    Assign,

    /// <summary>
    /// <c>+</c>.
    /// </summary>
    Plus,

    /// <summary>
    /// <c>-</c>.
    /// </summary>
    Minus,

    /// <summary>
    /// <c>*</c>.
    /// </summary>
    Star,

    /// <summary>
    /// <c>/</c>.
    /// </summary>
    Slash,

    /// <summary>
    /// <c>&lt;</c>.
    /// </summary>
    Less,

    /// <summary>
    /// <c>(</c>.
    /// </summary>
    LeftParenthesis,

    /// <summary>
    /// <c>)</c>.
    /// </summary>
    RightParenthesis,

    /// <summary>
    /// <c>:</c>, which ends the header of a block.
    /// </summary>
    Colon,

    /// <summary>
    /// The end of a line, with any blank lines after it and the indentation of the next line.
    /// </summary>
    Newline,

    /// <summary>
    /// The start of a block: the next line is indented further than the one before. The layout pass makes it.
    /// </summary>
    Indent,

    /// <summary>
    /// The end of a block: the next line is indented less. The layout pass makes one for each block it ends.
    /// </summary>
    Dedent,

    /// <summary>
    /// A line indented less than the line before, but not as far back as any block that is open. The layout pass makes it.
    /// </summary>
    Error,
}
