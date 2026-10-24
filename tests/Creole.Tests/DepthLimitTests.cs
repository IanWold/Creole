using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file sealed class Deep : CharGrammar
{
    public static readonly Parser<char, int> Binds = Char('[').Bind(static _ => BindsThenClose!).Or(Char('x').As(0));

    public static readonly Parser<char, int> BindsThenClose = Binds.Before(Char(']')).Select(static depth => depth + 1);

    public static readonly Parser<char, int> Brackets =
        Char('[').Then(Rec(() => Brackets)).Before(Char(']')).Select(static depth => depth + 1)
        .Or(Char('x').As(0));

    public static readonly Parser<char, int> Prefixes =
        Expression.Build(Char('x').As(0), op => [[op.Prefix(Char('-'), static (_, depth) => depth + 1)]]);

    public static readonly Parser<char, int> RightChain =
        Expression.Build(Char('x').As(0), op => [[op.InfixR(Char('^'), static (_, _, depth) => depth + 1)]]);
}

public class DepthLimitTests
{
    private static readonly Dictionary<string, (Parser<char, int> Parser, Func<int, string> Input, Func<int, long> AbortsAt)> _points = new()
    {
        ["Rec"] = (Deep.Brackets, Nested, static depth => depth + 1),
        ["Bind"] = (Deep.Binds, Nested, static depth => depth + 1),
        ["a prefix operand"] = (Deep.Prefixes, static levels => $"{new string('-', levels)}x", static depth => depth + 1),
        ["an InfixR right operand"] = (Deep.RightChain, static levels => $"x{string.Concat(Enumerable.Repeat("^x", levels))}", static depth => 2 * (depth + 1)),
    };

    private static readonly Dictionary<string, Func<Parser<char, int>, Parser<char, int>>> _wrappers = new()
    {
        ["Not"] = static parser => Not(parser).As(-1),
        ["Atomic"] = static parser => Atomic(parser),
        ["Or"] = static parser => parser.Or(Return(-1)),
        ["Atomic inside Or"] = static parser => Atomic(parser).Or(Return(-1)),
    };

    public static TheoryData<string> Points => [.. _points.Keys];

    public static MatrixTheoryData<string, int> PointsAndLimits => new([.. _points.Keys], [5, 100]);

    public static MatrixTheoryData<string, string> PointsAndWrappers => new([.. _points.Keys], [.. _wrappers.Keys]);

    private static DepthExceededException Abort<T>(Parser<char, T> parser, string input, int maxDepth) => Assert.Throws<DepthExceededException>(() =>
    {
        var cursor = new Cursor<char>(input) { MaxDepth = maxDepth };
        Execution.Run(parser, ref cursor);
    });

    private static (bool Ok, int Depth) DepthAfter(Parser<char, int> parser, string input)
    {
        var cursor = new Cursor<char>(input);
        var (ok, _) = Execution.Run(parser, ref cursor);

        return (ok, cursor.Depth);
    }

    private static string Nested(int levels) =>
        $"{new string('[', levels)}x{new string(']', levels)}";

    [Theory]
    [MemberData(nameof(Points))]
    public void AHundredThousandLevelsOnASmallStackEndThroughTheStackCheck(string point)
    {
        var (parser, input, abortsAt) = _points[point];
        var deep = input(100_000);
        Exception? thrown = null;

        var thread = new Thread(
            () =>
            {
                try
                {
                    var cursor = new Cursor<char>(deep);
                    Execution.Run(parser, ref cursor);
                }
                catch (Exception exception)
                {
                    thrown = exception;
                }
            },
            256 * 1024
        );

        thread.Start();
        thread.Join();

        var abort = Assert.IsType<DepthExceededException>(thrown);
        Assert.Equal(DepthCause.Stack, abort.Cause);
        Assert.InRange(abort.Depth, 20, 99_999);
        Assert.Equal(abortsAt(abort.Depth), abort.Position);
    }

    [Theory]
    [MemberData(nameof(Points))]
    public void ALevelIsLeftWhetherItSucceedsOrFails(string point)
    {
        var (parser, input, _) = _points[point];
        var parsed = input(3);
        var last = parsed.LastIndexOf('x');

        Assert.Equal((true, 0), DepthAfter(parser, parsed));
        Assert.Equal((false, 0), DepthAfter(parser, $"{parsed[..last]}y{parsed[(last + 1)..]}"));
    }

    [Theory]
    [MemberData(nameof(PointsAndLimits))]
    public void AnInputAtTheLimitParses(string point, int limit)
    {
        var (parser, input, _) = _points[point];
        var cursor = new Cursor<char>(input(limit)) { MaxDepth = limit };

        Assert.Equal((true, limit), Execution.Run(parser, ref cursor));
        Assert.True(cursor.IsAtEnd);
    }

    [Theory]
    [MemberData(nameof(PointsAndWrappers))]
    public void NotAtomicAndOrDoNotChangeTheOutcome(string point, string wrapper)
    {
        var (parser, input, abortsAt) = _points[point];
        var abort = Abort(_wrappers[wrapper](parser), input(6), 5);

        Assert.Equal((abortsAt(5), 5, DepthCause.MaxDepth), (abort.Position, abort.Depth, abort.Cause));
    }

    [Theory]
    [MemberData(nameof(PointsAndLimits))]
    public void OneLevelMoreEndsTheParseWhereThatLevelStarts(string point, int limit)
    {
        var (parser, input, abortsAt) = _points[point];
        var abort = Abort(parser, input(limit + 1), limit);

        Assert.Equal((abortsAt(limit), limit, DepthCause.MaxDepth), (abort.Position, abort.Depth, abort.Cause));
    }
}
