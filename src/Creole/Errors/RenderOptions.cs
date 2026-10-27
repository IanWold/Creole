namespace Creole;

/// <summary>
/// Limits on how much of an error <see cref="Diagnostics.Render()"/> and <see cref="ParseError.ToString()"/> show, so that rendering an error on untrusted input stays small.
/// </summary>
/// <remarks>
/// <para>
/// The limits apply only to rendered text. A <see cref="ParseError"/> keeps every context line.
/// </para>
/// <para>
/// Options are immutable. Make a variant with a <see langword="with"/> expression, such as <c>RenderOptions.Default with { MaxNotes = 4 }</c>.
/// </para>
/// </remarks>
public sealed record RenderOptions
{
    private readonly int _maxNotes = 10;

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
}
