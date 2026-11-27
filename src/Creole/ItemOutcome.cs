namespace Creole;

/// <summary>
/// What one call of <see cref="PipelineItems{TOut}.Next"/> found.
/// </summary>
internal enum ItemOutcome
{
    /// <summary>
    /// An item's result: its value, or the failure that ends the run.
    /// </summary>
    Result,

    /// <summary>
    /// No item remains, because the input has ended.
    /// </summary>
    End,

    /// <summary>
    /// The item ran out of the input received so far. Every layer is back at the item's bookmark, and the next call runs the item again.
    /// </summary>
    Starved,
}
