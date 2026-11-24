namespace Creole;

public abstract partial class Grammar<TIn>
{
    /// <summary>
    /// A parser that succeeds without consuming input and produces a zero-width <see cref="SourceSpan"/> at the current position.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The span is where the next item starts, so over a layer's items it lies after any input the layer skipped, such as whitespace.
    /// At the end of the input it is at the end of the last item, and in an empty input at offset 0.
    /// Over input held whole in memory, offsets are item positions.
    /// </para>
    /// <para>
    /// It reads the next item to find where it starts, as a lookahead would.
    /// </para>
    /// <para>
    /// Every use refers to the same parser.
    /// </para>
    /// </remarks>
    public static Parser<TIn, SourceSpan> Position { get; } = new PositionNode<TIn>();
}
