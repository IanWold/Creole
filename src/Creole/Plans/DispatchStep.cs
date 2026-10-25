namespace Creole;

/// <summary>
/// One step of a dispatched <c>Or</c> for one key: an alternative to try, or what the alternatives skipped before the next one would have expected.
/// </summary>
/// <param name="Alternative">The alternative to try, or null for a step that records.</param>
/// <param name="Skipped">What the skipped alternatives expect, in their order, or null for a step that tries an alternative.</param>
internal readonly record struct DispatchStep<TIn, T>(Plan<TIn, T>? Alternative, Expected[]? Skipped);
