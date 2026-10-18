namespace Creole;

/// <summary>
/// Identifies one source text, such as a file, within a <see cref="SourceRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see langword="default"/> is <see cref="Unnamed"/>, the source of a parse that was given no name.
/// Every other id is issued by <see cref="SourceRegistry.Add(string)"/>.
/// </para>
/// <para>
/// An id is meaningful only in the registry that issued it. Ids from two different registries may compare equal.
/// </para>
/// </remarks>
public readonly record struct SourceId
{
    private readonly int _value;

    internal SourceId(int value)
    {
        _value = value;
    }

    /// <summary>
    /// The id of a source that has no name. Equal to <see langword="default"/>.
    /// </summary>
    public static SourceId Unnamed => default;

    /// <summary>
    /// The registry's index for this id, starting at 1. Zero for <see cref="Unnamed"/>.
    /// </summary>
    internal int Value => _value;

    /// <inheritdoc/>
    public override string ToString() =>
        _value == 0
        ? "SourceId(unnamed)"
        : $"SourceId({_value})";
}
