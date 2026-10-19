namespace Creole;

/// <summary>
/// The text of one source, with the name and id its diagnostics are shown under.
/// </summary>
/// <remarks>
/// Pass sources to <see cref="Diagnostics.Render(ReadOnlySpan{SourceText})"/> to show errors against the lines they point into.
/// The <see cref="LineMap"/> is built on first use and kept, so many errors in one source share it.
/// A source text is safe to use from several threads.
/// </remarks>
public sealed class SourceText
{
    private LineMap? _lineMap;

    /// <summary>
    /// The source's id, which errors from it carry in <see cref="SourceSpan.Source"/>.
    /// </summary>
    public SourceId Id { get; }

    /// <summary>
    /// Lines and columns for <see cref="Text"/>, built on first use.
    /// </summary>
    public LineMap LineMap =>
        LazyInitializer.EnsureInitialized(ref _lineMap, () => LineMap.Create(Text));

    /// <summary>
    /// The name to show in diagnostics, such as <c>config.json</c>, or <see langword="null"/> to show none.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// The full text of the source.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Creates the text of a source that has no <see cref="SourceRegistry"/> id, such as the input to a parse given no source.
    /// </summary>
    /// <param name="text">The full text of the source.</param>
    /// <param name="name">The name to show in diagnostics, or <see langword="null"/> to show none.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    public SourceText(string text, string? name = null) : this(SourceId.Unnamed, name, text) { }

    /// <summary>
    /// Creates the text of a source with an id from a <see cref="SourceRegistry"/>.
    /// </summary>
    /// <param name="id">The source's id.</param>
    /// <param name="name">The name to show in diagnostics, usually the one the registry has for <paramref name="id"/>, or <see langword="null"/> to show none.</param>
    /// <param name="text">The full text of the source.</param>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    public SourceText(SourceId id, string? name, string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        Id = id;
        Name = name;
        Text = text;
    }
}
