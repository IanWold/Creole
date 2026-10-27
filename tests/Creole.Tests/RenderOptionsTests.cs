namespace Creole.Tests;

public class RenderOptionsTests
{
    [Fact]
    public void DefaultsShowTenNotes()
    {
        Assert.Equal(10, RenderOptions.Default.MaxNotes);
        Assert.Equal(RenderOptions.Default, new RenderOptions());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(int.MaxValue)]
    public void MaxNotesTakesAnyValueFromZero(int maxNotes) =>
        Assert.Equal(maxNotes, new RenderOptions { MaxNotes = maxNotes }.MaxNotes);

    [Fact]
    public void NegativeMaxNotesThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RenderOptions { MaxNotes = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => RenderOptions.Default with { MaxNotes = int.MinValue });
    }

    [Fact]
    public void AVariantLeavesTheDefaultsAlone()
    {
        var fewer = RenderOptions.Default with { MaxNotes = 2 };

        Assert.Equal(2, fewer.MaxNotes);
        Assert.Equal(10, RenderOptions.Default.MaxNotes);
    }
}
