using System.Buffers;
using System.Globalization;
using System.Text;

namespace Creole;

/// <summary>
/// One run of a <see cref="Utf8Layer"/>: each step decodes one Unicode scalar value into one character or a surrogate pair.
/// </summary>
/// <remarks>
/// <para>
/// It reads its input without a cursor, one byte at a time, so it never reads past the byte that ends or breaks a sequence.
/// It states its own pin as its horizon with each read, and releases the input itself.
/// The pin is where the oldest output its reader can still go back to began, or the step it is running if there is none.
/// On reaching a new segment, it releases what lies below the pin.
/// A read that would hold more of its input than the buffer's limit ends the parse with an error at the pin, whose character offset is that of the output whose step began there.
/// </para>
/// <para>
/// Where the input runs out of what it has received inside a sequence, the input has not ended, so the step starves instead of reporting the sequence as invalid.
/// </para>
/// <para>
/// Each output's root offsets are its input's, and its character offsets count the UTF-16 characters produced before it.
/// Where its input starts and ends keep their root offsets, at character 0 and at the number of characters decoded.
/// </para>
/// </remarks>
file class Utf8Upstream(ICursorSource<byte> input) : IUpstream<char>
{
    private long _bookmarkChars;
    private long _bookmarkPosition;
    private long _bookmarkSegmentStart;
    private long _chars;
    private readonly ICursorSource<byte> _input = input;
    private long _position;
    private long _segmentStart;

    public SourceSpan InputEnd
    {
        get
        {
            var end = _input.InputEnd;

            return new SourceSpan(end.Source, end.Start, end.End, _chars, _chars);
        }
    }

    public SourceSpan InputStart
    {
        get
        {
            var start = _input.InputStart;

            return new SourceSpan(start.Source, start.Start, start.End, 0, 0);
        }
    }

    public bool Starved => _input.Starved;

    private LayerFailedException Invalid(ReadOnlySpan<byte> bytes, long stepStart)
    {
        var span = _input.SpanOf(stepStart, stepStart + bytes.Length);
        var hex = string.Join(' ', bytes.ToArray().Select(static value => value.ToString("X2", CultureInfo.InvariantCulture)));

        var message = bytes.Length == 1
            ? $"invalid UTF-8 byte {hex}"
            : $"invalid UTF-8 bytes {hex}";

        return new LayerFailedException(
            new ParseError(
                new SourceSpan(span.Source, span.Start, span.End, _chars, _chars),
                Utf8Layer.Name,
                new CustomMessage(message),
                [],
                []
            )
        );
    }

    private bool Step(StreamBuffer<char> buffer)
    {
        var stepStart = _position;
        var pin = buffer.OldestStepStart ?? stepStart;

        if (!_input.TryGetSegment(stepStart, pin, out _, out var segmentStart))
        {
            return false;
        }

        if (segmentStart != _segmentStart)
        {
            _segmentStart = segmentStart;
            _input.Release(pin, failures: null);
        }

        Span<byte> bytes = stackalloc byte[4];
        var count = 0;
        var status = OperationStatus.NeedMoreData;
        var rune = default(Rune);
        var consumed = 0;

        while (status == OperationStatus.NeedMoreData && TryRead(stepStart + count, pin, out bytes[count]))
        {
            count++;
            status = Rune.DecodeFromUtf8(bytes[..count], out rune, out consumed);
        }

        if (status != OperationStatus.Done)
        {
            if (_input.Starved)
            {
                return false;
            }

            throw Invalid(bytes[..consumed], stepStart);
        }

        _position = stepStart + consumed;

        if (stepStart == 0 && rune.Value == 0xFEFF)
        {
            return true;
        }

        var inputSpan = _input.SpanOf(stepStart, _position);
        var length = rune.Utf16SequenceLength;
        var span = new SourceSpan(inputSpan.Source, inputSpan.Start, inputSpan.End, _chars, _chars + length);
        Span<char> chars = stackalloc char[2];

        rune.EncodeToUtf16(chars);

        for (var group = 0; group < length; group++)
        {
            buffer.Add(chars[group], new StreamSlot(span, stepStart, StateBefore: null, group));
        }

        _chars += length;

        return true;
    }

    private bool TryRead(long position, long pin, out byte value)
    {
        if (!_input.TryGetSegment(position, pin, out var segment, out var segmentStart))
        {
            value = 0;
            return false;
        }

        value = segment.Span[(int)(position - segmentStart)];
        return true;
    }

    public void Bookmark()
    {
        _bookmarkChars = _chars;
        _bookmarkPosition = _position;
        _bookmarkSegmentStart = _segmentStart;
        _input.Bookmark();
    }

    public void Dispose() => _input.Dispose();

    public void ForgetFailures() => _input.ForgetFailures();

    public bool Restart()
    {
        _chars = _bookmarkChars;
        _position = _bookmarkPosition;
        _segmentStart = _bookmarkSegmentStart;
        _input.Restart();

        return true;
    }

    public bool TryStep(StreamBuffer<char> buffer)
    {
        try
        {
            return Step(buffer);
        }
        catch (BufferLimitExceededException exception)
        {
            var chars = buffer.OldestStepStart is null
                ? _chars
                : buffer.SpanOf(buffer.Horizon, buffer.Horizon + 1).CharStart;

            throw new LayerFailedException(
                ParseErrors.BufferLimit<byte>(
                    exception,
                    failures: null,
                    Utf8Layer.Name,
                    (start, end) =>
                    {
                        var bytes = _input.SpanOf(start, end);

                        return new SourceSpan(bytes.Source, bytes.Start, bytes.End, chars, chars);
                    }
                )
            );
        }
    }
}

/// <summary>
/// Decodes UTF-8 bytes into UTF-16 characters. Backs <c>Layer.Utf8</c>.
/// </summary>
internal class Utf8Layer : Layer<byte, char>
{
    /// <summary>
    /// The layer's name in errors.
    /// </summary>
    public const string Name = "decoder";

    internal override bool DecodesUtf8 => true;

    public static Utf8Layer Instance { get; } = new();

    internal override IUpstream<char> Open(ICursorSource<byte> input, PipelineRun run) =>
        new Utf8Upstream(input);
}
