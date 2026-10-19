using System.Diagnostics;

namespace Creole;

/// <summary>
/// Stands in for the plan of a node that is still being lowered, where a cycle in the grammar reaches that node again.
/// Lowering fills in <see cref="Target"/> when the node's own plan is built.
/// </summary>
/// <remarks>
/// This is the one plan node that changes after it is created.
/// It is filled once, during lowering, before the plan is published, so it is immutable by the time any parse can see it.
/// </remarks>
internal sealed class ForwardPlan<TIn, TOut> : Plan<TIn, TOut>
{
    private Plan<TIn, TOut>? _target;

    /// <summary>
    /// The plan this one forwards to.
    /// </summary>
    public Plan<TIn, TOut> Target
    {
        get =>
            _target ?? throw new InvalidOperationException("The plan was used before lowering finished.");
        set
        {
            Debug.Assert(_target is null, "A forward plan is filled once.");
            Debug.Assert(!ReferenceEquals(value, this), "A forward plan cannot forward to itself.");
            _target = value;
        }
    }

    public override bool TryParse(ref Cursor<TIn> cursor, ref TOut result) =>
        Target.TryParse(ref cursor, ref result);
}
