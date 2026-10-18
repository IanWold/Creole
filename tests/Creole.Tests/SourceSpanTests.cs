namespace Creole.Tests;

public class SourceSpanTests
{
    [Fact]
    public void TextSpanHasEqualCharOffsets()
    {
        var span = new SourceSpan(SourceId.Unnamed, 3, 7);

        Assert.Equal(3, span.CharStart);
        Assert.Equal(7, span.CharEnd);
        Assert.Equal(4, span.Length);
    }

    [Fact]
    public void ByteSpanKeepsBothUnits()
    {
        var span = new SourceSpan(SourceId.Unnamed, 10, 14, 8, 9);

        Assert.Equal(10, span.Start);
        Assert.Equal(14, span.End);
        Assert.Equal(8, span.CharStart);
        Assert.Equal(9, span.CharEnd);
    }

    [Fact]
    public void OffsetsPast2GBAreRepresented()
    {
        const long start = 3L * int.MaxValue;
        var span = new SourceSpan(SourceId.Unnamed, start, start + 5);

        Assert.Equal(start, span.Start);
        Assert.Equal(5, span.Length);
    }

    [Fact]
    public void ZeroWidthSpanIsAllowed()
    {
        var span = new SourceSpan(SourceId.Unnamed, 4, 4);

        Assert.Equal(0, span.Length);
    }

    [Theory]
    [InlineData(-1, 0, 0, 0)]
    [InlineData(5, 4, 5, 5)]
    [InlineData(0, 0, -1, 0)]
    [InlineData(0, 0, 5, 4)]
    public void InvalidOffsetsThrow(long start, long end, long charStart, long charEnd) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new SourceSpan(SourceId.Unnamed, start, end, charStart, charEnd));

    [Fact]
    public void SpansWithEqualFieldsAreEqual()
    {
        var registry = new SourceRegistry();
        var file = registry.Add("a.json");

        Assert.Equal(new SourceSpan(file, 1, 2), new SourceSpan(file, 1, 2, 1, 2));
        Assert.NotEqual(new SourceSpan(file, 1, 2), new SourceSpan(SourceId.Unnamed, 1, 2));
        Assert.NotEqual(new SourceSpan(file, 1, 2), new SourceSpan(file, 1, 2, 0, 1));
    }

    [Fact]
    public void DeconstructsToSourceAndRootOffsets()
    {
        var (source, start, end) = new SourceSpan(SourceId.Unnamed, 2, 9, 1, 3);

        Assert.Equal(SourceId.Unnamed, source);
        Assert.Equal(2, start);
        Assert.Equal(9, end);
    }
}

public class SourceRegistryTests
{
    [Fact]
    public void AddIssuesDistinctIdsAndKeepsNames()
    {
        var registry = new SourceRegistry();
        var a = registry.Add("a.json");
        var b = registry.Add("a.json");

        Assert.NotEqual(a, b);
        Assert.NotEqual(SourceId.Unnamed, a);
        Assert.Equal("a.json", registry.GetName(a));
        Assert.Equal("a.json", registry.GetName(b));
        Assert.Equal(2, registry.Count);
    }

    [Fact]
    public void UnnamedIsDefaultAndHasNoName()
    {
        var registry = new SourceRegistry();

        Assert.Equal(default, SourceId.Unnamed);
        Assert.Null(registry.GetName(SourceId.Unnamed));
    }

    [Fact]
    public void IdFromAnotherRegistryIsRejected()
    {
        var other = new SourceRegistry();

        other.Add("x");
        var foreign = other.Add("y");

        var registry = new SourceRegistry();

        registry.Add("only");

        Assert.Throws<ArgumentException>(() => registry.GetName(foreign));
    }

    [Fact]
    public void NullNameIsRejected() =>
        Assert.Throws<ArgumentNullException>(() => new SourceRegistry().Add(null!));
}
