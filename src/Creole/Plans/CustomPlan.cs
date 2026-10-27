namespace Creole;

/// <summary>
/// Runs a hand-written scanner and produces its value.
/// </summary>
/// <remarks>
/// The plan trusts the scanner to fail without consuming input, as analysis trusts its declaration (ADR-0012).
/// A scanner that fails after moving the cursor fails committed, like any other parser that consumed input.
/// A failure is recorded where the scanner started, either way.
/// That start is held in the failure log while the scanner runs, so a stream buffer that releases it first copies the item there.
/// It is also pinned, so a scanner may keep the cursor at its start while a copy reads ahead, and assign the copy back only on success.
/// </remarks>
internal sealed class CustomPlan<TIn, TOut>(IScanner<TIn, TOut> scanner, Expected? expected) : Plan<TIn, TOut>
{
    private readonly Expected? _expected = expected;
    private readonly IScanner<TIn, TOut> _scanner = scanner;

    public override bool TryParse(ref Cursor<TIn> cursor, ref TOut result)
    {
        var start = cursor.Position;
        var floor = cursor.Pin();

        cursor.HoldStart();

        if (!_scanner.Scan(ref cursor, out var value))
        {
            cursor.FailAt(start, _expected);
            cursor.DropStart();
            cursor.Unpin(floor);
            return false;
        }

        cursor.DropStart();
        cursor.Unpin(floor);
        result = value;
        return true;
    }
}
