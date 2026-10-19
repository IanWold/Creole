namespace Creole;

/// <summary>
/// Runs a rule's compiled code in place of the plan the interpreter would build for it.
/// </summary>
internal sealed class StagedPlan<TIn, TOut>(StagedParse<TIn, TOut> parse) : Plan<TIn, TOut>
{
    private readonly StagedParse<TIn, TOut> _parse = parse;

    public override bool TryParse(ref Cursor<TIn> cursor, ref TOut result)
    {
        if (!_parse(ref cursor, out var value))
        {
            return false;
        }

        result = value;
        return true;
    }
}
