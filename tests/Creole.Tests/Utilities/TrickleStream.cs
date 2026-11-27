namespace Creole.Tests;

/// <summary>
/// A read-only stream over an array that gives one byte for each read, so that every multi-byte sequence is split across reads, and counts how many it has given.
/// </summary>
/// <param name="bytes">The bytes to give.</param>
internal class TrickleStream(byte[] bytes) : Stream
{
    private readonly byte[] _bytes = bytes;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    /// <summary>
    /// How many bytes it has given.
    /// </summary>
    public int Consumed { get; private set; }

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count) =>
        Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        if (Consumed == _bytes.Length || buffer.IsEmpty)
        {
            return 0;
        }

        buffer[0] = _bytes[Consumed++];

        return 1;
    }

    public override long Seek(long offset, SeekOrigin origin) =>
        throw new NotSupportedException();

    public override void SetLength(long value) =>
        throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();
}
