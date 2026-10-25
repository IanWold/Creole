using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Creole.Tests;

/// <summary>
/// Compiles small C# snippets against Creole, to check that misuse fails the build.
/// </summary>
internal static class SnippetCompiler
{
    /// <summary>
    /// Compiles <paramref name="source"/> against Creole as assembly <paramref name="assemblyName"/>, at the preview language version the repository builds with and with every warning an error, and returns the distinct error ids.
    /// </summary>
    public static string[] Compile(string assemblyName, string source) => [..
        CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview))],
            ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator)
                .Where(path => Path.GetFileName(path).StartsWith("System.", StringComparison.Ordinal) || Path.GetFileName(path) is "netstandard.dll" or "mscorlib.dll")
                .Append(typeof(Parser<,>).Assembly.Location)
                .Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable)
                .WithGeneralDiagnosticOption(ReportDiagnostic.Error)
                .WithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic>.Empty.Add("CS8524", ReportDiagnostic.Suppress))
        )
        .GetDiagnostics()
        .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
        .Select(diagnostic => diagnostic.Id)
        .Distinct()
    ];
}
