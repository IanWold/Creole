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
}
