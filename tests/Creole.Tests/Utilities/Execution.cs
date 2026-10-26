namespace Creole.Tests;

/// <summary>
/// Runs a parser on the interpreter, through its cached plan, until the public Parse API exists (INT-08).
/// </summary>
internal static class Execution
{
    /// <summary>
    /// How <c>Parse</c>, which runs without a failure log and again with one only if it fails, differs from one run with the log, or null if it does not.
    /// </summary>
    /// <remarks>
    /// Values are compared when they are value types or strings. A reference type is built afresh by each run, so for one only success is compared.
    /// </remarks>
    public static string? Disagreement<TIn, T>(Parser<TIn, T> parser, ReadOnlyMemory<TIn> input, ParseOptions options)
    {
        var once = Parsing.TryParse(new FailureLog<TIn>(), PlanCache.Get(parser), input, options, out var value, out var error);
        var comparesValues = typeof(T).IsValueType || typeof(T) == typeof(string);

        return parser.Parse(input, options) switch
        {
            Parsed<T> parsed when once && (!comparesValues || EqualityComparer<T>.Default.Equals(parsed.Value, value)) => null,
            Failed failed when !once && failed.Diagnostics.Single().Equals(error) => null,
            var result => $"on \"{string.Join("", input.ToArray())}\": one run with the log gave {(once ? value : error)}, Parse gave {result.Value}",
        };
    }

    /// <summary>
    /// Runs <paramref name="parser"/>, lowered with <paramref name="options"/>, on <paramref name="input"/> while recording failures, and returns the error for the furthest one, from layer <c>parser</c> in an unnamed source.
    /// It also checks that <c>Parse</c>, which records failures only in a second run after the first fails, agrees with one run with the log.
    /// </summary>
    /// <returns>The error, or null if nothing failed. A parse that succeeds can still have recorded failures, such as where a loop stopped.</returns>
    public static ParseError? Error<TOut>(Parser<char, TOut> parser, string input, LoweringOptions options = LoweringOptions.None)
    {
        var plan = new PlanBuilder(options).Lower(parser);
        var log = new FailureLog<char>();
        var cursor = new Cursor<char>(input) { Failures = log };
        TOut value = default!;

        _ = plan.TryParse(ref cursor, ref value);

        Assert.Null(Disagreement(parser, input.AsMemory(), ParseOptions.Default));

        return log.ToError(in cursor, "parser", static (start, end) => new SourceSpan(SourceId.Unnamed, start, end));
    }

    /// <summary>
    /// Runs <paramref name="parser"/>, lowered with <paramref name="options"/>, on <paramref name="input"/> while recording failures, and returns the error for the furthest one, from layer <c>parser</c>, with each item's span its index.
    /// It also checks that <c>Parse</c>, which records failures only in a second run after the first fails, agrees with one run with the log.
    /// </summary>
    /// <returns>The error, or null if nothing failed.</returns>
    public static ParseError? Error<TIn, TOut>(Parser<TIn, TOut> parser, TIn[] input, LoweringOptions options = LoweringOptions.None)
    {
        var plan = new PlanBuilder(options).Lower(parser);
        var log = new FailureLog<TIn>();
        var cursor = new Cursor<TIn>(input) { Failures = log };
        TOut value = default!;

        _ = plan.TryParse(ref cursor, ref value);

        Assert.Null(Disagreement(parser, input, ParseOptions.Default));

        return log.ToError(in cursor, "parser", static (start, end) => new SourceSpan(SourceId.Unnamed, start, end));
    }

    /// <summary>
    /// Runs <paramref name="parser"/> on <paramref name="input"/> and reports the outcome, or <see cref="Outcome.Throws"/> for a <see cref="GrammarException"/>, rendering a value with <paramref name="render"/> or <see cref="object.ToString"/>.
    /// </summary>
    public static Outcome Outcome<TOut>(Parser<char, TOut> parser, string input, Func<TOut, string>? render = null)
    {
        var cursor = new Cursor<char>(input);

        try
        {
            var (ok, value) = Run(parser, ref cursor);
            return Describe(ok, value, cursor.Position, render);
        }
        catch (GrammarException)
        {
            return new Outcome.Throws();
        }
    }

    /// <summary>
    /// Runs <paramref name="parser"/> on <paramref name="input"/> and reports the outcome, or <see cref="Outcome.Throws"/> for a <see cref="GrammarException"/>, rendering a value with <paramref name="render"/> or <see cref="object.ToString"/>.
    /// </summary>
    public static Outcome Outcome<TIn, TOut>(Parser<TIn, TOut> parser, TIn[] input, Func<TOut, string>? render = null)
    {
        var cursor = new Cursor<TIn>(input);

        try
        {
            var (ok, value) = Run(parser, ref cursor);
            return Describe(ok, value, cursor.Position, render);
        }
        catch (GrammarException)
        {
            return new Outcome.Throws();
        }
    }

    /// <summary>
    /// Runs <paramref name="parser"/> at <paramref name="cursor"/>, leaving the cursor where the parse stopped.
    /// </summary>
    public static (bool Ok, TOut Value) Run<TIn, TOut>(Parser<TIn, TOut> parser, ref Cursor<TIn> cursor)
    {
        TOut value = default!;
        var ok = PlanCache.Get(parser).TryParse(ref cursor, ref value);

        return (ok, value);
    }

    private static Outcome Describe<TOut>(bool ok, TOut value, long position, Func<TOut, string>? render) =>
        ok ? new Outcome.Ok(render is null ? value?.ToString() ?? "" : render(value), (int)position)
        : position > 0 ? new Outcome.FailConsumed()
        : new Outcome.FailEmpty();
}
