using System.Text;

namespace Creole.MemoryGate;

/// <summary>
/// An input of the memory gate: its name, the seed it is generated from, how to write one record of it, and how to parse a reader of it.
/// </summary>
/// <param name="Name">The name the gate's command line takes.</param>
/// <param name="Seed">The seed of the <see cref="Random"/> its records are written from.</param>
/// <param name="WriteRecord">Writes the next record.</param>
/// <param name="Parse">Parses the text a reader reads, and returns whether it parsed.</param>
/// <param name="WriteMiddle">Writes one record in place of the record that would bring the characters written past half the length, or null to write none.</param>
public sealed record GateInput(string Name, int Seed, Action<Random, StringBuilder> WriteRecord, Func<TextReader, bool> Parse, Action<StringBuilder>? WriteMiddle = null)
{
    /// <summary>
    /// Every input the harness runs: the Phase 1 gate's Example B and JSON lines, then one JSON document of the JSON-lines records, parsed from a pipe, and the JSON-lines records parsed from a pipe with <c>ParseManyAsync</c>, alone and with one record of 4 MiB in the middle.
    /// </summary>
    public static IReadOnlyList<GateInput> All { get; } =
    [
        new("example-b", 1, ExampleBInput.Write, ExampleBInput.Parse),
        new("json-lines", 2, JsonLinesInput.Write, JsonLinesInput.Parse),
        new("json-document", 2, JsonLinesInput.Write, JsonDocumentInput.Parse),
        new("json-lines-async", 2, JsonLinesInput.Write, JsonLinesAsyncInput.Parse),
        new("json-lines-async-large", 2, JsonLinesInput.Write, JsonLinesAsyncInput.Parse, JsonLinesAsyncInput.WriteLarge),
    ];
}
