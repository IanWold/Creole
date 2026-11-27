using System.Text;
using BenchmarkDotNet.Attributes;

namespace Creole.Benchmarks;

/// <summary>
/// <c>ParseMany</c> over 1,000 JSON lines held in memory, each the small JSON document on one line, as bytes through <see cref="Layer.Utf8"/> and as text.
/// </summary>
/// <remarks>
/// ADR-0045's fork 6 trial ran here, with a variant that ran each item without failure logs beside each of these.
/// </remarks>
[BenchmarkCategory("Records")]
public class RecordBenchmarks
{
    private const int Records = 1_000;

    private static readonly Pipeline<byte, Unit> _fromBytes = Pipeline.From(Layer.Utf8().Then(LayeredJson.TokensLexer)).Then(CreoleTokenJson.Value);

    private static readonly Pipeline<char, Unit> _fromText = Pipeline.From(LayeredJson.TokensLexer).Then(CreoleTokenJson.Value);

    private byte[] _bytes = [];
    private string _text = "";

    [Benchmark]
    public int Bytes() =>
        _fromBytes.ParseMany(_bytes).Count(static result => result.Value is Parsed<Unit>);

    [GlobalSetup]
    public void Setup()
    {
        _text = string.Join('\n', Enumerable.Repeat(JsonInputs.Get(JsonInputs.Small).ReplaceLineEndings(" "), Records));
        _bytes = Encoding.UTF8.GetBytes(_text);

        if (Bytes() != Records || Text() != Records)
        {
            throw new InvalidOperationException("A record did not parse.");
        }
    }

    [Benchmark]
    public int Text() =>
        _fromText.ParseMany(_text).Count(static result => result.Value is Parsed<Unit>);
}
