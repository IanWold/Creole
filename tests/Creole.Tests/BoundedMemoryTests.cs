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
/// With nothing pinned, a buffer keeps the segment before the reader's and the reader's own, and fills one item past the reader: at most two segments and one item.
/// Below a lexer of one-character tokens, the input also holds the step of every token still held, and the segment before the oldest.
/// </remarks>
public class BoundedMemoryTests
{
    private static readonly Gen<(int Seed, int Depth, int Length, int SegmentSize)> _runs =
        Gen.Select(Gen.Int, Gen.Int[1, 64], Gen.Int[1_000, 20_000], Gen.OneOf(1, 2, 16));

    [Fact]
    public void AParseOfDeeplyNestedChoicesHoldsTwoSegmentsAndOneItem() =>
        _runs.Sample(static (seed, depth, length, segmentSize) =>
        {
            var grammar = new NestedChoices(seed, depth);
            var input = grammar.Input(length);
            var characters = new MeasuringUpstream<char>(new ArrayUpstream<char>(input.ToCharArray()));
            var cursor = new Cursor<char>(new StreamBuffer<char>(characters, segmentSize)) { Failures = new FailureLog<char>() };
            var plan = new PlanBuilder(LoweringOptions.All).Lower(grammar.Parser);
            var value = default(Unit);

            return plan.TryParse(ref cursor, ref value)
                && cursor.Position == input.Length
                && characters.MostHeld <= 2 * segmentSize + 1;
        }, iter: 50);

    private static readonly Layer<char, char>[] _lexers =
    [
        Layer.Tokens(Any),
        Layer.Transduce(0, Any.Select(static c => Emit.One(c))),
    ];

    private static bool BoundedUnder(Layer<char, char> lexer, Parser<char, Unit> parser, string input, int segmentSize)
    {
        var characters = new MeasuringUpstream<char>(new ArrayUpstream<char>(input.ToCharArray()));
        var tokens = new MeasuringUpstream<char>(lexer.Open(new StreamBuffer<char>(characters, segmentSize), ParseOptions.Default, recordFailures: true));
        var cursor = new Cursor<char>(new StreamBuffer<char>(tokens, segmentSize)) { Failures = new FailureLog<char>() };
        var plan = new PlanBuilder(LoweringOptions.All).Lower(parser);
        var value = default(Unit);

        return plan.TryParse(ref cursor, ref value)
            && cursor.Position == input.Length
            && tokens.MostHeld <= 2 * segmentSize + 1
            && characters.MostHeld <= 3 * segmentSize + 2;
    }

    [Fact]
    public void ALexerAndAParserOfDeeplyNestedChoicesHoldAConstantNumberOfItemsAtEachBoundary() => _runs.Sample(
        static (seed, depth, length, segmentSize) =>
        {
            var grammar = new NestedChoices(seed, depth);
            var input = grammar.Input(length);

            return _lexers.All(lexer => BoundedUnder(lexer, grammar.Parser, input, segmentSize));
        },
        iter: 50
    );
}
