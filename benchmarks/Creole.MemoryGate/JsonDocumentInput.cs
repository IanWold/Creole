using System.IO.Pipelines;
using System.Text;
using Creole.Benchmarks;
using static Creole.TokenGrammar<Creole.Benchmarks.JsonToken, Creole.Benchmarks.JsonKind>;

namespace Creole.MemoryGate;

/// <summary>
/// One huge JSON document as an input of the memory harness: the JSON-lines records as the elements of one array, written as UTF-8 into a <see cref="Pipe"/> and parsed from it on a thread of its own.
/// </summary>
/// <remarks>
/// <para>
/// The records are <see cref="JsonLinesInput"/>'s, from the same seed, so a reader generates the same text; this input joins its lines into an array as it writes them to the pipe.
/// The parse decodes with <see cref="Layer.Utf8"/>, and its root reads each element with <see cref="CreoleTokenJson.Value"/> and skips it, so it keeps none of them.
/// </para>
/// <para>
/// It is not one of the Phase 1 gate's inputs. It measures a huge single document parsed from a pipe (STRM-04).
/// </para>
/// </remarks>
public static class JsonDocumentInput
{
    private static readonly Pipeline<byte, Unit> _pipeline = Pipeline.From(Layer.Utf8().Then(LayeredJson.TokensLexer)).Then(
        Kind(JsonKind.LBracket)
            .Then(CreoleTokenJson.Value)
            .Before(Kind(JsonKind.Comma).Then(CreoleTokenJson.Value).SkipMany())
            .Before(Kind(JsonKind.RBracket))
            .Before(Grammar<JsonToken>.End)
    );

    private static ParseResult<Unit> ParseAndComplete(PipeReader reader)
    {
        try
        {
            return _pipeline.Parse(reader.AsStream());
        }
        finally
        {
            reader.Complete();
        }
    }

    /// <summary>
    /// Parses the lines <paramref name="reader"/> reads as the elements of one JSON array, which it writes into a pipe while a thread of its own parses the pipe.
    /// </summary>
    /// <param name="reader">The reader of the records, one per line.</param>
    /// <returns>Whether the document parsed.</returns>
    public static bool Parse(TextReader reader)
    {
        var pipe = new Pipe();
        var parse = Task.Factory.StartNew(() => ParseAndComplete(pipe.Reader), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        var writer = new PipeTextWriter(pipe.Writer);
        var separator = "[\n";

        while (!writer.Stopped && reader.ReadLine() is { } line)
        {
            writer.Write(separator);
            writer.Write(line);
            separator = ",\n";
        }

        writer.Write("\n]\n");
        writer.Flush();
        pipe.Writer.Complete();

        return parse.GetAwaiter().GetResult() is Parsed<Unit>;
    }
}
