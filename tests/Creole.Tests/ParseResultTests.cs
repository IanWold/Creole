using System.Reflection;
using System.Runtime.CompilerServices;

namespace Creole.Tests;

public class ParseResultTests
{
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

    private static readonly HashSet<string> _coveredMembers =
    [
        "Creole.Parsing: Creole.ParseResult`1[TOut] Parse[TOut](Creole.Parser`2[System.Char,TOut], System.String, System.String, Creole.ParseOptions, System.String)",
        "Creole.Parsing: Creole.ParseResult`1[TOut] Parse[TOut](Creole.Parser`2[System.Char,TOut], System.ReadOnlyMemory`1[System.Char], System.String, Creole.ParseOptions, System.String)",
    ];
    private static readonly Diagnostics _oneError = new([new ParseError(new SourceSpan(SourceId.Unnamed, 3, 4), "parser", new UnexpectedItem("'x'"), [new ExpectedItem("','")], [])]);

    private static bool IsAccessor(MethodInfo method) =>
        method.IsSpecialName && (method.Name.StartsWith("get_", StringComparison.Ordinal) || method.Name.StartsWith("set_", StringComparison.Ordinal));

    private static bool IsParseResult(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ParseResult<>);

    private static bool IsVisible(bool isPublic, bool isProtected, bool isProtectedInternal) =>
        isPublic || isProtected || isProtectedInternal;

    // Public and protected methods, properties and fields whose type, return type or out or ref parameter mentions
    // ParseResult<T>, keyed by declaring type and full signature so that overloads are told apart. The special-name
    // types the compiler emits for an extension block are skipped: their members only mark the static methods it
    // implements the block with, which are scanned.
    private static IEnumerable<string> MembersExposingAResult(IEnumerable<Type> types)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (var type in types.Where(type => !type.IsSpecialName))
        {
            var methods = type.GetMethods(flags)
                .Where(method => IsVisible(method.IsPublic, method.IsFamily, method.IsFamilyOrAssembly) && !IsAccessor(method))
                .Where(method => Mentions(method.ReturnType) || method.GetParameters().Any(parameter => parameter.ParameterType.IsByRef && Mentions(parameter.ParameterType)))
                .Select(method => method.ToString());

            var properties = type.GetProperties(flags)
                .Where(property => property.GetAccessors(nonPublic: true).Any(accessor => IsVisible(accessor.IsPublic, accessor.IsFamily, accessor.IsFamilyOrAssembly)))
                .Where(property => Mentions(property.PropertyType))
                .Select(property => property.ToString());

            var fields = type.GetFields(flags)
                .Where(field => IsVisible(field.IsPublic, field.IsFamily, field.IsFamilyOrAssembly) && Mentions(field.FieldType))
                .Select(field => field.ToString());

            foreach (var member in methods.Concat(properties).Concat(fields))
            {
                yield return $"{type.FullName}: {member}";
            }
        }
    }

    private static bool Mentions(Type type) =>
        IsParseResult(type)
        || (type.HasElementType && Mentions(type.GetElementType()!))
        || (type.IsGenericType && type.GetGenericArguments().Any(Mentions));

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
    public void DefaultHasNoDiagnostics() =>
        Assert.Throws<SwitchExpressionException>(() => default(ParseResult<int>).Diagnostics);

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
    public void EveryPublicMemberReturningAResultIsCovered()
    {
        var uncovered = MembersExposingAResult(typeof(Parser<,>).Assembly.GetExportedTypes())
            .Where(member => !_coveredMembers.Contains(member));

        Assert.Empty(uncovered);
    }

    [Fact]
    public void FailedDeconstructs()
    {
        ParseResult<int> result = new Failed(_oneError);

        Assert.True(result is Failed(var diagnostics) && ReferenceEquals(diagnostics, _oneError));
    }

    [Fact]
    public void ParseNeverReturnsDefault()
    {
        Parser<char, char> nested = null!;
        nested = CharGrammar.Char('[').Then(Grammar<char>.Rec(() => nested)).Or(CharGrammar.Char('x'));
        var shallow = new ParseOptions { MaxDepth = 1 };
        ParseResult<char>[] results =
        [
            nested.Parse("x"),
            nested.Parse("y"),
            nested.Parse("[[x", options: shallow),
            nested.Parse("x".AsMemory()),
            nested.Parse("y".AsMemory()),
            nested.Parse("[[x".AsMemory(), options: shallow),
        ];

        Assert.All(results, result => Assert.NotNull(result.Value));
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
    public void TheMemberScanSkipsExtensionBlockMarkers()
    {
        var target = typeof(ExtensionScanTarget);

        Assert.Equal(
            [$"{target.FullName}: Creole.ParseResult`1[System.Int32] Parsed(System.String)"],
            MembersExposingAResult([target, .. target.GetNestedTypes()])
        );
    }

    [Fact]
    public void TheMemberScanFindsResultTypes()
    {
        var target = typeof(ScanTarget).FullName;

        Assert.Equal(
            [
                $"{target}: Boolean TryGet(Creole.ParseResult`1[System.Int32] ByRef)",
                $"{target}: Creole.ParseResult`1[System.Int32] Direct()",
                $"{target}: Creole.ParseResult`1[System.Int32] Direct(System.String)",
                $"{target}: Creole.ParseResult`1[System.Int32] Inherited()",
                $"{target}: Creole.ParseResult`1[System.Int32][] Field",
                $"{target}: Creole.ParseResult`1[System.Int32][] Many",
                $"{target}: System.Threading.Tasks.Task`1[Creole.ParseResult`1[System.Int32]] Wrapped()",
            ],
            MembersExposingAResult([typeof(ScanTarget)]).Order(StringComparer.Ordinal)
        );
    }

    [Fact]
    public void WhitepaperSwitchCoversEveryCase()
    {
        Assert.Equal("ok", Summarize("ok"));
        Assert.Equal("parsed with 1 errors", Summarize("recovered"));
        Assert.Equal("error[parser] at 3..4: unexpected 'x'; expected ','", Summarize("{"));
    }
}

file abstract class ScanTarget
{
    public static readonly ParseResult<int>[] Field = [];

    public abstract ParseResult<int>[] Many { get; }

    protected abstract ParseResult<int> Inherited();

    internal abstract ParseResult<int> Hidden();

    public abstract void Consume(ParseResult<int> result);

    public abstract ParseResult<int> Direct();

    public abstract ParseResult<int> Direct(string text);

    public abstract bool TryGet(out ParseResult<int> result);

    public abstract int Unrelated();

    public abstract Task<ParseResult<int>> Wrapped();
}

file static class ExtensionScanTarget
{
    extension(string text)
    {
        public ParseResult<int> Parsed() => new Parsed<int>(text.Length);
    }
}

file record JsonValue(string Text);

file class JsonStandIn(Diagnostics errors)
{
    public ParseResult<JsonValue> Parse(string text) => text switch
    {
        "ok" => new Parsed<JsonValue>(new JsonValue(text)),
        "recovered" => new Recovered<JsonValue>(new JsonValue(text), errors),
        _ => new Failed(errors),
    };
}
