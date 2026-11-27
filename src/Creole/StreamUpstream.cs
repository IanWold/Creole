using System.Buffers;

namespace Creole;

/// <summary>
/// The root of a pipeline over a <see cref="Stream"/>: each step reads the next chunk of bytes, blocking until the stream has some or ends.
/// </summary>
/// <remarks>
/// <para>
/// Byte <c>i</c> has the span <c>[i, i + 1)</c> in <see cref="SourceId.Unnamed"/>.
/// Once the stream ends, the input ends at the number of bytes read.
/// A read blocks rather than running out, so the input never starves.
/// </para>
/// <para>
/// The stream belongs to the caller, so disposing the upstream leaves it open.
/// </para>
/// </remarks>
/// <param name="stream">The caller's stream.</param>
/// <param name="chunkSize">The most bytes a step reads. A step reads fewer when the buffer has less <see cref="StreamBuffer{T}.Room"/> left.</param>
internal sealed class StreamUpstream(Stream stream, int chunkSize = 1024) : IUpstream<byte>
{
    private byte[]? _chunk = ArrayPool<byte>.Shared.Rent(chunkSize);
    private readonly int _chunkSize = chunkSize;
    private long _read;
    private readonly Stream _stream = stream;

    public SourceSpan InputEnd => new(SourceId.Unnamed, _read, _read);

    public SourceSpan InputStart => new(SourceId.Unnamed, 0, 0);

    public void Dispose()
    {
        if (_chunk is { } chunk)
        {
            _chunk = null;
            ArrayPool<byte>.Shared.Return(chunk);
        }
    }

    public bool TryStep(StreamBuffer<byte> buffer)
    {
        if (_chunk is not { } chunk)
        {
            return false;
        }

        var count = _stream.Read(chunk.AsSpan(0, Math.Min(_chunkSize, buffer.Room)));

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
