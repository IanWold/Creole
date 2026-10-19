using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// Argument checks for combinators.
/// </summary>
internal static class Guard
{
    /// <summary>
    /// Returns <paramref name="parser"/>, or throws a <see cref="GrammarException"/> naming the argument expression and suggesting <c>Rec</c> if it is null.
    /// This is the run-time half of CRL0004: a null parser is usually a field read before its initializer has run.
    /// </summary>
    public static Parser<TIn, TOut> NotNull<TIn, TOut>([NotNull] Parser<TIn, TOut>? parser, [CallerArgumentExpression(nameof(parser))] string expression = "") =>
        parser ?? throw new GrammarException($"'{expression}' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => {expression}).");

    /// <summary>
    /// Returns <paramref name="branch"/>, or throws a <see cref="GrammarException"/> naming the case it belongs to and suggesting <c>Rec</c> if it is null.
    /// Use it for a parser inside a case, whose own expression the compiler cannot capture.
    /// </summary>
    /// <param name="branch">The case's parser.</param>
    /// <param name="caseDescription">How the message names the case, such as <c>'(2, RecordV2)'</c> with its quotes, or <c>case 3</c>.</param>
    public static Parser<TIn, TOut> BranchNotNull<TIn, TOut>([NotNull] Parser<TIn, TOut>? branch, string caseDescription) =>
        branch ?? throw new GrammarException($"The branch in {caseDescription} is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => ...).");

    /// <summary>
    /// Returns <paramref name="value"/>, or throws a <see cref="GrammarException"/> naming the argument expression if it is null.
    /// </summary>
    public static T NotNull<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string expression = "") where T : class =>
        value ?? throw new GrammarException($"'{expression}' is null.");

    /// <summary>
    /// Returns <paramref name="value"/>, or throws a <see cref="GrammarException"/> naming the argument expression if it is null or empty.
    /// </summary>
    public static string NotNullOrEmpty([NotNull] string? value, [CallerArgumentExpression(nameof(value))] string expression = "") =>
        NotNull(value, expression) is { Length: > 0 } text
        ? text
        : throw new GrammarException($"'{expression}' is empty.");
}
