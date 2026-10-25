namespace Creole;

/// <summary>
/// The message for a chained non-associative operator, such as <c>'&lt;' cannot be chained; add parentheses</c>, built only when the error is reported.
/// </summary>
/// <remarks>
/// It names the operator as an alternative that dispatch skips would be named: by the label of the outermost <c>Named</c> on the way to its leading primitive, or by that primitive's expectation.
/// An operator with neither is named by its first item.
/// </remarks>
internal sealed class ChainMessage(Expected? display)
{
    private readonly Expected? _display = display;

    /// <summary>
    /// The message for an operator with nothing to name it by but its first item.
    /// </summary>
    public static ChainMessage Unnamed { get; } = new(null);

    private string Operator<T>(bool hasItem, T item)
    {
        if (_display is null)
        {
            return hasItem
                ? Display<T>.Item(item)
                : "the operator";
        }

        return _display.Build() switch
        {
            Label label => label.Name,
            ExpectedItem expected => expected.Display,
            EndOfInput => "end of input",
        };
    }

    /// <summary>
    /// The message, naming the operator whose first item is <paramref name="item"/> when <paramref name="hasItem"/> is set.
    /// </summary>
    public string Text<T>(bool hasItem, T item) =>
        $"{Operator(hasItem, item)} cannot be chained; add parentheses";
}
