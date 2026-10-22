using System.Text;

namespace Creole.Tests;

file enum TokenKind
{
    Identifier,
    RawString,
    UnterminatedRawString,
}

file readonly record struct RawStringLiteral(string Text, bool IsTerminated);

file readonly record struct Token(TokenKind Kind, string Text);

file sealed class RawStringLexer : CharGrammar
{
    public static readonly Parser<char, RawStringLiteral> RawString = Custom(new RawStringScanner());

    public static readonly Parser<char, Token> Identifier = LetterOrDigit.AtLeastOnce().Text().Select(text => new Token(TokenKind.Identifier, text));

    public static readonly Parser<char, Token> RawStringToken = RawString.Select(s => new Token(s.IsTerminated ? TokenKind.RawString : TokenKind.UnterminatedRawString, s.Text));

    public static readonly Parser<char, Token> AnyToken = Longest(RawStringToken, Identifier);

    public static readonly Parser<char, Token> CheckedToken = AnyToken.Bind(t => t.Kind == TokenKind.UnterminatedRawString ? Fail<Token>("unterminated raw string") : Return(t));
}

file sealed class RawStringScanner : IScanner<char, RawStringLiteral>
{
    public ScannerInfo Info { get; } = new(Nullable: false, First: CharSet.Of('r'), Name: "raw string");

    private static bool TryReadHashes(ref Cursor<char> cursor, int hashes)
    {
        var c = cursor;

        for (var i = 0; i < hashes; i++)
        {
            if (!c.TryRead(out var hash) || hash != '#')
            {
                return false;
            }
        }

        cursor = c;
        return true;
    }

    public bool Scan(ref Cursor<char> cursor, out RawStringLiteral value)
    {
        var c = cursor;
        value = default;

        if (!c.TryRead(out var r) || r != 'r')
        {
            return false;
        }

        var hashes = 0;

        while (c.TryPeek(out var hash) && hash == '#')
        {
            c.Advance(1);
            hashes++;
        }

        if (!c.TryRead(out var quote) || quote != '"')
        {
            return false;
        }

        var text = new StringBuilder();

        while (c.TryRead(out var next))
        {
            if (next == '"' && TryReadHashes(ref c, hashes))
            {
                cursor = c;
                value = new(text.ToString(), IsTerminated: true);
                return true;
            }

            text.Append(next);
        }

        cursor = c;
        value = new(text.ToString(), IsTerminated: false);
        return true;
    }
}

public class RawStringSampleTests
{
    public static TheoryData<int> SegmentSizes => [1, 2, 3, 5];

    private static (bool Matched, string? Text, bool IsTerminated, long Position) Scan(string input)
    {
        var cursor = new Cursor<char>(input);
        var matched = new RawStringScanner().Scan(ref cursor, out var value);

        return (matched, value.Text, value.IsTerminated, cursor.Position);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("")]
    [InlineData("r")]
    [InlineData("r#")]
    [InlineData("r#x")]
    [InlineData("rabc")]
    public void AMalformedOpenerFailsWithoutConsuming(string input) =>
        Assert.Equal((false, null, false, 0L), Scan(input));

    [Fact]
    public void AnUnterminatedStringSucceedsAndIsMarked() =>
        Assert.Equal((true, "abc\"", false, 7L), Scan("r#\"abc\""));

    [Fact]
    public void TheLexerChecksForAnUnterminatedStringAfterLongest()
    {
        var check = Assert.IsType<BindNode<char, Token, Token, Token>>(RawStringLexer.CheckedToken);

        Assert.Same(RawStringLexer.AnyToken, check.Source);
    }

    [Fact]
    public void TheScannerIsAnAlternativeInsideLongest()
    {
        INode token = Assert.IsType<LongestNode<char, Token>>(RawStringLexer.AnyToken);
        var rawString = Assert.IsType<CustomNode<char, RawStringLiteral>>(Assert.Single(token.Children[0].Children));

        Assert.Same(RawStringLexer.RawString, rawString);
        Assert.Same(RawStringLexer.Identifier, token.Children[1]);
        Assert.Equal(FirstSet.OfChars(CharSet.Of('r')), rawString.First);
        Assert.Equal("raw string", rawString.Name);
    }

    [Theory]
    [InlineData("r\"abc\"", "abc", 6)]
    [InlineData("r\"\"", "", 3)]
    [InlineData("r#\"a\"b\"#", "a\"b", 8)]
    [InlineData("r##\"x\"#y\"##", "x\"#y", 11)]
    [InlineData("r\"a\"rest", "a", 4)]
    public void ThreadsTheDelimitersAndStopsAfterTheClosingOne(string input, string text, long end) =>
        Assert.Equal((true, text, true, end), Scan(input));

    [Theory]
    [MemberData(nameof(SegmentSizes))]
    public void WorksAcrossSegments(int segmentSize)
    {
        var input = "r##\"x\"#y\"##tail";
        var cursor = new Cursor<char>(new SegmentedSource<char>(input.ToCharArray(), segmentSize));

        Assert.True(new RawStringScanner().Scan(ref cursor, out var value));
        Assert.Equal(new RawStringLiteral("x\"#y", IsTerminated: true), value);
        Assert.Equal(11, cursor.Position);
    }
}
