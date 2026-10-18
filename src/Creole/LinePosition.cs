namespace Creole;

/// <summary>
/// A line and column in a source text, both starting at 1.
/// </summary>
/// <param name="Line">The line number. The first line is 1.</param>
/// <param name="Column">The column, counted in Unicode scalar values. The first column is 1.</param>
public readonly record struct LinePosition(long Line, long Column)
{
    /// <summary>
    /// Formats the position as <c>line:column</c>, such as <c>3:16</c>.
    /// </summary>
    public override string ToString() => $"{Line}:{Column}";
}
