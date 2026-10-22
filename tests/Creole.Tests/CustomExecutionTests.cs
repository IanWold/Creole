namespace Creole.Tests;

file sealed class PairScanner : IScanner<int, int>
{
    public ScannerInfo Info { get; } = new(Nullable: false);

    public bool Scan(ref Cursor<int> cursor, out int value)
    {
        var c = cursor;
        value = 0;

        if (!c.TryRead(out var first) || !c.TryRead(out var second) || first != second)
        {
            return false;
        }

        cursor = c;
        value = first + second;
        return true;
    }
}

public class CustomExecutionTests
{
    private static readonly Parser<int, int> _pair = Grammar<int>.Custom(new PairScanner());

    [Fact]
    public void AChoiceTriesItsNextAlternativeWhenAScannerFails() =>
        Assert.Equal(new Outcome.Ok("3", 1), Execution.Outcome(_pair.Or(Grammar<int>.Any), [3, 4]));

    [Theory]
    [InlineData(new[] { 3, 4 })]
    [InlineData(new[] { 3 })]
    [InlineData(new int[0])]
    public void ACustomParserFailsWithoutConsumingWhenItsScannerFails(int[] input) =>
        Assert.Equal(new Outcome.FailEmpty(), Execution.Outcome(_pair, input));

    [Fact]
    public void ACustomParserProducesTheScannersValue() =>
        Assert.Equal(new Outcome.Ok("6", 2), Execution.Outcome(_pair, [3, 3, 4]));

    [Fact]
    public void ACustomParserRepeats() =>
        Assert.Equal(new Outcome.Ok("2,4", 4), Execution.Outcome(_pair.Many(), [1, 1, 2, 2, 3], items => string.Join(",", items)));
}
