namespace Creole;

/// <summary>
/// Limits on how much of an error <see cref="Diagnostics.Render()"/> and <see cref="ParseError.ToString()"/> show, so that rendering an error on untrusted input stays small.
/// </summary>
/// <remarks>
/// <para>
/// The limits apply only to rendered text. A <see cref="ParseError"/> keeps every context line and its unexpected item's full display.
/// </para>
/// <para>
/// Options are immutable. Make a variant with a <see langword="with"/> expression, such as <c>RenderOptions.Default with { MaxNotes = 4 }</c>.
/// </para>
/// </remarks>
public sealed record RenderOptions
{
    private readonly int _maxNotes = 10;
    private readonly int _maxWidth = 120;

    /// <summary>
    /// The options that rendering given none uses.
    /// </summary>
    public static RenderOptions Default { get; } = new();

    /// <summary>
    /// The most context lines shown for one error. The default is 10.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An error with more context lines shows its innermost half and its outermost half, with one line between them that says how many were left out, such as <c>… 990 more</c>.
    /// When the value is odd, the innermost half has the extra line. With 0, only the line that counts them is shown.
    /// </para>
    /// <para>
    /// <see cref="int.MaxValue"/> shows every context line.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative.</exception>
    public int MaxNotes
    {
        get => _maxNotes;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(MaxNotes));
            _maxNotes = value;
        }
    }

    /// <summary>
    /// The most columns that text from input takes in one place in a rendered error: the excerpt of a source line, and an unexpected item's display. The default is 120.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A source line wider than this is shown as a window centred on the error's span, with <c>…</c> where text was left out.
    /// A span wider than the window shows its start, with carets to the window's edge. The location still gives the true line and column.
    /// An unexpected item's display, such as a token's detail, is cut to this width with a trailing <c>…</c>.
    /// </para>
    /// <para>
    /// A grapheme cluster counts as at least one column, even when it shows as none, so characters with no width cannot pile up.
    /// Whatever the width, a cluster shows at most its first 32 scalar values.
    /// </para>
    /// <para>
    /// <see cref="int.MaxValue"/> shows whole lines and whole displays.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is less than 10.</exception>
    public int MaxWidth
    {
        get => _maxWidth;
        init
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 10, nameof(MaxWidth));
            _maxWidth = value;
        }
    }
}
