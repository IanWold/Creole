namespace Creole;

/// <summary>
/// Computes a parser's value from the input it matched. The cursor stands just after the match, which began at <paramref name="start"/>.
/// </summary>
/// <remarks>
/// A compiled automaton finds where a match ends but not how it was parsed, so it produces a value directly only when the value depends on the matched input alone.
/// Each function computes its value the way the node's own plan does, so the two agree on the value and on what they allocate.
/// </remarks>
internal delegate T MatchValue<T>(ref Cursor<char> cursor, long start);
