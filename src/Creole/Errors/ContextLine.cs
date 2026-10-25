namespace Creole;

/// <summary>
/// One line of an error's context: a construct the parse was inside when it failed, and where that construct started.
/// </summary>
/// <param name="Text">The line, such as <c>in object</c>, from <c>p.Context(text)</c>.</param>
/// <param name="Start">The span of the first item the construct read. A renderer shows where it starts, as in <c>in object starting at 2:1</c>.</param>
/// <remarks>
/// Lines come from <c>p.Context(text)</c>, and only for failures recorded past the position where <c>p</c> started, so a construct that never began adds none.
/// </remarks>
/// <exception cref="ArgumentNullException"><paramref name="Text"/> is null.</exception>
/// <exception cref="ArgumentException"><paramref name="Text"/> is empty.</exception>
public sealed record ContextLine(string Text, SourceSpan Start)
{
    /// <summary>
    /// The line, such as <c>in object</c>. Never null or empty.
    /// </summary>
    public string Text { get; init => field = CheckText(value, nameof(Text)); } = CheckText(Text, nameof(Text));

    private static string CheckText(string text, string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(text, name);
        return text;
    }
}
