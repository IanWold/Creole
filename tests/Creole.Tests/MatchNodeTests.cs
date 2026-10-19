using System.Reflection;

namespace Creole.Tests;

public class MatchNodeTests
{
    private const string Snippet = """
        using Creole;

        public sealed record V1Header(int Id);
        public sealed record V2Header(int Id);
        public union Header(V1Header, V2Header);
        public sealed record Record(int Id, string Body);
        public enum K { Ident, Num }
        public sealed record Ident(string Name);
        public sealed record Num(int Value);

        public union Tok(Ident, Num) : IKinded<K>
        {
            public K Kind => this switch { Ident => K.Ident, Num => K.Num };
        }

        public sealed class Formats : TokenGrammar<Tok, K>
        {
            public static readonly Parser<Tok, Header> HeaderParser = null!;
            public static readonly Parser<Tok, string> BodyV1 = null!;
            public static readonly Parser<Tok, string> BodyV2 = null!;
            public static readonly Parser<Tok, string> Body = null!;
            public static readonly Parser<Tok, Ident> Name = Case<Ident>();

            {{RULE}}
        }
        """;

    private static readonly Parser<char, string> _bodyA = Grammar<char>.Return("a");
    private static readonly Parser<char, string> _bodyB = Grammar<char>.Return("b");
    private static readonly MatchCase<char, string> _failedCase = Grammar<char>.Case(_bodyB, static (Failed _, string body) => body);
    private static readonly MatchCase<char, string> _parsedCase = Grammar<char>.Case(_bodyA, static (Parsed<int> parsed, string body) => $"{parsed.Value}{body}");
    private static readonly Parser<char, ParseResult<int>> _result = Grammar<char>.Return<ParseResult<int>>(new Parsed<int>(1));

    public static TheoryData<int> MatchArities => [.. Enumerable.Range(1, 8)];

    private static string[] CompileRule(string rule) =>
        SnippetCompiler.Compile("Snippet", Snippet.Replace("{{RULE}}", rule, StringComparison.Ordinal));

    /// <summary>
    /// Builds <paramref name="count"/> cases, each for a different type, so none is a duplicate.
    /// </summary>
    private static MatchCase<char, string>[] DistinctCases(int count) =>
    [
        .. new[]
        {
            Grammar<char>.Case(_bodyA, static (Parsed<int> _, string body) => body),
            Grammar<char>.Case(_bodyA, static (Recovered<int> _, string body) => body),
            Grammar<char>.Case(_bodyA, static (Failed _, string body) => body),
            Grammar<char>.Case(_bodyA, static (Parsed<string> _, string body) => body),
            Grammar<char>.Case(_bodyA, static (Recovered<string> _, string body) => body),
            Grammar<char>.Case(_bodyA, static (Parsed<long> _, string body) => body),
            Grammar<char>.Case(_bodyA, static (Recovered<long> _, string body) => body),
            Grammar<char>.Case(_bodyA, static (Parsed<char> _, string body) => body),
        }.Take(count)
    ];

    private static MethodInfo FixedOverload(int arity) =>
        MatchOverloads()
        .Single(m => m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(MatchCase<,>) && m.GetParameters().Length == 2 * arity + 2)
        .MakeGenericMethod(typeof(char), typeof(ParseResult<int>), typeof(string));

    private static Parser<char, string> InvokeFixed(object? scrutinee, object?[] cases, string[] caseExpressions)
    {
        try
        {
            return (Parser<char, string>)FixedOverload(cases.Length).Invoke(null, [scrutinee, .. cases, "scrutinee", .. caseExpressions])!;
        }
        catch (TargetInvocationException e) when (e.InnerException is not null)
        {
            throw e.InnerException;
        }
    }

    private static IEnumerable<MethodInfo> MatchOverloads() =>
        typeof(Choice).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == "Match");

    private static string NullMessage(string expression) =>
        $"'{expression}' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => {expression}).";

    [Fact]
    public void ACollectionOfCasesBuildsTheSameNode()
    {
        var node = Assert.IsType<MatchNode<char, ParseResult<int>, string>>(_result.Match([_parsedCase, _failedCase]));

        Assert.Equal([_parsedCase, _failedCase], node.Cases);
        Assert.Equal([_result, _bodyA, _bodyB], ((INode)node).Children);
    }

    [Fact]
    public void ACollectionOfCasesIsCopied()
    {
        List<MatchCase<char, string>> cases = [_parsedCase];
        var node = Assert.IsType<MatchNode<char, ParseResult<int>, string>>(_result.Match(cases));

        cases[0] = _failedCase;

        Assert.Equal([_parsedCase], node.Cases);
    }

    [Fact]
    public void ACollectionWithANullCaseNamesItsIndex() =>
        Assert.Equal("Case 1 of the Match is null.", Assert.Throws<GrammarException>(() => _result.Match([_parsedCase, null!])).Message);

    [Fact]
    public void ACollectionWithDuplicateCaseTypesNamesTheirIndexes()
    {
        var again = Grammar<char>.Case(_bodyB, static (Parsed<int> _, string body) => body);

        Assert.Equal(
            "Two cases of a Match are for the same type: case 0 and case 2. Each case type may appear in only one case.",
            Assert.Throws<GrammarException>(() => _result.Match([_parsedCase, _failedCase, again])).Message
        );
    }

    [Fact]
    public void AnEmptyCollectionIsRejected()
    {
        MatchCase<char, string>[] cases = [];

        Assert.Equal("'cases' has no cases.", Assert.Throws<GrammarException>(() => _result.Match(cases)).Message);
    }

    [Fact]
    public void ANullCollectionIsRejected()
    {
        MatchCase<char, string>[] cases = null!;

        Assert.Equal("'cases' is null.", Assert.Throws<GrammarException>(() => _result.Match(cases)).Message);
    }

    [Fact]
    public void CaseKeepsItsBranchCombineAndCaseType()
    {
        var typed = Assert.IsType<TypedMatchCase<char, Parsed<int>, string, string>>(_parsedCase);

        Assert.Same(_bodyA, typed.Branch);
        Assert.Equal("1a", typed.Combine(new Parsed<int>(1), "a"));
        Assert.Equal(typeof(Parsed<int>), typed.CaseType);
    }

    [Fact]
    public void CaseNamesANullBranchOrCombine()
    {
        Parser<char, string> later = null!;
        Func<Parsed<int>, string, string> combine = null!;

        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => Grammar<char>.Case(later, static (Parsed<int> _, string body) => body)).Message);
        Assert.Equal("'combine' is null.", Assert.Throws<GrammarException>(() => Grammar<char>.Case(_bodyA, combine)).Message);
    }

    [Fact]
    public void DuplicateCaseTypesNameBothCases()
    {
        var again = Grammar<char>.Case(_bodyB, static (Parsed<int> _, string body) => body);

        Assert.Equal(
            "Two cases of a Match are for the same type: '_parsedCase' and 'again'. Each case type may appear in only one case.",
            Assert.Throws<GrammarException>(() => _result.Match(_parsedCase, again)).Message
        );
    }

    [Theory]
    [MemberData(nameof(MatchArities))]
    public void EachMatchOverloadKeepsItsCasesInOrder(int arity)
    {
        var cases = DistinctCases(arity);
        var node = Assert.IsType<MatchNode<char, ParseResult<int>, string>>(InvokeFixed(_result, cases, [.. Enumerable.Repeat("", arity)]));

        Assert.Equal(cases, node.Cases);
        Assert.Equal([_result, .. Enumerable.Repeat(_bodyA, arity)], ((INode)node).Children);
    }

    [Theory]
    [MemberData(nameof(MatchArities))]
    public void EachMatchOverloadNamesANullCaseOrScrutinee(int arity)
    {
        var expressions = Enumerable.Range(1, arity).Select(i => $"case{i}").ToArray();

        Assert.Equal(NullMessage("scrutinee"), Assert.Throws<GrammarException>(() => InvokeFixed(null, DistinctCases(arity), expressions)).Message);

        for (var i = 0; i < arity; i++)
        {
            var cases = DistinctCases(arity).Select((c, j) => j == i ? null : c).ToArray();

            Assert.Equal($"'{expressions[i]}' is null.", Assert.Throws<GrammarException>(() => InvokeFixed(_result, cases, expressions)).Message);
        }
    }

    [Fact]
    public void MatchBuildsOneNodeWithEveryBranchReachable()
    {
        var node = Assert.IsType<MatchNode<char, ParseResult<int>, string>>(_result.Match(_parsedCase, _failedCase));
        var view = (IMatchNode)node;

        Assert.Equal(NodeKind.Match, view.Kind);
        Assert.Same(_result, node.Scrutinee);
        Assert.Same(_result, view.Scrutinee);
        Assert.Equal(typeof(ParseResult<int>), view.UnionType);
        Assert.Equal([new MatchCaseInfo(typeof(Parsed<int>), _bodyA), new MatchCaseInfo(typeof(Failed), _bodyB)], view.Cases);
        Assert.Equal([_result, _bodyA, _bodyB], view.Children);
    }

    [Fact]
    public void MatchIsOverloadedForOneToEightCasesAndACollection()
    {
        var fixedArities = MatchOverloads()
            .Where(m => m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(MatchCase<,>))
            .Select(m => (m.GetParameters().Length - 2) / 2)
            .Order();

        Assert.Equal(Enumerable.Range(1, 8), fixedArities);
        Assert.Single(MatchOverloads(), m => m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>));
    }

    [Fact]
    public void MatchNamesANullReceiver()
    {
        Parser<char, ParseResult<int>> later = null!;

        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => later.Match(_parsedCase)).Message);
    }

    [Fact]
    public void MatchOverAParserThatIsNotAUnionDoesNotCompile() =>
        Assert.Equal(["CS0311"], CompileRule("""
            public static readonly Parser<Tok, Record> Record =
                Body.Match(Case(BodyV1, static (string s, string body) => new Record(0, body)));
            """
        ));

    [Fact]
    public void TheWhitepapersCaseWithOneTypeArgumentDoesNotCompile() =>
        Assert.Equal(["CS1501"], CompileRule("""
            public static readonly Parser<Tok, Record> Record =
                HeaderParser.Match(
                    Case<V1Header>(BodyV1, static (h, body) => new Record(h.Id, body)),
                    Case<V2Header>(BodyV2, static (h, body) => new Record(h.Id, body))
                );
            """
        ));

    [Fact]
    public void TheWhitepapersMatchCompilesBesideTokenCase() =>
        Assert.Empty(CompileRule("""
            public static readonly Parser<Tok, Record> Record =
                HeaderParser.Match(
                    Case(BodyV1, static (V1Header h, string body) => new Record(h.Id, body)),
                    Case(BodyV2, static (V2Header h, string body) => new Record(h.Id, body))
                );
            """
        ));
}
