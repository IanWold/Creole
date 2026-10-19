using System.Reflection;

namespace Creole.Tests;

public class LongestNodeTests
{
    private static readonly Parser<char, int> _parserA = Grammar<char>.Return(1);
    private static readonly Parser<char, int> _parserB = Grammar<char>.Return(2);
    private static readonly Parser<char, int> _parserC = Grammar<char>.Return(3);

    public static TheoryData<int> LongestArities => [.. Enumerable.Range(2, 15)];

    private static Parser<char, int> InvokeLongest(object?[] alternatives, string[] expressions)
    {
        var method = LongestOverload(alternatives.Length);

        try
        {
            return (Parser<char, int>)method.Invoke(null, [.. alternatives, .. expressions])!;
        }
        catch (TargetInvocationException e) when (e.InnerException is not null)
        {
            throw e.InnerException;
        }
    }

    private static MethodInfo LongestOverload(int arity) =>
        typeof(Grammar<char>)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(Grammar<>.Longest) && m.GetParameters().Length == 2 * arity)
            .MakeGenericMethod(typeof(int));

    private static string NullMessage(string expression) =>
        $"'{expression}' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => {expression}).";

    [Fact]
    public void AnyNodeCanBeAnAlternative()
    {
        var custom = new CountingNode<int>(_parserA);
        var node = Assert.IsType<LongestNode<char, int>>(Grammar<char>.Longest(_parserB, custom));

        Assert.Equal([_parserB, custom], node.Alternatives);
    }

    [Theory]
    [MemberData(nameof(LongestArities))]
    public void EachLongestOverloadNamesTheNullAlternative(int arity)
    {
        var expressions = Enumerable.Range(1, arity).Select(i => $"alternative{i}").ToArray();

        for (var i = 0; i < arity; i++)
        {
            var alternatives = Enumerable.Range(0, arity).Select(j => j == i ? null : (object)_parserA).ToArray();

            Assert.Equal(NullMessage(expressions[i]), Assert.Throws<GrammarException>(() => InvokeLongest(alternatives, expressions)).Message);
        }
    }

    [Theory]
    [MemberData(nameof(LongestArities))]
    public void LongestBuildsOneLongestNodeWithItsAlternativesInOrder(int arity)
    {
        var alternatives = Enumerable.Range(0, arity).Select(i => Grammar<char>.Return(i)).ToArray();
        var node = Assert.IsType<LongestNode<char, int>>(InvokeLongest(alternatives, [.. Enumerable.Repeat("", arity)]));

        Assert.Equal(NodeKind.Longest, ((INode)node).Kind);
        Assert.Equal(alternatives, node.Alternatives);
        Assert.Equal(alternatives, ((INode)node).Children);
    }

    [Fact]
    public void LongestIsOverloadedForTwoToSixteenAlternatives() =>
        Assert.Equal(
            Enumerable.Range(2, 15),
            typeof(Grammar<char>)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(Grammar<>.Longest))
            .Select(m => m.GetParameters().Length / 2)
            .Order()
        );

    [Fact]
    public void LongestKeepsNestedLongestsAsWritten()
    {
        var inner = Grammar<char>.Longest(_parserA, _parserB);
        var node = Assert.IsType<LongestNode<char, int>>(Grammar<char>.Longest(inner, _parserC));

        Assert.Equal([inner, _parserC], node.Alternatives);
    }

    [Fact]
    public void LongestNamesTheCallersExpression() =>
        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => Grammar<char>.Longest(_parserA, _parserB, (Parser<char, int>)null!)).Message);
}
