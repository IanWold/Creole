namespace Creole;

/// <summary>
/// Reads one character in <c>set</c> and produces it.
/// Otherwise, or at the end of the input, fails without consuming and records <c>expected</c>.
/// </summary>
/// <remarks>
/// Every character class lowers to this plan: <c>Char</c>, <c>Range</c>, <c>NoneOf</c> and the named classes such as <c>Digit</c>.
/// An ASCII character is looked up in a table built when the plan is; any other is searched for in the set's ranges.
/// </remarks>
internal sealed class CharClassPlan(CharSet set, Expected expected) : Plan<char, char>
{
    private const int TableSize = 128;

    private readonly bool[] _ascii = Table(set);
    private readonly Expected _expected = expected;
    private readonly CharSet _set = set;

    private static bool[] Table(CharSet set)
    {
        var table = new bool[TableSize];

        for (var c = 0; c < TableSize; c++)
        {
            table[c] = set.Contains((char)c);
        }

        return table;
    }

    public override bool TryParse(ref Cursor<char> cursor, ref char result)
    {
        if (!cursor.TryPeek(out var item) || !(item < TableSize ? _ascii[item] : _set.Contains(item)))
        {
            cursor.Fail(_expected);
            return false;
        }

        cursor.Advance(1);
        result = item;
        return true;
    }
}
