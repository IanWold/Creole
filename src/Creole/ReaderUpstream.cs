using System.Buffers;

namespace Creole;

/// <summary>
/// The root of a pipeline over a <see cref="TextReader"/>: each step reads the next chunk of characters, blocking until the reader has some or ends.
/// </summary>
/// <remarks>
/// <para>
/// Character <c>i</c> has the span <c>[i, i + 1)</c> in <see cref="SourceId.Unnamed"/>.
/// Once the reader ends, the input ends at the number of characters read.
/// </para>
/// <para>
/// The reader belongs to the caller, so disposing the upstream leaves it open.
/// </para>
/// </remarks>
/// <param name="reader">The caller's reader.</param>
/// <param name="chunkSize">The most characters a step reads. A step reads fewer when the buffer has less <see cref="StreamBuffer{T}.Room"/> left.</param>
internal sealed class ReaderUpstream(TextReader reader, int chunkSize = 1024) : IUpstream<char>
{
    private char[]? _chunk = ArrayPool<char>.Shared.Rent(chunkSize);
    private readonly int _chunkSize = chunkSize;
    private long _read;
    private readonly TextReader _reader = reader;

    public SourceSpan InputEnd => new(SourceId.Unnamed, _read, _read);

    public SourceSpan InputStart => new(SourceId.Unnamed, 0, 0);

    public void Dispose()
    {
        if (_chunk is { } chunk)
        {
            _chunk = null;
            ArrayPool<char>.Shared.Return(chunk);
        }
    }

    public bool TryStep(StreamBuffer<char> buffer)
    {
        if (_chunk is not { } chunk)
        {
            return false;
        }

        var count = _reader.Read(chunk.AsSpan(0, Math.Min(_chunkSize, buffer.Room)));

        if (count == 0)
        {
            Dispose();
            return false;
        }

        for (var i = 0; i < count; i++)
        {
            var position = _read + i;

            buffer.Add(chunk[i], new StreamSlot(new SourceSpan(SourceId.Unnamed, position, position + 1), _read, StateBefore: null, GroupIndex: i));
        }

        _read += count;

        return true;
    }
}
