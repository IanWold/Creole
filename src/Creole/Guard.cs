using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// Argument checks for combinators.
/// </summary>
internal static class Guard
{
    /// <summary>
    /// Returns <paramref name="parser"/>, or throws a <see cref="GrammarException"/> naming the argument
    /// expression and suggesting <c>Rec</c> if it is null. This is the run-time half of CRL0004: a null parser
    /// is usually a field read before its initializer has run.
    /// </summary>
    public static Parser<TIn, TOut> NotNull<TIn, TOut>([NotNull] Parser<TIn, TOut>? parser, [CallerArgumentExpression(nameof(parser))] string expression = "") =>
        parser ?? throw new GrammarException($"'{expression}' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => {expression}).");

    /// <summary>
    /// Returns <paramref name="value"/>, or throws a <see cref="GrammarException"/> naming the argument
    /// expression if it is null.
    /// </summary>
    public static T NotNull<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string expression = "") where T : class =>
        value ?? throw new GrammarException($"'{expression}' is null.");
}
