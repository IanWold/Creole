using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Creole.Nodes;

namespace Creole;

public abstract partial class Grammar<TIn>
{
    /// <summary>
    /// Operator-precedence expressions, built from an atom and a table of operators.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The class is nested in <see cref="Grammar{TIn}"/> so that <typeparamref name="TIn"/> is fixed and a grammar
    /// class calls <c>Expression.Build</c> unqualified. Inside a grammar class it takes precedence over
    /// <see cref="System.Linq.Expressions.Expression"/>; the generic <c>Expression&lt;TDelegate&gt;</c> still resolves.
    /// </para>
    /// <para>
    /// The design and its reasons are in ADR-0004 and the whitepaper's § Expressions.
    /// </para>
    /// </remarks>
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Nesting fixes TIn from the grammar's base class, so grammar classes call Expression.Build unqualified (ADR-0004).")]
    public static class Expression
    {
        /// <summary>
        /// A parser for expressions made of <paramref name="atom"/> and the operators in <paramref name="table"/>.
        /// </summary>
        /// <typeparam name="T">The type the atom and every level of the expression produce.</typeparam>
        /// <param name="atom">The operand parser, such as a number, a name or a parenthesized expression.</param>
        /// <param name="table">
        /// Returns the operator table: a list of levels, tightest first, each a list of operators made with the
        /// <see cref="OperatorFactory{TIn, T}"/> it receives. It runs once, inside this method.
        /// </param>
        /// <param name="atomExpression">Filled in by the compiler with the expression passed as <paramref name="atom"/>.</param>
        /// <param name="tableExpression">Filled in by the compiler with the expression passed as <paramref name="table"/>.</param>
        /// <returns>A single expression node holding the atom and a copy of the table.</returns>
        /// <exception cref="GrammarException">
        /// An argument, the table, a level or an operator is null; the table or a level is empty;
        /// or a level mixes infix operators of different associativity (InfixL, InfixR, InfixN).
        /// </exception>
        /// <remarks>
        /// Prefix operators are tried where an operand is expected; postfix and infix operators are tried after one, tighter levels first.
        /// Prefix and postfix operators repeat. An operator that consumed input commits, so a  missing operand after it is an error.
        /// The full rules are in the whitepaper's § Expressions.
        /// </remarks>
        public static Parser<TIn, T> Build<T>(
            Parser<TIn, T> atom,
            Func<OperatorFactory<TIn, T>, IReadOnlyList<IReadOnlyList<ExpressionOperator<TIn, T>>>> table,
            [CallerArgumentExpression(nameof(atom))] string atomExpression = "",
            [CallerArgumentExpression(nameof(table))] string tableExpression = ""
        )
        {
            Guard.NotNull(atom, atomExpression);
            Guard.NotNull(table, tableExpression);

            return new ExpressionNode<TIn, T>(atom, ExpressionNode<TIn, T>.CheckTable(table(OperatorFactory<TIn, T>.Instance)));
        }
    }
}
