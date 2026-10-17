using System.Reflection;
using System.Runtime.Versioning;

namespace Creole.Tests;

public class ScaffoldingTests
{
    [Theory]
    [InlineData("Creole")]
    [InlineData("Creole.Testing")]
    public void RuntimeLibrariesTargetNet11(string assemblyName)
    {
        var assembly = Assembly.Load(assemblyName);

        var framework = assembly.GetCustomAttribute<TargetFrameworkAttribute>();

        Assert.Equal(".NETCoreApp,Version=v11.0", framework?.FrameworkName);
    }
}
