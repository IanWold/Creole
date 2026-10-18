using System.Reflection;
using Creole.Nodes;

namespace Creole.Tests;

public class NodeModelTests
{
    private static readonly Type[] _nodeTypes = [..
        typeof(Parser<,>).Assembly.GetTypes().Where(type => !type.IsAbstract && DerivesFromParser(type))
    ];

    [Fact]
    public void NodeTypesAreInternalAndSealed()
    {
        Assert.NotEmpty(_nodeTypes);
        Assert.All(_nodeTypes, type =>
        {
            Assert.False(type.IsVisible, $"{type} is public");
            Assert.True(type.IsSealed, $"{type} is not sealed");
        });
    }

    [Fact]
    public void NodeFieldsAreReadOnly()
    {
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        Assert.All(
            _nodeTypes.SelectMany(type => type.GetFields(Instance)),
            field => Assert.True(field.IsInitOnly, $"{field.DeclaringType}.{field.Name} is writable")
        );
    }

    [Fact]
    public void EachNodeKindHasOneNodeType() =>
        Assert.Equal(Enum.GetValues<NodeKind>().Length, _nodeTypes.Length);

    [Fact]
    public void NodesCompareByReference()
    {
        var first = new ReturnNode<char, int>(1);
        var second = new ReturnNode<char, int>(1);

        Assert.Equal(first, first);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ReturnIsALeafHoldingItsValue()
    {
        INode node = new ReturnNode<char, string>("x");

        Assert.Equal(NodeKind.Return, node.Kind);
        Assert.Empty(node.Children);
        Assert.Equal("x", ((ReturnNode<char, string>)node).Value);
    }

    [Fact]
    public void FailIsALeafHoldingItsMessage()
    {
        INode node = new FailNode<char, int>("unsupported version");

        Assert.Equal(NodeKind.Fail, node.Kind);
        Assert.Empty(node.Children);
        Assert.Equal("unsupported version", ((FailNode<char, int>)node).Message);
    }

    [Fact]
    public void FailRejectsANullMessage()
    {
        var exception = Assert.Throws<GrammarException>(() => new FailNode<char, int>(null!));

        Assert.Equal("'message' is null.", exception.Message);
    }

    [Fact]
    public void NullParserArgumentNamesTheExpressionAndSuggestsRec()
    {
        Parser<char, int>? arrayValue = null;

        var exception = Assert.Throws<GrammarException>(() => Guard.NotNull(arrayValue));

        Assert.Equal(
            "'arrayValue' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => arrayValue).",
            exception.Message
        );
    }

    private static bool DerivesFromParser(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Parser<,>))
            {
                return true;
            }
        }

        return false;
    }
}
