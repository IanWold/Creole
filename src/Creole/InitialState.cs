namespace Creole;

/// <summary>
/// The state a layer was given to start with, and the check that its steps keep a state of that type.
/// </summary>
/// <param name="Held">The state, as <see cref="StateBox"/> holds it.</param>
/// <param name="Type">The type the layer was given its state as.</param>
/// <param name="Holds">Whether a held state is of <paramref name="Type"/>, as <see cref="StateBox.Holds{TState}"/> decides.</param>
internal record InitialState(object Held, Type Type, Func<object, bool> Holds)
{
    /// <summary>
    /// Throws if a step of the layer named <paramref name="layer"/> left a state that is not of <see cref="Type"/>.
    /// </summary>
    /// <exception cref="GrammarException">The state is not of <see cref="Type"/>.</exception>
    public void Check(object? held, string layer)
    {
        if (held is not null && Holds(held))
        {
            return;
        }

        var type = Display.TypeName(Type);

        throw new GrammarException(
            held is null || ReferenceEquals(held, StateBox.Null)
                ? $"A step of the {layer} layer left its state null, but its initialState is of type {type}, which cannot be null."
                : $"A step of the {layer} layer left its state of type {Display.TypeName(held.GetType())}, but its initialState is of type {type}."
        );
    }

    /// <summary>
    /// The initial state <paramref name="state"/>, of type <typeparamref name="TState"/>.
    /// </summary>
    public static InitialState Of<TState>(TState state) =>
        new(StateBox.Hold(state), typeof(TState), StateBox.Holds<TState>);
}
