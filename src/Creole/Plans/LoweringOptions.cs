namespace Creole;

/// <summary>
/// The rewrites <see cref="PlanBuilder"/> applies while lowering.
/// </summary>
/// <remarks>
/// Every rewrite keeps the outcome, value, position and cut flag of the plan it replaces.
/// <see cref="PlanCache"/> applies all of them; tests and benchmarks turn one off to compare the plans with and without it.
/// </remarks>
[Flags]
internal enum LoweringOptions
{
    /// <summary>
    /// No rewrites: every node lowers to its own plan.
    /// </summary>
    None = 0,

    /// <summary>
    /// A skipped run of a character class becomes one vectorized search per segment (<see cref="ScanPlan"/>).
    /// </summary>
    Scan = 1,

    /// <summary>
    /// A <c>Select</c> over a <c>Select</c> becomes one plan that applies both functions (<see cref="Plan{TIn, TOut}.Select{TResult}"/>).
    /// </summary>
    CollapseSelect = 2,

    /// <summary>
    /// Every rewrite.
    /// </summary>
    All = Scan | CollapseSelect,
}
