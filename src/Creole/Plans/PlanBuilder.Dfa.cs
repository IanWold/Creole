namespace Creole;

internal sealed partial class PlanBuilder
{
    /// <summary>
    /// The most states a <c>Longest</c>'s automaton may have; a larger one runs on the interpreter.
    /// </summary>
    /// <remarks>
    /// A 12-rule JSON lexer has 39 states and lowers in about 0.24 ms; a 138-rule C#-like lexer has 513 and lowers in about 10 ms, allocating about 16 MB.
    /// In a new process, before the construction code is compiled, they take about 30 ms and 85 ms.
    /// The cap is about four times the larger.
    /// </remarks>
    public const int DfaStateCap = 2_000;

    /// <summary>
    /// The most paths building a <c>Longest</c>'s automaton may step, counting every path of a state for each transition explored from it; a costlier build runs on the interpreter.
    /// </summary>
    /// <remarks>
    /// The 12-rule JSON lexer steps 1,767 paths, the 138-rule C#-like lexer 90,816, and 300 keywords and an identifier 135,120.
    /// The budget is about three and a half times the largest. Keyword lexers reach the state cap long before it; it stops builds whose states hold many paths.
    /// </remarks>
    public const long DfaWorkBudget = 500_000;

    private DfaLongestPlan<T>? DfaLongest<T>(IReadOnlyList<Parser<char, T>> alternatives)
    {
        var flat = new List<Parser<char, T>>();
        Flatten(alternatives, flat);
        var terms = new RegularTerm[flat.Count];

        for (var i = 0; i < flat.Count; i++)
        {
            if (Regular.Of(flat[i]) is not { } term)
            {
                return null;
            }

            terms[i] = term;
        }

        if (CommitAutomaton.Build(terms, _dfaStateCap, _dfaWorkBudget) is not { } automaton)
        {
            return null;
        }

        Plan<char, T>[] plans = [.. flat.Select(Lower)];

        return new DfaLongestPlan<T>(automaton, plans, [.. flat.Select(static alternative => alternative.ValueOfMatch())]);
    }

    private static void Flatten<T>(IReadOnlyList<Parser<char, T>> alternatives, List<Parser<char, T>> flat)
    {
        foreach (var alternative in alternatives)
        {
            if (alternative is LongestNode<char, T> nested)
            {
                Flatten(nested.Alternatives, flat);
            }
            else
            {
                flat.Add(alternative);
            }
        }
    }
}
