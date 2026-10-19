namespace Creole;

/// <summary>
/// A parse that produced a value after recovering from one or more errors.
/// </summary>
/// <typeparam name="T">The type of the parsed value.</typeparam>
/// <remarks>
/// The value often contains error nodes that the grammar's recovery built in place of what it could not parse.
/// </remarks>
public sealed record Recovered<T>
{
    /// <summary>
    /// The errors the parse recovered from, ordered by span. Never empty.
    /// </summary>
    public Diagnostics Diagnostics { get; }

    /// <summary>
    /// The value the parse produced.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Creates a recovered result.
    /// </summary>
    /// <param name="value">The value the parse produced.</param>
    /// <param name="diagnostics">The errors it recovered from.</param>
    /// <exception cref="ArgumentNullException"><paramref name="diagnostics"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="diagnostics"/> is empty. A parse with no errors is <see cref="Parsed{T}"/>.</exception>
    public Recovered(T value, Diagnostics diagnostics)
    {
        Value = value;
        Diagnostics = Diagnostics.RequireAny(diagnostics);
    }

    /// <summary>
    /// Deconstructs the result into its value and errors.
    /// </summary>
    public void Deconstruct(out T value, out Diagnostics diagnostics)
    {
        value = Value;
        diagnostics = Diagnostics;
    }
}
