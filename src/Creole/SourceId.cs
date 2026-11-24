namespace Creole;

/// <summary>
/// Identifies the source text a span points into.
/// </summary>
/// <remarks>
/// Every parse reads from <see cref="Unnamed"/>, which is also <see langword="default"/>.
/// A parse given a name shows it through the <see cref="SourceText"/> its result holds, not through the id.
/// </remarks>
public readonly record struct SourceId
{
    private readonly int _value;

    /// <summary>
    /// The id's number, which orders sources. Zero for <see cref="Unnamed"/>.
    /// </summary>
    internal int Value => _value;

    /// <summary>
    /// The id of the source every parse reads from. Equal to <see langword="default"/>.
    /// </summary>
    public static SourceId Unnamed => default;

    internal SourceId(int value)
    {
        _value = value;
    }

    /// <inheritdoc/>
    public override string ToString() =>
        _value == 0
        ? "SourceId(unnamed)"
        : $"SourceId({_value})";
}
