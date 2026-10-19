using System.Globalization;
using System.Text;

namespace Creole;

/// <summary>
/// How a line of source text appears in a terminal: the text to show, and the columns each grapheme cluster takes.
/// </summary>
/// <remarks>
/// <para>
/// Clusters are extended grapheme clusters from <see cref="StringInfo"/>.
/// A cluster is two columns wide when its first scalar value is East Asian Wide or Fullwidth, or is a regional indicator, or when the cluster contains U+FE0F (emoji presentation).
/// It is zero columns wide when its first scalar value is a nonspacing mark, an enclosing mark or a format character, such as a lone U+0301 or U+200D.
/// Every other cluster, including East Asian Ambiguous ones, is one column wide.
/// </para>
/// <para>
/// A tab moves to the next multiple of <see cref="TabSize"/> columns and is shown as spaces.
/// Other control characters are shown so that printing a line cannot drive a terminal:
/// C0 controls and DEL as their Control Pictures (U+2400 to U+2421), C1 controls and lone surrogates as U+FFFD.
/// </para>
/// </remarks>
internal static class DisplayWidth
{
    /// <summary>
    /// The distance between tab stops, in columns.
    /// </summary>
    public const int TabSize = 4;

    private static int AppendShown(StringBuilder text, ReadOnlySpan<char> cluster, int column)
    {
        if (cluster is ['\t'])
        {
            var spaces = TabSize - column % TabSize;

            text.Append(' ', spaces);
            return spaces;
        }

        for (var rest = cluster; !rest.IsEmpty;)
        {
            Rune.DecodeFromUtf16(rest, out var rune, out var used);
            text.Append(Shown(rune));
            rest = rest[used..];
        }

        return OfCluster(cluster);
    }

    private static bool IsRegionalIndicator(Rune rune) =>
        rune.Value is >= 0x1F1E6 and <= 0x1F1FF;

    private static Rune Shown(Rune rune) => rune.Value switch
    {
        < 0x20 => new Rune(0x2400 + rune.Value),
        0x7F => new Rune(0x2421),
        >= 0x80 and <= 0x9F => Rune.ReplacementChar,
        _ => rune,
    };

    /// <summary>
    /// Returns the number of columns <paramref name="cluster"/> takes, apart from a tab, whose width depends on its column.
    /// </summary>
    /// <param name="cluster">One grapheme cluster.</param>
    public static int OfCluster(ReadOnlySpan<char> cluster)
    {
        if (cluster.IsEmpty)
        {
            return 0;
        }

        Rune.DecodeFromUtf16(cluster, out var first, out _);

        if (EastAsianWidth.IsWide(first.Value) || IsRegionalIndicator(first) || cluster.Contains('\uFE0F'))
        {
            return 2;
        }

        return Rune.GetUnicodeCategory(first) is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark or UnicodeCategory.Format
            ? 0
            : 1;
    }

    /// <summary>
    /// Returns the text to show for <paramref name="line"/> and where carets go under the characters from <paramref name="start"/> to <paramref name="end"/>.
    /// </summary>
    /// <param name="line">One line of source text, without its line break.</param>
    /// <param name="start">The offset in <paramref name="line"/> of the first character to mark. May be <paramref name="line"/>'s length or more, for a position at or past its end.</param>
    /// <param name="end">The offset just past the last character to mark, at least <paramref name="start"/>. May be past the end of <paramref name="line"/>.</param>
    /// <returns>
    /// The line as shown; the column, from 0, of the first caret; and the number of carets, at least 1.
    /// A range that starts or ends inside a cluster marks the whole cluster.
    /// An empty range marks the cluster it is in, and a range that starts past the last cluster puts one caret just after it.
    /// </returns>
    public static (string Text, int CaretColumn, int CaretCount) ShowLine(ReadOnlySpan<char> line, int start, int end)
    {
        var text = new StringBuilder(line.Length);
        var column = 0;
        var caretColumn = -1;
        var caretEnd = 0;

        for (var i = 0; i < line.Length;)
        {
            var length = StringInfo.GetNextTextElementLength(line[i..]);
            var width = AppendShown(text, line.Slice(i, length), column);
            var marked = start == end
                ? i <= start && start < i + length
                : i < end && start < i + length;

            if (marked)
            {
                if (caretColumn < 0)
                {
                    caretColumn = column;
                }

                caretEnd = column + width;
            }

            column += width;
            i += length;
        }

        return caretColumn < 0
            ? (text.ToString(), column, 1)
            : (text.ToString(), caretColumn, Math.Max(1, caretEnd - caretColumn));
    }
}
