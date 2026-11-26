using System.Security.Cryptography;
using System.Text;
using Creole.MemoryGate;

namespace Creole.Benchmarks.Tests;

/// <summary>
/// The memory gate's inputs: each parses, each is the same text on every run, and the reader that generates them ends and samples where it says.
/// </summary>
public class MemoryGateTests
{
    private const long Length = 100_000;

    public static TheoryData<string> Inputs =>
        [.. GateInput.All.Select(static input => input.Name)];

    private static GateInput Input(string name) =>
        GateInput.All.Single(input => input.Name == name);

    [Theory]
    [InlineData("example-b", "53ad565d8b908ba7f923b7aa1d3a804b69f5362ceccd3a7826f84c3c7e8ab0cc")]
    [InlineData("json-lines", "e9c6ebe2f2da1546da0304665317b9bac971da7de8032ac336af4ded0797ac66")]
    public void EachInputIsUnchanged(string name, string hash) =>
        Assert.Equal(hash, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(GeneratedReader.Text(Input(name), Length)))));

    [Theory]
    [MemberData(nameof(Inputs))]
    public void EachInputParses(string name) =>
        Assert.True(Input(name).Parse(new GeneratedReader(Input(name), Length, int.MaxValue, static () => { })));

    [Fact]
    public void ReadingOneCharacterAtATimeGivesTheSameText()
    {
        using var reader = new GeneratedReader(Input("example-b"), 1_000, int.MaxValue, static () => { });
        var text = new StringBuilder();

        while (reader.Peek() is var next and >= 0)
        {
            Assert.Equal(next, reader.Read());
            text.Append((char)next);
        }

        Assert.Equal(GeneratedReader.Text(Input("example-b"), 1_000), text.ToString());
    }

    [Fact]
    public void TheReaderEndsAfterTheRecordThatReachesItsLength()
    {
        using var reader = new GeneratedReader(Input("json-lines"), Length, int.MaxValue, static () => { });
        var text = reader.ReadToEnd();

        Assert.Equal(reader.Characters, text.Length);
        Assert.InRange(text.Length, Length, long.MaxValue);
        Assert.InRange(text[..^1].LastIndexOf('\n') + 1, 0, Length - 1);
    }

    [Fact]
    public void TheReaderSamplesBeforeRecordZeroAndEveryNthRecordAfter()
    {
        var samples = 0;
        using var reader = new GeneratedReader(Input("json-lines"), Length, 10, () => samples++);

        reader.ReadToEnd();

        Assert.Equal((reader.Records + 9) / 10, samples);
    }
}
