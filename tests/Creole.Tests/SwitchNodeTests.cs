using System.Reflection;

namespace Creole.Tests;

public class SwitchNodeTests
{
    private static readonly Parser<char, string> _default = Grammar<char>.Fail<string>("unsupported version");
    private static readonly Parser<char, string> _recordV1 = Grammar<char>.Return("v1");
    private static readonly Parser<char, string> _recordV2 = Grammar<char>.Return("v2");
    private static readonly Parser<char, int> _version = Grammar<char>.Return(1);

    public static TheoryData<int> SwitchArities => [.. Enumerable.Range(1, 8)];

    private static MethodInfo FixedOverload(int arity) =>
        SwitchOverloads()
            .Single(m => m.GetParameters()[1].ParameterType.IsGenericType && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(ValueTuple<,>) && m.GetParameters().Length == 2 * arity + 4)
            .MakeGenericMethod(typeof(char), typeof(int), typeof(string));

    private static Parser<char, string> InvokeFixed(object? scrutinee, object?[] branches, object? @default, string[] caseExpressions)
    {
        var cases = branches.Select((branch, i) => (object)(i, (Parser<char, string>?)branch)).ToArray();

        try
        {
            return (Parser<char, string>)FixedOverload(branches.Length).Invoke(null, [scrutinee, .. cases, @default, "scrutinee", .. caseExpressions, "fallback"])!;
        }
        catch (TargetInvocationException e) when (e.InnerException is not null)
        {
            throw e.InnerException;
        }
    }

    private static string NullMessage(string expression) =>
        $"'{expression}' is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => {expression}).";

    private static string NullBranchMessage(string description) =>
        $"The branch in {description} is null. If it refers to a rule declared later in the class, wrap the reference in Rec(() => ...).";

    private static IEnumerable<MethodInfo> SwitchOverloads() =>
        typeof(Choice).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(m => m.Name == "Switch");

    [Fact]
    public void ACollectionOfCasesBuildsTheSameNode()
    {
        var node = Assert.IsType<SwitchNode<char, int, string>>(_version.Switch([(1, _recordV1), (2, _recordV2)], _default));

        Assert.Equal([1, 2], node.Keys);
        Assert.Equal([_recordV1, _recordV2], node.Branches);
        Assert.Same(_default, node.Default);
        Assert.Equal([_version, _recordV1, _recordV2, _default], ((INode)node).Children);
    }

    [Fact]
    public void ACollectionOfCasesIsCopied()
    {
        List<(int, Parser<char, string>)> cases = [(1, _recordV1)];
        var node = Assert.IsType<SwitchNode<char, int, string>>(_version.Switch(cases, _default));

        cases[0] = (9, _recordV2);
        cases.Add((2, _recordV2));

        Assert.Equal([1], node.Keys);
        Assert.Equal([_recordV1], node.Branches);
    }

    [Fact]
    public void ACollectionWithANullBranchNamesItsIndex()
    {
        Parser<char, string> later = null!;

        Assert.Equal(NullBranchMessage("case 1"), Assert.Throws<GrammarException>(() => _version.Switch([(1, _recordV1), (2, later)], _default)).Message);
    }

    [Fact]
    public void ACollectionWithDuplicateKeysNamesTheirIndexes() =>
        Assert.Equal(
            "Two cases of a Switch have the same key: case 0 and case 2. Each key may appear in only one case.",
            Assert.Throws<GrammarException>(() => _version.Switch([(1, _recordV1), (2, _recordV2), (1, _recordV2)], _default)).Message
        );

    [Fact]
    public void AnEmptyCollectionIsRejected()
    {
        (int, Parser<char, string>)[] cases = [];

        Assert.Equal("'cases' has no cases.", Assert.Throws<GrammarException>(() => _version.Switch(cases, _default)).Message);
    }

    [Fact]
    public void ANullCollectionIsRejected()
    {
        (int, Parser<char, string>)[] cases = null!;

        Assert.Equal("'cases' is null.", Assert.Throws<GrammarException>(() => _version.Switch(cases, _default)).Message);
    }

    [Fact]
    public void DuplicateKeysNameBothCases() =>
        Assert.Equal(
            "Two cases of a Switch have the same key: '(1, _recordV1)' and '(1, _recordV2)'. Each key may appear in only one case.",
            Assert.Throws<GrammarException>(() => _version.Switch((1, _recordV1), (1, _recordV2), _default)).Message
        );

    [Theory]
    [MemberData(nameof(SwitchArities))]
    public void EachSwitchOverloadKeepsItsCasesInOrder(int arity)
    {
        var branches = Enumerable.Range(0, arity).Select(i => Grammar<char>.Return($"branch {i}")).ToArray();
        var node = Assert.IsType<SwitchNode<char, int, string>>(InvokeFixed(_version, branches, _default, [.. Enumerable.Repeat("", arity)]));

        Assert.Equal(Enumerable.Range(0, arity), node.Keys);
        Assert.Equal(branches, node.Branches);
        Assert.Equal([_version, .. branches, _default], ((INode)node).Children);
    }

    [Theory]
    [MemberData(nameof(SwitchArities))]
    public void EachSwitchOverloadNamesANullScrutineeOrDefault(int arity)
    {
        var branches = Enumerable.Repeat((object)_recordV1, arity).ToArray();
        var expressions = Enumerable.Repeat("", arity).ToArray();

        Assert.Equal(NullMessage("scrutinee"), Assert.Throws<GrammarException>(() => InvokeFixed(null, branches, _default, expressions)).Message);
        Assert.Equal(NullMessage("fallback"), Assert.Throws<GrammarException>(() => InvokeFixed(_version, branches, null, expressions)).Message);
    }

    [Theory]
    [MemberData(nameof(SwitchArities))]
    public void EachSwitchOverloadNamesTheCaseWithANullBranch(int arity)
    {
        var expressions = Enumerable.Range(1, arity).Select(i => $"case{i}").ToArray();

        for (var i = 0; i < arity; i++)
        {
            var branches = Enumerable.Range(0, arity).Select(j => j == i ? null : (object)_recordV1).ToArray();

            Assert.Equal(NullBranchMessage($"'{expressions[i]}'"), Assert.Throws<GrammarException>(() => InvokeFixed(_version, branches, _default, expressions)).Message);
        }
    }

    [Fact]
    public void SwitchBuildsOneNodeWithEveryBranchReachable()
    {
        var node = Assert.IsType<SwitchNode<char, int, string>>(
            _version.Switch(
                (1, _recordV1),
                (2, _recordV2),
                @default: _default
            )
        );

        Assert.Equal(NodeKind.Switch, ((INode)node).Kind);
        Assert.Same(_version, node.Scrutinee);
        Assert.Equal([1, 2], node.Keys);
        Assert.Equal([_recordV1, _recordV2], node.Branches);
        Assert.Same(_default, node.Default);
        Assert.Equal([_version, _recordV1, _recordV2, _default], ((INode)node).Children);
    }

    [Fact]
    public void SwitchIsOverloadedForOneToEightCasesAndACollection()
    {
        var fixedArities = SwitchOverloads()
            .Where(m => m.GetParameters()[1].ParameterType.IsGenericType && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(ValueTuple<,>))
            .Select(m => (m.GetParameters().Length - 4) / 2)
            .Order();

        Assert.Equal(Enumerable.Range(1, 8), fixedArities);
        Assert.Single(SwitchOverloads(), m => m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>));
    }

    [Fact]
    public void SwitchNamesANullReceiver()
    {
        Parser<char, int> later = null!;

        Assert.Equal(NullMessage("later"), Assert.Throws<GrammarException>(() => later.Switch((1, _recordV1), _default)).Message);
    }
}
