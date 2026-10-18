using System.Diagnostics.CodeAnalysis;
using Creole.Nodes;

namespace Creole;

/// <summary>
/// The base class for grammars over items of type <typeparamref name="TIn"/>.
/// </summary>
/// <typeparam name="TIn">The input item type that every combinator in the grammar reads.</typeparam>
/// <remarks>
/// <para>
/// C# cannot infer <typeparamref name="TIn"/> from a call such as <c>Return(1)</c>.
/// A grammar class derives from this class to fix the input type once, and its static combinators are then in scope unqualified.
/// </para>
/// <para>
/// Derive from <see cref="CharGrammar"/> for a character layer, or from <see cref="TokenGrammar{TToken, TKind}"/> for tokens with kinds.
/// Declare each rule once, in a <c>static readonly</c> field.
/// </para>
/// </remarks>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Static members on the generic base are the point: a derived grammar class calls them unqualified, with TIn fixed by the base.")]
public abstract partial class Grammar<TIn>
{
    /// <summary>
    /// Creates a grammar. Grammar classes hold static rules and are not usually instantiated.
    /// </summary>
    protected Grammar() { }

    /// <summary>
    /// A parser that succeeds without consuming input and produces <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="TOut">The type of the value produced.</typeparam>
    /// <param name="value">The value to produce.</param>
    public static Parser<TIn, TOut> Return<TOut>(TOut value) => new ReturnNode<TIn, TOut>(value);

    /// <summary>
    /// A parser that fails without consuming input, reporting <paramref name="message"/>.
    /// </summary>
    /// <typeparam name="TOut">
    /// The type of value the parser would produce. C# cannot infer it from the message, so pass it explicitly: <c>Fail&lt;int&gt;("unsupported version")</c>.
    /// </typeparam>
    /// <param name="message">The message reported when the parser fails.</param>
    /// <exception cref="GrammarException"><paramref name="message"/> is null.</exception>
    public static Parser<TIn, TOut> Fail<TOut>(string message) => new FailNode<TIn, TOut>(message);
}
