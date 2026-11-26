using BenchmarkDotNet.Attributes;

namespace Creole.Benchmarks;

/// <summary>
/// What reading a mode channel costs a lexer: the token JSON parser over each JSON input, behind a one-mode <c>Layer.Modal</c> whose modes are an <see cref="int"/>, behind one whose modes are a union, and behind the same <c>Layer.Tokens</c>.
/// </summary>
[BenchmarkCategory("Layers")]
public class LayerBenchmarks
{
    private string _text = "";

    [ParamsSource(nameof(InputNames))]
    public string Input { get; set; } = JsonInputs.Small;

    public static IEnumerable<string> InputNames =>
        JsonInputs.Names;

    [Benchmark]
    public bool Modal() =>
        LayeredJson.Modal.Parse(_text).Value is Parsed<Unit>;

    [Benchmark]
    public bool ModalUnion() =>
        LayeredJson.ModalUnion.Parse(_text).Value is Parsed<Unit>;

    [GlobalSetup]
    public void Setup() =>
        _text = JsonInputs.Get(Input);

    [Benchmark(Baseline = true)]
    public bool Tokens() =>
        LayeredJson.Tokens.Parse(_text).Value is Parsed<Unit>;
}
