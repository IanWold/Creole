namespace Creole;

/// <summary>
/// A parse that produced no value.
/// </summary>
public sealed record Failed
{
    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="diagnostics">The errors that made the parse fail.</param>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostics"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="diagnostics"/> is empty. A failed parse always has at least one error.</exception>
    public Failed(Diagnostics diagnostics)
    {
        Diagnostics = Diagnostics.RequireAny(diagnostics);
    }

    /// <summary>
    /// The errors that made the parse fail, ordered by span. Never empty.
    /// </summary>
    public Diagnostics Diagnostics { get; }
}
