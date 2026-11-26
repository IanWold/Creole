using System.Text;

namespace Creole.MemoryGate;

/// <summary>
/// An input of the memory gate: its name, the seed it is generated from, how to write one record of it, and how to parse a reader of it.
/// </summary>
/// <param name="Name">The name the gate's command line takes.</param>
/// <param name="Seed">The seed of the <see cref="Random"/> its records are written from.</param>
/// <param name="WriteRecord">Writes the next record.</param>
/// <param name="Parse">Parses the text a reader reads, and returns whether it parsed.</param>
public sealed record GateInput(string Name, int Seed, Action<Random, StringBuilder> WriteRecord, Func<TextReader, bool> Parse)
{
    /// <summary>
    /// Every input of the gate: Example B, then JSON lines.
    /// </summary>
    public static IReadOnlyList<GateInput> All { get; } =
    [
        new("example-b", 1, ExampleBInput.Write, ExampleBInput.Parse),
        new("json-lines", 2, JsonLinesInput.Write, JsonLinesInput.Parse),
    ];
}
