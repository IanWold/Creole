namespace Creole;

/// <summary>
/// One entry of a layer's context stack: a <c>Context</c> that the parse entered.
/// </summary>
/// <param name="Text">The context's text, such as <c>in object</c>.</param>
/// <param name="Start">The position where the context's parser started.</param>
/// <param name="Parent">The frame that was innermost when this one was entered, or -1 for none.</param>
/// <param name="StartSpan">The span of the item at <paramref name="Start"/>, copied before a stream buffer released it, or null while the input still holds it.</param>
internal readonly record struct ContextFrame(string Text, long Start, int Parent, SourceSpan? StartSpan = null);
