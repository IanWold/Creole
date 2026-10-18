namespace Creole.Plans;

/// <summary>
/// Succeeds without consuming input and produces a fixed value.
/// </summary>
internal sealed class ReturnPlan<TIn, TOut>(TOut value) : Plan<TIn, TOut>
{
    private readonly TOut _value = value;

    public override bool TryParse(ref Cursor<TIn> cursor, ref TOut result)
    {
        result = _value;
        return true;
    }
}
