using System.Globalization;
using System.Text;

namespace Creole;

/// <summary>
/// An immutable set of UTF-16 code units: the characters a character class matches, or the characters a custom scanner can start with.
/// </summary>
/// <remarks>
/// <para>
/// Build one with <see cref="Of"/>, <see cref="Range"/> or a named set such as <see cref="Digit"/>, and combine sets with <see cref="Union"/> and <see cref="Complement"/>.
/// Two sets are equal when they hold the same characters, however they were built.
/// </para>
/// <para>
/// Internally a set is held as sorted ranges of character codes, so analysis reads it as a FIRST set without copying.
/// </para>
/// </remarks>
public sealed class CharSet : IEquatable<CharSet>
{
    /// <summary>
    /// The character codes in the set.
    /// </summary>
    internal RangeSet Keys { get; }

    /// <summary>
    /// Every UTF-16 code unit, U+0000 to U+FFFF.
    /// </summary>
    public static CharSet All { get; } = new(RangeSet.FromRanges([(char.MinValue, char.MaxValue)]));

    /// <summary>
    /// The characters for which <see cref="char.IsDigit(char)"/> is true: every Unicode decimal digit, not only <c>0</c> to <c>9</c>.
    /// </summary>
    public static CharSet Digit { get; } = Where(char.IsDigit);

    /// <summary>
    /// The set with no characters.
    /// </summary>
    public static CharSet Empty { get; } = new(RangeSet.Empty);

    /// <summary>
    /// Whether the set has no characters.
    /// </summary>
    public bool IsEmpty => Keys.IsEmpty;

    /// <summary>
    /// The characters for which <see cref="char.IsLetter(char)"/> is true.
    /// </summary>
    public static CharSet Letter { get; } = Where(char.IsLetter);

    /// <summary>
    /// The characters for which <see cref="char.IsLetterOrDigit(char)"/> is true.
    /// </summary>
    public static CharSet LetterOrDigit { get; } = Where(char.IsLetterOrDigit);

    /// <summary>
    /// The characters for which <see cref="char.IsWhiteSpace(char)"/> is true.
    /// </summary>
    public static CharSet Whitespace { get; } = Where(char.IsWhiteSpace);

    private CharSet(RangeSet keys) => Keys = keys;

    private static void AppendEscaped(StringBuilder builder, long key)
    {
        var c = (char)key;

        if (c is '\\' or ']' or '-' or '^')
        {
            builder.Append('\\').Append(c);
        }
        else if (c is > ' ' and < '\u007F')
        {
            builder.Append(c);
        }
        else
        {
            builder.Append(CultureInfo.InvariantCulture, $"\\u{key:X4}");
        }
    }

    private static CharSet Where(Func<char, bool> predicate)
    {
        var ranges = new List<(long First, long Last)>();
        var start = -1;

        for (var code = 0; code <= char.MaxValue; code++)
        {
            if (predicate((char)code))
            {
                if (start < 0)
                {
                    start = code;
                }
            }
            else if (start >= 0)
            {
                ranges.Add((start, code - 1));
                start = -1;
            }
        }

        if (start >= 0)
        {
            ranges.Add((start, char.MaxValue));
        }

        return new(RangeSet.FromRanges(ranges));
    }

    /// <summary>
    /// A character written for a message: printable ASCII in quotes, anything else as a <c>\u</c> escape.
    /// </summary>
    internal static string Format(char c) =>
        c is >= ' ' and < '\u007F'
        ? $"'{c}'"
        : string.Create(CultureInfo.InvariantCulture, $"'\\u{(int)c:X4}'");

    /// <summary>
    /// The characters not in this set.
    /// </summary>
    public CharSet Complement() => new(Keys.Complement(char.MinValue, char.MaxValue));

    /// <summary>
    /// Whether <paramref name="c"/> is in the set.
    /// </summary>
    public bool Contains(char c) => Keys.Contains(c);

    /// <summary>
    /// Whether <paramref name="other"/> holds exactly the same characters.
    /// </summary>
    public bool Equals(CharSet? other) => other is not null && Keys.Equals(other.Keys);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as CharSet);

    /// <inheritdoc/>
    public override int GetHashCode() => Keys.GetHashCode();

    /// <summary>
    /// The set of the given characters. Order and repeats do not matter.
    /// </summary>
    public static CharSet Of(params ReadOnlySpan<char> chars)
    {
        var ranges = new List<(long First, long Last)>(chars.Length);

        foreach (var c in chars)
        {
            ranges.Add((c, c));
        }

        return new(RangeSet.FromRanges(ranges));
    }

    /// <summary>
    /// The characters from <paramref name="first"/> to <paramref name="last"/>, inclusive.
    /// </summary>
    /// <exception cref="GrammarException"><paramref name="last"/> comes before <paramref name="first"/>.</exception>
    public static CharSet Range(char first, char last) =>
        last < first
            ? throw new GrammarException($"Range {Format(first)}..{Format(last)} is empty: {Format(last)} comes before {Format(first)}.")
            : new(RangeSet.FromRanges([(first, last)]));

    /// <summary>
    /// The set in character-class notation, such as <c>[0-9A-Fa-f]</c>. Characters outside printable ASCII are written as <c>\u</c> escapes.
    /// </summary>
    public override string ToString()
    {
        var builder = new StringBuilder("[");

        foreach (var (first, last) in Keys.Ranges)
        {
            AppendEscaped(builder, first);

            if (last != first)
            {
                builder.Append('-');
                AppendEscaped(builder, last);
            }
        }

        return builder.Append(']').ToString();
    }

    /// <summary>
    /// The characters in this set, in <paramref name="other"/>, or in both.
    /// </summary>
    /// <exception cref="GrammarException"><paramref name="other"/> is null.</exception>
    public CharSet Union(CharSet other) => new(Keys.Union(Guard.NotNull(other).Keys));
}
