using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Creole;

file enum CandidateKind : byte
{
    Live,
    Dead,
    Matched,
}

file enum EntryKind : byte
{
    First,
    Second,
    Atomic,
}

/// <summary>
/// The paths a configuration holds for one alternative, in priority order.
/// </summary>
file readonly record struct AlternativePaths(int Alternative, List<Candidate> Candidates);

/// <summary>
/// A path through one alternative.
/// </summary>
/// <remarks>
/// Its scope lists, oldest first, the unresolved choices it is inside, in the first or the second branch, and the <c>Atomic</c>s it has entered and not left.
/// A dead path and a match keep only their second-branch entries, through which a higher path can still drop them.
/// </remarks>
file sealed class Candidate
{
    public bool Committed { get; set; }

    public bool Fresh { get; set; }

    public CandidateKind Kind { get; set; }

    public Continuation? Rest { get; set; }

    public required List<Entry> Scope { get; init; }

    private List<Entry> SecondBranches()
    {
        var second = new List<Entry>();

        foreach (var entry in Scope)
        {
            if (entry.Kind == EntryKind.Second)
            {
                second.Add(entry);
            }
        }

        return second;
    }

    public Candidate Advanced() => new()
    {
        Committed = Committed,
        Rest = Rest!.Tail,
        Scope = [.. Scope]
    };

    public Candidate Copy() => new()
    {
        Committed = Committed,
        Kind = Kind,
        Rest = Rest,
        Scope = [.. Scope]
    };

    public Candidate Dead() => new()
    {
        Committed = Committed,
        Kind = CandidateKind.Dead,
        Scope = SecondBranches()
    };

    public Candidate Match() => new()
    {
        Fresh = true,
        Kind = CandidateKind.Matched,
        Scope = SecondBranches()
    };
}

/// <summary>
/// Compares paths by kind, commitment, scope and continuation, so that duplicate paths in one alternative can be dropped.
/// </summary>
/// <remarks>
/// A <see cref="Run"/> frame is compared by the identity of its term.
/// </remarks>
file sealed class CandidateComparer : IEqualityComparer<Candidate>
{
    private static int Hash(Frame frame) => frame switch
    {
        Run run => RuntimeHelpers.GetHashCode(run.Term),
        EndChoice end => HashCode.Combine(1, end.Id),
        EndAtomic end => HashCode.Combine(2, end.Id),
        _ => throw new UnreachableException(),
    };

    private static bool Same(Frame left, Frame right) => (left, right) switch
    {
        (Run first, Run second) => ReferenceEquals(first.Term, second.Term),
        (EndChoice first, EndChoice second) => first.Id == second.Id,
        (EndAtomic first, EndAtomic second) => first.Id == second.Id,
        _ => false,
    };

    public bool Equals(Candidate? left, Candidate? right)
    {
        if (left is null || right is null)
        {
            return ReferenceEquals(left, right);
        }

        if (left.Kind != right.Kind
            || left.Committed != right.Committed
            || !CollectionsMarshal.AsSpan(left.Scope).SequenceEqual(CollectionsMarshal.AsSpan(right.Scope))
        )
        {
            return false;
        }

        var leftRest = left.Rest;
        var rightRest = right.Rest;

        while (!ReferenceEquals(leftRest, rightRest))
        {
            if (leftRest is null || rightRest is null || !Same(leftRest.Head, rightRest.Head))
            {
                return false;
            }

            leftRest = leftRest.Tail;
            rightRest = rightRest.Tail;
        }

        return true;
    }

    public int GetHashCode(Candidate candidate)
    {
        var hash = new HashCode();

        hash.Add(candidate.Kind);
        hash.Add(candidate.Committed);

        foreach (var entry in candidate.Scope)
        {
            hash.Add(entry);
        }

        for (var rest = candidate.Rest; rest is not null; rest = rest.Tail)
        {
            hash.Add(Hash(rest.Head));
        }

        return hash.ToHashCode();
    }
}

file sealed record Continuation(Frame Head, Continuation? Tail);

file readonly record struct Entry(EntryKind Kind, int Id, bool Consumed);

file abstract record Frame;

file sealed record EndAtomic(int Id) : Frame;

file sealed record EndChoice(int Id) : Frame;

file sealed record Run(RegularTerm Term) : Frame;

/// <summary>
/// Compares state keys, and lets a key held in a reused buffer be looked up without copying it.
/// </summary>
file sealed class KeyComparer : IEqualityComparer<int[]>, IAlternateEqualityComparer<ReadOnlySpan<int>, int[]>
{
    public int[] Create(ReadOnlySpan<int> alternate) =>
        alternate.ToArray();

    public bool Equals(int[]? left, int[]? right) =>
        left is null || right is null
            ? ReferenceEquals(left, right)
            : left.AsSpan().SequenceEqual(right);

    public bool Equals(ReadOnlySpan<int> alternate, int[] other) =>
        alternate.SequenceEqual(other);

    public int GetHashCode(int[] key) =>
        GetHashCode(key.AsSpan());

    public int GetHashCode(ReadOnlySpan<int> alternate)
    {
        var hash = new HashCode();

        hash.AddBytes(MemoryMarshal.AsBytes(alternate));

        return hash.ToHashCode();
    }
}

/// <summary>
/// Builds a <see cref="CommitAutomaton"/> by exploring every configuration of candidates reachable from the start, one character class at a time.
/// </summary>
/// <remarks>
/// A configuration holds only the alternatives that still have paths, so an alternative costs nothing once it can no longer match.
/// Configurations are compared by a key written after renaming their choice and <c>Atomic</c> ids in order of first appearance, so equivalent ones become one state.
/// A configuration is copied for storage only when it is a new state.
/// </remarks>
file sealed class Determinizer(IReadOnlyList<RegularTerm> alternatives, int maxCandidates)
{
    private readonly RegularTerm[] _alternatives = [.. alternatives];
    private readonly ushort[] _classOf = new ushort[char.MaxValue + 1];
    private readonly List<AlternativePaths[]> _configurations = [];
    private readonly HashSet<int> _firstIds = [];
    private readonly List<int> _fresh = [];
    private readonly Dictionary<int[], int>.AlternateLookup<ReadOnlySpan<int>> _ids = new Dictionary<int[], int>(new KeyComparer()).GetAlternateLookup<ReadOnlySpan<int>>();
    private readonly List<int> _key = [];
    private readonly int _maxCandidates = maxCandidates;
    private readonly Dictionary<int, int> _renamed = [];
    private readonly List<char> _representatives = [];
    private readonly List<int> _resolving = [];
    private readonly HashSet<Candidate> _seen = new(new CandidateComparer());
    private readonly List<AlternativePaths> _stepped = [];
    private readonly Dictionary<RegularTerm, int> _termIds = new(ReferenceEqualityComparer.Instance);

    private static int Bit(bool value) =>
        value
            ? 1
            : 0;

    private AlternativePaths[] Canonical(List<AlternativePaths> configuration)
    {
        var canonical = new AlternativePaths[configuration.Count];

        for (var i = 0; i < configuration.Count; i++)
        {
            var (alternative, candidates) = configuration[i];
            var copies = new List<Candidate>(candidates.Count);

            foreach (var candidate in candidates)
            {
                var scope = new List<Entry>(candidate.Scope.Count);

                foreach (var entry in candidate.Scope)
                {
                    scope.Add(entry with { Id = _renamed[entry.Id] });
                }

                copies.Add(new Candidate
                {
                    Committed = candidate.Committed,
                    Kind = candidate.Kind,
                    Rest = Renamed(candidate.Rest),
                    Scope = scope
                });
            }

            canonical[i] = new(alternative, copies);
        }

        return canonical;
    }

    private void Cleanup(List<Candidate> candidates)
    {
        if (candidates.Count == 0)
        {
            return;
        }

        _firstIds.Clear();

        foreach (var candidate in candidates)
        {
            foreach (var entry in candidate.Scope)
            {
                if (entry.Kind == EntryKind.First)
                {
                    _firstIds.Add(entry.Id);
                }
            }
        }

        foreach (var candidate in candidates)
        {
            var scope = candidate.Scope;
            var kept = 0;

            for (var i = 0; i < scope.Count; i++)
            {
                if (scope[i].Kind != EntryKind.Second || _firstIds.Contains(scope[i].Id))
                {
                    scope[kept++] = scope[i];
                }
            }

            scope.RemoveRange(kept, scope.Count - kept);
        }

        for (var i = candidates.FindIndex(static candidate => candidate.Kind == CandidateKind.Live) - 1; i >= 0; i--)
        {
            if (candidates[i].Kind == CandidateKind.Dead)
            {
                candidates.RemoveAt(i);
            }
        }

        if (candidates.Count < 2)
        {
            return;
        }

        _seen.Clear();

        for (var i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].Kind == CandidateKind.Live && !_seen.Add(candidates[i]))
            {
                candidates.RemoveAt(i--);
            }
        }

        _seen.Clear();

        for (var i = candidates.Count - 1; i >= 0; i--)
        {
            if (candidates[i].Kind == CandidateKind.Dead && !_seen.Add(candidates[i]))
            {
                candidates.RemoveAt(i);
            }
        }

        _seen.Clear();
    }

    private void Close(List<Candidate> candidates, ref int nextId)
    {
        for (var i = 0; i < candidates.Count; i++)
        {
            Expand(candidates, i, ref nextId);
        }
    }

    private static void Collect(RegularTerm term, List<CharSet> sets)
    {
        switch (term)
        {
            case RegularTerm.Chars chars:
                sets.Add(chars.Set);
                break;

            case RegularTerm.Sequence sequence:
                Collect(sequence.First, sets);
                Collect(sequence.Second, sets);
                break;

            case RegularTerm.Choice choice:
                Collect(choice.First, sets);
                Collect(choice.Second, sets);
                break;

            case RegularTerm.Many many:
                Collect(many.Item, sets);
                break;

            case RegularTerm.Atomic atomic:
                Collect(atomic.Item, sets);
                break;
        }
    }

    private void Commit(List<Candidate> candidates, int i, int from)
    {
        var candidate = candidates[i];
        var reachedAtomic = false;

        _resolving.Clear();

        for (var j = from - 1; j >= 0 && !reachedAtomic; j--)
        {
            var entry = candidate.Scope[j];

            if (entry.Kind == EntryKind.First)
            {
                _resolving.Add(entry.Id);
            }
            else if (entry.Kind == EntryKind.Atomic)
            {
                candidate.Scope[j] = entry with { Consumed = true };
                reachedAtomic = true;
            }
        }

        candidate.Committed |= !reachedAtomic;

        foreach (var id in _resolving)
        {
            Resolve(candidates, i, id);
        }
    }

    private void Encode(List<AlternativePaths> configuration)
    {
        _key.Clear();
        _renamed.Clear();

        foreach (var (alternative, candidates) in configuration)
        {
            _key.Add(alternative);
            _key.Add(candidates.Count);

            foreach (var candidate in candidates)
            {
                _key.Add(((int)candidate.Kind << 1) | Bit(candidate.Committed));
                _key.Add(candidate.Scope.Count);

                foreach (var entry in candidate.Scope)
                {
                    _key.Add((Rename(entry.Id) << 3) | ((int)entry.Kind << 1) | Bit(entry.Consumed));
                }

                for (var rest = candidate.Rest; rest is not null; rest = rest.Tail)
                {
                    _key.Add(rest.Head switch
                    {
                        Run run => TermId(run.Term) << 2,
                        EndChoice end => (Rename(end.Id) << 2) | 1,
                        EndAtomic end => (Rename(end.Id) << 2) | 2,
                        _ => throw new UnreachableException(),
                    });
                }

                _key.Add(-1);
            }
        }
    }

    private void Expand(List<Candidate> candidates, int i, ref int nextId)
    {
        var candidate = candidates[i];

        while (candidate.Kind == CandidateKind.Live)
        {
            if (candidate.Rest is not { } rest)
            {
                candidates[i] = candidate.Match();
                candidates.RemoveRange(i + 1, candidates.Count - i - 1);
                return;
            }

            switch (rest.Head)
            {
                case Run { Term: RegularTerm.Chars }:
                    return;

                case Run { Term: RegularTerm.Empty }:
                    candidate.Rest = rest.Tail;
                    break;

                case Run { Term: RegularTerm.Sequence sequence }:
                    candidate.Rest = new(new Run(sequence.First), new(new Run(sequence.Second), rest.Tail));
                    break;

                case Run { Term: RegularTerm.Choice choice }:
                    Fork(candidates, i, ref nextId, choice.First, new(new Run(choice.Second), rest.Tail), rest.Tail);
                    break;

                case Run { Term: RegularTerm.Many many }:
                    Fork(candidates, i, ref nextId, many.Item, rest.Tail, new(new Run(many), rest.Tail));
                    break;

                case Run { Term: RegularTerm.Atomic atomic }:
                    var atomicId = nextId++;

                    candidate.Scope.Add(new Entry(EntryKind.Atomic, atomicId, Consumed: false));
                    candidate.Rest = new(new Run(atomic.Item), new(new EndAtomic(atomicId), rest.Tail));

                    break;

                case EndChoice end:
                    candidate.Rest = rest.Tail;
                    Resolve(candidates, i, end.Id);
                    break;

                case EndAtomic end:
                    candidate.Rest = rest.Tail;

                    var at = IndexOf(candidate.Scope, EntryKind.Atomic, end.Id);
                    var consumed = candidate.Scope[at].Consumed;

                    candidate.Scope.RemoveAt(at);

                    if (consumed)
                    {
                        Commit(candidates, i, at);
                    }

                    break;

                default:
                    throw new UnreachableException();
            }
        }
    }

    private static void Fork(List<Candidate> candidates, int i, ref int nextId, RegularTerm first, Continuation? second, Continuation? after)
    {
        var candidate = candidates[i];
        var id = nextId++;
        var lower = new Candidate
        {
            Committed = candidate.Committed,
            Rest = second,
            Scope = [.. candidate.Scope, new Entry(EntryKind.Second, id, Consumed: false)]
        };

        candidate.Scope.Add(new Entry(EntryKind.First, id, Consumed: false));
        candidate.Rest = new(new Run(first), new(new EndChoice(id), after));
        candidates.Insert(i + 1, lower);
    }

    private int[] Fresh(List<AlternativePaths> configuration)
    {
        _fresh.Clear();

        foreach (var (alternative, candidates) in configuration)
        {
            if (candidates.Exists(static candidate => candidate.Fresh))
            {
                _fresh.Add(alternative);
            }
        }

        return [.. _fresh];
    }

    private static int IndexOf(List<Entry> scope, EntryKind kind, int id)
    {
        for (var i = 0; i < scope.Count; i++)
        {
            if (scope[i].Kind == kind && scope[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }

    private (int State, int[] Fresh)? Make(List<AlternativePaths> configuration)
    {
        var kept = 0;
        var paths = 0;

        for (var i = 0; i < configuration.Count; i++)
        {
            var candidates = configuration[i].Candidates;

            Cleanup(candidates);

            if (_alternatives.Length > 1 && candidates.TrueForAll(static candidate => candidate.Kind == CandidateKind.Dead))
            {
                continue;
            }

            configuration[kept++] = configuration[i];
            paths += candidates.Count;
        }

        configuration.RemoveRange(kept, configuration.Count - kept);

        if (paths > _maxCandidates)
        {
            return null;
        }

        var fresh = Fresh(configuration);

        Encode(configuration);

        if (!_ids.TryGetValue(CollectionsMarshal.AsSpan(_key), out var state))
        {
            state = _configurations.Count;
            _ids.TryAdd(CollectionsMarshal.AsSpan(_key), state);
            _configurations.Add(Canonical(configuration));
        }

        return (state, fresh);
    }

    private static int[] Matched(AlternativePaths[] configuration) =>
        [.. configuration.Where(static paths => paths.Candidates.Exists(static candidate => candidate.Kind == CandidateKind.Matched)).Select(static paths => paths.Alternative)];

    private static int NextId(List<AlternativePaths> configuration)
    {
        var max = -1;

        foreach (var (_, candidates) in configuration)
        {
            foreach (var candidate in candidates)
            {
                foreach (var entry in candidate.Scope)
                {
                    max = Math.Max(max, entry.Id);
                }

                for (var rest = candidate.Rest; rest is not null; rest = rest.Tail)
                {
                    max = rest.Head switch
                    {
                        EndAtomic end => Math.Max(max, end.Id),
                        EndChoice end => Math.Max(max, end.Id),
                        _ => max,
                    };
                }
            }
        }

        return max + 1;
    }

    private void Partition()
    {
        var sets = new List<CharSet>();

        foreach (var alternative in _alternatives)
        {
            Collect(alternative, sets);
        }

        var distinct = sets.Distinct().ToArray();
        var cuts = new SortedSet<int> { 0, char.MaxValue + 1 };

        foreach (var set in distinct)
        {
            foreach (var (first, last) in set.Keys.Ranges)
            {
                cuts.Add((int)first);
                cuts.Add((int)last + 1);
            }
        }

        var classes = new Dictionary<string, ushort>();
        var bounds = cuts.ToArray();

        for (var i = 0; i + 1 < bounds.Length; i++)
        {
            var c = (char)bounds[i];
            var signature = new string([.. distinct.Select(set => set.Contains(c) ? '1' : '0')]);

            if (!classes.TryGetValue(signature, out var id))
            {
                id = (ushort)classes.Count;
                classes.Add(signature, id);
                _representatives.Add(c);
            }

            Array.Fill(_classOf, id, bounds[i], bounds[i + 1] - bounds[i]);
        }
    }

    private static bool Reads(Candidate candidate, char c) =>
        ((RegularTerm.Chars)((Run)candidate.Rest!.Head).Term).Set.Contains(c);

    private int Rename(int id) =>
        _renamed.TryGetValue(id, out var to)
            ? to
            : _renamed[id] = _renamed.Count;

    private Continuation? Renamed(Continuation? rest) =>
        rest is null
            ? null
            : new(
                rest.Head switch
                {
                    EndAtomic end => new EndAtomic(_renamed[end.Id]),
                    EndChoice end => new EndChoice(_renamed[end.Id]),
                    var frame => frame,
                },
                Renamed(rest.Tail)
            );

    private static void Resolve(List<Candidate> candidates, int i, int id)
    {
        if (IndexOf(candidates[i].Scope, EntryKind.First, id) < 0)
        {
            return;
        }

        Debug.Assert(!candidates.Take(i).Any(candidate => IndexOf(candidate.Scope, EntryKind.Second, id) >= 0), "A path on a second branch sits above the first branch.");

        for (var j = candidates.Count - 1; j > i; j--)
        {
            if (IndexOf(candidates[j].Scope, EntryKind.Second, id) >= 0)
            {
                candidates.RemoveAt(j);
            }
        }

        foreach (var candidate in candidates)
        {
            for (var at = IndexOf(candidate.Scope, EntryKind.First, id); at >= 0; at = IndexOf(candidate.Scope, EntryKind.First, id))
            {
                candidate.Scope.RemoveAt(at);
            }
        }
    }

    private List<AlternativePaths> Step(AlternativePaths[] configuration, int symbol)
    {
        var atEnd = symbol == _representatives.Count;
        var c = atEnd
            ? '\0'
            : _representatives[symbol];

        _stepped.Clear();

        foreach (var (alternative, candidates) in configuration)
        {
            if (_alternatives.Length > 1 && !Survives(candidates, atEnd, c))
            {
                continue;
            }

            var next = new List<Candidate>(candidates.Count);

            foreach (var candidate in candidates)
            {
                if (candidate.Kind != CandidateKind.Live)
                {
                    next.Add(candidate.Copy());
                }
                else if (!atEnd && Reads(candidate, c))
                {
                    next.Add(candidate.Advanced());
                }
                else
                {
                    next.Add(candidate.Dead());
                }
            }

            _stepped.Add(new(alternative, next));
        }

        if (atEnd)
        {
            return _stepped;
        }

        var nextId = NextId(_stepped);

        foreach (var (_, candidates) in _stepped)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Kind == CandidateKind.Live)
                {
                    Commit(candidates, i, candidates[i].Scope.Count);
                }
            }

            Close(candidates, ref nextId);
        }

        return _stepped;
    }

    private static bool Survives(List<Candidate> candidates, bool atEnd, char c)
    {
        foreach (var candidate in candidates)
        {
            if (candidate.Kind == CandidateKind.Matched
                || (candidate.Kind == CandidateKind.Live
                    && !atEnd
                    && Reads(candidate, c)))
            {
                return true;
            }
        }

        return false;
    }

    private int TermId(RegularTerm term)
    {
        if (!_termIds.TryGetValue(term, out var id))
        {
            id = _termIds.Count;
            _termIds.Add(term, id);
        }

        return id;
    }

    public CommitAutomaton? Run(int maxStates)
    {
        var initial = new List<AlternativePaths>(_alternatives.Length);
        var nextId = 0;

        Partition();

        for (var alternative = 0; alternative < _alternatives.Length; alternative++)
        {
            initial.Add(new(alternative, [new() { Rest = new(new Run(_alternatives[alternative]), null), Scope = [] }]));
        }

        foreach (var (_, candidates) in initial)
        {
            Close(candidates, ref nextId);
        }

        if (Make(initial) is not { } start)
        {
            return null;
        }

        var symbols = _representatives.Count + 1;
        var next = new List<int>();
        var ends = new List<int[]>();
        var live = new List<bool>();
        var matched = new List<int[]>();
        var committed = new List<bool>();

        for (var state = 0; state < _configurations.Count; state++)
        {
            if (_configurations.Count > maxStates)
            {
                return null;
            }

            var configuration = _configurations[state];
            var isLive = configuration.Any(static paths => paths.Candidates.Exists(static candidate => candidate.Kind == CandidateKind.Live));

            live.Add(isLive);
            matched.Add(Matched(configuration));
            committed.Add(configuration is [{ Alternative: 0, Candidates: [.., var lowest] }, ..] && lowest.Committed);

            for (var symbol = 0; symbol < symbols; symbol++)
            {
                if (!isLive)
                {
                    next.Add(state);
                    ends.Add([]);
                }
                else if (Make(Step(configuration, symbol)) is { } target)
                {
                    next.Add(target.State);
                    ends.Add(target.Fresh);
                }
                else
                {
                    return null;
                }
            }
        }

        return new CommitAutomaton
        {
            Alternatives = _alternatives.Length,
            ClassOf = _classOf,
            Classes = _representatives.Count,
            Committed = [.. committed],
            Ends = [.. ends],
            Initial = start.State,
            Live = [.. live],
            Matched = [.. matched],
            Next = [.. next],
        };
    }
}

/// <summary>
/// The eager commit-aware automaton for one or more regular terms, determinized into dense tables (ADR-0017).
/// </summary>
/// <remarks>
/// <para>
/// Each alternative keeps its candidate paths in priority order. A choice resolves as soon as a path through its first branch consumes input outside any <c>Atomic</c>,
/// or finishes the branch; the paths through its second branch are then dropped. A match drops every path below it.
/// Alternatives never prune each other, because each runs atomically, so one automaton serves a <c>Longest</c>: a tie goes to the earliest alternative.
/// </para>
/// <para>
/// The rule is exact on terms that <see cref="Regular.Of{T}"/> accepts. Each alternative holds at most one match, so one register per alternative records where it ended.
/// </para>
/// </remarks>
internal sealed class CommitAutomaton
{
    /// <summary>
    /// The result of a scan: the winning alternative and where its match ended, or <c>-1</c> and whether the failure consumed input.
    /// </summary>
    public readonly record struct Result(int Alternative, int End, bool Committed);

    private const int MaxCandidates = 1_000;

    /// <summary>
    /// The number of alternatives.
    /// </summary>
    public required int Alternatives { get; init; }

    /// <summary>
    /// The number of character classes. Symbols <c>0</c> to <c>Classes - 1</c> are the classes, and symbol <see cref="Classes"/> is the end of input.
    /// </summary>
    public required int Classes { get; init; }

    /// <summary>
    /// The character class of each UTF-16 code unit.
    /// </summary>
    public required ushort[] ClassOf { get; init; }

    /// <summary>
    /// For each state, whether the lowest path of the first alternative consumed input that no <c>Atomic</c> gives back.
    /// When a single term's scan stops with no match, this says whether it failed after consuming. A <c>Longest</c> never does.
    /// </summary>
    public required bool[] Committed { get; init; }

    /// <summary>
    /// For each transition, the alternatives whose match ends after the character just read.
    /// </summary>
    public required int[][] Ends { get; init; }

    /// <summary>
    /// The initial state.
    /// </summary>
    public required int Initial { get; init; }

    /// <summary>
    /// For each state, whether any path can still read input.
    /// </summary>
    public required bool[] Live { get; init; }

    /// <summary>
    /// For each state, the alternatives that hold a match, in order.
    /// </summary>
    public required int[][] Matched { get; init; }

    /// <summary>
    /// The transition table: the state reached from state <c>s</c> on symbol <c>k</c> is <c>Next[s * Symbols + k]</c>.
    /// </summary>
    public required int[] Next { get; init; }

    /// <summary>
    /// The number of states.
    /// </summary>
    public int States => Live.Length;

    /// <summary>
    /// The number of symbols: the character classes and the end of input.
    /// </summary>
    public int Symbols => Classes + 1;

    /// <summary>
    /// Builds the automaton for <paramref name="alternatives"/>, or returns <see langword="null"/> if it would have more than <paramref name="maxStates"/> states.
    /// </summary>
    public static CommitAutomaton? Build(IReadOnlyList<RegularTerm> alternatives, int maxStates) =>
        new Determinizer(alternatives, MaxCandidates).Run(maxStates);

    /// <summary>
    /// Scans <paramref name="input"/> from its start.
    /// </summary>
    /// <remarks>
    /// An alternative that matches the empty input holds a match from the initial state, and its register starts at <c>0</c>.
    /// </remarks>
    public Result Match(ReadOnlySpan<char> input)
    {
        var ends = new int[Alternatives];
        var state = Initial;
        var position = 0;

        while (Live[state])
        {
            var symbol = position < input.Length ? ClassOf[input[position]] : Classes;
            var transition = (state * Symbols) + symbol;
            state = Next[transition];

            if (symbol == Classes)
            {
                break;
            }

            position++;

            foreach (var alternative in Ends[transition])
            {
                ends[alternative] = position;
            }
        }

        var best = -1;
        var winner = -1;

        foreach (var alternative in Matched[state])
        {
            if (ends[alternative] > best)
            {
                best = ends[alternative];
                winner = alternative;
            }
        }

        return winner >= 0
            ? new Result(winner, best, false)
            : new Result(-1, 0, Alternatives == 1 && Committed[state]);
    }
}
