namespace Creole.Tests;

public class NodeCompletenessTests
{
    private const string Friend = "Creole.Tests";

    [Fact]
    public void AnotherAssemblyCannotDeriveANode()
    {
        var errors = SnippetCompiler.Compile("Outsider", """
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
        var errors = SnippetCompiler.Compile(Friend, """
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

        var errors = SnippetCompiler.Compile(Friend, $$"""
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
        var errors = SnippetCompiler.Compile(Friend, """
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
}
