using CsCheck;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;
using StringGrammar = Creole.Grammar<string>;

namespace Creole.Tests;

public class TriviaTests
{
    private static readonly Parser<char, Unit> _blockComment =
        Seq(String("/*"), NoneOf('*').Or(Atomic(Seq(Char('*').SkipAtLeastOnce(), NoneOf('*', '/')).Select(static (_, c) => c))).SkipMany(), Char('*').SkipAtLeastOnce(), Char('/')).Ignore();

    private static readonly Parser<string, (IReadOnlyList<(SourceSpan Trivia, SourceSpan Span)> Items, SourceSpan End)> _file =
        StringGrammar.Seq(
            StringGrammar.Seq(StringGrammar.LeadingTrivia, StringGrammar.Any.Spanned()).Select(static (trivia, item) => (trivia, item.Span)).Many(),
            StringGrammar.LeadingTrivia
        )
        .Before(StringGrammar.End);

    private static readonly Parser<char, Unit> _lineComment = Seq(String("//"), NoneOf('\n').SkipMany()).Ignore();

    private static readonly Layer<char, string> _lexer = Layer.Tokens(
        Longest(Letter.AtLeastOnce().Text(), Digit.AtLeastOnce().Text(), OneOf(Char('('), Char(')'), Char(';'), Char('='), Char('+')).Select(static c => c.ToString())),
        skip: OneOf(Whitespace.Ignore(), _lineComment, _blockComment).SkipMany()
    );

    private static readonly Gen<string> _token = Gen.OneOf(
        Gen.Char['a', 'z'].Array[1, 4].Select(static letters => new string(letters)),
        Gen.Char['0', '9'].Array[1, 3].Select(static digits => new string(digits)),
        Gen.OneOf("(", ")", ";", "=", "+")
    );

    private static readonly ReadOnlyMemory<string> _tokens = new[] { "a", "b" };

    private static readonly Gen<string> _trivia = Gen.OneOf(
        Gen.OneOf(" ", "\t", "\n", "\r\n"),
        Gen.Char['a', 'z'].Array[0, 4].Select(static letters => $"//{new string(letters)}\n"),
        Gen.OneOf(Gen.Char['a', 'z'], Gen.Const(' '), Gen.Const('*')).Array[0, 4].Select(static text => $"/*{new string(text)}*/")
    )
    .Array[0, 3]
    .Select(static pieces => string.Concat(pieces));

    private static bool Joins(string first, string second) =>
        char.IsLetter(first[^1]) && char.IsLetter(second[0]) || char.IsDigit(first[^1]) && char.IsDigit(second[0]);

    private static (IReadOnlyList<(SourceSpan Trivia, SourceSpan Span)> Items, SourceSpan End) Read(Layer<char, string> layer, string text, int segmentSize = 1024)
    {
        var (ok, value, _) = LayerRun.Run(layer, _file, text, segmentSize);

        Assert.True(ok);

        return value;
    }

    private static List<(string Item, SourceSpan Trivia)> ReadItems(Layer<char, string> layer, string text)
    {
        var (ok, value, _) = LayerRun.Run(
            layer,
            StringGrammar.Seq(StringGrammar.LeadingTrivia, StringGrammar.Any).Select(static (trivia, item) => (item, trivia)).Many(),
            text
        );

        Assert.True(ok);

        return [.. value];
    }

    private static SourceSpan Span(long start, long end) => new(SourceId.Unnamed, start, end);

    private static string Text(string source, SourceSpan span) => source[(int)span.Start..(int)span.End];

    [Fact]
    public void AnEmptyInputHasZeroWidthEndTrivia()
    {
        var (items, end) = Read(_lexer, "");

        Assert.Empty(items);
        Assert.Equal(Span(0, 0), end);
    }

    [Fact]
    public void AnInputOfOnlyTriviaIsAllEndTrivia()
    {
        var (items, end) = Read(_lexer, " // note\n /* x */ ");

        Assert.Empty(items);
        Assert.Equal(Span(0, 18), end);
    }

    [Fact]
    public void AnItemThatStartsBeforeThePreviousEndsHasZeroWidthTrivia() =>
        Assert.Equal(
            [("ab", Span(0, 0)), ("ab'", Span(0, 0)), ("cd", Span(2, 3)), ("cd'", Span(3, 3))],
            ReadItems(_lexer.Then(Layer.Transduce(0, StringGrammar.Any.Select<Emit<string>>(static token => [token, $"{token}'"]))), "ab cd")
        );

    [Fact]
    public void CommentTokensALayerDropsAreTriviaOfTheNextToken()
    {
        var lexer = Layer.Tokens(Longest(Letter.AtLeastOnce().Text(), Seq(String("//"), NoneOf('\n').SkipMany()).Text()), skip: Whitespace.SkipMany());

        Assert.Equal(
            [("a", Span(0, 0)), ("b", Span(1, 12))],
            ReadItems(lexer.Then(Layer.Where<string>(static token => !token.StartsWith("//", StringComparison.Ordinal))), "a // note\n  b")
        );
    }

    [Fact]
    public void EachTokenTakesTheWhitespaceAndCommentsBeforeIt()
    {
        var (items, end) = Read(_lexer, "  a /* c */ b ");

        Assert.Equal([(Span(0, 2), Span(2, 3)), (Span(3, 12), Span(12, 13))], items);
        Assert.Equal(Span(13, 14), end);
    }

    [Fact]
    public void LeadingTriviaOverInputHeldInMemoryIsZeroWidthAtTheCursor()
    {
        Assert.Equal(
            new ParseResult<(SourceSpan, SourceSpan, SourceSpan)>(new Parsed<(SourceSpan, SourceSpan, SourceSpan)>((Span(0, 0), Span(1, 1), Span(2, 2)))),
            Seq(LeadingTrivia, Char('a'), LeadingTrivia, Char('b'), LeadingTrivia).Select(static (first, _, second, _, end) => (first, second, end)).Parse("ab")
        );
        Assert.Equal(
            new ParseResult<SourceSpan>(new Parsed<SourceSpan>(Span(1, 1))),
            StringGrammar.Any.Then(StringGrammar.LeadingTrivia).Parse(_tokens)
        );
    }

    [Fact]
    public void SyntheticItemsTakeTheGapBeforeThem()
    {
        var lexer = Layer.Tokens(Longest(Letter.AtLeastOnce().Text(), String("\n")), skip: Char(' ').SkipMany());
        var dedents = Layer.Transduce(
            initialState: 0,
            step: StringGrammar.OneOf(
                StringGrammar.Token("\n").Select<Emit<string>>(static newline => [newline, Emit.Synthetic("<dedent>"), Emit.Synthetic("<dedent>")]),
                StringGrammar.Any.Select(static token => Emit.One(token))
            ),
            atEnd: static _ => Emit.Synthetic("<end>")
        );

        var (items, end) = Read(lexer.Then(dedents), "a\n  b  ");

        Assert.Equal(
            [
                (Span(0, 0), Span(0, 1)),
                (Span(1, 1), Span(1, 2)),
                (Span(2, 4), Span(4, 4)),
                (Span(4, 4), Span(4, 4)),
                (Span(4, 4), Span(4, 5)),
                (Span(5, 5), Span(5, 5)),
            ],
            items
        );
        Assert.Equal(Span(5, 7), end);
    }

    [Fact]
    public void TokensAndTheirTriviaReproduceTheSource() =>
        Gen.Select(Gen.Select(_trivia, _token).Array[0, 8], _trivia, Gen.Int[1, 5])
            .Sample(static (pairs, endTrivia, segmentSize) =>
            {
                var source = new System.Text.StringBuilder();

                for (var i = 0; i < pairs.Length; i++)
                {
                    var (trivia, token) = pairs[i];

                    source.Append(trivia.Length == 0 && i > 0 && Joins(pairs[i - 1].Item2, token) ? " " : trivia).Append(token);
                }

                var text = source.Append(endTrivia).ToString();
                var (items, end) = Read(_lexer, text, segmentSize);
                var rebuilt = string.Concat(items.Select(item => Text(text, item.Trivia) + Text(text, item.Span))) + Text(text, end);

                Assert.Equal(text, rebuilt);
                Assert.Equal(pairs.Select(static pair => pair.Item2), items.Select(item => Text(text, item.Span)));
                Assert.Equal(endTrivia, Text(text, end));
            });
}
