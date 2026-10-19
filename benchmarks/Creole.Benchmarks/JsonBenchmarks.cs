using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Creole.Benchmarks;

[BenchmarkCategory("Json")]
public class JsonBenchmarks
{
    private string _text = "";
    private byte[] _utf8 = [];

    [ParamsSource(nameof(InputNames))]
    public string Input { get; set; } = JsonInputs.Small;

    public static IEnumerable<string> InputNames =>
        JsonInputs.Names;

    public static long InputBytes(BenchmarkCase benchmarkCase) =>
        JsonInputs.Utf8Length((string)benchmarkCase.Parameters[nameof(Input)]!);

    [Benchmark]
    public JsonValue Parlot() =>
        ParlotJson.Parse(ParlotJson.Interpreted, _text);

    [Benchmark]
    public JsonValue ParlotCompiled() =>
        ParlotJson.Parse(ParlotJson.Compiled, _text);

    [Benchmark]
    public JsonValue Pidgin() =>
        PidginJson.Parse(_text);

    [GlobalSetup]
    public void Setup()
    {
        _text = JsonInputs.Get(Input);
        _utf8 = Encoding.UTF8.GetBytes(_text);
    }

    [Benchmark]
    public JsonValue Sprache() =>
        SpracheJson.Parse(_text);

    [Benchmark]
    public JsonValue Superpower() =>
        SuperpowerJson.Parse(_text);

    [Benchmark(Baseline = true)]
    public JsonValue SystemTextJson() =>
        SystemTextJsonBaseline.Parse(_utf8);
}
