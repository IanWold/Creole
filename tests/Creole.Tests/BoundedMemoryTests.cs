using System.Text;
using CsCheck;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file abstract record Shape
{
    /// <summary>
    /// Alternatives that each start with a letter of their own, so each one that is not taken fails without consuming.
    /// </summary>
    public sealed record Choice(Shape[] Alternatives) : Shape;

    /// <summary>
    /// <paramref name="Letter"/>, then <paramref name="Tail"/> if there is one.
    /// </summary>
    public sealed record Prefixed(char Letter, Shape? Tail) : Shape;

    /// <summary>
    /// <paramref name="Letter"/> any number of times.
    /// </summary>
    public sealed record Repeat(char Letter) : Shape;

    /// <summary>
    /// <paramref name="Letter"/> or nothing.
    /// </summary>
    public sealed record Optional(char Letter) : Shape;
}

/// <summary>
/// A random grammar of choices nested <c>depth</c> deep, with no construct that pins, and sentences it matches to the end.
/// </summary>
/// <remarks>
/// Every letter appears in one place only, so no alternative that is not taken consumes, and nothing that follows a repetition or an optional letter starts with its letter.
/// Choices of four or more alternatives are lowered to dispatch. Half the sentences take the first alternative of every choice, which nests deepest, so each input reaches the full depth.
/// </remarks>
file sealed class NestedChoices
{
    private char _next = 'a';
    private readonly Random _random;

    public NestedChoices(int seed, int depth)
    {
        _random = new Random(seed);
        Root = Choice(depth);
        Parser = Build(Root).SkipMany();
    }

    public Parser<char, Unit> Parser { get; }

    public Shape Root { get; }

    private static Parser<char, Unit> Build(Shape shape) => shape switch
    {
        Shape.Choice { Alternatives: var alternatives } => alternatives.Length switch
        {
            2 => OneOf(Build(alternatives[0]), Build(alternatives[1])),
            3 => OneOf(Build(alternatives[0]), Build(alternatives[1]), Build(alternatives[2])),
            4 => OneOf(Build(alternatives[0]), Build(alternatives[1]), Build(alternatives[2]), Build(alternatives[3])),
            _ => OneOf(Build(alternatives[0]), Build(alternatives[1]), Build(alternatives[2]), Build(alternatives[3]), Build(alternatives[4])),
        },
        Shape.Prefixed { Tail: null } prefixed => Char(prefixed.Letter).Select(static _ => default(Unit)),
        Shape.Prefixed prefixed => Char(prefixed.Letter).Then(Build(prefixed.Tail)),
        Shape.Repeat repeat => Char(repeat.Letter).SkipMany(),
        Shape.Optional optional => Char(optional.Letter).Optional().Select(static _ => default(Unit)),
        _ => throw new ArgumentOutOfRangeException(nameof(shape)),
    };

    private Shape.Choice Choice(int depth)
    {
        var count = _random.Next(2, 6);
        var alternatives = new Shape[count];

        for (var i = 0; i < count; i++)
        {
            alternatives[i] = new Shape.Prefixed(
                _next++,
                i == 0 && depth > 1
                    ? Choice(depth - 1)
                    : _random.Next(4) switch
                    {
                        0 => null,
                        1 => new Shape.Repeat(_next++),
                        2 => new Shape.Optional(_next++),
                        _ => new Shape.Prefixed(_next++, null),
                    }
            );
        }

        return new Shape.Choice(alternatives);
    }

    private void Write(Shape shape, StringBuilder text, bool deepest)
    {
        switch (shape)
        {
            case Shape.Choice choice:
                Write(
                    choice.Alternatives[deepest
                        ? 0
                        : _random.Next(choice.Alternatives.Length)
                    ],
                    text,
                    deepest
                );
                break;

            case Shape.Prefixed prefixed:
                text.Append(prefixed.Letter);

                if (prefixed.Tail is not null)
                {
                    Write(prefixed.Tail, text, deepest);
                }

                break;

            case Shape.Repeat repeat:
                text.Append(repeat.Letter, _random.Next(4));
                break;

            case Shape.Optional optional when _random.Next(2) == 0:
                text.Append(optional.Letter);
                break;
        }
    }

    /// <summary>
    /// Sentences of the grammar, one after another, to at least <paramref name="length"/> characters.
    /// </summary>
    public string Input(int length)
    {
        var text = new StringBuilder();

        while (text.Length < length)
        {
            Write(Root, text, deepest: _random.Next(2) == 0);
        }

        return text.ToString();
    }
}

/// <summary>
/// An upstream that records the most items its buffer held after each step.
/// </summary>
file sealed class MeasuringUpstream<T>(IUpstream<T> inner) : IUpstream<T>
{
    private readonly IUpstream<T> _inner = inner;

    public SourceSpan InputEnd => _inner.InputEnd;

    public SourceSpan InputStart => _inner.InputStart;

    public long MostHeld { get; private set; }

    public bool TryStep(StreamBuffer<T> buffer)
    {
        var stepped = _inner.TryStep(buffer);
        MostHeld = Math.Max(MostHeld, buffer.Filled - buffer.Start);
        return stepped;
    }
}

/// <summary>
/// Guarantee 2: choices and repetitions pin nothing, so however deeply choices nest and however long the input, each boundary holds a constant number of items.
/// </summary>
/// <remarks>
/// <para>
/// With nothing pinned, a buffer keeps the segment before the reader's and the reader's own, and fills one item past the reader: at most two segments and one item.
/// A producing layer reads its input the same way when its steps read one item each, as every layer here does.
/// It keeps its input only from the step of the output its reader reads next, or from the step it is running, so the buffer below it holds the same.
/// </para>
/// <para>
/// A layer that drops most of its input keeps the same bound.
/// No output it has produced lies at or above its reader's horizon while it drops a run, so it lets the run go as it drops it, however long the run.
/// </para>
/// </remarks>
public class BoundedMemoryTests
{
    private const char Noise = '#';

    private static readonly Layer<char, char>[][] _filters =
    [
        [Layer.Where<char>(static c => c != Noise)],
        [Layer.Transduce(0, Any.Select(static c => c == Noise ? default : Emit.One(c)))],
        [Layer.Tokens(Any), Layer.Where<char>(static c => c != Noise)],
    ];

    private static readonly Layer<char, char>[][] _lexers =
    [
        [Layer.Tokens(Any)],
        [Layer.Transduce(0, Any.Select(static c => Emit.One(c)))],
    ];

    private static readonly int[] _runLengths = [10, 100, 1_000, 10_000];

    private static readonly Gen<(int Seed, int Depth, int Length, int SegmentSize)> _runs =
        Gen.Select(Gen.Int, Gen.Int[1, 64], Gen.Int[1_000, 20_000], Gen.OneOf(1, 2, 16));

    private static readonly int[] _segmentSizes = [1, 2, 16];

    private static bool BoundedUnder(Layer<char, char>[] layers, Parser<char, Unit> parser, string input, int length, int segmentSize) =>
        Held(layers, parser, input, length, segmentSize) is { } held
        && held.All(most => most <= 2 * segmentSize + 1);

    private static string Describe(Layer<char, char>[] layers) =>
        string.Join(" then ", layers.Select(static layer => layer.GetType().Name.Split('`')[0]));

    /// <summary>
    /// The most items each boundary held while <paramref name="parser"/> read the top layer's items to <paramref name="length"/>, from the root up, or null if it did not.
    /// </summary>
    private static long[]? Held(Layer<char, char>[] layers, Parser<char, Unit> parser, string input, int length, int segmentSize)
    {
        var boundaries = new List<MeasuringUpstream<char>> { new(new ArrayUpstream<char>(input.ToCharArray())) };

        foreach (var layer in layers)
        {
            boundaries.Add(new MeasuringUpstream<char>(layer.Open(new StreamBuffer<char>(boundaries[^1], segmentSize), ParseOptions.Default, recordFailures: true)));
        }

        var cursor = new Cursor<char>(new StreamBuffer<char>(boundaries[^1], segmentSize)) { Failures = new FailureLog<char>() };
        var plan = new PlanBuilder(LoweringOptions.All).Lower(parser);
        var value = default(Unit);

        return plan.TryParse(ref cursor, ref value) && cursor.Position == length
            ? [.. boundaries.Select(static boundary => boundary.MostHeld)]
            : null;
    }

    /// <summary>
    /// <paramref name="input"/> with a run of 1 to 256 <see cref="Noise"/> characters after about one character in sixteen, so most of the result is noise.
    /// </summary>
    private static string WithNoise(string input, int seed)
    {
        var random = new Random(seed);
        var text = new StringBuilder();

        foreach (var c in input)
        {
            text.Append(c);

            if (random.Next(16) == 0)
            {
                text.Append(Noise, random.Next(1, 257));
            }
        }

        return text.ToString();
    }

    [Fact]
    public void ALayerThatDropsALongRunHoldsTheSameWhateverTheRunsLength() =>
        Assert.Empty(
            from length in _runLengths
            from segmentSize in _segmentSizes
            from layers in _filters
            let run = new string(Noise, length)
            let held = Held(layers, Any.SkipMany(), $"{run}a{run}b{run}c{run}", 3, segmentSize)
            where held is null || held.Any(most => most > 2 * segmentSize + 1)
            select $"{Describe(layers)} over runs of {length} in segments of {segmentSize}: held {(held is null ? "nothing, as the parse failed" : string.Join(", ", held))}"
        );

    [Fact]
    public void ALexerAndAParserOfDeeplyNestedChoicesHoldAConstantNumberOfItemsAtEachBoundary() => _runs.Sample(
        static (seed, depth, length, segmentSize) =>
        {
            var grammar = new NestedChoices(seed, depth);
            var input = grammar.Input(length);

            return _lexers.All(layers => BoundedUnder(layers, grammar.Parser, input, input.Length, segmentSize));
        },
        iter: 50
    );

    [Fact]
    public void AParseOfDeeplyNestedChoicesHoldsTwoSegmentsAndOneItem() =>
        _runs.Sample(static (seed, depth, length, segmentSize) =>
        {
            var grammar = new NestedChoices(seed, depth);
            var input = grammar.Input(length);

            return BoundedUnder([], grammar.Parser, input, input.Length, segmentSize);
        }, iter: 50);

    [Fact]
    public void LayersThatDropMostOfTheirInputUnderAParserOfDeeplyNestedChoicesHoldAConstantNumberOfItemsAtEachBoundary() => _runs.Sample(
        static (seed, depth, length, segmentSize) =>
        {
            var grammar = new NestedChoices(seed, depth);
            var input = grammar.Input(length);
            var noisy = WithNoise(input, seed);

            return _filters.All(layers => BoundedUnder(layers, grammar.Parser, noisy, input.Length, segmentSize));
        },
        iter: 50
    );
}
