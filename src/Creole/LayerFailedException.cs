namespace Creole;

/// <summary>
/// Ends a parse whose layer could not produce its next item, carrying that layer's error.
/// </summary>
/// <remarks>
/// A layer fails when a step's parser fails, or goes deeper than the depth limit.
/// The layer reading its items sees neither an item nor the end of the input, so no combinator catches this, and the parse ends with the error.
/// </remarks>
/// <param name="error">The error, named for the layer that failed, with spans in the original source.</param>
internal sealed class LayerFailedException(ParseError error) : Exception(error.ToString())
{
    /// <summary>
    /// The error, named for the layer that failed, with spans in the original source.
    /// </summary>
    public ParseError Error { get; } = error;
}
