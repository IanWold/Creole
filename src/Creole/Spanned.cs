namespace Creole;

/// <summary>
/// A parser's value with the source span of the input it consumed, as <c>p.Spanned()</c> produces it.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <remarks>
/// The span runs from the start of the first item the parser consumed to the end of the last, as a layer gives a derived item's span.
/// A parser that consumed nothing gets a zero-width span where the next item starts, or at the end of the last item at the end of the input.
/// </remarks>
/// <param name="Value">The value the parser produced.</param>
/// <param name="Span">The span of the input the parser consumed.</param>
public readonly record struct Spanned<T>(T Value, SourceSpan Span);
