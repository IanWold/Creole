namespace Creole;

/// <summary>
/// Thrown where a grammar reads its layer's state as a type the state is not, or reads a state the layer does not have.
/// </summary>
/// <remarks>
/// The plan that reads the state does not know its layer's name, so the code that runs the layer catches this and throws <see cref="ToGrammarException"/> with the name.
/// </remarks>
/// <param name="expected">The type the grammar read the state as.</param>
/// <param name="held">What the cursor held: null for no state, <see cref="StateBox.Null"/> for a state set to null, or the state.</param>
internal sealed class LayerStateException(Type expected, object? held) : Exception
{
    private readonly Type _expected = expected;
    private readonly object? _held = held;

    /// <summary>
    /// The <see cref="GrammarException"/> to throw in place of this one, naming <paramref name="layer"/>.
    /// </summary>
    public GrammarException ToGrammarException(string layer)
    {
        var expected = Display.TypeName(_expected);

        var message = _held switch
        {
            null => $"The {layer} layer has no state, but its grammar reads it as {expected}. Give the layer an initialState, or set the state with SetState before reading it.",
            _ when ReferenceEquals(_held, StateBox.Null) => $"The {layer} layer's state is null, but its grammar reads it as {expected}, which cannot be null.",
            _ => $"The {layer} layer's state is of type {Display.TypeName(_held.GetType())}, but its grammar reads it as {expected}.",
        };

        return new GrammarException(message, this);
    }
}
