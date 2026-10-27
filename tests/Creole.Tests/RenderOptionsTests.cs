namespace Creole.Tests;

public class RenderOptionsTests
{
    [Fact]
    public void DefaultsShowTenNotesAnd120Columns()
    {
        Assert.Equal(10, RenderOptions.Default.MaxNotes);
        Assert.Equal(120, RenderOptions.Default.MaxWidth);
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

    [Theory]
    [InlineData(10)]
    [InlineData(80)]
    [InlineData(int.MaxValue)]
    public void MaxWidthTakesAnyValueFromTen(int maxWidth) =>
        Assert.Equal(maxWidth, new RenderOptions { MaxWidth = maxWidth }.MaxWidth);

    [Fact]
    public void MaxWidthBelowTenThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RenderOptions { MaxWidth = 9 });
        Assert.Throws<ArgumentOutOfRangeException>(() => RenderOptions.Default with { MaxWidth = 0 });
    }

    [Fact]
    public void AVariantLeavesTheDefaultsAlone()
    {
        var fewer = RenderOptions.Default with { MaxNotes = 2 };

        Assert.Equal(2, fewer.MaxNotes);
        Assert.Equal(10, RenderOptions.Default.MaxNotes);
    }
}
