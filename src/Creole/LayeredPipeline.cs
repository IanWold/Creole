using System.Diagnostics.CodeAnalysis;

namespace Creole;

/// <summary>
/// Runs <see cref="Layers"/> over the input and <see cref="Parser"/> over their items. Backs <c>Then</c> with a parser.
/// </summary>
/// <remarks>
/// The parser reads a buffer of the last layer's items, so it can rewind over them without the layers running again.
/// </remarks>
internal sealed class LayeredPipeline<TIn, TItem, TOut>(Layer<TIn, TItem> layers, Parser<TItem, TOut> parser) : Pipeline<TIn, TOut>
{
    private const string ParserLayer = "parser";

    public Layer<TIn, TItem> Layers { get; } = layers;

    public Parser<TItem, TOut> Parser { get; } = parser;

    internal override bool TryParse(ICursorSource<TIn> root, ParseOptions options, bool recordFailures, out TOut value, [NotNullWhen(false)] out ParseError? error)
    {
        var plan = PlanCache.Get(Parser);

        using var items = new StreamBuffer<TItem>(Layers.Open(root, options, recordFailures));

        var log = recordFailures
            ? new FailureLog<TItem>()
            : null;

        var cursor = new Cursor<TItem>(items) { Failures = log, MaxDepth = options.MaxDepth };
        value = default!;
        error = null;

        try
        {
            if (plan.TryParse(ref cursor, ref value))
            {
                return true;
            }

            error = ParseErrors.Failure(in cursor, log, cursor.Position, ParserLayer, items.SpanOf);
        }
        catch (LayerFailedException exception)
        {
            error = exception.Error;
        }
        catch (DepthExceededException exception)
        {
            error = ParseErrors.Depth(exception, in cursor, log, ParserLayer, items.SpanOf);
        }

        return false;
    }
}
