namespace Creole;

/// <summary>
/// The lowered form of one <see cref="MatchCase{TIn, TOut}"/>: whether it applies to a union's value, and its branch.
/// </summary>
internal abstract class MatchCasePlan<TIn, TOut>
{
    /// <summary>
    /// Whether this case applies to <paramref name="value"/>, a union's case value.
    /// </summary>
    public abstract bool Applies(object? value);

    /// <summary>
    /// Runs the branch and combines <paramref name="value"/>, which this case applies to, with the branch's result.
    /// </summary>
    public abstract bool TryParse(object? value, ref Cursor<TIn> cursor, ref TOut result);
}
