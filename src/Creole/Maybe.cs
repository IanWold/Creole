using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// <c>Optional</c>, called as a method on a parser: <c>sign.Optional()</c>.
/// </summary>
/// <remarks>
/// This is an extension member rather than an instance method of <see cref="Parser{TIn, TOut}"/>.
/// An instance method that builds a <c>Parser&lt;TIn, Maybe&lt;TOut&gt;&gt;</c> from a <c>Parser&lt;TIn, TOut&gt;</c>
/// makes Native AOT expand the parser type without end (IL3054).
/// </remarks>
public static class Maybe
{
    extension<TIn, TOut>(Parser<TIn, TOut> parser)
    {
        /// <summary>
        /// A parser that runs this parser if it can, and produces its result or no value.
        /// </summary>
        /// <param name="parserExpression">Filled in by the compiler with the expression this method is called on.</param>
        /// <returns>A parser that produces an <see cref="Maybe{T}"/> holding this parser's result, or <see cref="Nothing"/>.</returns>
        /// <exception cref="GrammarException">This parser is null.</exception>
        /// <remarks>
        /// If this parser fails without consuming input, the optional parser succeeds without consuming input and produces <see cref="Nothing"/>.
        /// If this parser fails after consuming input, the optional parser fails too.
        /// </remarks>
        public Parser<TIn, Maybe<TOut>> Optional([CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
            new OptionalNode<TIn, TOut>(Guard.NotNull(parser, parserExpression));
    }
}

/// <summary>
/// A value that may be absent: either a <typeparamref name="T"/> or <see cref="Nothing"/>. Produced by <c>Optional</c>.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Maybe{T}"/> is a C# 15 union of <typeparamref name="T"/> and <see cref="Nothing"/>, so a switch must handle both cases:
/// <c>sign switch { char c =&gt; c, Nothing =&gt; '+' }</c>.
/// It converts implicitly from either case: <c>Maybe&lt;int&gt; m = 3;</c>, <c>Maybe&lt;int&gt; m = new Nothing();</c>.
/// </para>
/// <para>
/// It is a struct that stores its case without boxing, so creating and matching one does not allocate (ADR-0007).
/// <c>default(Maybe&lt;T&gt;)</c> is <see cref="Nothing"/>.
/// Two maybes are equal if both are <see cref="Nothing"/>, or both hold equal values.
/// </para>
/// <para>
/// When <typeparamref name="T"/> is nullable, or a type parameter in generic code, a switch over <c>T x</c> and <see cref="Nothing"/> warns that null is not covered (CS8655).
/// Add a <c>null</c> arm to satisfy it; a maybe that holds null still matches the <typeparamref name="T"/> arm.
/// </para>
/// </remarks>
[Union]
public readonly record struct Maybe<T> : IUnion
{
    private readonly bool _isJust;
    private readonly T _value;

    /// <summary>
    /// Creates a maybe that holds <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value to hold. It may be null.</param>
    public Maybe(T value)
    {
        _value = value;
        _isJust = true;
    }

    /// <summary>
    /// Creates a maybe that holds no value. The same as <c>default(Maybe&lt;T&gt;)</c>.
    /// </summary>
    /// <param name="nothing">The empty case.</param>
    public Maybe(Nothing nothing) => _value = default!;

    /// <summary>
    /// The case this maybe holds, boxed: the value, or a <see cref="Nothing"/>.
    /// </summary>
    /// <remarks>
    /// The C# 15 union pattern requires this property. Pattern matching does not use it and does not box; match on the union instead of reading it.
    /// It is null if this maybe holds a null value.
    /// </remarks>
    public object? Value => _isJust ? _value : Nothing.Boxed;

    /// <summary>
    /// Returns the value this maybe holds, or <c>default(T)</c> if it is <see cref="Nothing"/>.
    /// </summary>
    /// <returns>The value, or <c>default(T)</c>.</returns>
    public T? GetValueOrDefault() => _value;

    /// <summary>
    /// Returns the value this maybe holds, or <paramref name="defaultValue"/> if it is <see cref="Nothing"/>.
    /// </summary>
    /// <param name="defaultValue">The value to return if this maybe is <see cref="Nothing"/>.</param>
    /// <returns>The value, or <paramref name="defaultValue"/>.</returns>
    public T GetValueOrDefault(T defaultValue) => _isJust ? _value : defaultValue;

    /// <summary>
    /// Returns <c>Just(value)</c> or <c>Nothing</c>.
    /// </summary>
    /// <returns>A description of this maybe for debugging.</returns>
    public override string ToString() => _isJust ? $"Just({_value})" : "Nothing";

    /// <summary>
    /// Gets the value this maybe holds, if it holds one. Pattern matching calls this for a <typeparamref name="T"/> arm; match on the union instead of calling it.
    /// </summary>
    /// <param name="value">The value, or <c>default(T)</c> if this maybe is <see cref="Nothing"/>.</param>
    /// <returns>Whether this maybe holds a value.</returns>
    /// <remarks>
    /// It is public because the C# 15 union pattern matches without boxing only through public <c>TryGetValue</c> methods. It is hidden from IntelliSense.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = _value;
        return _isJust;
    }

    /// <summary>
    /// Gets the <see cref="Nothing"/> case, if this maybe is <see cref="Nothing"/>. Pattern matching calls this for a <c>Nothing</c> arm; match on the union instead of calling it.
    /// </summary>
    /// <param name="value">The empty case.</param>
    /// <returns>Whether this maybe is <see cref="Nothing"/>.</returns>
    /// <remarks>
    /// It is public because the C# 15 union pattern matches without boxing only through public <c>TryGetValue</c> methods. It is hidden from IntelliSense.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryGetValue(out Nothing value)
    {
        value = default;
        return !_isJust;
    }
}
