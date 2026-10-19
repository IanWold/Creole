namespace Creole;

/// <summary>
/// Converts character offsets in a source text to lines and columns.
/// </summary>
/// <remarks>
/// <para>
/// A line map is built once per source, by one pass over its text, and does not keep the text.
/// Each lookup is a binary search, so it costs O(log n) in the number of lines.
/// </para>
/// <para>
/// Line breaks are LF, CRLF and CR. Other Unicode line separators (NEL, U+2028, U+2029) do not end a line.
/// Columns count Unicode scalar values, so a surrogate pair is one column. A lone surrogate also counts as one column.
/// </para>
/// </remarks>
public sealed class LineMap
{
    /// <summary>
    /// Builds a <see cref="LineMap"/> from text that arrives in chunks, such as a source too long to hold in one string.
    /// </summary>
    /// <remarks>
    /// A CRLF or a surrogate pair may be split across two chunks.
    /// </remarks>
    public sealed class Builder
    {
        private bool _endsWithCr;
        private bool _endsWithHighSurrogate;
        private long _length;
        private readonly List<long> _lineStarts = [0];
        private readonly List<long> _pairStarts = [];

        /// <summary>
        /// The number of characters appended so far.
        /// </summary>
        public long Length => _length;

        private void ScanLines(ReadOnlySpan<char> chunk, long start)
        {
            var i = 0;

            if (_endsWithCr)
            {
                _endsWithCr = false;

                if (chunk[0] == '\n')
                {
                    i = 1;
                }

                _lineStarts.Add(start + i);
            }

            while (i < chunk.Length)
            {
                var found = chunk[i..].IndexOfAny('\r', '\n');

                if (found < 0)
                {
                    return;
                }

                i += found + 1;

                if (chunk[i - 1] == '\r')
                {
                    if (i == chunk.Length)
                    {
                        _endsWithCr = true;
                        return;
                    }

                    if (chunk[i] == '\n')
                    {
                        i++;
                    }
                }

                _lineStarts.Add(start + i);
            }
        }

        private void ScanPairs(ReadOnlySpan<char> chunk, long start)
        {
            var i = 0;

            if (_endsWithHighSurrogate)
            {
                _endsWithHighSurrogate = false;

                if (char.IsLowSurrogate(chunk[0]))
                {
                    _pairStarts.Add(start - 1);
                    i = 1;
                }
            }

            while (i < chunk.Length)
            {
                var found = chunk[i..].IndexOfAnyInRange('\uD800', '\uDBFF');

                if (found < 0)
                {
                    return;
                }

                i += found;

                if (i + 1 == chunk.Length)
                {
                    _endsWithHighSurrogate = true;
                    return;
                }

                if (char.IsLowSurrogate(chunk[i + 1]))
                {
                    _pairStarts.Add(start + i);
                    i += 2;
                }
                else
                {
                    i++;
                }
            }
        }

        /// <summary>
        /// Appends the next chunk of the source text.
        /// </summary>
        public void Append(ReadOnlySpan<char> chunk)
        {
            if (chunk.IsEmpty)
            {
                return;
            }

            var start = _length;

            ScanLines(chunk, start);
            ScanPairs(chunk, start);
            _length += chunk.Length;
        }

        /// <summary>
        /// Returns a line map for the text appended so far. The builder can still be appended to afterwards.
        /// </summary>
        public LineMap Build()
        {
            var lineStarts = _lineStarts.ToArray();

            if (_endsWithCr)
            {
                lineStarts = [.. lineStarts, _length];
            }

            return new LineMap(lineStarts, [.. _pairStarts], _length);
        }
    }

    private readonly long[] _lineStarts;
    private readonly long[] _pairStarts;

    /// <summary>
    /// The number of characters in the source text.
    /// </summary>
    public long Length { get; }

    /// <summary>
    /// The number of lines. Text that ends with a line break has an empty last line after it, and empty text has one line.
    /// </summary>
    public long LineCount => _lineStarts.Length;

    private LineMap(long[] lineStarts, long[] pairStarts, long length)
    {
        _lineStarts = lineStarts;
        _pairStarts = pairStarts;
        Length = length;
    }

    private static int LowerBound(long[] values, long value)
    {
        var found = Array.BinarySearch(values, value);

        return found >= 0 ? found : ~found;
    }

    /// <summary>
    /// Builds a line map for text held in memory.
    /// </summary>
    public static LineMap Create(ReadOnlySpan<char> text)
    {
        var builder = new Builder();

        builder.Append(text);
        return builder.Build();
    }

    /// <summary>
    /// Returns the line and column of the character at <paramref name="offset"/>.
    /// </summary>
    /// <param name="offset">A character offset from <c>0</c> to <see cref="Length"/>. <see cref="Length"/> is the position just past the last character.</param>
    /// <remarks>
    /// The LF of a CRLF is on the same line as its CR, one column after it.
    /// An offset on the second half of a surrogate pair has the same column as the first half.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is negative or greater than <see cref="Length"/>.</exception>
    public LinePosition GetPosition(long offset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, Length);

        var found = Array.BinarySearch(_lineStarts, offset);
        var line = found >= 0 ? found : ~found - 1;
        var lineStart = _lineStarts[line];

        if (offset > 0 && Array.BinarySearch(_pairStarts, offset - 1) >= 0)
        {
            offset--;
        }

        var pairs = LowerBound(_pairStarts, offset) - LowerBound(_pairStarts, lineStart);

        return new LinePosition(line + 1, offset - lineStart - pairs + 1);
    }
}
