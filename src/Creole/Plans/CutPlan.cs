namespace Creole;

/// <summary>
/// Puts a cut in force and succeeds without consuming.
/// </summary>
internal sealed class CutPlan<TIn> : Plan<TIn, Unit>
{
    public override bool TryParse(ref Cursor<TIn> cursor, ref Unit result)
    {
        cursor.Cut = true;
        return true;
    }
}
