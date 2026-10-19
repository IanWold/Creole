using System.Reflection;
using System.Runtime.CompilerServices;

namespace Creole.Tests;

public class ParseResultTests
{
    private static class ScanTarget
    {
        public static ParseResult<int> Direct() => new Parsed<int>(1);

        public static Task<ParseResult<int>> Wrapped() => Task.FromResult<ParseResult<int>>(new Parsed<int>(1));

        public static ParseResult<int>[] Many { get; } = [];

        public static int Unrelated() => 0;
    }

    private sealed record JsonValue(string Text);

    private sealed class JsonStandIn(Diagnostics errors)
    {
        public ParseResult<JsonValue> Parse(string text) => text switch
        {
            "ok" => new Parsed<JsonValue>(new JsonValue(text)),
            "recovered" => new Recovered<JsonValue>(new JsonValue(text), errors),
            _ => new Failed(errors),
        };
    }

    private static readonly HashSet<string> _coveredMembers = [];
    private static readonly Diagnostics _oneError = new([ new ParseError(new SourceSpan(SourceId.Unnamed, 3, 4), "parser", new UnexpectedItem("'x'"), [new ExpectedItem("','")], []) ]);

    private static string Summarize(string text)
    {
        var json = new JsonStandIn(_oneError);

        var summary = json.Parse(text) switch
        {
            Parsed<JsonValue> => "ok",
            Recovered<JsonValue> r => $"parsed with {r.Diagnostics.Count} errors",
            Failed f => f.Diagnostics.Render(),
        };

        return summary;
    }

    [Fact]
    public void WhitepaperSwitchCoversEveryCase()
    {
        Assert.Equal("ok", Summarize("ok"));
        Assert.Equal("parsed with 1 errors", Summarize("recovered"));
        Assert.Equal("error[parser] at 3..4: unexpected 'x'; expected ','", Summarize("{"));
    }

    private const string Consumer = """
        sealed record JsonValue;

        static class Consumer
        {
            static Creole.ParseResult<JsonValue> Parse(string text) => new Creole.Parsed<JsonValue>(new JsonValue());

            static string Summary(string text) => Parse(text) switch
            {
                Creole.Parsed<JsonValue>         => "ok",
                Creole.Recovered<JsonValue> r    => $"parsed with {r.Diagnostics.Count} errors",
                {{LastArm}}
            };
        }
        """;

    [Fact]
    public void SwitchInAnotherAssemblyNeedsNoDefaultArm()
    {
        var errors = SnippetCompiler.Compile("Outsider", Consumer.Replace("{{LastArm}}", "Creole.Failed f => f.Diagnostics.Render(),", StringComparison.Ordinal));

        Assert.Empty(errors);
    }

    [Fact]
    public void SwitchMissingACaseFailsTheBuild()
    {
        var errors = SnippetCompiler.Compile("Outsider", Consumer.Replace("{{LastArm}}", "", StringComparison.Ordinal));

        Assert.Equal(["CS8509"], errors);
    }

    [Fact]
    public void DiagnosticsComeFromTheCase()
    {
        ParseResult<int> parsed = new Parsed<int>(1);
        ParseResult<int> recovered = new Recovered<int>(1, _oneError);
        ParseResult<int> failed = new Failed(_oneError);

        Assert.Same(Diagnostics.Empty, parsed.Diagnostics);
        Assert.Same(_oneError, recovered.Diagnostics);
        Assert.Same(_oneError, failed.Diagnostics);
    }

    [Fact]
    public void RecoveredAndFailedNeedAnError()
    {
        Assert.Throws<ArgumentException>(() => new Recovered<int>(1, Diagnostics.Empty));
        Assert.Throws<ArgumentException>(() => new Failed(Diagnostics.Empty));
        Assert.Throws<ArgumentNullException>(() => new Recovered<int>(1, null!));
        Assert.Throws<ArgumentNullException>(() => new Failed(null!));
    }

    [Fact]
    public void RecoveredDeconstructs()
    {
        var (value, diagnostics) = new Recovered<int>(7, _oneError);

        Assert.Equal(7, value);
        Assert.Same(_oneError, diagnostics);
    }

    [Fact]
    public void DefaultMatchesOnlyANullArm()
    {
        var result = default(ParseResult<int>);

        var described = result switch
        {
            Parsed<int> => "parsed",
            Recovered<int> => "recovered",
            Failed => "failed",
            null => "default",
        };

        Assert.Equal("default", described);
    }

    [Fact]
    public void DefaultHasNoDiagnostics() =>
        Assert.Throws<SwitchExpressionException>(() => default(ParseResult<int>).Diagnostics);

    [Fact]
    public void DefaultThrowsInASwitchWithoutANullArm()
    {
        var result = default(ParseResult<int>);

        Assert.Throws<SwitchExpressionException>(() => result switch
        {
            Parsed<int> => 0,
            Recovered<int> => 1,
            Failed => 2,
        });
    }


    [Fact]
    public void EveryPublicMemberReturningAResultIsCovered()
    {
        var uncovered = MembersReturningAResult(typeof(Parser<,>).Assembly.GetExportedTypes().Where(type => !IsParseResult(type)))
            .Where(member => !_coveredMembers.Contains(member));

        Assert.Empty(uncovered);
    }

    [Fact]
    public void TheMemberScanFindsResultTypes() =>
        Assert.Equal(
            ["ScanTarget.Direct", "ScanTarget.Wrapped", "ScanTarget.get_Many", "ScanTarget.Many"],
            MembersReturningAResult([typeof(ScanTarget)])
        );

    private static IEnumerable<string> MembersReturningAResult(IEnumerable<Type> types)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (var type in types)
        {
            var returned = type.GetMethods(flags).Select(method => (method.Name, method.ReturnType))
                .Concat(type.GetProperties(flags).Select(property => (property.Name, property.PropertyType)))
                .Concat(type.GetFields(flags).Select(field => (field.Name, field.FieldType)));

            foreach (var (name, returnType) in returned)
            {
                if (Mentions(returnType))
                {
                    yield return $"{type.Name}.{name}";
                }
            }
        }
    }

    private static bool Mentions(Type type) =>
        IsParseResult(type)
        || (type.HasElementType && Mentions(type.GetElementType()!))
        || (type.IsGenericType && type.GetGenericArguments().Any(Mentions));

    private static bool IsParseResult(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ParseResult<>);
}
