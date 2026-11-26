namespace Creole;

/// <summary>
/// How a layer's state is held on the cursor: as one reference, which marks save and restore.
/// </summary>
/// <remarks>
/// A null reference means the layer has no state.
/// A state set to null is held as <see cref="Null"/>, so it stays apart from no state.
/// A value-type state is boxed.
/// </remarks>
internal static class StateBox
{
    /// <summary>
    /// Stands for a state set to null.
    /// </summary>
    public static readonly object Null = new();

    /// <summary>
    /// The reference that holds <paramref name="state"/>.
    /// </summary>
    public static object Hold<TState>(TState state) =>
        (object?)state ?? Null;

    /// <summary>
    /// Whether <paramref name="held"/> holds a <typeparamref name="TState"/>. A state set to null counts when <typeparamref name="TState"/> admits null.
    /// </summary>
    public static bool Holds<TState>(object held) =>
        ReferenceEquals(held, Null)
            ? default(TState) is null
            : held is TState;

    /// <summary>
    /// The state <paramref name="held"/> holds, as a <typeparamref name="TState"/>.
    /// </summary>
    /// <exception cref="LayerStateException"><paramref name="held"/> is null, for no state, or does not hold a <typeparamref name="TState"/>.</exception>
    public static TState Read<TState>(object? held)
    {
        if (held is not null && Holds<TState>(held))
        {
            return ReferenceEquals(held, Null)
                ? default!
                : (TState)held;
        }

        throw new LayerStateException(typeof(TState), held);
    }
}
