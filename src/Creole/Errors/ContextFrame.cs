namespace Creole;

/// <summary>
/// One entry of a layer's context stack: a <c>Context</c> that the parse entered.
/// </summary>
/// <param name="Text">The context's text, such as <c>in object</c>.</param>
/// <param name="Start">The position where the context's parser started.</param>
/// <param name="Parent">The frame that was innermost when this one was entered, or -1 for none.</param>
internal readonly record struct ContextFrame(string Text, long Start, int Parent);
