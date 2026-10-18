using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Creole.Tests;

public class NodeCompletenessTests
{
    private const string Friend = "Creole.Tests";

    [Fact]
    public void AnotherAssemblyCannotDeriveANode()
    {
        var errors = Compile("Outsider", """
            sealed class MyParser : Creole.Parser<char, int>
            {
            }
            """
        );

        Assert.Contains("CS1729", errors);
    }

    [Fact]
    public void ANodeMissingAnAbstractMemberDoesNotCompile()
    {
        var errors = Compile(Friend, """
            sealed class Incomplete : Creole.Parser<char, int>
            {
                internal override Creole.NodeKind Kind => Creole.NodeKind.Return;
            }
            """
        );

        Assert.Equal(["CS0534"], errors);
    }

    [Fact]
    public void ASwitchOverEveryNodeKindCompiles()
    {
        var arms = string.Concat(Enum.GetNames<NodeKind>().Select(name => $"Creole.NodeKind.{name} => 0,\n"));

        var errors = Compile(Friend, $$"""
            static class Consumer
            {
                static int Arity(Creole.NodeKind kind) => kind switch
                {
                    {{arms}}
                };
            }
            """
        );

        Assert.Empty(errors);
    }

    [Fact]
    public void ASwitchMissingANodeKindFailsTheBuild()
    {
        var errors = Compile(Friend, """
            static class Consumer
            {
                static int Arity(Creole.NodeKind kind) => kind switch
                {
                    Creole.NodeKind.Return => 0,
                };
            }
            """
        );

        Assert.Equal(["CS8509"], errors);
    }

    private static string[] Compile(string assemblyName, string source)
    {
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithNullableContextOptions(NullableContextOptions.Enable)
            .WithGeneralDiagnosticOption(ReportDiagnostic.Error)
            .WithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic>.Empty.Add("CS8524", ReportDiagnostic.Suppress));

        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            References(),
            options
        );

        return [.. compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.Id)
            .Distinct()
        ];
    }

    private static IEnumerable<MetadataReference> References()
    {
        var platform = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => Path.GetFileName(path).StartsWith("System.", StringComparison.Ordinal) || Path.GetFileName(path) is "netstandard.dll" or "mscorlib.dll");

        return platform
            .Append(typeof(Parser<,>).Assembly.Location)
            .Select(path => MetadataReference.CreateFromFile(path));
    }
}
