using System.Text;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;
using StringGrammar = Creole.Grammar<string>;

namespace Creole.Tests;

/// <summary>
/// <see cref="ParseOptions.MaxBufferedItems"/> in producing layers: the layer that reads a buffer reports its limit as its own error, and a lower layer reaches it first when it keeps more input for the layer above.
/// </summary>
public class LayerBufferLimitTests
{
    private const int Limit = 1000;

    private static readonly ParseOptions _limited = new() { MaxBufferedItems = Limit };

    private static readonly Parser<char, string> _unterminated = Char('"').Then(Letter.SkipMany()).Then(Char('"')).Text();

    private static readonly char[] _unterminatedAfterX = ("x\"" + new string('a', 100_000)).ToCharArray();

    private static SourceSpan At(long position) =>
        new(SourceId.Unnamed, position, position + 1);

    private static ParseError Error<TIn, T, TOut>(Layer<TIn, T> layer, Parser<T, TOut> parser, TIn[] input, ParseOptions options) =>
        Assert.Throws<LayerFailedException>(() => LayerRun.Run(layer, parser, input, options: options)).Error;

    private static ParseError Expected(SourceSpan span, string layer, int limit = Limit) =>
        new(span, layer, new CustomMessage($"buffered input from here exceeds ParseOptions.MaxBufferedItems of {limit}"), [], []);

    [Fact]
    public void ALexerStepThatNeverEndsFailsAtItsStart() =>
        Assert.Equal(
            Expected(At(1), "lexer"),
            Error(Layer.Tokens(Letter.AtLeastOnce().Text().Or(_unterminated)), StringGrammar.Any.SkipMany(), _unterminatedAfterX, _limited)
        );

    [Fact]
    public void ALexersLimitErrorReadsOnOneLineAsTheDocsShow() =>
        Assert.Equal(
            "error[lexer] at 1..2: buffered input from here exceeds ParseOptions.MaxBufferedItems of 1000",
            Error(Layer.Tokens(Letter.AtLeastOnce().Text().Or(_unterminated)), StringGrammar.Any.SkipMany(), _unterminatedAfterX, _limited).ToString()
        );

    [Fact]
    public void ATransduceStepThatNeverEndsFailsAtItsStart() =>
        Assert.Equal(
            Expected(At(1), "strings"),
            Error(
                Layer.Transduce(0, OneOf(Letter.Select(static c => Emit.One(c)), _unterminated.Select(static _ => Emit.One('s'))), name: "strings"),
                Any.SkipMany(),
                _unterminatedAfterX,
                _limited
            )
        );

    [Theory]
    [InlineData(999)]
    [InlineData(1000)]
    [InlineData(1001)]
    public void ATransduceLayerReportsTheLimitWhereverItsStepPulls(int limit) =>
        Assert.Equal(
            Expected(At(0), "layout", limit),
            Error(
                Layer.Transduce(0, Any.Then(Any).Then(Any).Select<Emit<char>>(static c => [c, Emit.Synthetic('#')])),
                Lookahead(Any.SkipMany()),
                new string('a', 100_000).ToCharArray(),
                new ParseOptions { MaxBufferedItems = limit }
            )
        );

    [Fact]
    public void AParsersLookaheadMakesTheLexerReachTheLimitFirst() =>
        Assert.Equal(
            Expected(At(3), "lexer"),
            Error(
                Layer.Tokens(Letter.AtLeastOnce().Text(), skip: Char(' ').SkipMany()),
                StringGrammar.Any.Then(StringGrammar.Any).Then(StringGrammar.Lookahead(StringGrammar.Any.SkipMany())),
                string.Join(' ', Enumerable.Repeat("a", 100_000)).ToCharArray(),
                _limited
            )
        );

    [Fact]
    public void ALexerWhoseStepsPullOnlyAsTheyStartReportsTheLimitAsItsOwn() =>
        Assert.Equal(
            Expected(At(2), "lexer"),
            Error(
                Layer.Tokens(Letter.Select(static c => c.ToString())),
                StringGrammar.Any.Then(StringGrammar.Any).Then(StringGrammar.Lookahead(StringGrammar.Any.SkipMany())),
                new string('a', 100_000).ToCharArray(),
                _limited
            )
        );

    [Fact]
    public void ATransduceLayerWhoseStepsPullOnlyAsTheyStartReportsTheLimitAsItsOwn() =>
        Assert.Equal(
            Expected(At(2), "layout"),
            Error(
                Layer.Transduce(0, Any.Select(static c => Emit.One(c))),
                Any.Then(Any).Then(Lookahead(Any.SkipMany())),
                new string('a', 100_000).ToCharArray(),
                _limited
            )
        );

    [Fact]
    public void AWhereKeepingInputForAPinnedReaderReportsItsOwnLimit() =>
        Assert.Equal(
            Expected(At(4), "where"),
            Error(
                Layer.Where<char>(static c => c != ' '),
                Char('a').Then(Char('a')).Then(Lookahead(Char('a').SkipMany())),
                string.Join(' ', Enumerable.Repeat("a", 100_000)).ToCharArray(),
                _limited
            )
        );

    [Fact]
    public void ASelectKeepingInputForAPinnedReaderReportsItsOwnLimit() =>
        Assert.Equal(
            Expected(At(2), "select"),
            Error(
                Layer.Select<char, char>(static c => char.ToUpperInvariant(c)),
                Char('A').Then(Char('A')).Then(Lookahead(Char('A').SkipMany())),
                new string('a', 100_000).ToCharArray(),
                _limited
            )
        );

    [Fact]
    public void TheDecoderReportsItsLimitAtTheCharacterThePinnedReaderCanGoBackTo() =>
        Assert.Equal(
            Expected(new SourceSpan(SourceId.Unnamed, 4, 5, 2, 2), "decoder"),
            Error(
                Layer.Utf8(),
                Char('é').Then(Char('é')).Then(Lookahead(Char('é').SkipMany())),
                Encoding.UTF8.GetBytes(new string('é', 100_000)),
                _limited
            )
        );

    [Fact]
    public void TheDecoderReportsALimitInsideOneSequenceAtTheCharactersDecodedSoFar() =>
        Assert.Equal(
            Expected(new SourceSpan(SourceId.Unnamed, 1, 2, 1, 1), "decoder", limit: 1),
            Error(Layer.Utf8(), Any.SkipMany(), Encoding.UTF8.GetBytes("aé"), new ParseOptions { MaxBufferedItems = 1 })
        );
}
