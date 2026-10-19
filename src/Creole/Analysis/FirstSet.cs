using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// The items a parser can start with, and whether it can start at the end of input.
/// One representation serves every layer: characters, token kinds, union case types and literal tokens.
/// </summary>
/// <remarks>
/// <para>
/// A FIRST set has four parts:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="Keys"/>: items with an integer identity, as ranges. A character is its UTF-16 code; a token kind, or any other enum item, is its value.</description></item>
/// <item><description><see cref="Cases"/>: union case types, from <c>Case&lt;T&gt;()</c>. Analysis treats each case type as a kind.</description></item>
/// <item><description><see cref="Values"/>: literal items with no integer identity, such as a record token passed to <c>Token(x)</c>, compared with <see cref="object.Equals(object?)"/>.</description></item>
/// <item><description><see cref="MatchesAnyItem"/> (⊤): any item at all, from <c>Any</c> or an opaque <c>Satisfy</c>. It absorbs the other three.</description></item>
/// </list>
/// <para>
/// <see cref="MatchesEnd"/> says the parser can start at the end of input, as <c>End</c> does.
/// </para>
/// <para>
/// One layer has one input type, so a set never mixes character codes with kind values.
/// A token layer can mix parts, for example <c>Kind(k)</c> and <c>Case&lt;T&gt;()</c>.
/// Analysis cannot tell which kind a case type or a literal token has, so <see cref="Overlaps"/> treats items in different parts as possibly the same.
/// </para>
/// </remarks>
internal sealed class FirstSet : IEquatable<FirstSet>
{
    private readonly Type[] _cases;
    private readonly object?[] _values;

    private bool HasItems => MatchesAnyItem || !Keys.IsEmpty || _cases.Length > 0 || _values.Length > 0;

    /// <summary>
    /// ⊤: any item, but not the end of input.
    /// </summary>
    public static FirstSet AnyItem { get; } = new(RangeSet.Empty, [], [], matchesAnyItem: true, matchesEnd: false);

    /// <summary>
    /// The union case types the parser can start with.
    /// </summary>
    public IReadOnlyList<Type> Cases => _cases;

    /// <summary>
    /// No items, and not the end of input: the FIRST set of a parser that never consumes, such as <c>Return</c> or <c>Fail</c>.
    /// </summary>
    public static FirstSet Empty { get; } = new(RangeSet.Empty, [], [], matchesAnyItem: false, matchesEnd: false);

    /// <summary>
    /// Only the end of input: the FIRST set of <c>End</c>.
    /// </summary>
    public static FirstSet EndOfInput { get; } = new(RangeSet.Empty, [], [], matchesAnyItem: false, matchesEnd: true);

    /// <summary>
    /// The item keys the parser can start with: character codes or enum values.
    /// </summary>
    public RangeSet Keys { get; }

    public bool MatchesAnyItem { get; }

    public bool MatchesEnd { get; }

    /// <summary>
    /// The literal items, with no integer key, that the parser can start with.
    /// </summary>
    public IReadOnlyList<object?> Values => _values;

    private FirstSet(RangeSet keys, Type[] cases, object?[] values, bool matchesAnyItem, bool matchesEnd)
    {
        Keys = matchesAnyItem ? RangeSet.Empty : keys;
        _cases = matchesAnyItem ? [] : cases;
        _values = matchesAnyItem ? [] : values;
        MatchesAnyItem = matchesAnyItem;
        MatchesEnd = matchesEnd;
    }

    private static long EnumKey<T>(T value) => Unsafe.SizeOf<T>() switch
    {
        1 => Unsafe.As<T, byte>(ref value),
        2 => Unsafe.As<T, ushort>(ref value),
        4 => Unsafe.As<T, uint>(ref value),
        _ => unchecked((long)Unsafe.As<T, ulong>(ref value)),
    };

    private static bool Related(Type a, Type b) => a.IsAssignableFrom(b) || b.IsAssignableFrom(a);

    private static bool SetEquals<T>(T[] a, T[] b) =>
        a.Length == b.Length && a.All(b.Contains) && b.All(a.Contains);

    private static T[] UnionOf<T>(T[] a, T[] b) =>
        b.Length == 0 ? a
        : a.Length == 0 ? b
        : [.. a.Union(b)];

    public bool Equals(FirstSet? other) =>
        other is not null
        && MatchesAnyItem == other.MatchesAnyItem
        && MatchesEnd == other.MatchesEnd
        && Keys.Equals(other.Keys)
        && SetEquals(_cases, other._cases)
        && SetEquals(_values, other._values);

    public override bool Equals(object? obj) => Equals(obj as FirstSet);

    /// <remarks>
    /// Order-insensitive, like <see cref="Equals(FirstSet?)"/>: the hash uses only the flags, the keys and the part sizes.
    /// </remarks>
    public override int GetHashCode() => HashCode.Combine(MatchesAnyItem, MatchesEnd, Keys, _cases.Length, _values.Length);

    /// <summary>
    /// The set holding one union case type.
    /// </summary>
    public static FirstSet OfCase(Type caseType) => new(RangeSet.Empty, [caseType], [], matchesAnyItem: false, matchesEnd: false);

    public static FirstSet OfChars(CharSet chars) => new(chars.Keys, [], [], matchesAnyItem: false, matchesEnd: false);

    /// <summary>
    /// The set holding one item: a key for a <see cref="char"/> or an enum value, a literal value for anything else.
    /// </summary>
    public static FirstSet OfItem<TIn>(TIn item)
    {
        if (item is char c)
        {
            return new(RangeSet.FromRanges([(c, c)]), [], [], matchesAnyItem: false, matchesEnd: false);
        }

        if (typeof(TIn).IsEnum)
        {
            var key = EnumKey(item);
            return new(RangeSet.FromRanges([(key, key)]), [], [], matchesAnyItem: false, matchesEnd: false);
        }

        return new(RangeSet.Empty, [], [item], matchesAnyItem: false, matchesEnd: false);
    }

    /// <summary>
    /// Whether some input could start both a parser with this FIRST set and one with <paramref name="other"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Exact within a part: keys by range, case types by identity or inheritance, values by equality.
    /// </para>
    /// <para>
    /// Conservative across parts: a key, a case type and a literal value may all describe the same token, so items in different parts overlap.
    /// </para>
    /// </remarks>
    public bool Overlaps(FirstSet other)
    {
        if (MatchesEnd && other.MatchesEnd)
        {
            return true;
        }

        if (!HasItems || !other.HasItems)
        {
            return false;
        }

        if (MatchesAnyItem || other.MatchesAnyItem)
        {
            return true;
        }

        var otherKeys = !other.Keys.IsEmpty;
        var otherCases = other._cases.Length > 0;
        var otherValues = other._values.Length > 0;

        return Keys.Overlaps(other.Keys)
            || _cases.Any(a => other._cases.Any(b => Related(a, b)))
            || _values.Any(other._values.Contains)
            || (!Keys.IsEmpty && (otherCases || otherValues))
            || (_cases.Length > 0 && (otherKeys || otherValues))
            || (_values.Length > 0 && (otherKeys || otherCases));
    }

    public override string ToString()
    {
        var parts = new List<string>();

        if (MatchesAnyItem)
        {
            parts.Add("any item");
        }

        if (!Keys.IsEmpty)
        {
            parts.Add($"keys {Keys}");
        }

        if (_cases.Length > 0)
        {
            parts.Add($"cases {{{string.Join(", ", _cases.Select(type => type.Name))}}}");
        }

        if (_values.Length > 0)
        {
            parts.Add($"values {{{string.Join(", ", _values)}}}");
        }

        if (MatchesEnd)
        {
            parts.Add("end");
        }

        return parts.Count == 0 ? "empty" : string.Join("; ", parts);
    }

    /// <summary>
    /// The items and end-of-input flags of this set and <paramref name="other"/> together.
    /// </summary>
    public FirstSet Union(FirstSet other) => new(
        Keys.Union(other.Keys),
        UnionOf(_cases, other._cases),
        UnionOf(_values, other._values),
        MatchesAnyItem || other.MatchesAnyItem,
        MatchesEnd || other.MatchesEnd
    );
}
