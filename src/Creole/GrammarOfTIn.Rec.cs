using System.Runtime.CompilerServices;

namespace Creole;

public abstract partial class Grammar<TIn>
{
    /// <summary>
    /// A parser that defers to the parser <paramref name="reference"/> returns, so a rule can refer to a rule declared after it, or to itself.
    /// </summary>
    /// <typeparam name="TOut">The type of value the parser produces.</typeparam>
    /// <param name="reference">
    /// Returns the parser to run, usually a field: <c>Rec(() =&gt; Value)</c>.
    /// It is called once, when the grammar is first used, not when <c>Rec</c> is called, so the field may still be null at that point.
    /// Its return type is nullable for that reason: with nullable reference types enabled, C# treats a field declared later as maybe null inside the lambda.
    /// </param>
    /// <param name="referenceExpression">Filled in by the compiler with the expression passed as <paramref name="reference"/>.</param>
    /// <param name="memberName">Filled in by the compiler with the member that calls <c>Rec</c>, such as the field being initialized.</param>
    /// <returns>A parser that behaves exactly like the one <paramref name="reference"/> returns.</returns>
    /// <exception cref="GrammarException"><paramref name="reference"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Field initializers run in declaration order, so a field read before its initializer has run is null.
    /// Wrap such a reference, and every reference that closes a cycle between rules, in <c>Rec</c>.
    /// </para>
    /// <para>
    /// <c>Rec</c> also names the rule for diagnostics.
    /// If <paramref name="reference"/> is a lambda that returns a member, such as <c>() =&gt; Value</c>, the rule is named after that member: <c>Value</c>.
    /// Otherwise it is named after the member that calls <c>Rec</c>, so <c>Value = Rec(() =&gt; OneOf(...))</c> is also named <c>Value</c>.
    /// </para>
    /// </remarks>
    public static Parser<TIn, TOut> Rec<TOut>(
        Func<Parser<TIn, TOut>?> reference,
        [CallerArgumentExpression(nameof(reference))] string referenceExpression = "",
        [CallerMemberName] string memberName = ""
    ) =>
        new RecNode<TIn, TOut>(
            Guard.NotNull(reference, referenceExpression),
            referenceExpression,
            RuleName.From(referenceExpression, memberName)
        );
}
