using System.Runtime.InteropServices;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;
using SliceGrammar = Creole.Grammar<System.ReadOnlyMemory<char>>;
using WordGrammar = Creole.Grammar<string>;

namespace Creole.Tests;

public class PipelineTests
{
    private static readonly Layer<char, string> _words = Layer.Tokens(Letter.AtLeastOnce().Text(), skip: Whitespace.SkipMany());

    private static readonly Layer<char, ReadOnlyMemory<char>> _slices = Layer.Tokens(Letter.AtLeastOnce().Slice(), skip: Whitespace.SkipMany());

    private static readonly Parser<char, char> _nested = OneOf(Letter, Char('(').Then(Rec(() => _nested)).Before(Char(')')));

    private static readonly Parser<string, int> _count = WordGrammar.Any.Many().Select(static words => words.Count);

    private static readonly Pipeline<char, IReadOnlyList<string>> _sentence =
        Pipeline.From(_words).Then(WordGrammar.Token("the").Then(WordGrammar.Any.AtLeastOnce()).Before(WordGrammar.End));

    private static readonly Pipeline<byte, int> _countFromBytes = Pipeline.From(Layer.Utf8().Then(_words)).Then(_count);

    private static string Error<T>(ParseResult<T> result) =>
        Assert.Single(result.Diagnostics).ToString();

    [Fact]
    public void ALayersDepthErrorEndsTheParse() =>
        Assert.Equal(
            "error[lexer] at 3..4: nesting exceeds ParseOptions.MaxDepth of 2",
            Error(Pipeline.From(Layer.Tokens(_nested)).Then(Any.Many()).Parse("((((a))))", options: new ParseOptions { MaxDepth = 2 }))
        );

    [Fact]
    public void ALayersErrorEndsTheParse() =>
        Assert.Equal(
            "error[lexer] at 4..5: unexpected '1'; expected whitespace or letter",
            Error(_sentence.Parse("the 1 cat"))
        );

    [Fact]
    public void ALayersErrorIsItsFurthestFailureAcrossEveryStepFromTheStart()
    {
        var lookingAhead = Layer.Tokens(Char('a').Before(Atomic(Char('b').Then(Char('c'))).Optional()));
        var result = Pipeline.From(lookingAhead).Then(Any.Many()).Parse("ab");
        var error = Assert.Single(result.Diagnostics);

        Assert.Equal(new SourceSpan(SourceId.Unnamed, 2, 2), error.Span);
        Assert.Equal(LayerRun.Error(lookingAhead, "ab"), error);
    }

    [Fact]
    public void AParsersDepthErrorEndsTheParse() =>
        Assert.Equal(
            "error[parser] at 3..4: nesting exceeds ParseOptions.MaxDepth of 2",
            Error(Pipeline.From(Layer.Tokens(Any)).Then(_nested).Parse("((((a))))", options: new ParseOptions { MaxDepth = 2 }))
        );

    [Fact]
    public void AParsersErrorHasSpansInTheText() =>
        Assert.Equal(
            "error[parser] at 9..12: unexpected dog; expected end of input",
            Error(Pipeline.From(_words).Then(WordGrammar.Token("the").Then(WordGrammar.Any).Before(WordGrammar.End)).Parse("the  cat dog"))
        );

    [Fact]
    public void AFailedResultHoldsTheTextUnderItsName() =>
        Assert.Equal(
            """
            error[parser]: expected end of input
             --> story.txt:2:1
              |
            2 | dog
              | ^^^ unexpected dog
            """
                .ReplaceLineEndings("\n"),
            Pipeline.From(_words).Then(WordGrammar.Token("the").Then(WordGrammar.Any).Before(WordGrammar.End)).Parse("the cat\ndog", source: "story.txt").Diagnostics.Render()
        );

    [Fact]
    public void APipelineEndsInItsParser() =>
        Assert.Equal(["cat", "sat"], Assert.IsType<Parsed<IReadOnlyList<string>>>(_sentence.Parse(" the cat  sat ").Value).Value);

    [Fact]
    public void APipelineRunsOnSeveralThreadsAtOnce()
    {
        string[] inputs = [.. Enumerable.Range(0, 64).Select(static i => i % 3 == 0 ? $"the {new string('a', i + 1)} 1" : $"the {new string('b', i + 1)} cat")];
        var expected = inputs.Select(static input => _sentence.Parse(input).Diagnostics.Render()).ToArray();
        var actual = new string[inputs.Length];

        Parallel.For(0, inputs.Length, i => actual[i] = _sentence.Parse(inputs[i]).Diagnostics.Render());

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AParseNeedNotReachTheEndOfTheInput() =>
        Assert.Equal("the", Assert.IsType<Parsed<string>>(Pipeline.From(_words).Then(WordGrammar.Any).Parse("the cat").Value).Value);

    [Fact]
    public void IdentityBeforeAParserPassesTheInputThrough() =>
        Assert.Equal("(a)", Assert.IsType<Parsed<string>>(Pipeline.From(Layer.Identity<char>()).Then(_nested.Text()).Parse("(a)").Value).Value);

    [Fact]
    public void NullsThrow()
    {
        Assert.Throws<GrammarException>(static () => Pipeline.From<char, char>(null!));
        Assert.Throws<GrammarException>(static () => _words.Then((Parser<string, int>)null!));
        Assert.Throws<GrammarException>(static () => ((Pipeline<char, int>)null!).Parse("a"));
        Assert.Throws<GrammarException>(static () => ((Pipeline<char, int>)null!).Parse("a".AsMemory()));
        Assert.Throws<ArgumentNullException>(static () => Pipeline.From(_words).Then(_count).Parse((string)null!));
        Assert.Throws<GrammarException>(static () => ((Pipeline<byte, int>)null!).Parse(Stream.Null));
        Assert.Throws<ArgumentNullException>(static () => _countFromBytes.Parse((Stream)null!));
    }

    [Fact]
    public void SlicesAreViewsOfMemory()
    {
        var text = "xx a bc yy".ToCharArray();
        var result = Pipeline.From(_slices).Then(SliceGrammar.Any.Many()).Parse(text.AsMemory(2, 6));
        var slices = Assert.IsType<Parsed<IReadOnlyList<ReadOnlyMemory<char>>>>(result.Value).Value;

        Assert.All(slices, slice => Assert.True(MemoryMarshal.TryGetArray(slice, out var segment) && segment.Array == text));
        Assert.Equal(["a", "bc"], slices.Select(static slice => slice.ToString()));
    }

    [Fact]
    public void SlicesAreViewsOfTheString()
    {
        var text = "the cat";
        var result = Pipeline.From(_slices).Then(SliceGrammar.Any.Many()).Parse(text);
        var slices = Assert.IsType<Parsed<IReadOnlyList<ReadOnlyMemory<char>>>>(result.Value).Value;

        Assert.All(slices, slice => Assert.True(MemoryMarshal.TryGetString(slice, out var held, out _, out _) && ReferenceEquals(held, text)));
        Assert.Equal([(0, 3), (4, 3)], slices.Select(static slice => MemoryMarshal.TryGetString(slice, out _, out var start, out var length) ? (start, length) : default));
    }

    [Fact]
    public void TheBuffersOfARunAreDisposedHoweverItEnds()
    {
        var thrower = Layer.Tokens(Letter.Select(static letter => letter == 'x' ? throw new InvalidOperationException("x") : letter));
        var pipeline = Pipeline.From(thrower).Then(Any.Many().Before(End));

        foreach (var (text, recordFailures) in new[] { ("ab", false), ("ab!", false), ("ab!", true) })
        {
            var root = new ArrayUpstream<char>(text.ToCharArray());

            pipeline.TryParse(new StreamBuffer<char>(root, segmentSize: 1), ParseOptions.Default, recordFailures, out _, out _);

            Assert.True(root.Disposed, text);
        }

        var thrown = new ArrayUpstream<char>("axb".ToCharArray());

        Assert.Throws<InvalidOperationException>(() => { pipeline.TryParse(new StreamBuffer<char>(thrown, segmentSize: 1), ParseOptions.Default, recordFailures: false, out _, out _); });
        Assert.True(thrown.Disposed);
    }

    [Fact]
    public void ADepthErrorFarIntoAReaderPointsAtItsItem()
    {
        var text = new string('x', 3000) + "((((a))))";
        var pipeline = Pipeline.From(Layer.Tokens(Any)).Then(Char('x').SkipMany().Then(_nested));
        var options = new ParseOptions { MaxDepth = 2 };

        Assert.Equal("error[parser] at 3003..3004: nesting exceeds ParseOptions.MaxDepth of 2", Error(pipeline.Parse(new StringReader(text), options)));
        Assert.Equal(Error(pipeline.Parse(text, options: options)), Error(pipeline.Parse(new StringReader(text), options)));
    }

    [Fact]
    public void AFailedParseOverAReaderRendersOnOneLine()
    {
        var result = _sentence.Parse(new StringReader("the\n1"));

        Assert.Equal("error[lexer] at 4..5: unexpected '1'; expected whitespace or letter", result.Diagnostics.Render());
    }

    [Fact]
    public void AFailedParseOverAStreamRendersOnOneLine() =>
        Assert.Equal("error[lexer] at 4..5: unexpected '1'; expected whitespace or letter", _countFromBytes.Parse(new MemoryStream("the\n1"u8.ToArray())).Diagnostics.Render());

    [Theory]
    [InlineData("the cat sat")]
    [InlineData("the 1")]
    [InlineData("a cat")]
    [InlineData("")]
    public void ParseOverAReaderMatchesParseOverText(string text)
    {
        var fromText = _sentence.Parse(text);
        var fromReader = _sentence.Parse(new StringReader(text));

        Assert.Equal(fromText.Diagnostics, fromReader.Diagnostics);
        Assert.Equal((fromText.Value as Parsed<IReadOnlyList<string>>)?.Value, (fromReader.Value as Parsed<IReadOnlyList<string>>)?.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void PositionOverAReaderWithNoItemsIsTheStart(string text) =>
        Assert.Equal(new SourceSpan(SourceId.Unnamed, 0, 0), Assert.IsType<Parsed<SourceSpan>>(Pipeline.From(_words).Then(WordGrammar.Position).Parse(new StringReader(text)).Value).Value);

    [Fact]
    public void SlicesOverAReaderAreCopies()
    {
        var result = Pipeline.From(_slices).Then(SliceGrammar.Any.Many()).Parse(new StringReader("the cat"));
        var slices = Assert.IsType<Parsed<IReadOnlyList<ReadOnlyMemory<char>>>>(result.Value).Value;

        Assert.All(slices, static slice => Assert.False(MemoryMarshal.TryGetString(slice, out _, out _, out _)));
        Assert.Equal(["the", "cat"], slices.Select(static slice => slice.ToString()));
    }

    [Fact]
    public void TheReaderIsLeftOpen()
    {
        var reader = new StringReader("the cat");

        Assert.IsType<Parsed<IReadOnlyList<string>>>(_sentence.Parse(reader).Value);
        Assert.Equal(-1, reader.Read());
    }

    [Fact]
    public void TheStreamIsLeftOpen()
    {
        var stream = new MemoryStream("the cat"u8.ToArray());

        Assert.Equal(2, Assert.IsType<Parsed<int>>(_countFromBytes.Parse(stream).Value).Value);
        Assert.Equal(-1, stream.ReadByte());
    }

    [Fact]
    public void TriviaAtTheEndRunsToWhereTheInputEnds()
    {
        var pipeline = Pipeline.From(_words).Then(WordGrammar.Any.SkipMany().Then(WordGrammar.LeadingTrivia));
        var expected = new SourceSpan(SourceId.Unnamed, 3, 5);

        Assert.Equal(expected, Assert.IsType<Parsed<SourceSpan>>(pipeline.Parse("a b  ").Value).Value);
        Assert.Equal(expected, Assert.IsType<Parsed<SourceSpan>>(pipeline.Parse(new StringReader("a b  ")).Value).Value);
    }

    [Fact]
    public void AParserThatReadsItsStateAsAnotherTypeThrows()
    {
        const string Message = "The parser layer's state is of type String, but its grammar reads it as Int32.";
        var pipeline = Pipeline.From(_words).Then(WordGrammar.SetState("x").Then(WordGrammar.GetState<int>()));

        Assert.Equal(Message, Assert.Throws<GrammarException>(() => pipeline.Parse("a")).Message);
        Assert.Equal(Message, Assert.Throws<GrammarException>(() => pipeline.Parse(new StringReader("a"))).Message);
        Assert.Equal(Message, Assert.Throws<GrammarException>(() => pipeline.ParseMany("a").ToList()).Message);
    }

    [Fact]
    public void EachItemOfParseManyStartsWithNoParserState()
    {
        var item = WordGrammar.OneOf(WordGrammar.Token("set").Then(WordGrammar.SetState(1)).As(0), WordGrammar.Token("get").Then(WordGrammar.GetState<int>()));
        var pipeline = Pipeline.From(_words).Then(item);

        Assert.Equal(
            "The parser layer has no state, but its grammar reads it as Int32. Give the layer an initialState, or set the state with SetState before reading it.",
            Assert.Throws<GrammarException>(() => pipeline.ParseMany("set get").ToList()).Message
        );
    }
}
