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
/// <para>
/// Limits on width count cells: a cluster takes as many cells as columns, and at least one, so that clusters with no width cannot pile up.
/// A cluster shows at most its first <see cref="MaxScalarsShown"/> scalar values.
/// Where text is left out, an ellipsis (U+2026, one column) marks it.
/// </para>
/// </remarks>
internal static class DisplayWidth
{
    private const char Ellipsis = '\u2026';

    /// <summary>
    /// The most scalar values shown of one cluster. Only stacked combining marks make a cluster longer.
    /// </summary>
    public const int MaxScalarsShown = 32;

    /// <summary>
    /// The distance between tab stops, in columns.
    /// </summary>
    public const int TabSize = 4;

    private static void AppendShown(StringBuilder text, ReadOnlySpan<char> cluster, int width)
    {
        if (cluster is ['\t'])
        {
            text.Append(' ', width);
            return;
        }

        var rest = cluster;

        for (var count = 0; count < MaxScalarsShown && !rest.IsEmpty; count++)
        {
            Rune.DecodeFromUtf16(rest, out var rune, out var used);
            text.Append(Shown(rune));
            rest = rest[used..];
        }
    }

    private static bool IsMarked(int index, int length, int start, int end) =>
        start == end
            ? index <= start && start < index + length
            : index < end && start < index + length;

    private static bool IsRegionalIndicator(Rune rune) =>
        rune.Value is >= 0x1F1E6 and <= 0x1F1FF;

    /// <summary>
    /// Measures <paramref name="line"/> in cells, and finds the cells the characters from <paramref name="start"/> to <paramref name="end"/> take.
    /// A range that marks no cluster takes one cell just after the line.
    /// </summary>
    private static (long Cells, long MarkStart, long MarkEnd) Measure(ReadOnlySpan<char> line, int start, int end)
    {
        var column = 0L;
        var cells = 0L;
        var markStart = -1L;
        var markEnd = 0L;

        for (var i = 0; i < line.Length;)
        {
            var length = StringInfo.GetNextTextElementLength(line[i..]);
            var width = Width(line.Slice(i, length), column);
            var taken = Math.Max(width, 1);

            if (IsMarked(i, length, start, end))
            {
                if (markStart < 0)
                {
                    markStart = cells;
                }

                markEnd = cells + taken;
            }

            column += width;
            cells += taken;
            i += length;
        }

        return markStart < 0
            ? (cells, cells, cells + 1)
            : (cells, markStart, markEnd);
    }

    private static Rune Shown(Rune rune) => rune.Value switch
    {
        < 0x20 => new Rune(0x2400 + rune.Value),
        0x7F => new Rune(0x2421),
        >= 0x80 and <= 0x9F => Rune.ReplacementChar,
        _ => rune,
    };

    /// <summary>
    /// Returns the columns <paramref name="cluster"/> takes when it starts at <paramref name="column"/>.
    /// </summary>
    private static int Width(ReadOnlySpan<char> cluster, long column) =>
        cluster is ['\t']
            ? TabSize - (int)(column % TabSize)
            : OfCluster(cluster);

    /// <summary>
    /// Chooses the cells to show of a line <paramref name="cells"/> wide, so that those from <paramref name="markStart"/> to <paramref name="markEnd"/> are in view and the excerpt, with its ellipses, takes at most <paramref name="maxWidth"/> cells.
    /// </summary>
    private static (long From, long To) Window(long cells, long markStart, long markEnd, int maxWidth)
    {
        if (cells <= maxWidth)
        {
            return (0, cells);
        }

        var room = maxWidth - 2;

        if (markEnd - markStart > room)
        {
            return markStart == 0
                ? (0, maxWidth - 1)
                : (markStart, markStart + room);
        }

        var from = markStart - (room - (markEnd - markStart)) / 2;

        if (from <= 0)
        {
            return (0, maxWidth - 1);
        }

        return from + room >= cells
            ? (cells - (maxWidth - 1), cells)
            : (from, from + room);
    }

    /// <summary>
    /// Returns <paramref name="text"/> as it is shown, cut to <paramref name="maxWidth"/> cells with a trailing ellipsis if it is wider.
    /// </summary>
    /// <param name="text">Text that may come from input, such as a token's detail.</param>
    /// <param name="maxWidth">The most cells the result takes, at least 1.</param>
    /// <remarks>Tabs expand to stops counted from the start of <paramref name="text"/>. Measuring stops once the text is known to be too wide.</remarks>
    public static string Cut(ReadOnlySpan<char> text, int maxWidth)
    {
        var column = 0L;
        var cells = 0L;
        var fits = true;

        for (var i = 0; i < text.Length && fits;)
        {
            var length = StringInfo.GetNextTextElementLength(text[i..]);
            var width = Width(text.Slice(i, length), column);

            column += width;
            cells += Math.Max(width, 1);
            fits = cells <= maxWidth;
            i += length;
        }

        var shown = new StringBuilder(Math.Min(text.Length, maxWidth));
        var room = fits
            ? maxWidth
            : maxWidth - 1;

        column = 0;
        cells = 0;

        for (var i = 0; i < text.Length;)
        {
            var length = StringInfo.GetNextTextElementLength(text[i..]);
            var cluster = text.Slice(i, length);
            var width = Width(cluster, column);
            var taken = Math.Max(width, 1);

            if (cells + taken > room)
            {
                break;
            }

            AppendShown(shown, cluster, width);
            column += width;
            cells += taken;
            i += length;
        }

        return fits
            ? shown.ToString()
            : shown.Append(Ellipsis).ToString();
    }

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
    /// Returns the text to show for <paramref name="line"/>, at most <paramref name="maxWidth"/> cells of it, and where carets go under the characters from <paramref name="start"/> to <paramref name="end"/>.
    /// </summary>
    /// <param name="line">One line of source text, without its line break.</param>
    /// <param name="start">The offset in <paramref name="line"/> of the first character to mark. May be <paramref name="line"/>'s length or more, for a position at or past its end.</param>
    /// <param name="end">The offset just past the last character to mark, at least <paramref name="start"/>. May be past the end of <paramref name="line"/>.</param>
    /// <param name="maxWidth">The most cells the text, and the carets under it, take; at least 10. <see cref="int.MaxValue"/> shows the whole line.</param>
    /// <returns>
    /// The line as shown; the column, from 0, of the first caret; and the number of carets, at least 1.
    /// A range that starts or ends inside a cluster marks the whole cluster.
    /// An empty range marks the cluster it is in, and a range that starts past the last cluster puts one caret just after it.
    /// </returns>
    /// <remarks>
    /// A line that does not fit is shown as a window around the marked clusters, centred where the line allows, with an ellipsis where text was left out.
    /// If the marked clusters do not fit, the window starts at the first of them and the carets run to its end.
    /// A cluster that would cross the window's edge is left out. Tabs keep the widths they have in the whole line.
    /// </remarks>
    public static (string Text, int CaretColumn, int CaretCount) ShowLine(ReadOnlySpan<char> line, int start, int end, int maxWidth)
    {
        var (cells, markStart, markEnd) = Measure(line, start, end);
        var (from, to) = Window(Math.Max(cells, markEnd), markStart, markEnd, maxWidth);
        var text = new StringBuilder((int)Math.Min(line.Length, to - from));
        var column = 0L;
        var at = 0L;
        var shownColumn = 0;
        var caretColumn = -1;
        var caretEnd = 0;

        if (from > 0)
        {
            text.Append(Ellipsis);
            shownColumn = 1;
        }

        for (var i = 0; i < line.Length && at < to;)
        {
            var length = StringInfo.GetNextTextElementLength(line[i..]);
            var cluster = line.Slice(i, length);
            var width = Width(cluster, column);
            var taken = Math.Max(width, 1);

            if (at >= from && at + taken <= to)
            {
                AppendShown(text, cluster, width);

                if (IsMarked(i, length, start, end))
                {
                    if (caretColumn < 0)
                    {
                        caretColumn = shownColumn;
                    }

                    caretEnd = shownColumn + width;
                }

                shownColumn += width;
            }

            column += width;
            at += taken;
            i += length;
        }

        if (to < cells)
        {
            text.Append(Ellipsis);
        }

        return caretColumn < 0
            ? (text.ToString(), shownColumn, 1)
            : (text.ToString(), caretColumn, Math.Max(1, caretEnd - caretColumn));
    }
}
