using System.Buffers;
using CsCheck;

namespace Creole.Tests;

public class CursorTests
{
    // _identifierChars and ScanIdentifier are the code sample in docs/engines/cursor.md. Keep the two in step.
    private static readonly SearchValues<char> _identifierChars = SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_");

    private static bool ScanIdentifier(ref Cursor<char> c)
    {
        if (!c.TryPeek(out var first) || !(char.IsAsciiLetter(first) || first == '_'))
        {
            return false;
        }

        c.Advance(1);

        while (c.Available is { IsEmpty: false } available)
        {
            var length = available.IndexOfAnyExcept(_identifierChars);

            if (length >= 0)
            {
                c.Advance(length);
                return true;
            }

            c.Advance(available.Length);
        }

        return true;
    }

    [Fact]
    public void AdvanceByMoreThanRemainsThrows() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => { var c = new Cursor<char>("ab"); c.Advance(3); });

    [Fact]
    public void AdvanceByNegativeThrows() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => { var c = new Cursor<char>("ab"); c.Advance(-1); });

    [Fact]
    public void AdvanceByZeroDoesNothing()
    {
        var cursor = new Cursor<char>("ab");

        cursor.Advance(0);
        Assert.Equal(0, cursor.Position);
    }

    [Fact]
    public void AvailableAtASegmentBoundaryLoadsTheNextSegment()
    {
        var cursor = new Cursor<char>(new SegmentedSource<char>("abcdefg".ToCharArray(), 3));

        Assert.Equal("abc", cursor.Available.ToString());
        cursor.Advance(3);
        Assert.Equal("def", cursor.Available.ToString());
        cursor.Advance(4);
        Assert.True(cursor.Available.IsEmpty);
    }

    [Fact]
    public void AvailableIsTheRestOfTheInput()
    {
        var cursor = new Cursor<char>("hello");
        cursor.Advance(2);

        Assert.Equal("llo", cursor.Available.ToString());
    }

    [Fact]
    public void CopiesMoveIndependently()
    {
        var cursor = new Cursor<char>("ab");
        var copy = cursor;

        copy.Advance(1);

        Assert.Equal(0, cursor.Position);
        Assert.Equal(1, copy.Position);
    }

    [Fact]
    public void DefaultCursorIsAtEnd()
    {
        Cursor<char> cursor = default;

        Assert.True(cursor.IsAtEnd);
        Assert.Equal(0, cursor.Position);
    }

    [Fact]
    public void EmptyInputIsAtEnd()
    {
        var cursor = new Cursor<char>("");

        Assert.True(cursor.IsAtEnd);
        Assert.False(cursor.TryPeek(out var item));
        Assert.Equal('\0', item);
        Assert.True(cursor.Available.IsEmpty);
    }

    [Fact]
    public void FailedReadAtEndDoesNotMove()
    {
        var cursor = new Cursor<char>("a");
        cursor.Advance(1);

        Assert.False(cursor.TryRead(out _));
        Assert.Equal(1, cursor.Position);
    }

    [Fact]
    public void PeekDoesNotMove()
    {
        var cursor = new Cursor<char>("xy");

        Assert.True(cursor.TryPeek(out var first));
        Assert.True(cursor.TryPeek(out var again));
        Assert.Equal('x', first);
        Assert.Equal('x', again);
        Assert.Equal(0, cursor.Position);
    }

    [Fact]
    public void PositionCountsItemsConsumed()
    {
        int[] input = [10, 20, 30, 40];
        var cursor = new Cursor<int>(input);

        cursor.Advance(1);
        Assert.Equal(1, cursor.Position);
        cursor.Advance(2);
        Assert.Equal(3, cursor.Position);
        cursor.TryRead(out var last);
        Assert.Equal(40, last);
        Assert.Equal(4, cursor.Position);
    }

    [Fact]
    public void ReadingMatchesTheInputForAnySegmentation() =>
        Gen.Int[0, 50].Array.Select(Gen.Int[1, 8], Gen.Int[1, 5].Array[0, 20]).Sample((input, segmentSize, steps) =>
        {
            var cursor = new Cursor<int>(new SegmentedSource<int>(input, segmentSize));
            var expected = 0;

            foreach (var step in steps)
            {
                var count = Math.Min(step, input.Length - expected);
                cursor.Advance(count);
                expected += count;

                if (cursor.Position != expected || cursor.TryPeek(out var item) != expected < input.Length || (expected < input.Length && item != input[expected]))
                {
                    return false;
                }
            }

            return true;
        });

    [Fact]
    public void ReadsAcrossSegments()
    {
        var cursor = new Cursor<char>(new SegmentedSource<char>("abcdefg".ToCharArray(), 3));
        var read = new List<char>();

        while (cursor.TryRead(out var item))
        {
            read.Add(item);
        }

        Assert.Equal("abcdefg", new string([.. read]));
        Assert.Equal(7, cursor.Position);
    }

    [Fact]
    public void ReadsASlice()
    {
        var cursor = new Cursor<char>("xabcx".AsSpan(1, 3));

        Assert.Equal("abc", cursor.Available.ToString());
        Assert.Equal(0, cursor.Position);
    }

    [Fact]
    public void ReadsAStringInOrder()
    {
        var cursor = new Cursor<char>("abc");

        Assert.True(cursor.TryRead(out var a));
        Assert.True(cursor.TryRead(out var b));
        Assert.True(cursor.TryRead(out var c));
        Assert.Equal("abc", $"{a}{b}{c}");
        Assert.False(cursor.TryRead(out _));
        Assert.True(cursor.IsAtEnd);
    }

    [Fact]
    public void ItemAtReadsAnEarlierItemWithoutMoving()
    {
        var cursor = new Cursor<char>(new SegmentedSource<char>("abcde".ToCharArray(), 2));
        cursor.Advance(4);

        Assert.Equal('b', cursor.ItemAt(1));
        Assert.Equal(4, cursor.Position);
    }

    [Fact]
    public void ResetAcrossSegmentsReloadsTheEarlierSegment()
    {
        var cursor = new Cursor<char>(new SegmentedSource<char>("abcdefg".ToCharArray(), 3));
        cursor.Advance(5);

        cursor.Reset(1);

        Assert.True(cursor.TryRead(out var item));
        Assert.Equal('b', item);
        Assert.Equal(2, cursor.Position);
    }

    [Fact]
    public void ResetOutsideTheInputThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => { var c = new Cursor<char>("ab"); c.Reset(3); });
        Assert.Throws<ArgumentOutOfRangeException>(() => { var c = new Cursor<char>("ab"); c.Reset(-1); });
    }

    [Fact]
    public void ResetRewindsToAnEarlierPosition()
    {
        var cursor = new Cursor<char>("abcd");
        cursor.Advance(3);

        cursor.Reset(1);

        Assert.Equal(1, cursor.Position);
        Assert.True(cursor.TryPeek(out var item));
        Assert.Equal('b', item);
    }

    [Fact]
    public void ResetPastTheEndOfSegmentedInputThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var c = new Cursor<char>(new SegmentedSource<char>("abc".ToCharArray(), 2));
            c.Reset(4);
        });
    }

    [Fact]
    public void ResetToTheEndOfSegmentedInputFromAnEarlierSegmentIsAllowed()
    {
        var cursor = new Cursor<char>(new SegmentedSource<char>("abcde".ToCharArray(), 2));

        cursor.Advance(1);
        cursor.Reset(5);

        Assert.Equal(5, cursor.Position);
        Assert.True(cursor.IsAtEnd);
    }

    [Fact]
    public void ResetToTheEndIsAllowed()
    {
        var cursor = new Cursor<char>("ab");

        cursor.Reset(2);

        Assert.True(cursor.IsAtEnd);
    }

    [Fact]
    public void ScanIdentifierFailsWithoutMoving()
    {
        var cursor = new Cursor<char>("1abc");

        Assert.False(ScanIdentifier(ref cursor));
        Assert.Equal(0, cursor.Position);
    }

    [Fact]
    public void ScanIdentifierReadsAcrossSegments()
    {
        var cursor = new Cursor<char>(new SegmentedSource<char>("abcdefgh;".ToCharArray(), 3));

        Assert.True(ScanIdentifier(ref cursor));
        Assert.Equal(8, cursor.Position);
    }
}
