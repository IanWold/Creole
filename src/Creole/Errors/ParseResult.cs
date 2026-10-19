namespace Creole;

/// <summary>
/// The result of a parse: <see cref="Parsed{T}"/>, <see cref="Recovered{T}"/> or <see cref="Failed"/>.
/// </summary>
/// <typeparam name="T">The type of the parsed value.</typeparam>
/// <remarks>
/// <para>
/// A switch expression that covers all three cases needs no default arm and no <see langword="null"/> arm.
/// Leaving a case out is compiler error CS8509 when warnings are errors.
/// </para>
/// <para>
/// Creole never returns <see langword="default"/>(<see cref="ParseResult{T}"/>).
/// A <see langword="default"/> value holds no case, and a switch over it throws <see cref="System.Runtime.CompilerServices.SwitchExpressionException"/>.
/// Code that might hold one, such as an uninitialized array element, can add a <see langword="null"/> arm, which still reports missing cases.
/// </para>
/// </remarks>
public union ParseResult<T>(Parsed<T>, Recovered<T>, Failed)
{
    /// <summary>
    /// The errors the parse recorded, ordered by span. Empty for <see cref="Parsed{T}"/> and for a <see langword="default"/> value.
    /// </summary>
    public Diagnostics Diagnostics => this switch
    {
        Parsed<T> => Diagnostics.Empty,
        Recovered<T> recovered => recovered.Diagnostics,
        Failed failed => failed.Diagnostics,
    };
}
