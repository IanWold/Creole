namespace Creole;

internal sealed partial class PlanBuilder
{
    /// <summary>
    /// The most states a <c>Longest</c>'s automaton may have; a larger one runs on the interpreter.
    /// </summary>
    /// <remarks>
    /// A 12-rule JSON lexer has 39 states and lowers in about 0.9 ms; a 138-rule C#-like lexer has 513 and lowers in about 107 ms, roughly 0.2 ms a state.
    /// The cap is about four times the larger, so lowering a <c>Longest</c> stays under about half a second.
    /// </remarks>
    public const int DfaStateCap = 2_000;

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

        if (CommitAutomaton.Build(terms, _dfaStateCap) is not { } automaton)
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
