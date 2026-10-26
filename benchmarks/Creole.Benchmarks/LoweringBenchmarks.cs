using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Benchmarks;

/// <summary>
/// Each interpreter lowering rewrite on a grammar it targets, lowered with the rewrite and without it.
/// The rewrite stays if it is at least 5% faster on its target and no other target is more than 5% slower.
/// </summary>
/// <remarks>
/// <para>
/// Each category compares one rewrite: the plan without it is the baseline, and every other rewrite is on in both.
/// Plans are run directly, without the public <c>Parse</c> API.
/// </para>
/// <para>
/// <c>DfaCharTokens</c> is a trial, not a rewrite. It compiles the character-level JSON token choice by hand, as a one-alternative <see cref="DfaLongestPlan{T}"/>,
/// to show whether compiling a regular subtree other than <c>Longest</c> pays against dispatch and scans.
/// </para>
/// </remarks>
[BenchmarkCategory("Lowering")]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class LoweringBenchmarks
{
    private const string CollapseSelect = "CollapseSelect";
    private const string DfaCharTokens = "DfaCharTokens";
    private const string DfaCSharpLexer = "DfaCSharpLexer";
    private const string DfaJsonLexer = "DfaJsonLexer";
    private const string DfaLowerCSharp = "DfaLowerCSharp";
    private const string DfaLowerJson = "DfaLowerJson";
    private const string DispatchChars = "DispatchChars";
    private const string DispatchKinds = "DispatchKinds";
    private const string FourKeysLast = "FourKeysLast";
    private const string FourKeysSkipped = "FourKeysSkipped";
    private const string ScanLongRuns = "ScanLongRuns";
    private const string ScanShortRuns = "ScanShortRuns";

    private static readonly Parser<char, Unit> _charJsonNumber =
        Char('-').Optional()
            .Then(Digit.SkipAtLeastOnce())
            .Then(Char('.').Then(Digit.SkipAtLeastOnce()).Optional())
            .Then(OneOf(Char('e'), Char('E')).Then(OneOf(Char('+'), Char('-')).Optional()).Then(Digit.SkipAtLeastOnce()).Optional())
            .Ignore();

    private static readonly Parser<char, Unit> _charJsonString =
        Char('"').Then(OneOf(Char('\\').Then(Any).Ignore(), NoneOf('"', '\\').SkipAtLeastOnce()).SkipMany()).Before(Char('"'));

    private static readonly Parser<char, int> _charJsonToken = OneOf(
        Char('{').As(1),
        Char('}').As(2),
        Char('[').As(3),
        Char(']').As(4),
        Char(':').As(5),
        Char(',').As(6),
        _charJsonString.As(7),
        _charJsonNumber.As(8),
        String("true").As(9),
        String("false").As(10),
        String("null").As(11));

    private static readonly Parser<char, Unit> _charJsonTokens =
        Whitespace.SkipMany().Then(_charJsonToken.Before(Whitespace.SkipMany()).SkipMany());

    private static readonly Parser<char, Unit> _digitChain =
        Digit.Select(static c => c - '0').Select(static d => d * 10).SkipMany();

    private static readonly Parser<char, Unit> _fourKeysLast =
        OneOf(Whitespace.SkipAtLeastOnce().As(2), Letter.SkipAtLeastOnce().As(1), Char('#').As(0), Char('@').As(3), Char('$').As(4), Char('%').As(5)).SkipMany();

    private static readonly Parser<char, Unit> _fourKeysSkipped =
        OneOf(Char('#').As(0), Char('@').As(3), Char('$').As(4), Char('%').As(5), Letter.SkipAtLeastOnce().As(1), Whitespace.SkipAtLeastOnce().As(2)).SkipMany();

    private static readonly Parser<char, Unit> _quotedStrings =
        Char('"').Then(NoneOf('"', '\\').SkipMany()).Before(Char('"')).SkipMany();

    private static readonly Parser<char, Unit> _words =
        Letter.SkipAtLeastOnce().Before(Whitespace.SkipAtLeastOnce()).SkipMany();

    private readonly Plan<char, int> _charJsonTokenCompiled = CompileTerm(_charJsonToken);
    private readonly Plan<char, int> _charJsonTokenInterpreted = Lower(_charJsonToken, LoweringOptions.All);
    private readonly Plan<char, Unit> _charJsonWithDispatch = Lower(_charJsonTokens, LoweringOptions.All);
    private readonly Plan<char, Unit> _charJsonWithoutDispatch = Lower(_charJsonTokens, LoweringOptions.All & ~LoweringOptions.Dispatch);
    private readonly string _csharpInput = LoweringInputs.CSharp;
    private readonly Parser<char, int> _csharpLexer = LongestLexers.CSharp;
    private readonly Plan<char, int> _csharpLexerWithDfa = Lower(LongestLexers.CSharp, LoweringOptions.All);
    private readonly Plan<char, int> _csharpLexerWithoutDfa = Lower(LongestLexers.CSharp, LoweringOptions.All & ~LoweringOptions.Dfa);
    private readonly Plan<char, Unit> _digitChainWithCollapse = Lower(_digitChain, LoweringOptions.All);
    private readonly Plan<char, Unit> _digitChainWithoutCollapse = Lower(_digitChain, LoweringOptions.All & ~LoweringOptions.CollapseSelect);
    private readonly string _digitInput = LoweringInputs.Digits;
    private readonly Plan<char, Unit> _fourKeysLastWithDispatch = Lower(_fourKeysLast, LoweringOptions.All);
    private readonly Plan<char, Unit> _fourKeysLastWithoutDispatch = Lower(_fourKeysLast, LoweringOptions.All & ~LoweringOptions.Dispatch);
    private readonly Plan<char, Unit> _fourKeysSkippedWithDispatch = Lower(_fourKeysSkipped, LoweringOptions.All);
    private readonly Plan<char, Unit> _fourKeysSkippedWithoutDispatch = Lower(_fourKeysSkipped, LoweringOptions.All & ~LoweringOptions.Dispatch);
    private readonly string _jsonInput = LoweringInputs.Json;
    private readonly Parser<char, int> _jsonLexer = LongestLexers.Json;
    private readonly Plan<char, int> _jsonLexerWithDfa = Lower(LongestLexers.Json, LoweringOptions.All);
    private readonly Plan<char, int> _jsonLexerWithoutDfa = Lower(LongestLexers.Json, LoweringOptions.All & ~LoweringOptions.Dfa);
    private readonly JsonToken[] _jsonTokens = LoweringInputs.JsonTokens;
    private readonly Plan<char, Unit> _quotedStringsWithoutScan = Lower(_quotedStrings, LoweringOptions.All & ~LoweringOptions.Scan);
    private readonly Plan<char, Unit> _quotedStringsWithScan = Lower(_quotedStrings, LoweringOptions.All);
    private readonly string _stringInput = LoweringInputs.Strings;
    private readonly Plan<JsonToken, Unit> _tokenJsonWithDispatch = Lower(CreoleTokenJson.Document, LoweringOptions.All);
    private readonly Plan<JsonToken, Unit> _tokenJsonWithoutDispatch = Lower(CreoleTokenJson.Document, LoweringOptions.All & ~LoweringOptions.Dispatch);
    private readonly string _wordInput = LoweringInputs.Words;
    private readonly Plan<char, Unit> _wordsWithoutScan = Lower(_words, LoweringOptions.All & ~LoweringOptions.Scan);
    private readonly Plan<char, Unit> _whitespace = Lower(Whitespace.SkipMany(), LoweringOptions.All);
    private readonly Plan<char, Unit> _wordsWithScan = Lower(_words, LoweringOptions.All);

    /// <summary>
    /// <paramref name="parser"/> as one automaton. Its fallback is a one-alternative <c>Longest</c>, which fails without consuming where <paramref name="parser"/> might fail after consuming,
    /// so the plan is fit only for timing input on which every attempt matches.
    /// </summary>
    private static Plan<char, T> CompileTerm<T>(Parser<char, T> parser)
    {
        var automaton = CommitAutomaton.Build([Regular.Of(parser)!], PlanBuilder.DfaStateCap)!;

        return new DfaLongestPlan<T>(automaton, [Lower(parser, LoweringOptions.All)], [parser.ValueOfMatch()]);
    }

    /// <summary>
    /// Folds one token's end and value into a tokenizer's running fingerprint, so lexers that split the input differently or pick different winners give different results.
    /// </summary>
    private static long Fingerprint<T>(long fingerprint, long end, T value) =>
        unchecked((((fingerprint * 31) + end) * 31) + EqualityComparer<T>.Default.GetHashCode(value!));

    private static Plan<TIn, T> Lower<TIn, T>(Parser<TIn, T> parser, LoweringOptions options) =>
        new PlanBuilder(options).Lower(parser);

    private static long Run<T>(Plan<char, T> plan, string input)
    {
        var cursor = new Cursor<char>(input);
        T result = default!;

        return plan.TryParse(ref cursor, ref result) ? cursor.Position : -1;
    }

    private static long Run<TIn, T>(Plan<TIn, T> plan, TIn[] input)
    {
        var cursor = new Cursor<TIn>(input);
        T result = default!;

        return plan.TryParse(ref cursor, ref result) ? cursor.Position : -1;
    }

    private static long Tokenize<T>(Plan<char, T> token, Plan<char, Unit> whitespace, string input)
    {
        var cursor = new Cursor<char>(input);
        T result = default!;
        var skipped = default(Unit);
        _ = whitespace.TryParse(ref cursor, ref skipped);

        var fingerprint = 0L;

        while (!cursor.IsAtEnd)
        {
            if (!token.TryParse(ref cursor, ref result))
            {
                return -1;
            }

            fingerprint = Fingerprint(fingerprint, cursor.Position, result);
            _ = whitespace.TryParse(ref cursor, ref skipped);
        }

        return fingerprint;
    }

    private static long Tokenize<T>(Plan<char, T> lexer, string input)
    {
        var cursor = new Cursor<char>(input);
        T result = default!;

        var fingerprint = 0L;

        while (!cursor.IsAtEnd)
        {
            if (lexer.TryParse(ref cursor, ref result))
            {
                fingerprint = Fingerprint(fingerprint, cursor.Position, result);
            }
            else
            {
                cursor.Advance(1);
                fingerprint = Fingerprint(fingerprint, -cursor.Position, 0);
            }
        }

        return fingerprint;
    }

    [Benchmark, BenchmarkCategory(CollapseSelect)]
    public long ChainWithCollapse() =>
        Run(_digitChainWithCollapse, _digitInput);

    [Benchmark(Baseline = true), BenchmarkCategory(CollapseSelect)]
    public long ChainWithoutCollapse() =>
        Run(_digitChainWithoutCollapse, _digitInput);

    [Benchmark, BenchmarkCategory(DfaCharTokens)]
    public long CharTokensCompiled() =>
        Tokenize(_charJsonTokenCompiled, _whitespace, _jsonInput);

    [Benchmark(Baseline = true), BenchmarkCategory(DfaCharTokens)]
    public long CharTokensInterpreted() =>
        Tokenize(_charJsonTokenInterpreted, _whitespace, _jsonInput);

    [Benchmark, BenchmarkCategory(DispatchChars)]
    public long CharsWithDispatch() =>
        Run(_charJsonWithDispatch, _jsonInput);

    [Benchmark(Baseline = true), BenchmarkCategory(DispatchChars)]
    public long CharsWithoutDispatch() =>
        Run(_charJsonWithoutDispatch, _jsonInput);

    [Benchmark, BenchmarkCategory(DfaCSharpLexer)]
    public long CSharpLexerWithDfa() =>
        Tokenize(_csharpLexerWithDfa, _csharpInput);

    [Benchmark(Baseline = true), BenchmarkCategory(DfaCSharpLexer)]
    public long CSharpLexerWithoutDfa() =>
        Tokenize(_csharpLexerWithoutDfa, _csharpInput);

    [Benchmark, BenchmarkCategory(FourKeysLast)]
    public long FourKeysLastWithDispatch() =>
        Run(_fourKeysLastWithDispatch, _wordInput);

    [Benchmark(Baseline = true), BenchmarkCategory(FourKeysLast)]
    public long FourKeysLastWithoutDispatch() =>
        Run(_fourKeysLastWithoutDispatch, _wordInput);

    [Benchmark, BenchmarkCategory(FourKeysSkipped)]
    public long FourKeysSkippedWithDispatch() =>
        Run(_fourKeysSkippedWithDispatch, _wordInput);

    [Benchmark(Baseline = true), BenchmarkCategory(FourKeysSkipped)]
    public long FourKeysSkippedWithoutDispatch() =>
        Run(_fourKeysSkippedWithoutDispatch, _wordInput);

    [Benchmark, BenchmarkCategory(DispatchKinds)]
    public long KindsWithDispatch() =>
        Run(_tokenJsonWithDispatch, _jsonTokens);

    [Benchmark(Baseline = true), BenchmarkCategory(DispatchKinds)]
    public long KindsWithoutDispatch() =>
        Run(_tokenJsonWithoutDispatch, _jsonTokens);

    [Benchmark, BenchmarkCategory(DfaJsonLexer)]
    public long JsonLexerWithDfa() =>
        Tokenize(_jsonLexerWithDfa, _jsonInput);

    [Benchmark(Baseline = true), BenchmarkCategory(DfaJsonLexer)]
    public long JsonLexerWithoutDfa() =>
        Tokenize(_jsonLexerWithoutDfa, _jsonInput);

    [Benchmark(Baseline = true), BenchmarkCategory(ScanLongRuns)]
    public long LongRunsWithoutScan() =>
        Run(_quotedStringsWithoutScan, _stringInput);

    [Benchmark, BenchmarkCategory(ScanLongRuns)]
    public long LongRunsWithScan() =>
        Run(_quotedStringsWithScan, _stringInput);

    [Benchmark, BenchmarkCategory(DfaLowerCSharp)]
    public object LowerCSharpWithDfa() =>
        Lower(_csharpLexer, LoweringOptions.All);

    [Benchmark(Baseline = true), BenchmarkCategory(DfaLowerCSharp)]
    public object LowerCSharpWithoutDfa() =>
        Lower(_csharpLexer, LoweringOptions.All & ~LoweringOptions.Dfa);

    [Benchmark, BenchmarkCategory(DfaLowerJson)]
    public object LowerJsonWithDfa() =>
        Lower(_jsonLexer, LoweringOptions.All);

    [Benchmark(Baseline = true), BenchmarkCategory(DfaLowerJson)]
    public object LowerJsonWithoutDfa() =>
        Lower(_jsonLexer, LoweringOptions.All & ~LoweringOptions.Dfa);

    [Benchmark(Baseline = true), BenchmarkCategory(ScanShortRuns)]
    public long ShortRunsWithoutScan() =>
        Run(_wordsWithoutScan, _wordInput);

    [Benchmark, BenchmarkCategory(ScanShortRuns)]
    public long ShortRunsWithScan() =>
        Run(_wordsWithScan, _wordInput);
}
