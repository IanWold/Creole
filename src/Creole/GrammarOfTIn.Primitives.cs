using System.Runtime.CompilerServices;

namespace Creole;

public abstract partial class Grammar<TIn>
{
    /// <summary>
    /// A parser that consumes any one item and produces it.
    /// </summary>
    /// <remarks>
    /// It fails without consuming input only at the end of input.
    /// Every use refers to the same parser.
    /// </remarks>
    public static Parser<TIn, TIn> Any { get; } = new AnyNode<TIn>();

    /// <summary>
    /// A parser that succeeds without consuming input at the end of input, and fails without consuming input anywhere else.
    /// </summary>
    /// <remarks>
    /// Put it last in a top-level rule to require that the rule reads the whole input.
    /// Every use refers to the same parser.
    /// </remarks>
    public static Parser<TIn, Unit> End { get; } = new EndNode<TIn>();

    /// <summary>
    /// A parser that runs a hand-written scanner, for tokens no combinator can describe.
    /// </summary>
    /// <typeparam name="TOut">The type of value the scanner produces.</typeparam>
    /// <param name="scanner">The scanner. Its <see cref="IScanner{TIn, TOut}.Info"/> is read once, now.</param>
    /// <param name="scannerExpression">Filled in by the compiler with the expression passed as <paramref name="scanner"/>.</param>
    /// <returns>A parser that succeeds where the scanner does, and fails without consuming input where it fails.</returns>
    /// <exception cref="GrammarException">
    /// <paramref name="scanner"/> or its <c>Info</c> is null;
    /// <c>Info.First</c> is given but <typeparamref name="TIn"/> is not <see cref="char"/>;
    /// or <c>Info.Name</c> is empty or white space.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Analysis trusts the scanner's declaration. Without a declared <c>First</c>, analysis treats the parser as matching any item, as it does <see cref="Satisfy"/>.
    /// </para>
    /// <para>
    /// When it fails, the parser expects the scanner's <c>Name</c>, or the characters of its <c>First</c> if it has no name.
    /// </para>
    /// </remarks>
    public static Parser<TIn, TOut> Custom<TOut>(IScanner<TIn, TOut> scanner, [CallerArgumentExpression(nameof(scanner))] string scannerExpression = "") =>
        new CustomNode<TIn, TOut>(scanner, scannerExpression);

    /// <summary>
    /// A parser that consumes one item for which <paramref name="predicate"/> returns <see langword="true"/>, and produces it.
    /// </summary>
    /// <param name="predicate">Decides whether an item matches. It must not have side effects, because an engine may call it more than once for the same item.</param>
    /// <returns>A parser that fails without consuming input on an item the predicate rejects, or at the end of input.</returns>
    /// <exception cref="GrammarException"><paramref name="predicate"/> is null.</exception>
    /// <remarks>
    /// The predicate is code, so analysis cannot tell which items it accepts and treats it as matching any item.
    /// Prefer <see cref="Token"/>, or a character class or <c>Kind</c> where one fits, so that analysis stays precise.
    /// </remarks>
    public static Parser<TIn, TIn> Satisfy(Func<TIn, bool> predicate) => new SatisfyNode<TIn>(predicate);

    /// <summary>
    /// A parser that consumes one item equal to <paramref name="token"/> and produces the item read.
    /// </summary>
    /// <param name="token">The item to match, compared with <see cref="EqualityComparer{T}.Default"/>.</param>
    /// <returns>A parser that fails without consuming input on any other item, or at the end of input.</returns>
    public static Parser<TIn, TIn> Token(TIn token) => new TokenNode<TIn>(token);
}
