namespace Creole;

/// <summary>
/// Tries each alternative in order, each as one attempt, while the previous one failed without consuming.
/// Fails when an alternative fails after consuming, or when every alternative fails without consuming.
/// </summary>
internal sealed class OrPlan<TIn, T>(Plan<TIn, T>[] alternatives) : Plan<TIn, T>
{
    /// <summary>
    /// The alternatives, in order.
    /// </summary>
    public IReadOnlyList<Plan<TIn, T>> Alternatives => alternatives;

    public override bool TryParse(ref Cursor<TIn> cursor, ref T result)
    {
        foreach (var alternative in alternatives)
        {
            var mark = LightMark.Take(ref cursor);

            if (alternative.TryParse(ref cursor, ref result))
            {
                return true;
            }

            if (!mark.FailedEmpty(ref cursor))
            {
                return false;
            }
        }

        return false;
    }
}
