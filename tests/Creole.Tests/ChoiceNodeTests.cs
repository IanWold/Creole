using System.Reflection;

namespace Creole.Tests;

public class ChoiceNodeTests
{
    private static readonly Parser<char, int> _parserA = Grammar<char>.Return(1);
    private static readonly Parser<char, int> _parserB = Grammar<char>.Return(2);
    private static readonly Parser<char, int> _parserC = Grammar<char>.Return(3);

    public static TheoryData<int> OneOfArities => [.. Enumerable.Range(2, 15)];

    private static Parser<char, int> InvokeOneOf(object?[] alternatives, string[] expressions)
    {
        var method = OneOfOverload(alternatives.Length);

        try
        {
            return (Parser<char, int>)method.Invoke(null, [.. alternatives, .. expressions])!;
        }
        catch (TargetInvocationException e) when (e.InnerException is not null)
        {
            throw e.InnerException;
        }
    }

    private static string NullMessage(string expression) =>
        $"'{expression}' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => {expression}).";

    private static MethodInfo OneOfOverload(int arity) =>
        typeof(Grammar<char>)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(m => m.Name == nameof(Grammar<>.OneOf) && m.GetParameters().Length == 2 * arity)
            .MakeGenericMethod(typeof(int));

    [Theory]
    [MemberData(nameof(OneOfArities))]
    public void EachOneOfOverloadNamesTheNullAlternative(int arity)
    {
        var expressions = Enumerable.Range(1, arity).Select(i => $"alternative{i}").ToArray();

        for (var i = 0; i < arity; i++)
        {
            var alternatives = Enumerable.Range(0, arity).Select(j => j == i ? null : (object)_parserA).ToArray();

            Assert.Equal(NullMessage(expressions[i]), Assert.Throws<GrammarException>(() => InvokeOneOf(alternatives, expressions)).Message);
        }
    }

    [Theory]
    [MemberData(nameof(OneOfArities))]
    public void OneOfBuildsOneOrNodeWithItsAlternativesInOrder(int arity)
    {
        var alternatives = Enumerable.Range(0, arity).Select(i => Grammar<char>.Return(i)).ToArray();
        var node = Assert.IsType<OrNode<char, int>>(InvokeOneOf(alternatives, [.. Enumerable.Repeat("", arity)]));

        Assert.Equal(NodeKind.Or, ((INode)node).Kind);
        Assert.Equal(alternatives, node.Alternatives);
        Assert.Equal(alternatives, ((INode)node).Children);
    }

    [Fact]
    public void OneOfIsOverloadedForTwoToSixteenAlternatives()
    {
        var arities = typeof(Grammar<char>)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == nameof(Grammar<>.OneOf))
            .Select(m => m.GetParameters().Length / 2)
            .Order();

        Assert.Equal(Enumerable.Range(2, 15), arities);
    }

    [Fact]
    public void OneOfNamesTheCallersExpression()
    {
        Parser<char, int> later = null!;

        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => Grammar<char>.OneOf(_parserA, _parserB, later)).Message);
    }

    [Fact]
    public void OrBuildsOneOrNodeWithBothAlternativesInOrder()
    {
        var node = Assert.IsType<OrNode<char, int>>(_parserA.Or(_parserB));

        Assert.Equal(NodeKind.Or, ((INode)node).Kind);
        Assert.Equal([_parserA, _parserB], node.Alternatives);
        Assert.Equal([_parserA, _parserB], ((INode)node).Children);
    }

    [Fact]
    public void OrKeepsNestedChoicesAsWritten()
    {
        var inner = _parserA.Or(_parserB);
        var node = Assert.IsType<OrNode<char, int>>(inner.Or(_parserC));

        Assert.Equal([inner, _parserC], node.Alternatives);
    }

    [Fact]
    public void OrNamesANullAlternative()
    {
        Parser<char, int> later = null!;

        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => _parserA.Or(later)).Message);
    }

    [Fact]
    public void OrNamesANullReceiver()
    {
        Parser<char, int> later = null!;

        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => later.Or(_parserA)).Message);
    }
}
