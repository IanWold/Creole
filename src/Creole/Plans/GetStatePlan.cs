namespace Creole;

/// <summary>
/// Produces the layer's state and succeeds without consuming.
/// </summary>
/// <remarks>
/// It throws <see cref="LayerStateException"/> when the layer has no state or its state is not a <typeparamref name="TState"/>.
/// </remarks>
internal class GetStatePlan<TIn, TState> : Plan<TIn, TState>
{
    public override bool TryParse(ref Cursor<TIn> cursor, ref TState result)
    {
        result = StateBox.Read<TState>(cursor.State);
        return true;
    }
}
