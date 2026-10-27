namespace Creole;

/// <summary>
/// What a <see cref="StreamBuffer{T}"/> keeps beside each item: where it came from, and what is needed to produce it again.
/// </summary>
/// <remarks>
/// <para>
/// Running the producing step again from <paramref name="StepStart"/> with <paramref name="StateBefore"/>, and skipping its first <paramref name="GroupIndex"/> outputs, produces the item again.
/// </para>
/// <para>
/// This is the slot as an upstream adds it and as the buffer reads it back. The buffer stores the span packed, in two 32-bit numbers where it fits.
/// </para>
/// </remarks>
/// <param name="Span">The item's span in the original source.</param>
/// <param name="StepStart">The upstream position where the step that produced the item began.</param>
/// <param name="StateBefore">The producing layer's state before that step, or null for a stateless layer.</param>
/// <param name="GroupIndex">The item's index among its step's outputs: 0 unless the step produced several.</param>
internal readonly record struct StreamSlot(SourceSpan Span, long StepStart, object? StateBefore, int GroupIndex);
