using static Creole.CharGrammar;
using static Creole.Grammar<char>;
using WordGrammar = Creole.Grammar<string>;

namespace Creole.Tests;

/// <summary>
/// A reader that gives one character for each read, and counts how many it has given.
/// </summary>
file sealed class TrickleReader(string text) : TextReader
{
    private readonly string _text = text;

    public int Consumed { get; private set; }

    public override int Peek() =>
        Consumed < _text.Length
        ? _text[Consumed]
        : -1;

    public override int Read() =>
        Consumed < _text.Length
        ? _text[Consumed++]
        : -1;

    public override int Read(Span<char> buffer)
    {
        if (Consumed == _text.Length || buffer.IsEmpty)
        {
            return 0;
        }

        buffer[0] = _text[Consumed++];

        return 1;
    }
}

public class ParseManyTests
{
    private static readonly Layer<char, string> _words = Layer.Tokens(Letter.AtLeastOnce().Text(), skip: Whitespace.SkipMany());

    private static readonly Pipeline<char, string> _word = Pipeline.From(_words).Then(WordGrammar.Any);

    private static readonly Pipeline<char, string> _a = Pipeline.From(_words).Then(WordGrammar.Token("a"));

    private static string Describe<T>(ParseResult<T> result) => result switch
    {
        Parsed<T> parsed => $"{parsed.Value}",
        Recovered<T> recovered => $"recovered {recovered.Value}",
        Failed failed => string.Join("; ", failed.Diagnostics),
    };

    [Fact]
    public void AFailedItemIsTheLastResult() =>
        Assert.Equal(["a", "a", "error[parser] at 4..5: unexpected b; expected a"], _a.ParseMany("a a b a").Select(Describe));

    [Fact]
    public void AFailedResultFromAReaderRendersOnOneLine()
    {
        var failed = _a.ParseMany(new StringReader("a\nb")).Last();

        Assert.Equal("error[parser] at 2..3: unexpected b; expected a", failed.Diagnostics.Render());
    }

    [Fact]
    public void AFailedResultHoldsTheTextUnderItsName() =>
        Assert.Equal(
            """
            error[parser]: expected a
             --> log.txt:2:1
              |
            2 | b
              | ^ unexpected b
            """
                .ReplaceLineEndings("\n"),
            _a.ParseMany("a\nb", source: "log.txt").Last().Diagnostics.Render()
        );

    [Fact]
    public void ALayerThatFailsWhileTheNextItemIsLookedForEndsTheSequence() =>
        Assert.Equal(["a", "b", "error[lexer] at 4..5: unexpected '1'; expected whitespace or letter"], _word.ParseMany("a b 1 c").Select(Describe));

    [Fact]
    public void AnItemThatConsumesNothingThrows() =>
        Assert.Throws<GrammarException>(static () => Pipeline.From(_words).Then(WordGrammar.Token("x").Optional()).ParseMany("a").ToList());

    [Fact]
    public void AReaderIsReadOnlyAsFarAsEachItemNeeds()
    {
        var reader = new TrickleReader("a b c d");
        var results = _word.ParseMany(reader);

        Assert.Equal(0, reader.Consumed);

        using var items = results.GetEnumerator();

        Assert.True(items.MoveNext());
        Assert.Equal("a", Describe(items.Current));
        Assert.Equal(2, reader.Consumed);
    }

    [Fact]
    public void EachItemReturnsItsResult() =>
        Assert.Equal(["a", "bc", "d"], _word.ParseMany(" a bc  d ").Select(Describe));

    [Fact]
    public void EachItemStartsWithNoFurthestFailureInALayer()
    {
        var lookingAhead = Layer.Tokens(Char('a').Before(Atomic(Char('b').Then(Char('c'))).Optional()));

        Assert.Equal(["a", "error[lexer] at 1..2: unexpected 'b'; expected 'a'"], Pipeline.From(lookingAhead).Then(Any).ParseMany("ab").Select(Describe));
    }

    [Fact]
    public void EachItemStartsWithNoFurthestFailureInTheParser()
    {
        var item = WordGrammar.Token("a").Before(WordGrammar.Atomic(WordGrammar.Token("b").Then(WordGrammar.Token("c"))).Optional());

        Assert.Equal(["a", "error[parser] at 2..3: unexpected b; expected a"], Pipeline.From(_words).Then(item).ParseMany("a b a d").Select(Describe));
    }

    [Fact]
    public void EndingAnEnumerationReturnsItsBuffers()
    {
        var early = new ArrayUpstream<char>("a b c".ToCharArray());
        var whole = new ArrayUpstream<char>("a b c".ToCharArray());

        Assert.Equal("a", Describe(_word.ParseMany(() => new StreamBuffer<char>(early, segmentSize: 1), ParseOptions.Default, text: null).First()));
        Assert.Equal(3, _word.ParseMany(() => new StreamBuffer<char>(whole, segmentSize: 1), ParseOptions.Default, text: null).Count());
        Assert.True(early.Disposed);
        Assert.True(whole.Disposed);
    }

    [Fact]
    public void InputWithNoItemsHasNoResults()
    {
        Assert.Empty(_word.ParseMany("  "));
        Assert.Empty(_word.ParseMany(new StringReader("")));
    }

    [Fact]
    public void NullsThrowBeforeEnumerating()
    {
        Assert.Throws<GrammarException>(static () => ((Pipeline<char, string>)null!).ParseMany("a"));
        Assert.Throws<GrammarException>(static () => ((Pipeline<char, string>)null!).ParseMany("a".AsMemory()));
        Assert.Throws<GrammarException>(static () => ((Pipeline<char, string>)null!).ParseMany(new StringReader("a")));
        Assert.Throws<ArgumentNullException>(static () => _word.ParseMany((string)null!));
        Assert.Throws<ArgumentNullException>(static () => _word.ParseMany((TextReader)null!));
    }

    [Theory]
    [InlineData("a bc")]
    [InlineData("a 1")]
    [InlineData(" ")]
    [InlineData("ab cd ef")]
    public void ResultsOverAReaderMatchThoseOverText(string text) =>
        Assert.Equal(_word.ParseMany(text).Select(Describe), _word.ParseMany(new StringReader(text)).Select(Describe));

    [Fact]
    public void TheReaderIsLeftOpen()
    {
        var reader = new StringReader("a b");

        Assert.Equal(2, _word.ParseMany(reader).Count());
        Assert.Equal(-1, reader.Read());
    }
}
