using System.Text;
using static Creole.Grammar<string>;

namespace Creole.Tests;

/// <summary>
/// Errors from each layer of a pipeline over UTF-8 bytes: the decoder, a lexer above it, and a parser above the lexer.
/// </summary>
/// <remarks>
/// Each source starts with a byte-order mark and ends its lines with CRLF, and characters of two, three or four bytes come before each error, so byte offsets, character offsets and columns all differ.
/// Each error's span is checked in bytes and characters, and its line and column by rendering it against the decoded text, as a parse over input held in memory does.
/// </remarks>
public class ByteSourceDiagnosticsTests
{
    private static readonly byte[] _byteOrderMark = [0xEF, 0xBB, 0xBF];

    private static readonly Layer<byte, string> _lexer = Layer.Utf8().Then(
        Layer.Tokens(
            CharGrammar.Longest(
                CharGrammar.Letter.AtLeastOnce().Text(),
                CharGrammar.Digit.AtLeastOnce().Text(),
                CharGrammar.String("="),
                CharGrammar.Satisfy(static c => c != '"').SkipMany().Between(CharGrammar.Char('"'), CharGrammar.Char('"')).Text()
            ),
            skip: CharGrammar.Whitespace.SkipMany()
        )
    );

    private static readonly Parser<string, IReadOnlyList<string>> _settings = Satisfy(static token => char.IsLetter(token[0])).Named("name")
        .Before(Token("="))
        .Then(Satisfy(static token => char.IsDigit(token[0]) || token[0] == '"').Named("value"))
        .Many()
        .Before(End);

    private static string Render(ParseError error, byte[] bytes) =>
        new Diagnostics([error]).Render(new SourceText(Encoding.UTF8.GetString(bytes.AsSpan(_byteOrderMark.Length)), "settings.txt"));

    private static byte[] Source(string text) =>
        [.. _byteOrderMark, .. Encoding.UTF8.GetBytes(text)];

    [Fact]
    public void ADecoderErrorReportsItsBytesAndItsLineAndColumn()
    {
        byte[] bytes = [.. Source("naïve = 1\r\ngrö"), 0xFF, .. Encoding.UTF8.GetBytes("ße = 2\r\n")];
        var error = LayerRun.Error(_lexer, bytes);

        Assert.Equal(new SourceSpan(SourceId.Unnamed, 19, 20, 14, 14), error.Span);
        Assert.Equal(
            """
            error[decoder]: invalid UTF-8 byte FF
             --> settings.txt:2:4
              |
            2 | grö�ße = 2
              |    ^
            """.ReplaceLineEndings("\n"),
            Render(error, bytes)
        );
    }

    [Fact]
    public void ALexerErrorAboveTheDecoderReportsItsBytesAndItsLineAndColumn()
    {
        var bytes = Source("naïve = 1\r\ngröße = 2€\r\n");
        var error = LayerRun.Error(_lexer, bytes);

        Assert.Equal(new SourceSpan(SourceId.Unnamed, 26, 29, 20, 21), error.Span);
        Assert.Equal(
            """
            error[lexer]: expected whitespace, letter, digit, "=" or '"'
             --> settings.txt:2:10
              |
            2 | größe = 2€
              |          ^ unexpected '€'
            """.ReplaceLineEndings("\n"),
            Render(error, bytes)
        );
    }

    [Fact]
    public void AParserErrorTwoLayersAboveReportsItsBytesAndItsLineAndColumn()
    {
        var bytes = Source("naïve = 1\r\nemoji = \"😀\" 7\r\n");
        var error = LayerRun.Error(_lexer, _settings, bytes)!;

        Assert.Equal(new SourceSpan(SourceId.Unnamed, 30, 31, 24, 25), error.Span);
        Assert.Equal(
            """
            error[parser]: expected name or end of input
             --> settings.txt:2:13
              |
            2 | emoji = "😀" 7
              |              ^ unexpected 7
            """.ReplaceLineEndings("\n"),
            Render(error, bytes)
        );
    }
}
