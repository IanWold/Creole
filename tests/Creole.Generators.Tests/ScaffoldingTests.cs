using System.Reflection;

namespace Creole.Generators.Tests;

public class ScaffoldingTests
{
    [Fact]
    public void GeneratorsTargetNetStandard20()
    {
        // The SDK emits no TargetFrameworkAttribute for netstandard; the netstandard reference shows the target.
        var assembly = Assembly.Load("Creole.Generators");

        var netstandard = assembly.GetReferencedAssemblies().Single(reference => reference.Name == "netstandard");

        Assert.Equal(new Version(2, 0, 0, 0), netstandard.Version);
    }
}
