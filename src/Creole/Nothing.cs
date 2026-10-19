using System.Diagnostics.CodeAnalysis;

namespace Creole;

/// <summary>
/// The empty case of <see cref="Maybe{T}"/>: no value.
/// </summary>
/// <remarks>
/// It carries no data, so all instances are equal.
/// Match it in a switch over a maybe (<c>Nothing =&gt; ...</c>), or convert it to one: <c>Maybe&lt;int&gt; m = new Nothing();</c>.
/// </remarks>
[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Nothing pairs with Maybe as in Haskell (ADR-0007). Only Visual Basic reserves it, and VB code can write [Nothing].")]
public readonly record struct Nothing
{
    /// <summary>
    /// A boxed instance, so <see cref="Maybe{T}.Value"/> does not allocate for the empty case.
    /// </summary>
    internal static readonly object Boxed = default(Nothing);
}
