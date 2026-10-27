using System.Globalization;
using CsCheck;

namespace Creole.Tests;

public class DisplayWidthTests
{
    private static int Columns(string shown)
    {
        var columns = 0;

        for (var i = 0; i < shown.Length;)
        {
            var length = StringInfo.GetNextTextElementLength(shown, i);

            columns += DisplayWidth.OfCluster(shown.AsSpan(i, length));
            i += length;
        }

        return columns;
    }

    private static string Under(string shown, int caretColumn, int caretCount)
    {
        var under = "";
        var column = 0;

        for (var i = 0; i < shown.Length;)
        {
            var length = StringInfo.GetNextTextElementLength(shown, i);

            if (column >= caretColumn && column < caretColumn + caretCount)
            {
                under += shown.Substring(i, length);
            }

            column += DisplayWidth.OfCluster(shown.AsSpan(i, length));
            i += length;
        }

        return under;
    }

    [Theory]
    [InlineData("a", 1)]
    [InlineData("\u00E9", 1)]                                     // U+00E9, precomposed
    [InlineData("e\u0301", 1)]                                    // e + combining acute
    [InlineData("\u0301", 0)]                                     // a combining mark with no base
    [InlineData("\u200D", 0)]                                     // a lone zero-width joiner
    [InlineData("\u4E2D", 2)]                                     // CJK ideograph (W)
    [InlineData("\u3000", 2)]                                     // ideographic space (F)
    [InlineData("\uFF21", 2)]                                     // fullwidth A (F)
    [InlineData("\uFF71", 1)]                                     // halfwidth katakana (H)
    [InlineData("\u1100\u1161\u11A8", 2)]                         // Hangul syllable from conjoining jamo
    [InlineData("\U0001F44D", 2)]                                 // thumbs up (W)
    [InlineData("\U0001F44D\U0001F3FD", 2)]                       // thumbs up with a skin tone modifier
    [InlineData("\U0001F468\u200D\U0001F469\u200D\U0001F467", 2)] // family, joined by ZWJ
    [InlineData("\U0001F1EB\U0001F1F7", 2)]                       // flag of France, two regional indicators
    [InlineData("\u2764\uFE0F", 2)]                               // heart (N) with emoji presentation
    [InlineData("\u2764", 1)]                                     // heart without it
    [InlineData("\u00B1", 1)]                                     // plus-minus, East Asian Ambiguous
    [InlineData("\uD800", 1)]                                     // lone surrogate, shown as U+FFFD
    [InlineData("\u001B", 1)]                                     // ESC, shown as a Control Picture
    public void ClusterWidths(string cluster, int width) =>
        Assert.Equal(width, DisplayWidth.OfCluster(cluster));

    [Theory]
    [InlineData(0x0041, false)]
    [InlineData(0x1100, true)]    // first and last code point of the first range
    [InlineData(0x115F, true)]
    [InlineData(0x1160, false)]
    [InlineData(0x4E2D, true)]
    [InlineData(0x3400, true)]    // CJK Extension A
    [InlineData(0xFF21, true)]
    [InlineData(0x1F1E6, false)]  // regional indicators are N; DisplayWidth widens them
    [InlineData(0x1F44D, true)]
    [InlineData(0x3FFFD, true)]   // unassigned in plane 3, which defaults to W
    [InlineData(0x3FFFE, false)]
    [InlineData(0x10FFFF, false)]
    public void EastAsianWidthTableMatchesTheUcd(int codePoint, bool wide) =>
        Assert.Equal(wide, EastAsianWidth.IsWide(codePoint));

    [Fact]
    public void EastAsianWidthTableMatchesTheRuntimeUnicodeVersion()
    {
        // .NET 11 uses Unicode 16.0.0 (dotnet/runtime-assets System.Private.Runtime.UnicodeData).
        // When the runtime moves on, regenerate with scripts/gen-east-asian-width.py --version <new>.
        Assert.Equal("16.0.0", EastAsianWidth.UnicodeVersion);
    }

    [Theory]
    [InlineData("\tx", 1, 2, "    x", 4)]
    [InlineData("ab\tx", 3, 4, "ab  x", 4)]
    [InlineData("abcd\tx", 5, 6, "abcd    x", 8)]
    [InlineData("a\tb\tx", 4, 5, "a   b   x", 8)]
    public void TabsMoveToTheNextStop(string line, int start, int end, string shown, int caretColumn) =>
        Assert.Equal((shown, caretColumn, 1), DisplayWidth.ShowLine(line, start, end, int.MaxValue));

    [Fact]
    public void TabsUnderTheSpanTakeTheirExpandedWidth() =>
        Assert.Equal(("ab  x", 2, 2), DisplayWidth.ShowLine("ab\tx", 2, 3, int.MaxValue));

    [Fact]
    public void WideCharactersTakeTwoColumns()
    {
        Assert.Equal(("中文x", 4, 1), DisplayWidth.ShowLine("中文x", 2, 3, int.MaxValue));
        Assert.Equal(("中文x", 0, 4), DisplayWidth.ShowLine("中文x", 0, 2, int.MaxValue));
        Assert.Equal(("a\U0001F44Db", 3, 1), DisplayWidth.ShowLine("a\U0001F44Db", 3, 4, int.MaxValue));
    }

    [Fact]
    public void CombiningMarksTakeNoColumn()
    {
        Assert.Equal(("e\u0301x", 1, 1), DisplayWidth.ShowLine("e\u0301x", 2, 3, int.MaxValue));
        Assert.Equal(("e\u0301x", 0, 1), DisplayWidth.ShowLine("e\u0301x", 0, 2, int.MaxValue));
    }

    [Fact]
    public void ARangeInsideAClusterMarksTheWholeCluster()
    {
        // Offset 1 is the combining mark; offset 2 is the second half of a surrogate pair.
        Assert.Equal(("e\u0301x", 0, 1), DisplayWidth.ShowLine("e\u0301x", 1, 2, int.MaxValue));
        Assert.Equal(("a\U0001F44Db", 1, 2), DisplayWidth.ShowLine("a\U0001F44Db", 2, 3, int.MaxValue));
        Assert.Equal(("a\U0001F468\u200D\U0001F469b", 1, 2), DisplayWidth.ShowLine("a\U0001F468\u200D\U0001F469b", 3, 4, int.MaxValue));
    }

    [Fact]
    public void AnEmptyRangeMarksTheClusterItIsIn()
    {
        Assert.Equal(("abc", 1, 1), DisplayWidth.ShowLine("abc", 1, 1, int.MaxValue));
        Assert.Equal(("a中c", 1, 2), DisplayWidth.ShowLine("a中c", 1, 1, int.MaxValue));
        Assert.Equal(("e\u0301x", 0, 1), DisplayWidth.ShowLine("e\u0301x", 1, 1, int.MaxValue));
    }

    [Fact]
    public void ARangeAtOrPastTheEndPutsOneCaretAfterTheLine()
    {
        Assert.Equal(("abc", 3, 1), DisplayWidth.ShowLine("abc", 3, 3, int.MaxValue));
        Assert.Equal(("abc", 3, 1), DisplayWidth.ShowLine("abc", 3, 5, int.MaxValue));
        Assert.Equal(("a中", 3, 1), DisplayWidth.ShowLine("a中", 2, 2, int.MaxValue));
        Assert.Equal(("", 0, 1), DisplayWidth.ShowLine("", 0, 0, int.MaxValue));
    }

    [Fact]
    public void ARangePastTheEndIsCutAtTheEnd() =>
        Assert.Equal(("abc", 1, 2), DisplayWidth.ShowLine("abc", 1, 9, int.MaxValue));

    [Fact]
    public void ARangeOverOnlyZeroWidthClustersStillGetsOneCaret()
    {
        Assert.Equal(("\u200Db", 0, 1), DisplayWidth.ShowLine("\u200Db", 0, 1, int.MaxValue));
        Assert.Equal(("\u0301b", 0, 1), DisplayWidth.ShowLine("\u0301b", 0, 1, int.MaxValue));
    }

    [Fact]
    public void ControlCharactersAreShownAsPictures()
    {
        Assert.Equal(("a\u241Bb", 2, 1), DisplayWidth.ShowLine("a\u001Bb", 2, 3, int.MaxValue));
        Assert.Equal(("\u2400\u2421", 1, 1), DisplayWidth.ShowLine("\u0000\u007F", 1, 2, int.MaxValue));
        Assert.Equal(("a\uFFFDb", 2, 1), DisplayWidth.ShowLine("a\u009Bb", 2, 3, int.MaxValue));
    }

    [Fact]
    public void ALineThatFitsIsShownWhole()
    {
        Assert.Equal(("abcdefghij", 3, 1), DisplayWidth.ShowLine("abcdefghij", 3, 4, 10));
        Assert.Equal(("abcdefghi", 9, 1), DisplayWidth.ShowLine("abcdefghi", 9, 9, 10));
    }

    [Theory]
    [InlineData(12, 13, "…jklmnopq…", 4, 1)]   // centred: three cells before the span, four after
    [InlineData(1, 2, "abcdefghi…", 1, 1)]     // near the start, the window starts with the line
    [InlineData(24, 25, "…rstuvwxyz", 8, 1)]   // near the end, it ends with the line
    [InlineData(26, 26, "…stuvwxyz", 9, 1)]    // a caret after the line takes the last cell
    [InlineData(5, 20, "…fghijklm…", 1, 8)]    // a wider span shows its start, with carets to the edge
    [InlineData(0, 20, "abcdefghi…", 0, 9)]
    public void ALongLineIsShownAsAWindowAroundTheSpan(int start, int end, string shown, int caretColumn, int caretCount) =>
        Assert.Equal((shown, caretColumn, caretCount), DisplayWidth.ShowLine("abcdefghijklmnopqrstuvwxyz", start, end, 10));

    [Fact]
    public void TabsInAWindowKeepTheirStopsFromTheWholeLine()
    {
        var line = $"{new string('x', 15)}\txy{new string('z', 20)}";

        Assert.Equal(("…x xyzzzz…", 4, 1), DisplayWidth.ShowLine(line, 17, 18, 10));
    }

    [Fact]
    public void AClusterThatWouldCrossAnEdgeIsLeftOut()
    {
        var tab = $"{new string('x', 20)}\ty{new string('z', 20)}";
        var wide = string.Concat(Enumerable.Repeat("\u4E2D", 20));

        Assert.Equal(("…yzzzz…", 1, 1), DisplayWidth.ShowLine(tab, 21, 22, 10));
        Assert.Equal(("…\u4E2D\u4E2D\u4E2D…", 3, 2), DisplayWidth.ShowLine(wide, 10, 11, 10));
    }

    [Fact]
    public void ClustersWithNoWidthTakeACellEach()
    {
        var line = $"{new string('\u200B', 30)}x";

        Assert.Equal(($"…{new string('\u200B', 8)}x", 1, 1), DisplayWidth.ShowLine(line, 30, 31, 10));
    }

    [Fact]
    public void AClusterShowsAtMost32ScalarValues()
    {
        var marks = new string('\u0301', 40);

        Assert.Equal(($"a{marks[..31]}x", 1, 1), DisplayWidth.ShowLine($"a{marks}x", 41, 42, int.MaxValue));
        Assert.Equal($"a{marks[..31]}", DisplayWidth.Cut($"a{marks}", 10));
    }

    [Theory]
    [InlineData("short", "short")]
    [InlineData("exactly 10", "exactly 10")]
    [InlineData("eleven char", "eleven ch…")]
    [InlineData("\u4E2D\u4E2D\u4E2D\u4E2D\u4E2D\u4E2D", "\u4E2D\u4E2D\u4E2D\u4E2D…")]
    [InlineData("a\u001Bb\nc", "a\u241Bb\u240Ac")]
    public void CutKeepsTextWithinTheWidth(string text, string shown) =>
        Assert.Equal(shown, DisplayWidth.Cut(text, 10));

    [Fact]
    public void CutShowsTheStartOfALongText() =>
        Assert.Equal($"{new string('a', 9)}…", DisplayWidth.Cut(new string('a', 600_000), 10));

    [Fact]
    public void AWindowShowsTheStartOfTheSpanUnderItsCarets()
    {
        var gen =
            from parts in Gen.OneOf("a", "b", "c", "\t", "\u4E2D", "\U0001F44D").Array[0, 80]
            let line = string.Concat(parts)
            from start in Gen.Int[0, line.Length]
            from end in Gen.Int[start, line.Length]
            from maxWidth in Gen.Int[10, 40]
            select (line, start, end, maxWidth);

        gen.Sample(sample =>
        {
            var (line, start, end, maxWidth) = sample;
            var (wholeText, wholeCaretColumn, wholeCaretCount) = DisplayWidth.ShowLine(line, start, end, int.MaxValue);
            var (windowText, windowCaretColumn, windowCaretCount) = DisplayWidth.ShowLine(line, start, end, maxWidth);
            var marked = Under(wholeText, wholeCaretColumn, wholeCaretCount);
            var underCarets = Under(windowText, windowCaretColumn, windowCaretCount);

            return Columns(windowText) <= maxWidth
                && windowCaretColumn + windowCaretCount <= maxWidth
                && marked.StartsWith(underCarets, StringComparison.Ordinal)
                && (underCarets.Length > 0 || marked.Length == 0)
                && (wholeCaretCount > maxWidth - 2 || underCarets == marked);
        });
    }

    [Fact]
    public void LoneSurrogatesAreShownAsReplacementCharacters()
    {
        Assert.Equal(("a\uFFFDb", 2, 1), DisplayWidth.ShowLine("a\uD800b", 2, 3, int.MaxValue));
        Assert.Equal(("\uFFFDb", 1, 1), DisplayWidth.ShowLine("\uDC00b", 1, 2, int.MaxValue));
    }
}
