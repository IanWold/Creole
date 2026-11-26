namespace Creole.Samples.Indentation;

/// <summary>
/// A whole program.
/// </summary>
/// <param name="Body">Its top-level statements.</param>
public sealed record Script(IReadOnlyList<Statement> Body)
{
    /// <summary>
    /// The program written out in one standard form: four spaces for each block, no blank lines, and every operation in parentheses.
    /// </summary>
    public override string ToString() =>
        Write(Body, "");

    private static string Write(IReadOnlyList<Statement> statements, string indentation) =>
        string.Concat(statements.Select(statement => statement switch
        {
            IfStatement conditional => $"{indentation}if {conditional.Condition}:\n{Write(conditional.Body.Body, indentation + "    ")}",
            WhileStatement loop => $"{indentation}while {loop.Condition}:\n{Write(loop.Body.Body, indentation + "    ")}",
            Assignment assignment => $"{indentation}{assignment.Name} = {assignment.Value}\n",
        }));
}
