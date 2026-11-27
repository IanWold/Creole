namespace Creole.Tests;

file enum K
{
    Name,
    Keyword,
    LBrace,
    RBrace,
    Semicolon,
}

file readonly record struct Lexeme(K Kind, string Text) : IKinded<K>;

file record Plain;

file record Keyword(string Word);

file readonly union LexMode(Plain, Keyword);

file record Operand;

file record Operator;

/// <summary>
/// The union in docs/concepts/mode-channels.md § Modes that carry data that compares without allocating. Keep the two in step.
/// </summary>
file readonly union OperatorMode(Operand, Operator) : IEquatable<OperatorMode>
{
    public bool Equals(OperatorMode other) => Equals(Value, other.Value);

    public override bool Equals(object? obj) => obj is OperatorMode other && Equals(other);

    public override int GetHashCode() => Value?.GetHashCode() ?? 0;
}

file enum Side
{
    Left,
    Right,
}

/// <summary>
/// A mode whose data a test changes after the mode is written, which a grammar should not do.
/// </summary>
file class MutableMode(string name) : IEquatable<MutableMode>
{
    public string Name { get; set; } = name;

    public bool Equals(MutableMode? other) => other?.Name == Name;

    public override bool Equals(object? obj) => Equals(obj as MutableMode);

    public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
}

/// <summary>
/// A mode that counts how often it is compared.
/// </summary>
file class CountedMode(string name) : IEquatable<CountedMode>
{
    private static int _comparisons;

    public static int Comparisons => _comparisons;

    public string Name { get; } = name;

    public bool Equals(CountedMode? other)
    {
        Interlocked.Increment(ref _comparisons);

        return other?.Name == Name;
    }

    public override bool Equals(object? obj) => Equals(obj as CountedMode);

    public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
}

/// <summary>
/// The lexer of the code sample in docs/concepts/mode-channels.md § Modes that carry data. Keep the two in step.
/// </summary>
file class BlockLexer : CharGrammar
{
    public static readonly ModeChannel<LexMode> Modes = new(new Plain());

    private static readonly Parser<char, Lexeme> _punctuation = OneOf(
        Char('{').As(new Lexeme(K.LBrace, "{")),
        Char('}').As(new Lexeme(K.RBrace, "}")),
        Char(';').As(new Lexeme(K.Semicolon, ";"))
    );

    private static readonly Parser<char, string> _word = Letter.AtLeastOnce().Text();

    public static readonly Layer<char, Lexeme> Lexer = Layer.Modal(Modes, static mode => mode switch
    {
        Plain => _word.Select(static text => new Lexeme(K.Name, text)).Or(_punctuation),
        Keyword keyword => _word.Select(text => new Lexeme(text == keyword.Word ? K.Keyword : K.Name, text)).Or(_punctuation),
    }, Whitespace.SkipMany());
}

/// <summary>
/// The parser of the code sample in docs/concepts/mode-channels.md § Modes that carry data. Keep the two in step.
/// </summary>
file class BlockGrammar : TokenGrammar<Lexeme, K>
{
    // keyword w makes w a keyword until the block ends.
    private static readonly Parser<Lexeme, Unit> _declaration =
        Kind(K.Name).Where(static name => name.Text == "keyword")
            .Then(Kind(K.Name))
            .Bind(static word => PushMode(BlockLexer.Modes, new Keyword(word.Text)));

    private static readonly Parser<Lexeme, string> _word = OneOf(Kind(K.Name), Kind(K.Keyword)).Select(static word => $"{word.Kind} {word.Text}");

    private static readonly Parser<Lexeme, string> _block =
        Kind(K.LBrace).Then(_declaration).Then(Kind(K.Semicolon)).Then(_word.Many())
            .Before(Kind(K.RBrace)).Before(PopMode(BlockLexer.Modes))
            .Select(static words => string.Join(", ", words));

    public static readonly Parser<Lexeme, string> Script = _block.Or(_word).Many().Before(End).Select(static parts => string.Join(", ", parts));
}

/// <summary>
/// The item parsers of <see cref="BlockLexer"/>, for lexers the tests build.
/// </summary>
file static class BlockTokens
{
    private static readonly Parser<char, Lexeme> _punctuation = Grammar<char>.OneOf(
        CharGrammar.Char('{').As(new Lexeme(K.LBrace, "{")),
        CharGrammar.Char('}').As(new Lexeme(K.RBrace, "}")),
        CharGrammar.Char(';').As(new Lexeme(K.Semicolon, ";"))
    );

    private static readonly Parser<char, string> _word = CharGrammar.Letter.AtLeastOnce().Text();

    /// <summary>
    /// A lexer like <see cref="BlockLexer"/> that counts, in <paramref name="calls"/>, how often its selector runs for each mode.
    /// </summary>
    public static Layer<char, Lexeme> Counted(Dictionary<LexMode, int> calls) =>
        Layer.Modal<char, Lexeme, LexMode>(
            BlockLexer.Modes,
            mode =>
            {
                calls[mode] = calls.GetValueOrDefault(mode) + 1;

                return Item(mode);
            },
            CharGrammar.Whitespace.SkipMany()
        );

    public static Parser<char, Lexeme> Item(LexMode mode) => mode switch
    {
        Plain => _word.Select(static text => new Lexeme(K.Name, text)).Or(_punctuation),
        Keyword keyword => _word.Select(text => new Lexeme(text == keyword.Word ? K.Keyword : K.Name, text)).Or(_punctuation),
    };
}

/// <summary>
/// Modes that carry data: a selector sees each mode value, data included, and a <c>Layer.Modal</c> keeps the plans of the 64 modes used most recently (ADR-0043).
/// </summary>
public class ModePayloadTests
{
    private static long Allocated(Pipeline<char, int> pipeline, int names)
    {
        var text = string.Join(" ", Enumerable.Repeat("a", names));

        for (var i = 0; i < 100; i++)
        {
            Assert.Equal($"{names}", Value(pipeline.Parse(text)));
        }

        return Allocation.Fewest(() => pipeline.Parse(text), attempts: 20);
    }

    private static Pipeline<char, int> Alternating<TMode>(TMode first, TMode second)
    {
        var channel = new ModeChannel<TMode>(first);
        var lexer = Layer.Modal<char, Lexeme, TMode>(channel, static _ => CharGrammar.Letter.AtLeastOnce().Text().Select(static text => new Lexeme(K.Name, text)), CharGrammar.Whitespace.SkipMany());
        var name = TokenGrammar<Lexeme, K>.Kind(K.Name);
        var pair = name.Before(TokenGrammar<Lexeme, K>.SetMode(channel, second)).Then(name).Before(TokenGrammar<Lexeme, K>.SetMode(channel, first));

        return Pipeline.From(lexer).Then(pair.Many().Before(TokenGrammar<Lexeme, K>.End).Select(static pairs => pairs.Count * 2));
    }

    private static string Value<T>(ParseResult<T> result) => result.Value switch
    {
        Parsed<T> parsed => $"{parsed.Value}",
        Failed failed => failed.Diagnostics.Render(),
        _ => "",
    };

    private static string Word(int index) =>
        index < 26
        ? $"{(char)('a' + index)}"
        : Word(index / 26 - 1) + (char)('a' + index % 26);

    [Fact]
    public void ALayerForgetsAModeThatChangedAfterItWasPlanned()
    {
        var channel = new ModeChannel<MutableMode>(new("start"));
        var planned = new List<MutableMode>();
        var lexer = Layer.Modal<char, Lexeme, MutableMode>(
            channel,
            mode =>
            {
                planned.Add(mode);

                return CharGrammar.Letter.AtLeastOnce().Text().Select(static text => new Lexeme(K.Name, text));
            },
            CharGrammar.Whitespace.SkipMany()
        );
        var names = TokenGrammar<Lexeme, K>.Kind(K.Name).Bind(name => TokenGrammar<Lexeme, K>.SetMode(channel, new MutableMode(name.Text)));
        var pipeline = Pipeline.From(lexer).Then(names.Many().Before(TokenGrammar<Lexeme, K>.End).Select(static all => all.Count));

        Assert.Equal("100", Value(pipeline.Parse(string.Join(" ", Enumerable.Range(0, 100).Select(Word)))));

        foreach (var mode in planned)
        {
            mode.Name += "!";
        }

        Assert.Equal("100", Value(pipeline.Parse(string.Join(" ", Enumerable.Range(100, 100).Select(Word)))));
        Assert.Equal(64, ((ModalLayer<char, Lexeme, MutableMode>)lexer).PlannedCount);
    }

    [Fact]
    public void ALayerKeepsAModeEachParseReadsWhileOtherModesComeAndGo()
    {
        var calls = new Dictionary<LexMode, int>();
        var pipeline = Pipeline.From(BlockTokens.Counted(calls)).Then(BlockGrammar.Script);

        foreach (var word in Enumerable.Range(0, 200).Select(Word))
        {
            Assert.Equal($"Keyword {word}", Value(pipeline.Parse($"{{ keyword {word}; {word} }}")));
        }

        Assert.Equal(1, calls[new Plain()]);
    }

    [Fact]
    public void ALayerKeepsThePlansOfThe64ModesUsedMostRecently()
    {
        var calls = new Dictionary<LexMode, int>();
        var lexer = BlockTokens.Counted(calls);
        var words = Enumerable.Range(0, 10_000).Select(Word).ToList();
        var pipeline = Pipeline.From(lexer).Then(BlockGrammar.Script);

        Assert.Equal(string.Join(", ", words.Select(static word => $"Keyword {word}")), Value(pipeline.Parse(string.Join(" ", words.Select(static word => $"{{ keyword {word}; {word} }}")))));
        Assert.Equal(1, calls[new Plain()]);
        Assert.All(words, word => Assert.Equal(1, calls[new Keyword(word)]));
        Assert.Equal(64, ((ModalLayer<char, Lexeme, LexMode>)lexer).PlannedCount);

        Assert.Equal("Keyword a", Value(pipeline.Parse("{ keyword a; a }")));
        Assert.Equal(2, calls[new Keyword("a")]);
        Assert.Equal(64, ((ModalLayer<char, Lexeme, LexMode>)lexer).PlannedCount);
    }

    [Fact]
    public void ANullModeIsAModeLikeAnyOther()
    {
        var channel = new ModeChannel<string?>(null);
        var lexer = Layer.Modal<char, Lexeme, string?>(
            channel,
            static mode => CharGrammar.Letter.AtLeastOnce().Text().Select(text => new Lexeme(mode is null ? K.Name : K.Keyword, text)),
            CharGrammar.Whitespace.SkipMany()
        );
        var parser = TokenGrammar<Lexeme, K>
            .Seq(
                TokenGrammar<Lexeme, K>.Kind(K.Name),
                TokenGrammar<Lexeme, K>.SetMode(channel, "on"),
                TokenGrammar<Lexeme, K>.Kind(K.Keyword),
                TokenGrammar<Lexeme, K>.SetMode(channel, null),
                TokenGrammar<Lexeme, K>.Kind(K.Name)
            )
            .Select(static (first, _, second, _, third) => $"{first.Text} {second.Text} {third.Text}");

        Assert.Equal("a b c", Value(Pipeline.From(lexer).Then(parser).Parse("a b c")));
    }

    [Fact]
    public void AStepWhoseModeHasNotChangedComparesNoModes()
    {
        var channel = new ModeChannel<CountedMode>(new("first"));
        var lexer = Layer.Modal<char, Lexeme, CountedMode>(channel, static _ => CharGrammar.Letter.AtLeastOnce().Text().Select(static text => new Lexeme(K.Name, text)), CharGrammar.Whitespace.SkipMany());
        var parser = TokenGrammar<Lexeme, K>.Kind(K.Name)
            .Then(TokenGrammar<Lexeme, K>.SetMode(channel, new CountedMode("second")))
            .Then(TokenGrammar<Lexeme, K>.Kind(K.Name).Many())
            .Select(static names => names.Count);
        var pipeline = Pipeline.From(lexer).Then(parser);

        int Comparisons(int names)
        {
            var before = CountedMode.Comparisons;

            Assert.Equal($"{names}", Value(pipeline.Parse(string.Join(" ", Enumerable.Repeat("a", names + 1)))));

            return CountedMode.Comparisons - before;
        }

        Comparisons(1);

        Assert.Equal(Comparisons(10), Comparisons(1_000));
    }

    [Fact]
    public void AUnionModeThatImplementsIEquatableChangesAsCheaplyAsAnEnum()
    {
        var enumModes = Alternating(Side.Left, Side.Right);
        var unionModes = Alternating<OperatorMode>(new Operand(), new Operator());

        Assert.Equal(Allocated(enumModes, 200) - Allocated(enumModes, 20), Allocated(unionModes, 200) - Allocated(unionModes, 20));
    }

    [Fact]
    public void AWordIsAKeywordOnlyInTheBlockThatDeclaresIt() =>
        Assert.Equal("Name foo, Keyword foo, Name bar, Name foo", Value(Pipeline.From(BlockLexer.Lexer).Then(BlockGrammar.Script).Parse("foo { keyword foo; foo bar } foo")));

    [Fact]
    public void EachPayloadIsLexedWithItsOwnData() =>
        Assert.Equal("Keyword foo, Name bar, Name foo, Keyword bar", Value(Pipeline.From(BlockLexer.Lexer).Then(BlockGrammar.Script).Parse("{ keyword foo; foo bar } { keyword bar; foo bar }")));
}
