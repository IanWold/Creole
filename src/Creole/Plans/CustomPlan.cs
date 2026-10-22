namespace Creole;

/// <summary>
/// Runs a hand-written scanner and produces its value.
/// </summary>
/// <remarks>
/// The plan trusts the scanner to fail without consuming input, as analysis trusts its declaration (ADR-0012).
/// A scanner that fails after moving the cursor fails committed, like any other parser that consumed input.
/// </remarks>
internal sealed class CustomPlan<TIn, TOut>(IScanner<TIn, TOut> scanner) : Plan<TIn, TOut>
{
    private readonly IScanner<TIn, TOut> _scanner = scanner;

    public override bool TryParse(ref Cursor<TIn> cursor, ref TOut result)
    {
        if (!_scanner.Scan(ref cursor, out var value))
        {
            return false;
        }

        result = value;
        return true;
    }
}
