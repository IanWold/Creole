namespace Creole;

internal sealed partial class PlanBuilder
{
    public static Plan<TIn, TIn> Any<TIn>() =>
        new ItemPlan<TIn>(static _ => true, null);

    public static Plan<TToken, TCase> Case<TToken, TCase>(Expected expected) =>
        new CasePlan<TToken, TCase>(expected);

    public static Plan<char, char> CharClass(CharSet set, Expected expected) =>
        new ItemPlan<char>(set.Contains, expected);

    public static Plan<TIn, TOut> Custom<TIn, TOut>(IScanner<TIn, TOut> scanner, Expected? expected) =>
        new CustomPlan<TIn, TOut>(scanner, expected);

    public static Plan<TIn, Unit> End<TIn>() =>
        new EndPlan<TIn>();

    public static Plan<TToken, TToken> Kind<TToken, TKind>(TKind kind, Expected expected)
        where TToken : IKinded<TKind>
        where TKind : struct, Enum =>
        new ItemPlan<TToken>(token => EqualityComparer<TKind>.Default.Equals(token.Kind, kind), expected);

    public static Plan<TIn, TIn> Satisfy<TIn>(Func<TIn, bool> predicate) =>
        new ItemPlan<TIn>(predicate, null);

    public static Plan<char, string> String(string text, bool ignoreCase, Expected expected) =>
        new StringPlan(text, ignoreCase, expected);

    public static Plan<TIn, TIn> Token<TIn>(TIn value, Expected expected) =>
        new ItemPlan<TIn>(item => EqualityComparer<TIn>.Default.Equals(item, value), expected);
}
