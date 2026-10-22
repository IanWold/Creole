namespace Creole;

internal sealed partial class PlanBuilder
{
    public static Plan<TIn, TIn> Any<TIn>() => new ItemPlan<TIn>(static _ => true);

    public static Plan<TToken, TCase> Case<TToken, TCase>() => new CasePlan<TToken, TCase>();

    public static Plan<char, char> CharClass(CharSet set) => new ItemPlan<char>(set.Contains);

    public static Plan<TIn, Unit> End<TIn>() => new EndPlan<TIn>();

    public static Plan<TToken, TToken> Kind<TToken, TKind>(TKind kind)
        where TToken : IKinded<TKind>
        where TKind : struct, Enum =>
        new ItemPlan<TToken>(token => EqualityComparer<TKind>.Default.Equals(token.Kind, kind));

    public static Plan<TIn, TIn> Satisfy<TIn>(Func<TIn, bool> predicate) => new ItemPlan<TIn>(predicate);

    public static Plan<char, string> String(string text, bool ignoreCase) => new StringPlan(text, ignoreCase);

    public static Plan<TIn, TIn> Token<TIn>(TIn value) => new ItemPlan<TIn>(item => EqualityComparer<TIn>.Default.Equals(item, value));
}
