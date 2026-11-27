namespace Creole;

/// <summary>
/// Replaces the layer's state and succeeds without consuming.
/// </summary>
/// <remarks>
/// The new state is held once, when the plan is built, so setting it allocates nothing.
/// </remarks>
/// <param name="held">The new state, as <see cref="StateBox.Hold{TState}"/> holds it.</param>
internal class SetStatePlan<TIn>(object held) : Plan<TIn, Unit>
{
    private readonly object _held = held;

    public override bool TryParse(ref Cursor<TIn> cursor, ref Unit result)
    {
        cursor.State = _held;
        return true;
    }
}
