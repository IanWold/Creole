using System.Diagnostics;
using System.Text;

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

    public List<Entry> Scope { get; init; } = [];

    private List<Entry> SecondBranches() =>
        [.. Scope.Where(static entry => entry.Kind == EntryKind.Second)];

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

file sealed record Continuation(Frame Head, Continuation? Tail);

file readonly record struct Entry(EntryKind Kind, int Id, bool Consumed);

file abstract record Frame;

file sealed record EndAtomic(int Id) : Frame;

file sealed record EndChoice(int Id) : Frame;

file sealed record Run(RegularTerm Term) : Frame;

/// <summary>
/// Builds a <see cref="CommitAutomaton"/> by exploring every configuration of candidates reachable from the start, one character class at a time.
/// </summary>
/// <remarks>
/// Configurations are compared after renaming their choice and <c>Atomic</c> ids in order of first appearance, so equivalent ones become one state.
/// </remarks>
file sealed class Determinizer
{
    private readonly RegularTerm[] _alternatives;
    private readonly ushort[] _classOf = new ushort[char.MaxValue + 1];
    private readonly List<List<Candidate>[]> _configurations = [];
    private readonly Dictionary<string, int> _ids = [];
    private readonly int _maxCandidates;
    private readonly List<char> _representatives = [];
    private readonly Dictionary<RegularTerm, int> _termIds = new(ReferenceEqualityComparer.Instance);

    public Determinizer(IReadOnlyList<RegularTerm> alternatives, int maxCandidates)
    {
        _alternatives = [.. alternatives];
        _maxCandidates = maxCandidates;
        Partition();
    }

    private static void Advance(List<Candidate> candidates, char c)
    {
        for (var i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];

            if (candidate.Kind != CandidateKind.Live)
            {
                continue;
            }

            var chars = (RegularTerm.Chars)((Run)candidate.Rest!.Head).Term;

            if (chars.Set.Contains(c))
            {
                candidate.Rest = candidate.Rest.Tail;
                Commit(candidates, i, candidate.Scope.Count);
            }
            else
            {
                candidates[i] = candidate.Dead();
            }
        }
    }

    private (string Key, List<Candidate>[] Configuration) Canonicalize(List<Candidate>[] configuration)
    {
        var renamed = new Dictionary<int, int>();
        int Rename(int id) => renamed.TryGetValue(id, out var to) ? to : renamed[id] = renamed.Count;

        Continuation? RenameRest(Continuation? rest) =>
            rest is null
            ? null
            : new(
                rest.Head switch
                {
                    EndAtomic end => new EndAtomic(Rename(end.Id)),
                    EndChoice end => new EndChoice(Rename(end.Id)),
                    var frame => frame,
                },
                RenameRest(rest.Tail)
            );

        var key = new StringBuilder();
        var canonical = new List<Candidate>[configuration.Length];

        for (var alternative = 0; alternative < configuration.Length; alternative++)
        {
            key.Append('|');
            canonical[alternative] = [];

            foreach (var candidate in configuration[alternative])
            {
                var scope = candidate.Scope.Select(entry => entry with { Id = Rename(entry.Id) }).ToList();
                var copy = new Candidate
                {
                    Committed = candidate.Committed,
                    Kind = candidate.Kind,
                    Scope = scope,
                    Rest = RenameRest(candidate.Rest)
                };

                canonical[alternative].Add(copy);
                Describe(copy, key);
            }
        }

        return (key.ToString(), canonical);
    }

    private void Cleanup(List<Candidate> candidates)
    {
        var firsts = candidates
            .SelectMany(static candidate => candidate.Scope)
            .Where(static entry => entry.Kind == EntryKind.First)
            .Select(static entry => entry.Id)
            .ToHashSet();

        foreach (var candidate in candidates)
        {
            candidate.Scope.RemoveAll(entry => entry.Kind == EntryKind.Second && !firsts.Contains(entry.Id));
        }

        for (var i = candidates.FindIndex(static candidate => candidate.Kind == CandidateKind.Live) - 1; i >= 0; i--)
        {
            if (candidates[i].Kind == CandidateKind.Dead)
            {
                candidates.RemoveAt(i);
            }
        }

        var seen = new HashSet<string>();

        for (var i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].Kind == CandidateKind.Live && !seen.Add(Describe(candidates[i], new StringBuilder()).ToString()))
            {
                candidates.RemoveAt(i--);
            }
        }

        seen.Clear();

        for (var i = candidates.Count - 1; i >= 0; i--)
        {
            if (candidates[i].Kind == CandidateKind.Dead && !seen.Add(Describe(candidates[i], new StringBuilder()).ToString()))
            {
                candidates.RemoveAt(i);
            }
        }
    }

    private static void Close(List<Candidate> candidates, ref int nextId)
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

    private static void Commit(List<Candidate> candidates, int i, int from)
    {
        var candidate = candidates[i];
        var firsts = new List<int>();
        var reachedAtomic = false;

        for (var j = from - 1; j >= 0 && !reachedAtomic; j--)
        {
            var entry = candidate.Scope[j];

            if (entry.Kind == EntryKind.First)
            {
                firsts.Add(entry.Id);
            }
            else if (entry.Kind == EntryKind.Atomic)
            {
                candidate.Scope[j] = entry with { Consumed = true };
                reachedAtomic = true;
            }
        }

        candidate.Committed |= !reachedAtomic;

        foreach (var id in firsts)
        {
            Resolve(candidates, i, id);
        }
    }

    private StringBuilder Describe(Candidate candidate, StringBuilder key)
    {
        key.Append(candidate.Kind switch { CandidateKind.Live => 'L', CandidateKind.Dead => 'D', _ => 'M' }).Append(candidate.Committed ? '!' : '.');

        foreach (var entry in candidate.Scope)
        {
            key.Append(entry.Kind switch { EntryKind.First => 'f', EntryKind.Second => 's', _ => entry.Consumed ? 'A' : 'a' }).Append(entry.Id);
        }

        key.Append(':');

        for (var rest = candidate.Rest; rest is not null; rest = rest.Tail)
        {
            _ = rest.Head switch
            {
                Run run => key.Append('r').Append(TermId(run.Term)),
                EndChoice end => key.Append('c').Append(end.Id),
                EndAtomic end => key.Append('x').Append(end.Id),
                _ => throw new UnreachableException(),
            };

            key.Append(',');
        }

        return key.Append(';');
    }

    private static void Expand(List<Candidate> candidates, int i, ref int nextId)
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

                    var at = candidate.Scope.FindIndex(entry => entry.Kind == EntryKind.Atomic && entry.Id == end.Id);
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

    private (int State, int[] Fresh)? Make(List<Candidate>[] configuration)
    {
        foreach (var candidates in configuration)
        {
            Cleanup(candidates);
        }

        if (configuration.Sum(static candidates => candidates.Count) > _maxCandidates)
        {
            return null;
        }

        int[] fresh = [.. Enumerable.Range(0, configuration.Length).Where(alternative => configuration[alternative].Exists(static candidate => candidate.Fresh))];
        var (key, canonical) = Canonicalize(configuration);

        if (!_ids.TryGetValue(key, out var state))
        {
            state = _configurations.Count;
            _ids.Add(key, state);
            _configurations.Add(canonical);
        }

        return (state, fresh);
    }

    private static int NextId(List<Candidate>[] configuration)
    {
        var max = -1;

        foreach (var candidate in configuration.SelectMany(static candidates => candidates))
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

    private static void Resolve(List<Candidate> candidates, int i, int id)
    {
        if (!candidates[i].Scope.Exists(entry => entry.Kind == EntryKind.First && entry.Id == id))
        {
            return;
        }

        Debug.Assert(!candidates.Take(i).Any(candidate => candidate.Scope.Exists(entry => entry.Kind == EntryKind.Second && entry.Id == id)), "A path on a second branch sits above the first branch.");

        for (var j = candidates.Count - 1; j > i; j--)
        {
            if (candidates[j].Scope.Exists(entry => entry.Kind == EntryKind.Second && entry.Id == id))
            {
                candidates.RemoveAt(j);
            }
        }

        foreach (var candidate in candidates)
        {
            candidate.Scope.RemoveAll(entry => entry.Kind == EntryKind.First && entry.Id == id);
        }
    }

    private List<Candidate>[] Step(List<Candidate>[] configuration, int symbol)
    {
        var next =
            configuration
            .Select(static candidates =>
                candidates
                .Select(static candidate => new Candidate
                {
                    Committed = candidate.Committed,
                    Kind = candidate.Kind,
                    Rest = candidate.Rest,
                    Scope = [.. candidate.Scope],
                })
                .ToList()
            )
            .ToArray();

        var nextId = NextId(next);

        foreach (var candidates in next)
        {
            if (symbol == _representatives.Count)
            {
                for (var i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i].Kind == CandidateKind.Live)
                    {
                        candidates[i] = candidates[i].Dead();
                    }
                }
            }
            else
            {
                Advance(candidates, _representatives[symbol]);
                Close(candidates, ref nextId);
            }
        }

        return next;
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
        var initial = _alternatives.Select(static alternative => new List<Candidate> { new() { Rest = new(new Run(alternative), null) } }).ToArray();
        var nextId = 0;

        foreach (var candidates in initial)
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
            var isLive = configuration.Any(static candidates => candidates.Exists(static candidate => candidate.Kind == CandidateKind.Live));

            live.Add(isLive);
            matched.Add([.. Enumerable.Range(0, configuration.Length).Where(alternative => configuration[alternative].Exists(static candidate => candidate.Kind == CandidateKind.Matched))]);
            committed.Add(configuration[0] is [.., var lowest] && lowest.Committed);

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
