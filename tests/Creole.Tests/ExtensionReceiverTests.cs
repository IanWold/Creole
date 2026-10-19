using System.Reflection;
using System.Runtime.CompilerServices;

namespace Creole.Tests;

file static class ReceiverFixtures
{
    extension(Parser<char, int> parser)
    {
        public Parser<char, int> Guarded([CallerArgumentExpression(nameof(parser))] string _ = "") =>
            parser;

        public Parser<char, int> Unguarded() =>
            parser;
    }

    extension(string text)
    {
        public string NotOnAParser() =>
            text;
    }
}

public class ExtensionReceiverTests
{
    private static string Describe(MethodInfo member) =>
        $"{member.DeclaringType!.Name}.{member.Name}({string.Join(", ", member.GetParameters().Skip(1).Select(parameter => parameter.ParameterType.Name))})";

    private static List<MethodInfo> ExtensionMembersOnParsers(IEnumerable<Type> types) => [..
        types
        .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
        .Where(method => method.IsDefined(typeof(ExtensionAttribute), false) && method.GetParameters() is [var receiver, ..] && IsParser(receiver.ParameterType)),
    ];

    private static bool IsParser(Type type) =>
        (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Parser<,>)) || (type.BaseType is { } baseType && IsParser(baseType));

    private static bool NamesItsReceiver(MethodInfo member)
    {
        var parameters = member.GetParameters();

        return parameters.Any(parameter => parameter.GetCustomAttribute<CallerArgumentExpressionAttribute>()?.ParameterName == parameters[0].Name);
    }

    [Fact]
    public void EveryExtensionMemberOnAParserNamesItsReceiver()
    {
        var members = ExtensionMembersOnParsers(typeof(Parser<,>).Assembly.GetExportedTypes());

        Assert.NotEmpty(members);
        Assert.Empty(members.Where(member => !NamesItsReceiver(member)).Select(Describe));
    }

    [Fact]
    public void TheCheckReportsOnlyAMemberThatDoesNotNameItsReceiver()
    {
        var members = ExtensionMembersOnParsers([typeof(ReceiverFixtures)]);

        Assert.Equal(["Guarded", "Unguarded"], members.Select(member => member.Name).Order());
        Assert.Equal(["Unguarded"], members.Where(member => !NamesItsReceiver(member)).Select(member => member.Name));
    }
}
