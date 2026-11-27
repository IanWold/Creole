namespace Creole;

/// <summary>
/// Runs its first plan, then its second, and produces both results if the second's span starts where the first's ends. Fails as either plan does.
/// </summary>
/// <remarks>
/// <para>
/// It reads the end of the first's span as soon as the first succeeds, from the item just before the cursor, which the input still holds.
/// It holds the second's start while the second runs, so a stream buffer that releases the item there first copies its span.
/// A first plan that consumed nothing touches whatever follows it.
/// </para>
/// <para>
/// When the spans do not touch, it fails as a rejected filter over the pair: the cursor stays after the second, so the failure is committed if either consumed input.
/// It is reported where the first started, over the input both consumed, after the failures the pair recorded are dropped.
/// The message names both parsers by <paramref name="firstName"/> and <paramref name="secondName"/>, and is built the first time a rejection is recorded.
/// </para>
/// </remarks>
/// <param name="first">The plan for the first parser.</param>
/// <param name="second">The plan for the second parser.</param>
/// <param name="firstName">What names the first parser in the message, or null if nothing does.</param>
/// <param name="secondName">What names the second parser in the message, or null if nothing does.</param>
internal class AdjacentPlan<TIn, T1, T2>(Plan<TIn, T1> first, Plan<TIn, T2> second, Expected? firstName, Expected? secondName) : Plan<TIn, (T1, T2)>
{
    private readonly Plan<TIn, T1> _first = first;
    private readonly Expected? _firstName = firstName;
    private string? _message;
    private readonly Plan<TIn, T2> _second = second;
    private readonly Expected? _secondName = secondName;

    private string Message =>
        _message ??= _firstName is { } firstName && _secondName is { } secondName
            ? $"{Name(secondName)} must come right after {Name(firstName)}, with nothing between them"
            : "these items must touch, with nothing between them";

    private static string Name(Expected name) => name.Build() switch
    {
        Label label => label.Name,
        ExpectedItem item => item.Display,
        EndOfInput => "end of input",
    };

    private static bool Touches(SourceSpan first, SourceSpan second) =>
        first.Source == second.Source && first.End == second.Start;

    public override bool TryParse(ref Cursor<TIn> cursor, ref (T1, T2) result)
    {
        var start = cursor.Position;
        var failures = cursor.SaveFailures();
        T1 first = default!;

        cursor.HoldStart();

        if (!_first.TryParse(ref cursor, ref first))
        {
            cursor.DropStart();
            cursor.KeepFailures(in failures);
            return false;
        }

        var middle = cursor.Position;
        var firstSpan = middle > start ? cursor.SpanSince(middle - 1) : default;
        T2 second = default!;

        cursor.HoldSpan();

        if (!_second.TryParse(ref cursor, ref second))
        {
            cursor.DropSpan();
            cursor.DropStart();
            cursor.KeepFailures(in failures);
            return false;
        }

        var touches = middle == start || Touches(firstSpan, cursor.SpanSince(middle));

        cursor.DropSpan();

        if (!touches)
        {
            cursor.RestoreFailures(in failures);

            if (cursor.Failures is not null)
            {
                cursor.Reject(start, Message, null);
            }

            cursor.DropStart();
            return false;
        }

        cursor.DropStart();
        cursor.KeepFailures(in failures);
        result = (first, second);
        return true;
    }
}
