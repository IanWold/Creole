using System.Threading.Tasks.Sources;

namespace Creole.Tests;

/// <summary>
/// A read that does its work only once it is awaited, so a reader that checks it first always finds it pending.
/// </summary>
/// <param name="read">Does the read and returns how many bytes it gave.</param>
file class AwaitedRead(Func<int> read) : IValueTaskSource<int>
{
    private bool _done;
    private readonly Func<int> _read = read;
    private int _result;

    public int GetResult(short token) => _result;

    public ValueTaskSourceStatus GetStatus(short token) =>
        _done
            ? ValueTaskSourceStatus.Succeeded
            : ValueTaskSourceStatus.Pending;

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        _result = _read();
        _done = true;
        ThreadPool.UnsafeQueueUserWorkItem(static pair => pair.continuation(pair.state), (continuation, state), preferLocal: false);
    }
}

/// <summary>
/// A stream over bytes in memory whose reads each give at most as many bytes as <paramref name="size"/> says, and, if <paramref name="pending"/>, stay pending until they are awaited.
/// </summary>
/// <param name="bytes">The whole input.</param>
/// <param name="size">The most bytes the next read gives.</param>
/// <param name="pending">Whether each asynchronous read stays pending until it is awaited, as a read waiting on a slow producer does.</param>
internal class ChunkedStream(byte[] bytes, Func<int> size, bool pending) : Stream
{
    private readonly byte[] _bytes = bytes;
    private readonly bool _pending = pending;
    private int _position;
    private readonly Func<int> _size = size;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// How many reads have given bytes or the end.
    /// </summary>
    public int Reads { get; private set; }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count) =>
        Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        var count = Math.Min(Math.Min(buffer.Length, _size()), _bytes.Length - _position);

        _bytes.AsSpan(_position, count).CopyTo(buffer);
        _position += count;
        Reads++;

        return count;
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        _pending
            ? new ValueTask<int>(new AwaitedRead(() => Read(buffer.Span)), 0)
            : new ValueTask<int>(Read(buffer.Span));

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
