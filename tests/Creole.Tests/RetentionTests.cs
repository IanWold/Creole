using System.Text;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file sealed class CopyScanner : IScanner<char, string>
{
    public ScannerInfo Info { get; } = new(Nullable: false, Name: "terminated word");

    public bool Scan(ref Cursor<char> cursor, out string value)
    {
        var copy = cursor;
        var letters = new StringBuilder();

        while (copy.TryRead(out var c))
        {
            if (c == ';')
            {
                cursor = copy;
                value = letters.ToString();
                return true;
            }

            letters.Append(c);
        }

        value = "";
        return false;
    }
}

/// <summary>
/// Streamed input released as the parse goes, against the same input in memory, for every construct that goes back to where it started.
/// </summary>
/// <remarks>
/// Each grammar first consumes a run of <c>-</c>, so the buffer releases segments before the construct starts and goes on releasing while it runs.
/// Released arrays are overwritten before they are reused, so a construct that reads a released segment gets other characters.
/// </remarks>
public class RetentionTests
{
    private sealed record Case(string Name, Parser<char, string> Parser, string Input);

    private readonly record struct Outcome(bool Ok, string Value, long Position, bool Threw);

    private static readonly Parser<char, string> _word = Letter.SkipAtLeastOnce().Text();

    private static readonly Parser<char, Unit> _terminated = Letter.SkipAtLeastOnce().Then(Char(';')).Select(static _ => default(Unit));

    private static readonly Case[] _cases =
    [
        new("Slice", Letter.SkipAtLeastOnce().Slice().Select(static slice => slice.ToString()), "----abcdef;"),
        new("Text", _word, "----abcdef;"),
        new("String", String("abcdex").Or(String("abcdef")), "----abcdef;"),
        new("CIString", CIString("ABCDEX").Or(CIString("ABCDEF")), "----abcdef;"),
        new("Atomic", Atomic(_terminated.Select(static _ => "atomic")).Or(_word), "----abcdef"),
        new("Lookahead", Lookahead(_word).Then(_word), "----abcdef;"),
        new("Not", Not(_terminated).Then(_word), "----abcdef"),
        new("Longest", Longest(_terminated.Select(static _ => "terminated"), _word), "----abcdef"),
        new("Compiled Longest", Longest(String("abcdefgh"), String("abc")).Then(_word), "----abcdefx;"),
        new("Custom", Custom(new CopyScanner()).Or(_word), "----abcdef"),
    ];

    private static readonly int[] _segmentSizes = [1, 2];

    private static string? Difference(Case @case, LoweringOptions options, int segmentSize)
    {
        var plan = new PlanBuilder(options).Lower(Char('-').SkipAtLeastOnce().Then(@case.Parser));
        var inMemory = new Cursor<char>(@case.Input);
        var expected = Run(plan, ref inMemory);
        var pool = new OverwritingPool<char>('#');
        var buffer = new StreamBuffer<char>(new ArrayUpstream<char>(@case.Input.ToCharArray()), segmentSize, pool);
        var streamed = new Cursor<char>(buffer);
        var actual = Run(plan, ref streamed);

        return actual != expected
            ? $"{@case.Name} ({options}, segments of {segmentSize}): in memory {expected}, streamed {actual}"
            : pool.Returns == 0
                ? $"{@case.Name} ({options}, segments of {segmentSize}): nothing was released"
                : null;
    }

    private static Outcome Run(Plan<char, string> plan, ref Cursor<char> cursor)
    {
        var value = "";

        try
        {
            var ok = plan.TryParse(ref cursor, ref value);

            return new Outcome(ok, value, cursor.Position, Threw: false);
        }
        catch (ArgumentOutOfRangeException)
        {
            return new Outcome(false, "", -1, Threw: true);
        }
    }

    [Fact]
    public void ACompiledLongestCaseRunsTheAutomaton() =>
        Assert.IsType<DfaLongestPlan<string>>(new PlanBuilder(LoweringOptions.All).Lower(Longest(String("abcdefgh"), String("abc"))));

    [Fact]
    public void ACopyOfAPinnedCursorKeepsTheFloor()
    {
        var buffer = new StreamBuffer<char>(new ArrayUpstream<char>("abcdefgh".ToCharArray()), segmentSize: 1);
        var cursor = new Cursor<char>(buffer);

        cursor.Advance(2);

        var floor = cursor.Pin();
        var copy = cursor;

        copy.Advance(5);
        cursor.Unpin(floor);

        Assert.Equal(1, buffer.Start);
        Assert.Equal('c', cursor.ItemAt(2));
    }

    [Fact]
    public void EveryConstructThatGoesBackGivesTheInMemoryResultOverReleasedSegments()
    {
        var differences = (
            from @case in _cases
            from options in new[] { LoweringOptions.All, LoweringOptions.None }
            from size in _segmentSizes
            select Difference(@case, options, size)
        ).OfType<string>().ToList();

        Assert.Empty(differences);
    }

    [Fact]
    public void NothingBelowTheCursorIsKeptWhenNothingIsPinned()
    {
        var buffer = new StreamBuffer<char>(new ArrayUpstream<char>(new string('a', 100).ToCharArray()), segmentSize: 4);
        var cursor = new Cursor<char>(buffer);

        cursor.Advance(99);

        Assert.Equal(92, buffer.Start);
    }
}
