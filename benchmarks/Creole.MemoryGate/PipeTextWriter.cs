using System.IO.Pipelines;
using System.Text;

namespace Creole.MemoryGate;

/// <summary>
/// Writes text into a pipe as UTF-8, flushing every few kilobytes, and stops once the parse has stopped reading.
/// </summary>
/// <param name="writer">The pipe's writer.</param>
internal sealed class PipeTextWriter(PipeWriter writer)
{
    private const int BytesPerFlush = 4096;

    private int _unflushed;
    private readonly PipeWriter _writer = writer;

    /// <summary>
    /// Whether the parse has stopped reading, so nothing more is written.
    /// </summary>
    public bool Stopped { get; private set; }

    /// <summary>
    /// How many bytes it has written.
    /// </summary>
    public long Written { get; private set; }

    public void Flush()
    {
        if (!Stopped)
        {
            Stopped = _writer.FlushAsync().AsTask().GetAwaiter().GetResult().IsCompleted;
            _unflushed = 0;
        }
    }

    public void Write(ReadOnlySpan<char> text)
    {
        if (Stopped)
        {
            return;
        }

        var count = Encoding.UTF8.GetBytes(text, _writer.GetSpan(Encoding.UTF8.GetMaxByteCount(text.Length)));

        _writer.Advance(count);
        _unflushed += count;
        Written += count;

        if (_unflushed >= BytesPerFlush)
        {
            Flush();
        }
    }
}
