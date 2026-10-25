using BenchmarkDotNet.Attributes;
using static Creole.Grammar<Creole.Benchmarks.JsonToken>;
using static Creole.TokenGrammar<Creole.Benchmarks.JsonToken, Creole.Benchmarks.JsonKind>;

namespace Creole.Benchmarks;

/// <summary>
/// What recording failures costs: the medium JSON document's tokens, and an invalid copy of them, parsed with and without a failure log, and the invalid copy's error built.
/// </summary>
/// <remarks>
/// <c>Labelled</c> adds <c>Named</c> and <c>Context</c> to the grammar. <c>BuildError</c> builds only the reported error, from a log that <see cref="Setup"/> filled, so its allocation is the error's alone.
/// </remarks>
[BenchmarkCategory("Errors")]
public class ErrorBenchmarks
{
    private static readonly Parser<JsonToken, Unit> _labelledValue = Rec(() => OneOf(
        Kind(JsonKind.StringLiteral).Ignore(),
        Kind(JsonKind.Number).Ignore(),
        Kind(JsonKind.True).Ignore(),
        Kind(JsonKind.False).Ignore(),
        Kind(JsonKind.Null).Ignore(),
        _labelledArray!,
        _labelledObject!
    )).Named("value");

    private static readonly Parser<JsonToken, Unit> _labelledArray =
        _labelledValue.SepBy(Kind(JsonKind.Comma)).Between(Kind(JsonKind.LBracket), Kind(JsonKind.RBracket)).Ignore().Context("in array");

    private static readonly Parser<JsonToken, Unit> _labelledObject =
        Seq(Kind(JsonKind.StringLiteral), Kind(JsonKind.Colon), _labelledValue)
        .SepBy(Kind(JsonKind.Comma))
        .Between(Kind(JsonKind.LBrace), Kind(JsonKind.RBrace))
        .Ignore()
        .Context("in object");

    private readonly FailureLog<JsonToken> _failed = new();
    private readonly JsonToken[] _invalid = Invalid(LoweringInputs.JsonTokens);
    private readonly Plan<JsonToken, Unit> _labelled = new PlanBuilder().Lower(_labelledValue.Before(End));
    private readonly FailureLog<JsonToken> _log = new();
    private readonly Plan<JsonToken, Unit> _plain = new PlanBuilder().Lower(CreoleTokenJson.Document);
    private readonly JsonToken[] _tokens = LoweringInputs.JsonTokens;

    /// <summary>
    /// <paramref name="tokens"/> with the first comma after the middle replaced by a colon, so a parse fails there.
    /// </summary>
    private static JsonToken[] Invalid(JsonToken[] tokens)
    {
        JsonToken[] copy = [.. tokens];
        var comma = Array.FindIndex(copy, copy.Length / 2, static token => token.Kind == JsonKind.Comma);

        copy[comma] = new JsonToken(JsonKind.Colon);
        return copy;
    }

    private static long Run(Plan<JsonToken, Unit> plan, JsonToken[] input, FailureLog<JsonToken>? log)
    {
        log?.Clear();
        var cursor = new Cursor<JsonToken>(input) { Failures = log };
        var result = default(Unit);

        return plan.TryParse(ref cursor, ref result)
            ? cursor.Position
            : -1;
    }

    [Benchmark]
    public ParseError? BuildError()
    {
        var cursor = new Cursor<JsonToken>(_invalid);

        return _failed.ToError(in cursor, "parser", static (start, end) => new SourceSpan(SourceId.Unnamed, start, end));
    }

    [Benchmark]
    public long Invalid() =>
        Run(_plain, _invalid, null);

    [Benchmark]
    public long InvalidRecording() =>
        Run(_plain, _invalid, _log);

    [Benchmark]
    public ParseError? InvalidWithError()
    {
        _log.Clear();

        var cursor = new Cursor<JsonToken>(_invalid) { Failures = _log };
        var result = default(Unit);

        return _plain.TryParse(ref cursor, ref result)
            ? null
            : _log.ToError(in cursor, "parser", static (start, end) => new SourceSpan(SourceId.Unnamed, start, end));
    }

    [Benchmark]
    public long Labelled() =>
        Run(_labelled, _tokens, null);

    [Benchmark]
    public long LabelledRecording() =>
        Run(_labelled, _tokens, _log);

    [Benchmark(Baseline = true)]
    public long Plain() =>
        Run(_plain, _tokens, null);

    [GlobalSetup]
    public void Setup() =>
        Run(_plain, _invalid, _failed);

    [Benchmark]
    public long PlainRecording() =>
        Run(_plain, _tokens, _log);
}
