namespace Creole;

/// <summary>
/// Reads one item that <c>accepts</c> returns <see langword="true"/> for, and produces it.
/// Otherwise, or at the end of the input, fails without consuming and records <c>expected</c>.
/// </summary>
/// <remarks>
/// Every primitive that matches one item and produces it lowers to this plan: <c>Any</c>, <c>Token</c>, <c>Satisfy</c>, character classes and <c>Kind</c>.
/// </remarks>
internal sealed class ItemPlan<TIn>(Func<TIn, bool> accepts, Expected? expected) : Plan<TIn, TIn>
{
    private readonly Func<TIn, bool> _accepts = accepts;
    private readonly Expected? _expected = expected;

    public override bool TryParse(ref Cursor<TIn> cursor, ref TIn result)
    {
        if (!cursor.TryPeek(out var item) || !_accepts(item))
        {
            cursor.Fail(_expected);
            return false;
        }

        cursor.Advance(1);
        result = item;
        return true;
    }
}
