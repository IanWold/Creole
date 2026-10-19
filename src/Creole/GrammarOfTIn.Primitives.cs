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
