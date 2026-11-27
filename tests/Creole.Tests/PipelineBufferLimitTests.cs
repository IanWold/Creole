using System.Text;
using static Creole.CharGrammar;
using WordGrammar = Creole.Grammar<string>;

namespace Creole.Tests;

/// <summary>
/// <see cref="ParseOptions.MaxBufferedItems"/> through the public pipeline API: every buffer a pipeline builds is bounded, from the root a reader or a stream fills to the buffer the parser reads, and a root reads no further ahead than the limit allows.
/// </summary>
public class PipelineBufferLimitTests
{
    private const int Limit = 100;

    private static readonly Parser<string, Unit> _holdingEveryWord = WordGrammar.Lookahead(WordGrammar.Any.SkipMany()).Then(WordGrammar.Any.SkipMany());

    private static readonly ParseOptions _limited = new() { MaxBufferedItems = Limit };

    private static readonly string _text = string.Join(' ', Enumerable.Repeat("ab", 1_000));

    private static readonly Layer<char, string> _words = Layer.Tokens(Letter.AtLeastOnce().Text(), skip: Whitespace.SkipMany());

    private static readonly Pipeline<byte, string> _eachWordFromBytes = Pipeline.From(Layer.Utf8().Then(_words)).Then(WordGrammar.Any);

    private static readonly Pipeline<char, string> _eachWordFromText = Pipeline.From(_words).Then(WordGrammar.Any);

    private static readonly Pipeline<byte, Unit> _fromBytes = Pipeline.From(Layer.Utf8().Then(_words)).Then(_holdingEveryWord);

    private static readonly Pipeline<char, Unit> _fromText = Pipeline.From(_words).Then(_holdingEveryWord);

    private static string Error<T>(ParseResult<T> result) =>
        Assert.Single(result.Diagnostics).ToString();

    private static string Message(string layer, string span) =>
        $"error[{layer}] at {span}: buffered input from here exceeds ParseOptions.MaxBufferedItems of {Limit}";

    [Fact]
    public void ALexerAfterTheDecoderHoldsAtMostTheLimitOfCharacters() =>
        Assert.Equal(Message("lexer", "0..1"), Error(_fromBytes.Parse(Encoding.UTF8.GetBytes(_text), options: _limited)));

    [Fact]
    public void ALexerOverAReaderHoldsAtMostTheLimitOfCharacters() =>
        Assert.Equal(Message("lexer", "0..1"), Error(_fromText.Parse(new StringReader(_text), options: _limited)));

    [Fact]
    public void AParserOverCharactersInMemoryHoldsAtMostTheLimitOfTokens() =>
        Assert.Equal(Message("parser", "0..2"), Error(_fromText.Parse(_text.AsMemory(), options: _limited)));

    [Fact]
    public void AParserOverTextHoldsAtMostTheLimitOfTokens() =>
        Assert.Equal(Message("parser", "0..2"), Error(_fromText.Parse(_text, options: _limited)));

    [Fact]
    public void AReaderReadsNoFurtherAheadThanTheLimitAllows() =>
        Assert.Equal(1_000, _eachWordFromText.ParseMany(new StringReader(_text), options: _limited).Count(static result => result is Parsed<string>));

    [Fact]
    public void AStreamReadsNoFurtherAheadThanTheLimitAllows() =>
        Assert.Equal(1_000, _eachWordFromBytes.ParseMany(new MemoryStream(Encoding.UTF8.GetBytes(_text)), options: _limited).Count(static result => result is Parsed<string>));

    [Fact]
    public void EveryPipelineParsesUnderTheDefaultLimit() =>
        Assert.All(
            [
                _fromText.Parse(_text),
                _fromText.Parse(new StringReader(_text)),
                _fromBytes.Parse(Encoding.UTF8.GetBytes(_text)),
                _fromBytes.Parse(new MemoryStream(Encoding.UTF8.GetBytes(_text))),
                Assert.Single(_fromText.ParseMany(_text)),
            ],
            static result => Assert.True(result is Parsed<Unit>)
        );

    [Fact]
    public void ParseManyEndsWithTheLimitsError() =>
        Assert.Equal(Message("parser", "0..2"), Error(Assert.Single(_fromText.ParseMany(_text, options: _limited))));

    [Fact]
    public void TheDecoderOverAStreamHoldsAtMostTheLimitOfBytes() =>
        Assert.Equal(Message("decoder", "0..0"), Error(_fromBytes.Parse(new MemoryStream(Encoding.UTF8.GetBytes(_text)), options: _limited)));
}
