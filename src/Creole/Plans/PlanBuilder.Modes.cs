namespace Creole;

internal partial class PlanBuilder
{
    public static Plan<TIn, Unit> PopMode<TIn, TMode>(ModeChannel<TMode> channel) => new PopModePlan<TIn, TMode>(channel);

    public static Plan<TIn, Unit> PushMode<TIn, TMode>(ModeChannel<TMode> channel, TMode mode) => new PushModePlan<TIn, TMode>(channel, mode);

    public static Plan<TIn, Unit> SetMode<TIn, TMode>(ModeChannel<TMode> channel, TMode mode) => new SetModePlan<TIn, TMode>(channel, mode);
}
