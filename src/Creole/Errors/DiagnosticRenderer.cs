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
    private static long LineContentEnd(ReadOnlySpan<char> text, long lineStart, long nextLineStart)
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
    /// <param name="options">The limits on what is shown.</param>
    public static void Append(StringBuilder builder, ParseError error, SourceText source, IReadOnlyDictionary<SourceId, SourceText> sources, RenderOptions options)
    {
        var span = error.Span;
        var map = source.LineMap;
        var position = map.GetPosition(span.CharStart);
        var lineStart = map.GetLineStart(position.Line);
        var lineEnd = LineContentEnd(source.Text.Span, lineStart, map.GetLineStart(position.Line + 1));
        var (shown, caretColumn, caretCount) = DisplayWidth.ShowLine(
            source.Text.Span.Slice((int)lineStart, (int)(lineEnd - lineStart)),
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

        foreach (var (line, omitted) in Notes(error.Context, options))
        {
            builder.Append('\n').Append(gutter).Append(" = note: ");

            if (line is null)
            {
                builder.Append("… ").Append(Format(omitted)).Append(" more");
                continue;
            }

            var start = sources[line.Start.Source].LineMap.GetPosition(line.Start.CharStart);

            builder.Append(line.Text).Append(" starting at ").Append(Format(start.Line)).Append(':').Append(Format(start.Column));
        }
    }

    /// <summary>
    /// Returns the context lines to show, innermost first, as <see cref="RenderOptions.MaxNotes"/> allows.
    /// </summary>
    /// <returns>
    /// Each line to show with an <c>Omitted</c> of 0.
    /// When lines are left out, one entry between the innermost and outermost lines has a null <c>Line</c> and the number left out.
    /// </returns>
    public static IEnumerable<(ContextLine? Line, int Omitted)> Notes(IReadOnlyList<ContextLine> context, RenderOptions options)
    {
        var shown = Math.Min(context.Count, options.MaxNotes);
        var innermost = shown - shown / 2;
        var outermost = shown / 2;

        for (var i = 0; i < innermost; i++)
        {
            yield return (context[i], 0);
        }

        if (context.Count > shown)
        {
            yield return (null, context.Count - shown);
        }

        for (var i = context.Count - outermost; i < context.Count; i++)
        {
            yield return (context[i], 0);
        }
    }
}
