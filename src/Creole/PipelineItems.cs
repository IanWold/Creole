namespace Creole;

/// <summary>
/// One run of a pipeline's parser over its input, one item at a time, as <c>ParseMany</c> and <c>ParseManyAsync</c> drive it.
/// </summary>
/// <typeparam name="TOut">The type of the parser's value.</typeparam>
/// <remarks>
/// <para>
/// Each item starts with no furthest failure in any layer, and with the channels the parser writes at their initial modes.
/// Every layer records failures as it goes.
/// </para>
/// <para>
/// A run that restarts takes a bookmark of every layer at each item's start, and holds the item's start as the parser's floor, so that every buffer keeps what the item needs to run again.
/// When the item starves, every layer goes back to the bookmark before <see cref="Next"/> returns.
/// A run that does not restart holds no more than its parser can still go back to, and its input must never starve.
/// </para>
/// </remarks>
internal abstract class PipelineItems<TOut> : IDisposable
{
    /// <summary>
    /// Returns every buffer of the run to its pool.
    /// </summary>
    public abstract void Dispose();

    /// <summary>
    /// Runs the parser over the next item, or over the same item again if the last call found it starved.
    /// </summary>
    /// <param name="result">The item's result, when this returns <see cref="ItemOutcome.Result"/>.</param>
    /// <returns>Whether it found an item's result, the end of the input, or a starved item.</returns>
    /// <exception cref="GrammarException">The parser succeeded without consuming input, or a grammar is not valid.</exception>
    public abstract ItemOutcome Next(out ParseResult<TOut> result);
}
