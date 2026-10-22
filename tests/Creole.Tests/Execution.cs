namespace Creole.Tests;

/// <summary>
/// Runs a parser on the interpreter, through its cached plan, until the public Parse API exists (INT-08).
/// </summary>
internal static class Execution
{
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
