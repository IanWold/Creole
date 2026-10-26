using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace Creole.Benchmarks;

/// <summary>
/// What a run measured, written as <c>run.json</c> in its folder: the commit, whether the working tree had uncommitted changes, the versions of the gate baselines, and the arguments.
/// </summary>
/// <remarks>
/// <see cref="Commit"/> and <see cref="Dirty"/> are null when git cannot be run.
/// </remarks>
internal sealed record RunManifest(string? Commit, bool? Dirty, string Parlot, string Pidgin, string[] Arguments)
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private static string? Git(string arguments)
    {
        try
        {
            using var git = Process.Start(new ProcessStartInfo("git", arguments)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            });

            if (git is null)
            {
                return null;
            }

            var output = git.StandardOutput.ReadToEnd();
            git.WaitForExit();

            return git.ExitCode == 0
                ? output.Trim()
                : null;
        }
        catch (Win32Exception)
        {
            return null;
        }
    }

    private static string Version(Type type) =>
        type.Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? type.Assembly.GetName().Version?.ToString()
        ?? "unknown";

    public static RunManifest Create(string[] arguments) =>
        new(
            Git("rev-parse HEAD"),
            Git("status --porcelain") is { } status
                ? status.Length > 0
                : null,
            Version(typeof(Parlot.Fluent.Parsers)),
            Version(typeof(Pidgin.Parser)),
            arguments
        );

    public void Write(string folder)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "run.json"), JsonSerializer.Serialize(this, _options));
    }
}
