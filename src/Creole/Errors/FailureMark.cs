namespace Creole;

/// <summary>
/// A layer's furthest failure as it was at one moment, so a combinator can put it back or relabel what was recorded after it.
/// </summary>
/// <param name="Position">The furthest failure's position, or -1 if none had been recorded.</param>
/// <param name="Start">Where its expectations began in the log's array.</param>
/// <param name="Count">Where they ended.</param>
/// <param name="Message">Its message, if any.</param>
/// <param name="Context">The context frame of its first failure, or -1 for none.</param>
/// <param name="Records">How many failures the log had recorded, which tells <c>Named</c> whether its parser recorded one.</param>
/// <param name="Protected">How much of the log's array was protected for an enclosing combinator that may put it back.</param>
internal readonly record struct FailureMark(long Position, int Start, int Count, string? Message, int Context, int Records, int Protected);
