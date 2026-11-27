using System.Diagnostics.CodeAnalysis;

namespace Creole;

/// <summary>
/// The item plans of at most <see cref="Capacity"/> modes, found by mode, which forgets the plan used longest ago to make room for a new one.
/// </summary>
/// <remarks>
/// Modes compare with <see cref="EqualityComparer{T}.Default"/>, and a null mode is a mode like any other.
/// Each plan is filed under the hash its mode had when it was kept, so a mode that changes afterwards is still forgotten in its turn and the count stays bounded.
/// It is not safe for use from several threads at once.
/// </remarks>
internal sealed class RecentPlans<TIn, TOut, TMode>
{
    /// <summary>
    /// A mode as a dictionary key, which may be null, with the hash the mode had when the key was made.
    /// </summary>
    /// <param name="mode">The mode.</param>
    private readonly struct Key(TMode mode) : IEquatable<Key>
    {
        private readonly int _hash = mode is null
            ? 0
            : EqualityComparer<TMode>.Default.GetHashCode(mode);

        private readonly TMode _mode = mode;

        public bool Equals(Key other) => _hash == other._hash && EqualityComparer<TMode>.Default.Equals(_mode, other._mode);

        public override bool Equals(object? obj) => obj is Key other && Equals(other);

        public override int GetHashCode() => _hash;
    }

    /// <summary>
    /// The most modes whose plans are kept.
    /// </summary>
    public const int Capacity = 64;

    private readonly Dictionary<Key, LinkedListNode<(Key Key, Plan<TIn, TOut> Plan)>> _byMode = [];
    private readonly LinkedList<(Key Key, Plan<TIn, TOut> Plan)> _byUse = new();

    /// <summary>
    /// The number of modes whose plans are kept.
    /// </summary>
    public int Count => _byMode.Count;

    /// <summary>
    /// Keeps <paramref name="plan"/> as the plan for <paramref name="mode"/>, which has none, and forgets the plan used longest ago if that makes more than <see cref="Capacity"/>.
    /// </summary>
    public void Add(TMode mode, Plan<TIn, TOut> plan)
    {
        var key = new Key(mode);

        _byMode.Add(key, _byUse.AddFirst((key, plan)));

        if (_byMode.Count > Capacity)
        {
            _byMode.Remove(_byUse.Last!.Value.Key);
            _byUse.RemoveLast();
        }
    }

    /// <summary>
    /// Finds the plan kept for <paramref name="mode"/>, and counts it as the plan used most recently.
    /// </summary>
    public bool TryGet(TMode mode, [MaybeNullWhen(false)] out Plan<TIn, TOut> plan)
    {
        if (!_byMode.TryGetValue(new Key(mode), out var node))
        {
            plan = null;

            return false;
        }

        _byUse.Remove(node);
        _byUse.AddFirst(node);
        plan = node.Value.Plan;

        return true;
    }
}
