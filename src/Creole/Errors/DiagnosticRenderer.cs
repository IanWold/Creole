using System.Globalization;
using System.Text;

namespace Creole;

/// <summary>
/// Writes an error in full against its source: a headline, the location, the source line with carets under the span, and notes.
/// </summary>
/// <remarks>
/// The format is the one in the whitepaper's § The error model. Carets are placed by <see cref="DisplayWidth"/>.
/// </remarks>
internal static class DiagnosticRenderer
{
    private static string Format(long value) =>
        value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Returns the offset just past the last character of the line from <paramref name="lineStart"/> to <paramref name="nextLineStart"/>, leaving out its line break.
    /// </summary>
    private static long LineContentEnd(string text, long lineStart, long nextLineStart)
    {
        var end = nextLineStart;

        if (end > lineStart && text[(int)end - 1] == '\n')
        {
            end--;
        }

        if (end > lineStart && text[(int)end - 1] == '\r')
        {
            end--;
        }

        return end;
    }

    /// <summary>
    /// Appends <paramref name="error"/>, shown against <paramref name="source"/>, to <paramref name="builder"/>, with no line break after it.
    /// </summary>
    /// <param name="builder">The text so far.</param>
    /// <param name="error">An error whose span ends within <paramref name="source"/>'s text.</param>
    /// <param name="source">The text of the source the error points into.</param>
    /// <param name="sources">The texts by id, holding every source the error's context lines start in.</param>
    public static void Append(StringBuilder builder, ParseError error, SourceText source, IReadOnlyDictionary<SourceId, SourceText> sources)
    {
        var span = error.Span;
        var map = source.LineMap;
        var position = map.GetPosition(span.CharStart);
        var lineStart = map.GetLineStart(position.Line);
        var lineEnd = LineContentEnd(source.Text, lineStart, map.GetLineStart(position.Line + 1));
        var (shown, caretColumn, caretCount) = DisplayWidth.ShowLine(
            source.Text.AsSpan((int)lineStart, (int)(lineEnd - lineStart)),
            (int)(span.CharStart - lineStart),
            (int)(span.CharEnd - lineStart)
        );

        var number = Format(position.Line);
        var gutter = new string(' ', number.Length);
        var hasExpected = error.Expected.Count > 0;

        builder.Append("error[").Append(error.Layer).Append("]: ");
        builder.Append(hasExpected ? $"expected {error.DescribeExpected()}" : error.DescribeUnexpected()).Append('\n');

        builder.Append(gutter).Append("--> ");

        if (source.Name is not null)
        {
            builder.Append(source.Name).Append(':');
        }

        builder.Append(number).Append(':').Append(Format(position.Column)).Append('\n');
        builder.Append(gutter).Append(" |\n");
        builder.Append(number).Append(" |");

        if (shown.Length > 0)
        {
            builder.Append(' ').Append(shown);
        }

        builder.Append('\n');
        builder.Append(gutter).Append(" | ").Append(' ', caretColumn).Append('^', caretCount);

        if (hasExpected)
        {
            builder.Append(' ').Append(error.DescribeUnexpected());
        }

        foreach (var line in error.Context)
        {
            var start = sources[line.Start.Source].LineMap.GetPosition(line.Start.CharStart);

            builder.Append('\n').Append(gutter).Append(" = note: ").Append(line.Text).Append(" starting at ").Append(Format(start.Line)).Append(':').Append(Format(start.Column));
        }
    }
}
