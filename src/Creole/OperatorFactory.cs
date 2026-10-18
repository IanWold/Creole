using System.Runtime.CompilerServices;
using Creole.Nodes;

namespace Creole;

/// <summary>
/// Creates the operators of an <c>Expression.Build</c> table. <c>Expression.Build</c> passes one to the table lambda.
/// </summary>
/// <typeparam name="TIn">The input item type of the grammar.</typeparam>
/// <typeparam name="T">The type every level of the expression produces, taken from the atom.</typeparam>
/// <remarks>
/// <para>
/// Each method takes the parser that matches the operator and a function that combines the operator's value with its operands.
/// The operator parser may produce any type, such as a token with its span, so the combine function can record where the operator was.
/// </para>
/// <para>
/// Because the factory fixes <typeparamref name="T"/>, the combine functions need no parameter types:
/// <c>op.InfixL(Char('+'), (l, _, r) =&gt; l + r)</c>.
/// </para>
/// </remarks>
public sealed class OperatorFactory<TIn, T>
{
    internal static readonly OperatorFactory<TIn, T> Instance = new();

    private OperatorFactory() { }

    /// <summary>
    /// A prefix operator, such as unary minus in <c>-x</c>.
    /// </summary>
    /// <typeparam name="TOp">The type the operator parser produces.</typeparam>
    /// <param name="op">The parser that matches the operator.</param>
    /// <param name="combine">Combines the operator's value and the operand.</param>
    /// <param name="opExpression">Filled in by the compiler with the expression passed as <paramref name="op"/>.</param>
    /// <param name="combineExpression">Filled in by the compiler with the expression passed as <paramref name="combine"/>.</param>
    /// <returns>The operator, to place in a level of the table.</returns>
    /// <exception cref="GrammarException"><paramref name="op"/> or <paramref name="combine"/> is null.</exception>
    /// <remarks>
    /// Prefix operators repeat: <c>- -x</c> applies the operator twice. The operand is parsed at this operator's level,
    /// so an operator from a looser level cannot be its operand without parentheses.
    /// </remarks>
    public ExpressionOperator<TIn, T> Prefix<TOp>(
        Parser<TIn, TOp> op,
        Func<TOp, T, T> combine,
        [CallerArgumentExpression(nameof(op))] string opExpression = "",
        [CallerArgumentExpression(nameof(combine))] string combineExpression = ""
    ) =>
        new PrefixOperator<TIn, TOp, T>(Guard.NotNull(op, opExpression), Guard.NotNull(combine, combineExpression));

    /// <summary>
    /// A postfix operator, such as factorial in <c>n!</c> or a call in <c>f()</c>.
    /// </summary>
    /// <typeparam name="TOp">The type the operator parser produces.</typeparam>
    /// <param name="op">The parser that matches the operator.</param>
    /// <param name="combine">Combines the operand and the operator's value.</param>
    /// <param name="opExpression">Filled in by the compiler with the expression passed as <paramref name="op"/>.</param>
    /// <param name="combineExpression">Filled in by the compiler with the expression passed as <paramref name="combine"/>.</param>
    /// <returns>The operator, to place in a level of the table.</returns>
    /// <exception cref="GrammarException"><paramref name="op"/> or <paramref name="combine"/> is null.</exception>
    /// <remarks>
    /// Postfix operators repeat: <c>f()()</c> applies the operator twice.
    /// </remarks>
    public ExpressionOperator<TIn, T> Postfix<TOp>(
        Parser<TIn, TOp> op,
        Func<T, TOp, T> combine,
        [CallerArgumentExpression(nameof(op))] string opExpression = "",
        [CallerArgumentExpression(nameof(combine))] string combineExpression = ""
    ) =>
        new PostfixOperator<TIn, TOp, T>(Guard.NotNull(op, opExpression), Guard.NotNull(combine, combineExpression));

    /// <summary>
    /// A left-associative infix operator: <c>a - b - c</c> is <c>(a - b) - c</c>.
    /// </summary>
    /// <typeparam name="TOp">The type the operator parser produces.</typeparam>
    /// <param name="op">The parser that matches the operator.</param>
    /// <param name="combine">Combines the left operand, the operator's value and the right operand.</param>
    /// <param name="opExpression">Filled in by the compiler with the expression passed as <paramref name="op"/>.</param>
    /// <param name="combineExpression">Filled in by the compiler with the expression passed as <paramref name="combine"/>.</param>
    /// <returns>The operator, to place in a level of the table.</returns>
    /// <exception cref="GrammarException"><paramref name="op"/> or <paramref name="combine"/> is null.</exception>
    public ExpressionOperator<TIn, T> InfixL<TOp>(
        Parser<TIn, TOp> op,
        Func<T, TOp, T, T> combine,
        [CallerArgumentExpression(nameof(op))] string opExpression = "",
        [CallerArgumentExpression(nameof(combine))] string combineExpression = ""
    ) =>
        Infix(Fixity.InfixL, op, combine, opExpression, combineExpression);

    /// <summary>
    /// A right-associative infix operator: <c>a ^ b ^ c</c> is <c>a ^ (b ^ c)</c>.
    /// </summary>
    /// <typeparam name="TOp">The type the operator parser produces.</typeparam>
    /// <param name="op">The parser that matches the operator.</param>
    /// <param name="combine">Combines the left operand, the operator's value and the right operand.</param>
    /// <param name="opExpression">Filled in by the compiler with the expression passed as <paramref name="op"/>.</param>
    /// <param name="combineExpression">Filled in by the compiler with the expression passed as <paramref name="combine"/>.</param>
    /// <returns>The operator, to place in a level of the table.</returns>
    /// <exception cref="GrammarException"><paramref name="op"/> or <paramref name="combine"/> is null.</exception>
    public ExpressionOperator<TIn, T> InfixR<TOp>(
        Parser<TIn, TOp> op,
        Func<T, TOp, T, T> combine,
        [CallerArgumentExpression(nameof(op))] string opExpression = "",
        [CallerArgumentExpression(nameof(combine))] string combineExpression = ""
    ) =>
        Infix(Fixity.InfixR, op, combine, opExpression, combineExpression);

    /// <summary>
    /// A non-associative infix operator: <c>a &lt; b</c> is allowed, but <c>a &lt; b &lt; c</c> is an error.
    /// </summary>
    /// <typeparam name="TOp">The type the operator parser produces.</typeparam>
    /// <param name="op">The parser that matches the operator.</param>
    /// <param name="combine">Combines the left operand, the operator's value and the right operand.</param>
    /// <param name="opExpression">Filled in by the compiler with the expression passed as <paramref name="op"/>.</param>
    /// <param name="combineExpression">Filled in by the compiler with the expression passed as <paramref name="combine"/>.</param>
    /// <returns>The operator, to place in a level of the table.</returns>
    /// <exception cref="GrammarException"><paramref name="op"/> or <paramref name="combine"/> is null.</exception>
    /// <remarks>
    /// A second operator from the same level directly after the right operand fails, committed, with a message saying the operator cannot be chained.
    /// Operators from other levels continue as usual: <c>a &lt; b + 1</c>.
    /// </remarks>
    public ExpressionOperator<TIn, T> InfixN<TOp>(
        Parser<TIn, TOp> op,
        Func<T, TOp, T, T> combine,
        [CallerArgumentExpression(nameof(op))] string opExpression = "",
        [CallerArgumentExpression(nameof(combine))] string combineExpression = ""
    ) =>
        Infix(Fixity.InfixN, op, combine, opExpression, combineExpression);

    private static InfixOperator<TIn, TOp, T> Infix<TOp>(Fixity fixity, Parser<TIn, TOp> op, Func<T, TOp, T, T> combine, string opExpression, string combineExpression) =>
        new(fixity, Guard.NotNull(op, opExpression), Guard.NotNull(combine, combineExpression));
}
