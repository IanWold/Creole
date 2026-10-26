using System.Buffers;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file sealed class GroupUpstream(char[][] steps, object?[] states) : IUpstream<char>
{
    private readonly object?[] _states = states;
    private readonly char[][] _steps = steps;
    private int _step;

    public bool TryStep(StreamBuffer<char> buffer)
    {
        if (_step == _steps.Length)
        {
            return false;
        }

        var outputs = _steps[_step];

        for (var group = 0; group < outputs.Length; group++)
        {
            buffer.Add(outputs[group], new StreamSlot(new SourceSpan(SourceId.Unnamed, _step, _step + 1), _step, _states[_step], group));
        }

        _step++;
        return true;
    }
}

file sealed class RepeatUpstream(char item, int count) : IUpstream<char>
{
    private readonly int _count = count;
    private readonly char _item = item;
    private int _next;

    public bool TryStep(StreamBuffer<char> buffer)
    {
        if (_next == _count)
        {
            return false;
        }

        buffer.Add(_item, new StreamSlot(new SourceSpan(SourceId.Unnamed, _next, _next + 1), _next, null, 0));
        _next++;
        return true;
    }
}

file sealed class SpanUpstream(string[] tokens, SourceSpan[] spans) : IUpstream<string>
{
    private readonly SourceSpan[] _spans = spans;
    private readonly string[] _tokens = tokens;
    private int _next;

    public bool TryStep(StreamBuffer<string> buffer)
    {
        if (_next == _tokens.Length)
        {
            return false;
        }

        buffer.Add(_tokens[_next], new StreamSlot(_spans[_next], _spans[_next].Start, null, 0));
        _next++;
        return true;
    }
}

file sealed class TokenUpstream(StreamBuffer<char> characters, Plan<char, string> item) : IUpstream<string>
{
    private readonly StreamBuffer<char> _characters = characters;
    private readonly Plan<char, string> _item = item;
    private long _position;

    public List<string> Produced { get; } = [];

    public bool TryStep(StreamBuffer<string> buffer)
    {
        var cursor = new Cursor<char>(_characters);
        cursor.Reset(_position);

        if (cursor.IsAtEnd)
        {
            return false;
        }

        var token = "";

        if (!_item.TryParse(ref cursor, ref token))
        {
            throw new InvalidOperationException($"No token at {_position}.");
        }

        buffer.Add(token, new StreamSlot(_characters.SpanOf(_position, cursor.Position), _position, null, 0));
        Produced.Add(token);
        _position = cursor.Position;
        return true;
    }
}

public class StreamBufferTests
{
    private readonly record struct Observed(bool Ok, string Value, long Position, bool Cut, bool Threw, ParseError? Error);

    private static readonly int[] _segmentSizes = [1, 2];

    private static Cursor<char> Over(string text, int segmentSize, out ArrayUpstream<char> upstream, out StreamBuffer<char> buffer)
    {
        upstream = new ArrayUpstream<char>(text.ToCharArray());
        buffer = new StreamBuffer<char>(upstream, segmentSize);

        return new Cursor<char>(buffer);
    }

    private static Observed Observe(Plan<char, string> plan, ref Cursor<char> cursor)
    {
        var value = "";

        try
        {
            var ok = plan.TryParse(ref cursor, ref value);
            var error = cursor.Failures!.ToError(in cursor, "parser", static (start, end) => new SourceSpan(SourceId.Unnamed, start, end));

            return new Observed(ok, value, cursor.Position, cursor.Cut, Threw: false, error);
        }
        catch (GrammarException)
        {
            return new Observed(false, "", cursor.Position, cursor.Cut, Threw: true, Error: null);
        }
    }

    private static string? ShapeDifference(ControlShape shape, string[] inputs)
    {
        var plan = new PlanBuilder(LoweringOptions.All, dispatchThreshold: 1).Lower(ControlTable.Build(shape));

        foreach (var input in inputs)
        {
            var inMemory = new Cursor<char>(input) { Failures = new FailureLog<char>() };
            var expected = Observe(plan, ref inMemory);

            foreach (var size in _segmentSizes)
            {
                var streamed = Over(input, size, out var upstream, out _) with { Failures = new FailureLog<char>() };
                var actual = Observe(plan, ref streamed);

                if (actual != expected)
                {
                    return $"{ControlModel.Describe(shape)} on \"{input}\" in segments of {size}: in memory {expected}, streamed {actual}";
                }

                if (upstream.Produced.Any(static count => count > 1))
                {
                    return $"{ControlModel.Describe(shape)} on \"{input}\" in segments of {size}: produced [{string.Join(", ", upstream.Produced)}]";
                }
            }
        }

        return null;
    }

    [Fact]
    public void ASliceKeepsItsTextAfterItsSegmentIsReused()
    {
        var pool = ArrayPool<char>.Create(maxArrayLength: 16, maxArraysPerBucket: 4);
        var buffer = new StreamBuffer<char>(new ArrayUpstream<char>("abcdefgh;".ToCharArray()), segmentSize: 4, pool);
        var cursor = new Cursor<char>(buffer);
        var plan = new PlanBuilder(LoweringOptions.All).Lower(Letter.SkipAtLeastOnce().Slice());
        ReadOnlyMemory<char> slice = default;

        Assert.True(plan.TryParse(ref cursor, ref slice));
        buffer.Release(cursor.Position);

        var reused = 0;

        for (var i = 0; i < 2; i++)
        {
            var array = pool.Rent(4);

            if (array.AsSpan(0, 4) is "abcd" or "efgh")
            {
                reused++;
            }

            array.AsSpan().Fill('x');
        }

        Assert.Equal(2, reused);
        Assert.Equal("abcdefgh", slice.ToString());
    }

    [Fact]
    public void AStepMayAddSeveralItemsOrNone()
    {
        var state = new object();
        var buffer = new StreamBuffer<char>(new GroupUpstream([['a', 'b', 'c'], [], ['d']], [null, state, state]), segmentSize: 2);
        var cursor = new Cursor<char>(buffer);
        var read = new List<char>();

        while (cursor.TryRead(out var item))
        {
            read.Add(item);
        }

        Assert.Equal("abcd", new string([.. read]));
        Assert.Equal(new StreamSlot(new SourceSpan(SourceId.Unnamed, 0, 1), 0, null, 0), buffer.SlotAt(0));
        Assert.Equal(new StreamSlot(new SourceSpan(SourceId.Unnamed, 0, 1), 0, null, 2), buffer.SlotAt(2));
        Assert.Equal(new StreamSlot(new SourceSpan(SourceId.Unnamed, 2, 3), 2, state, 0), buffer.SlotAt(3));
    }

    [Fact]
    public void ATokenBufferOverACharacterBufferProducesEachItemOnce()
    {
        var characters = new ArrayUpstream<char>("abaab".ToCharArray());
        var characterBuffer = new StreamBuffer<char>(characters, segmentSize: 2);
        var tokens = new TokenUpstream(characterBuffer, new PlanBuilder(LoweringOptions.All).Lower(Longest(String("ab"), String("a"), String("b"))));
        var cursor = new Cursor<string>(new StreamBuffer<string>(tokens, segmentSize: 2));
        var parser = Grammar<string>.Atomic(Grammar<string>.Token("ab").Then(Grammar<string>.Token("ab")))
            .Or(Grammar<string>.Lookahead(Grammar<string>.Token("ab").Then(Grammar<string>.Token("a"))).Then(Grammar<string>.Token("ab")))
            .Then(Grammar<string>.Token("a"))
            .Then(Grammar<string>.Token("ab"));
        var plan = new PlanBuilder(LoweringOptions.All).Lower(parser);
        var value = "";

        Assert.True(plan.TryParse(ref cursor, ref value));
        Assert.Equal(3, cursor.Position);
        Assert.Equal(["ab", "a", "ab"], tokens.Produced);
        Assert.Equal([1, 1, 1, 1, 1], characters.Produced);
    }

    [Fact]
    public void EveryControlShapeToDepthThreeRunsOverTheBufferAsInMemoryAndProducesEachItemOnce()
    {
        var inputs = ControlModel.AllInputs(3).ToArray();
        var difference = ControlModel.AllShapes(3)
            .Select(shape => ShapeDifference(shape, inputs))
            .FirstOrDefault(static difference => difference is not null);

        Assert.Null(difference);
    }

    [Fact]
    public void PullsOnlyAsFarAsTheCursorReads()
    {
        var cursor = Over("abcdef", 4, out var upstream, out _);

        Assert.Equal(0, upstream.Steps);
        Assert.True(cursor.TryPeek(out _));
        Assert.Equal(1, upstream.Steps);

        for (var i = 0; i < 3; i++)
        {
            cursor.TryRead(out _);
        }

        Assert.Equal(3, upstream.Steps);
        Assert.True(cursor.TryPeek(out var fourth));
        Assert.Equal('d', fourth);
        Assert.Equal(4, upstream.Steps);
    }

    [Fact]
    public void ReadingAndReleasingAsItGoesAllocatesNothingPerItem()
    {
        const int length = 1 << 20;
        var read = 0L;

        var allocated = Allocation.Fewest(
            () =>
            {
                var buffer = new StreamBuffer<char>(new RepeatUpstream('x', length), segmentSize: 1024);
                var cursor = new Cursor<char>(buffer);

                while (cursor.TryRead(out _))
                {
                    if (cursor.Position % 1024 == 0)
                    {
                        buffer.Release(cursor.Position);
                    }
                }

                read = cursor.Position;
            },
            attempts: 5
        );

        Assert.Equal(length, read);
        Assert.True(allocated < 4096, $"{allocated} bytes allocated over {length} items.");
    }

    [Fact]
    public void ReleasedPositionsCannotBeRead()
    {
        var cursor = Over("abcdefg", 2, out _, out var buffer);

        cursor.Advance(5);
        buffer.Release(5);

        Assert.Equal(4, buffer.Start);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SlotAt(3));

        try
        {
            _ = cursor.ItemAt(3);
            Assert.Fail("A released position was read.");
        }
        catch (ArgumentOutOfRangeException)
        {
        }

        Assert.Equal('e', cursor.ItemAt(4));
        Assert.True(cursor.TryRead(out var item));
        Assert.Equal('f', item);

        cursor.Advance(1);
        buffer.Release(cursor.Position);

        Assert.Equal(6, buffer.Start);
        Assert.True(cursor.IsAtEnd);
    }

    [Fact]
    public void SpansComeFromTheSlots()
    {
        static SourceSpan At(long start, long end) => new(SourceId.Unnamed, start, end);

        var buffer = new StreamBuffer<string>(new SpanUpstream(["ab", "c", "de"], [At(0, 2), At(3, 4), At(5, 7)]), segmentSize: 2);
        var empty = new StreamBuffer<string>(new SpanUpstream([], []));
        var cursor = new Cursor<string>(buffer);

        while (cursor.TryRead(out _)) { }

        Assert.Equal(At(0, 4), buffer.SpanOf(0, 2));
        Assert.Equal(At(3, 7), buffer.SpanOf(1, 3));
        Assert.Equal(At(3, 3), buffer.SpanOf(1, 1));
        Assert.Equal(At(7, 7), buffer.SpanOf(3, 3));
        Assert.Equal(At(0, 0), empty.SpanOf(0, 0));
    }
}
