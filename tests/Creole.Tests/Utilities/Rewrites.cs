namespace Creole.Tests;

/// <summary>
/// Runs a parser lowered with and without a rewrite, so tests can check that the rewrite changes nothing a parse can observe, including the failure it reports.
/// </summary>
/// <remarks>
/// The rewritten plan dispatches every <c>Or</c> with a keyed alternative, whatever <see cref="PlanBuilder.DispatchThreshold"/> is: the threshold only tunes speed, and dispatch must be exact wherever it applies.
/// </remarks>
internal static class Rewrites
{
    /// <summary>
    /// What a parse left behind: whether it succeeded, its value, where the cursor stopped, whether a cut is in force, and the furthest failure it recorded.
    /// A <see cref="GrammarException"/> is reported as <paramref name="Threw"/>.
    /// </summary>
    public readonly record struct Observed(bool Ok, string? Value, long Position, bool Cut, bool Threw, ParseError? Error);

    private static Observed Observe<TIn, T>(Plan<TIn, T> plan, TIn[] input, int? segmentSize, bool cut)
    {
        var log = new FailureLog<TIn>();
        var cursor = segmentSize is { } size
            ? new Cursor<TIn>(new SegmentedSource<TIn>(input, size)) { Failures = log }
            : new Cursor<TIn>(input) { Failures = log };
        cursor.Cut = cut;
        T value = default!;

        try
        {
            var ok = plan.TryParse(ref cursor, ref value);
            var error = log.ToError(in cursor, "parser", static (start, end) => new SourceSpan(SourceId.Unnamed, start, end));

            return new Observed(ok, ok ? value?.ToString() : null, cursor.Position, cursor.Cut, Threw: false, error);
        }
        catch (GrammarException)
        {
            return new Observed(false, null, cursor.Position, cursor.Cut, Threw: true, Error: null);
        }
    }

    /// <summary>
    /// Runs <paramref name="parser"/> on <paramref name="input"/> lowered with no rewrites and with <paramref name="rewrite"/>, from the same cut flag, in one segment or in segments of <paramref name="segmentSize"/>.
    /// </summary>
    public static (Observed Without, Observed With) Compare<T>(Parser<char, T> parser, LoweringOptions rewrite, string input, int? segmentSize = null, bool cut = false) =>
        Compare(parser, rewrite, input.ToCharArray(), segmentSize, cut);

    /// <summary>
    /// Runs <paramref name="parser"/> on <paramref name="input"/> lowered with no rewrites and with <paramref name="rewrite"/>, from the same cut flag, in one segment or in segments of <paramref name="segmentSize"/>.
    /// </summary>
    public static (Observed Without, Observed With) Compare<TIn, T>(Parser<TIn, T> parser, LoweringOptions rewrite, TIn[] input, int? segmentSize = null, bool cut = false) =>
    (
        Observe(new PlanBuilder(LoweringOptions.None).Lower(parser), input, segmentSize, cut),
        Observe(new PlanBuilder(rewrite, dispatchThreshold: 1).Lower(parser), input, segmentSize, cut)
    );
}
