using System.Buffers;
using System.Diagnostics;

namespace Creole;

/// <summary>
/// The root of <c>ParseManyAsync</c> over a <see cref="Stream"/>: it holds the bytes the driver has received, each step adds them as the layer above asks, and once it has added them all it starves until the driver receives more.
/// </summary>
/// <remarks>
/// <para>
/// Byte <c>i</c> has the span <c>[i, i + 1)</c> in <see cref="SourceId.Unnamed"/>.
/// Once the stream ends, the input ends at the number of bytes read.
/// </para>
/// <para>
/// After an item starves, the driver calls <see cref="ReceiveAsync"/>, which reads until it holds at least twice what the attempt saw, counted from the buffer's horizon, and one read's size more.
/// Each read asks for the whole shortfall, so a read that returns fewer bytes means the source had no more ready.
/// It returns sooner when the stream ends, or when the source has nothing more ready: a read returned fewer bytes than it asked for, and the next read does not complete at once.
/// That next read stays pending, and the next call awaits it first.
/// It never holds more than the buffer's limit and one byte past the buffer's horizon, counting the bytes it has not yet added, so an item that needs more ends with the limit's error at the pull that exceeds it.
/// </para>
/// <para>
/// The stream belongs to the caller, so disposing the upstream leaves it open.
/// Disposing it cancels a pending read, and lets that read's memory go to the garbage collector rather than back to the pool, since the stream may still write into it.
/// </para>
/// </remarks>
/// <param name="stream">The caller's stream.</param>
/// <param name="cancellationToken">Cancels the reads.</param>
internal class AsyncStreamUpstream(Stream stream, CancellationToken cancellationToken) : IUpstream<byte>
{
    private const int MinimumRead = 4_096;

    private int _asked;
    private bool _ended;
    private ValueTask<int>? _pending;
    private long _read;
    private readonly CancellationTokenSource _reads = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    private byte[]? _staged = ArrayPool<byte>.Shared.Rent(MinimumRead);
    private int _stagedEnd;
    private int _stagedStart;
    private readonly Stream _stream = stream;

    public SourceSpan InputEnd => new(SourceId.Unnamed, _read, _read);

    public SourceSpan InputStart => new(SourceId.Unnamed, 0, 0);

    /// <summary>
    /// How many bytes it has received: those added to the buffer and those it holds to add.
    /// </summary>
    public long Received => _read + (_stagedEnd - _stagedStart);

    public bool Starved => _stagedStart == _stagedEnd && !_ended;

    /// <summary>
    /// Starts a read of up to <paramref name="count"/> bytes after those it holds, first moving or growing its memory to make room.
    /// </summary>
    private ValueTask<int> Read(int count)
    {
        var staged = _staged!;

        if (staged.Length - _stagedEnd < count)
        {
            var held = _stagedEnd - _stagedStart;

            if (staged.Length - held < count)
            {
                var grown = ArrayPool<byte>.Shared.Rent(held + count);

                staged.AsSpan(_stagedStart, held).CopyTo(grown);
                ArrayPool<byte>.Shared.Return(staged);
                _staged = staged = grown;
            }
            else
            {
                staged.AsSpan(_stagedStart, held).CopyTo(staged);
            }

            _stagedStart = 0;
            _stagedEnd = held;
        }

        _asked = count;

        return _stream.ReadAsync(staged.AsMemory(_stagedEnd, count), _reads.Token);
    }

    public void Dispose()
    {
        if (_staged is null)
        {
            return;
        }

        _reads.Cancel();

        if (_pending is { } pending)
        {
            _pending = null;

            if (!pending.IsCompleted)
            {
                _staged = null;
            }

            _ = pending.AsTask().ContinueWith(static task => _ = task.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        if (_staged is { } staged)
        {
            _staged = null;
            ArrayPool<byte>.Shared.Return(staged);
        }

        _reads.Dispose();
    }

    /// <summary>
    /// Receives more bytes, after an item that started at <paramref name="buffer"/>'s horizon starved.
    /// </summary>
    /// <param name="buffer">The buffer this upstream adds to, whose horizon is where the starved item's input starts.</param>
    /// <param name="limit">The buffer's limit, <see cref="ParseOptions.MaxBufferedItems"/>.</param>
    /// <exception cref="OperationCanceledException">The token was cancelled while a read was pending.</exception>
    public async ValueTask ReceiveAsync(StreamBuffer<byte> buffer, int limit)
    {
        var horizon = buffer.Horizon;
        var seen = Received - horizon;
        var target = Math.Min(horizon + Math.Max(2 * seen, seen + MinimumRead), horizon + limit + 1L);
        var received = false;
        var wasShort = false;

        Debug.Assert(target > Received, "An item starved with as much input as the limit allows.");

        while (!_ended && Received < target)
        {
            var read = _pending ?? Read((int)Math.Min(target - Received, Array.MaxLength - (_stagedEnd - _stagedStart)));

            _pending = null;

            if (received && wasShort && !read.IsCompleted)
            {
                _pending = read;
                return;
            }

            var count = await read.ConfigureAwait(false);

            if (count == 0)
            {
                _ended = true;
                return;
            }

            _stagedEnd += count;
            received = true;
            wasShort = count < _asked;
        }
    }

    public bool TryStep(StreamBuffer<byte> buffer)
    {
        if (_staged is not { } staged || _stagedStart == _stagedEnd)
        {
            return false;
        }

        var count = Math.Min(_stagedEnd - _stagedStart, buffer.Room);

        for (var i = 0; i < count; i++)
        {
            var position = _read + i;

            buffer.Add(staged[_stagedStart + i], new StreamSlot(new SourceSpan(SourceId.Unnamed, position, position + 1), _read, StateBefore: null, GroupIndex: i));
        }

        _stagedStart += count;
        _read += count;

        return true;
    }
}
