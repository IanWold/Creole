using System.Globalization;
using System.IO.Pipelines;
using System.Text;
using Creole.Benchmarks;

namespace Creole.MemoryGate;

/// <summary>
/// JSON lines parsed with <c>ParseManyAsync</c> as an input of the memory harness: the JSON-lines records, written as UTF-8 into a <see cref="Pipe"/> and parsed from it without a thread of their own.
/// </summary>
/// <remarks>
/// <para>
/// The records are <see cref="JsonLinesInput"/>'s, from the same seed, so a reader generates the same text.
/// The parse decodes with <see cref="Layer.Utf8"/>, reads each record with <see cref="CreoleTokenJson.Value"/>, and keeps only the span of the last, which must start at the byte the last record was written at.
/// So past 4 GiB the run checks that offsets beyond <see cref="uint.MaxValue"/> are right, not only that the parse ends.
/// </para>
/// <para>
/// The samples are taken on the writing thread between records.
/// A line of 1 MiB or more is written in 16 parts, with a sample after each, because the parse holds a record whole until its last byte arrives, and a sample between records would miss it.
/// </para>
/// <para>
/// It is not one of the Phase 1 gate's inputs. It measures records parsed from a stream that does not block (STRM-02).
/// </para>
/// </remarks>
public static class JsonLinesAsyncInput
{
    private const int LargeLength = 4 * 1_024 * 1_024;
    private const int LongLine = 1_024 * 1_024;
    private const int Parts = 16;

    private static readonly Pipeline<byte, Spanned<Unit>> _pipeline = Pipeline.From(Layer.Utf8().Then(LayeredJson.TokensLexer)).Then(CreoleTokenJson.Value.Spanned());

    /// <summary>
    /// Parses every record the pipe receives, then completes the pipe's reader.
    /// </summary>
    /// <returns>Where the last record starts in bytes, or null if a record did not parse.</returns>
    private static async Task<long?> ParseAndComplete(PipeReader reader)
    {
        try
        {
            var last = -1L;

            await foreach (var result in _pipeline.ParseManyAsync(reader.AsStream()).ConfigureAwait(false))
            {
                if (result.Value is not Parsed<Spanned<Unit>> parsed)
                {
                    return null;
                }

                last = parsed.Value.Span.Start;
            }

            return last;
        }
        finally
        {
            await reader.CompleteAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Writes the lines <paramref name="reader"/> reads into a pipe that the parse reads, and returns whether every record parsed and the last one's span starts where it was written.
    /// </summary>
    public static bool Parse(TextReader reader)
    {
        var pipe = new Pipe();
        var parse = Task.Run(() => ParseAndComplete(pipe.Reader));
        var writer = new PipeTextWriter(pipe.Writer);
        var lastStart = -1L;

        while (!writer.Stopped && reader.ReadLine() is { } line)
        {
            lastStart = writer.Written;

            if (line.Length >= LongLine && reader is GeneratedReader generated)
            {
                var part = line.Length / Parts;

                for (var i = 0; i < Parts - 1; i++)
                {
                    writer.Write(line.AsSpan(i * part, part));
                    writer.Flush();
                    generated.Sample();
                }

                writer.Write(line.AsSpan((Parts - 1) * part));
            }
            else
            {
                writer.Write(line);
            }

            writer.Write("\n");
        }

        writer.Flush();
        pipe.Writer.Complete();

        return parse.GetAwaiter().GetResult() is { } start && start == lastStart;
    }

    /// <summary>
    /// Writes one record of at least 4 MiB: an object whose array holds the numbers 0 to 999 over and over.
    /// </summary>
    public static void WriteLarge(StringBuilder record)
    {
        record.Append("{\"id\":-1,\"values\":[");

        for (var i = 0; record.Length < LargeLength; i++)
        {
            record.Append((i % 1_000).ToString(CultureInfo.InvariantCulture)).Append(',');
        }

        record.Append("0]}\n");
    }
}
