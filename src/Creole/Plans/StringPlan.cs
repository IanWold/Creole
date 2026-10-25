namespace Creole;

/// <summary>
/// Reads the characters of a string in order. Atomic: if any character does not match, rewinds, fails without consuming and records the string where it started.
/// </summary>
/// <remarks>
/// With <c>ignoreCase</c>, characters are compared by <see cref="char.ToLowerInvariant(char)"/> and the result is the input's own characters.
/// Otherwise the result is the text itself, so a match allocates nothing.
/// </remarks>
internal sealed class StringPlan(string text, bool ignoreCase, Expected expected) : Plan<char, string>
{
    private readonly Expected _expected = expected;
    private readonly bool _ignoreCase = ignoreCase;
    private readonly string _text = text;

    public override bool TryParse(ref Cursor<char> cursor, ref string result)
    {
        var start = cursor.Position;

        foreach (var expected in _text)
        {
            if (!cursor.TryRead(out var c) || !(c == expected || (_ignoreCase && char.ToLowerInvariant(c) == char.ToLowerInvariant(expected))))
            {
                cursor.Reset(start);
                cursor.Fail(_expected);
                return false;
            }
        }

        result = _ignoreCase ? cursor.Since(start).ToString() : _text;
        return true;
    }
}
