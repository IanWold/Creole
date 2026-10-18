using CsCheck;

namespace Creole.Tests;

public class LineMapTests
{
    [Theory]
    [InlineData("a\nb", 2, 2, 1)]
    [InlineData("a\r\nb", 3, 2, 1)]
    [InlineData("a\rb", 2, 2, 1)]
    [InlineData("a\nb\r\nc\rd", 7, 4, 1)]
    [InlineData("a\nb\r\nc\rd", 4, 2, 3)]
    [InlineData("a\nb\r\nc\rd", 3, 2, 2)]
    [InlineData("ab\n\ncd", 4, 3, 1)]
    [InlineData("\r\r\n\n", 4, 4, 1)]
    public void FindsLineAndColumnForEachLineEnding(string text, long offset, long line, long column)
    {
        var map = LineMap.Create(text);

        Assert.Equal(new LinePosition(line, column), map.GetPosition(offset));
    }

    [Fact]
    public void DocsSample()
    {
        var map = LineMap.Create("{\r\n  \"port\": 8080\r\n}");

        Assert.Equal("2:3", map.GetPosition(5).ToString());
        Assert.Equal(3, map.LineCount);
    }

    [Fact]
    public void LfOfCrlfIsOnTheCrLine()
    {
        var map = LineMap.Create("ab\r\ncd");

        Assert.Equal(new LinePosition(1, 3), map.GetPosition(2));
        Assert.Equal(new LinePosition(1, 4), map.GetPosition(3));
        Assert.Equal(new LinePosition(2, 1), map.GetPosition(4));
    }

    [Theory]
    [InlineData("", 1)]
    [InlineData("abc", 1)]
    [InlineData("abc\n", 2)]
    [InlineData("abc\r\n", 2)]
    [InlineData("abc\r", 2)]
    [InlineData("\n\n", 3)]
    public void CountsLines(string text, long lineCount) =>
        Assert.Equal(lineCount, LineMap.Create(text).LineCount);

    [Fact]
    public void EndOfTextIsAPosition()
    {
        var map = LineMap.Create("ab\ncd\n");

        Assert.Equal(new LinePosition(3, 1), map.GetPosition(6));
        Assert.Equal(new LinePosition(1, 1), LineMap.Create("").GetPosition(0));
    }

    [Fact]
    public void ColumnsCountScalarValues()
    {
        var map = LineMap.Create("x\U0001F600y");

        Assert.Equal(new LinePosition(1, 2), map.GetPosition(1));
        Assert.Equal(new LinePosition(1, 2), map.GetPosition(2));
        Assert.Equal(new LinePosition(1, 3), map.GetPosition(3));
        Assert.Equal(new LinePosition(1, 4), map.GetPosition(4));
    }

    [Fact]
    public void LoneSurrogatesCountAsOneColumnEach()
    {
        var map = LineMap.Create("\uD800a\uDC00b");

        Assert.Equal(new LinePosition(1, 4), map.GetPosition(3));
    }

    [Fact]
    public void OtherUnicodeSeparatorsDoNotEndALine()
    {
        var map = LineMap.Create($"a{(char)0x85}b{(char)0x2028}c{(char)0x2029}d");

        Assert.Equal(1, map.LineCount);
        Assert.Equal(new LinePosition(1, 7), map.GetPosition(6));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void OffsetOutsideTheTextThrows(long offset) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => LineMap.Create("abc").GetPosition(offset));

    [Fact]
    public void CrlfSplitAcrossChunksIsOneBreak()
    {
        var builder = new LineMap.Builder();

        builder.Append("a\r");
        builder.Append("\nb");

        var map = builder.Build();

        Assert.Equal(2, map.LineCount);
        Assert.Equal(new LinePosition(2, 1), map.GetPosition(3));
    }

    [Fact]
    public void CrAtTheEndOfTheLastChunkEndsALine()
    {
        var builder = new LineMap.Builder();

        builder.Append("a\r");

        var map = builder.Build();

        Assert.Equal(2, map.LineCount);
        Assert.Equal(new LinePosition(2, 1), map.GetPosition(2));
    }

    [Fact]
    public void BuilderCanContinueAfterBuild()
    {
        var builder = new LineMap.Builder();

        builder.Append("a\r");
        _ = builder.Build();
        builder.Append("\nb");

        Assert.Equal(2, builder.Build().LineCount);
    }

    [Fact]
    public void SurrogatePairSplitAcrossChunksIsOneColumn()
    {
        var builder = new LineMap.Builder();

        builder.Append("x\uD83D");
        builder.Append("\uDE00y");

        Assert.Equal(new LinePosition(1, 3), builder.Build().GetPosition(3));
    }

    [Fact]
    public void MatchesALineByLineModelForAnyChunking() =>
        Gen.OneOfConst("a", "b", "\r", "\n", "\r\n", "\U0001F600", "\uD800", "\uDC00")
            .Array[0, 40]
            .Select(Gen.Int[1, 7])
            .Sample((parts, chunkSize) =>
            {
                var text = string.Concat(parts);
                var builder = new LineMap.Builder();

                for (var i = 0; i < text.Length; i += chunkSize)
                {
                    builder.Append(text.AsSpan(i, Math.Min(chunkSize, text.Length - i)));
                }

                var map = builder.Build();
                var expected = ModelPositions(text);

                return map.Length == text.Length
                    && Enumerable.Range(0, text.Length + 1).All(offset => map.GetPosition(offset) == expected[offset]);
            });

    [Fact]
    public void OffsetsPast2GBAreCorrect()
    {
        const int chunkSize = 1 << 20;
        const int chunkCount = 2100;

        var chunk = new string('a', chunkSize).ToCharArray();

        chunk[100] = '\n';
        chunk[200] = '\r';
        chunk[201] = '\n';
        chunk[300] = '\r';
        chunk[400] = '\uD83D';
        chunk[401] = '\uDE00';

        var builder = new LineMap.Builder();

        for (var i = 0; i < chunkCount; i++)
        {
            builder.Append(chunk);
        }

        var map = builder.Build();

        const long lastChunk = (long)(chunkCount - 1) * chunkSize;

        Assert.True(lastChunk > int.MaxValue);
        Assert.Equal((long)chunkCount * chunkSize, map.Length);
        Assert.Equal(3L * chunkCount + 1, map.LineCount);
        Assert.Equal(new LinePosition(3L * (chunkCount - 1) + 1, chunkSize - 302), map.GetPosition(lastChunk - 1));
        Assert.Equal(new LinePosition(3L * (chunkCount - 1) + 1, chunkSize - 301), map.GetPosition(lastChunk));
        Assert.Equal(new LinePosition(3L * (chunkCount - 1) + 2, 1), map.GetPosition(lastChunk + 101));
        Assert.Equal(new LinePosition(3L * (chunkCount - 1) + 3, 1), map.GetPosition(lastChunk + 202));
        Assert.Equal(new LinePosition(3L * (chunkCount - 1) + 4, 100), map.GetPosition(lastChunk + 400));
        Assert.Equal(new LinePosition(3L * (chunkCount - 1) + 4, 101), map.GetPosition(lastChunk + 402));
        Assert.Equal(new LinePosition(3L * chunkCount + 1, chunkSize - 402 + 100 + 1), map.GetPosition(map.Length));
    }

    private static LinePosition[] ModelPositions(string text)
    {
        var positions = new LinePosition[text.Length + 1];

        long line = 1;
        long column = 1;

        for (var i = 0; i < text.Length; i++)
        {
            positions[i] = new LinePosition(line, column);

            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                positions[++i] = new LinePosition(line, column);
                column++;
            }
            else if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
            {
                positions[++i] = new LinePosition(line, column + 1);
                line++;
                column = 1;
            }
            else if (text[i] is '\r' or '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        positions[text.Length] = new LinePosition(line, column);
        return positions;
    }
}
