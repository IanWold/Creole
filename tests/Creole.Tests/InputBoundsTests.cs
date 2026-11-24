using static Creole.CharGrammar;
using StringGrammar = Creole.Grammar<string>;

namespace Creole.Tests;

public class InputBoundsTests
{
    private static readonly Layer<char, string> _lexer = Layer.Tokens(Letter.AtLeastOnce().Text(), skip: Whitespace.SkipMany());

    private static readonly Layer<string, string> _passThrough = Layer.Transduce(
        initialState: 0,
        step: StringGrammar.Any.Select(static token => Emit.One(token)),
        atEnd: static _ => Emit.Synthetic("<end>")
    );

    public static TheoryData<string> LayersAboveTheLexer => ["Where", "Select", "Identity", "Transduce", "Then"];

    private static Layer<char, string> Above(string name) => name switch
    {
        "Where" => _lexer.Then(Layer.Where<string>(static token => token != "cd")),
        "Select" => _lexer.Then(Layer.Select<string, string>(static token => token.ToUpperInvariant())),
        "Identity" => _lexer.Then(Layer.Identity<string>()),
        "Transduce" => _lexer.Then(_passThrough),
        "Then" => _lexer.Then(Layer.Where<string>(static token => token != "cd").Then(_passThrough)),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
    };

    private static (SourceSpan Start, SourceSpan End) Bounds(Layer<char, string> layer, string text)
    {
        var items = LayerRun.Open(layer, text, out _);

        for (var position = 0L; items.TryGetSegment(position, horizon: 0, out _, out _); position++) { }

        return (items.InputStart, items.InputEnd);
    }

    private static SourceSpan Span(long start, long end) => new(SourceId.Unnamed, start, end);

    [Fact]
    public void ALayerOverAnEmptyInputStartsAndEndsAtZero() =>
        Assert.Equal((Span(0, 0), Span(0, 0)), Bounds(_lexer, ""));

    [Fact]
    public void ALayerOverOnlySkippedInputEndsAtTheEndOfTheSource() =>
        Assert.Equal((Span(0, 0), Span(3, 3)), Bounds(_lexer, "   "));

    [Fact]
    public void ATokensLayerEndsWhereItsInputEndsNotWhereItsLastItemEnds()
    {
        Assert.Equal([Span(2, 4), Span(6, 8)], LayerRun.Items(_lexer, "  ab  cd  ").Select(static entry => entry.Slot.Span));
        Assert.Equal((Span(0, 0), Span(10, 10)), Bounds(_lexer, "  ab  cd  "));
    }

    [Theory]
    [MemberData(nameof(LayersAboveTheLexer))]
    public void EachLayerAboveALexerTakesTheLexersBounds(string layer) =>
        Assert.Equal((Span(0, 0), Span(10, 10)), Bounds(Above(layer), "  ab  cd  "));
}
