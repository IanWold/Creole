namespace Creole;

/// <summary>
/// One error from a parse: where it happened, which layer raised it, what was found and what was expected.
/// </summary>
/// <param name="Span">Where the error is, in the original source.</param>
/// <param name="Layer">The name of the layer that raised the error, such as <c>lexer</c> or <c>parser</c>.</param>
/// <param name="Unexpected">What the parse found at <paramref name="Span"/>.</param>
/// <param name="Expected">What the parse would have accepted there, in the order it was recorded. May be empty.</param>
/// <param name="Context">The context lines for the error, innermost first, such as <c>in object starting at 2:3</c>. May be empty.</param>
/// <remarks>
/// Two errors are equal when their span, layer and unexpected value are equal and their <paramref name="Expected"/> and <paramref name="Context"/> lists hold equal elements in the same order.
/// </remarks>
public sealed record ParseError(
    SourceSpan Span,
    string Layer,
    Unexpected Unexpected,
    IReadOnlyList<Expectation> Expected,
    IReadOnlyList<string> Context
)
{
    /// <inheritdoc/>
    public bool Equals(ParseError? other) =>
        other is not null
        && Span == other.Span
        && Layer == other.Layer
        && Equals(Unexpected.Value, other.Unexpected.Value)
        && Expected.Select(e => e.Value).SequenceEqual(other.Expected.Select(e => e.Value))
        && Context.SequenceEqual(other.Context);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(Span);
        hash.Add(Layer);
        hash.Add(Unexpected.Value);

        foreach (var expectation in Expected)
        {
            hash.Add(expectation.Value);
        }

        foreach (var line in Context)
        {
            hash.Add(line);
        }

        return hash.ToHashCode();
    }
}
