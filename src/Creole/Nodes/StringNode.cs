namespace Creole;

/// <summary>
/// Consumes the characters of <see cref="Text"/>, in order. Backs <c>String</c> and <c>CIString</c>.
/// </summary>
/// <remarks>
/// <para>
/// The node is atomic: if any character does not match, it fails without consuming input.
/// </para>
/// <para>
/// With <see cref="IgnoreCase"/>, an input character matches when its <see cref="char.ToLowerInvariant(char)"/> equals that of the text's character,
/// and the node produces the input as written. Without it, the node produces <see cref="Text"/> itself.
/// </para>
/// </remarks>
internal sealed class StringNode : Parser<char, string>, IPrimitiveNode
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.String;

    public FirstSet First { get; }

    public bool IgnoreCase { get; }

    public bool Nullable => false;

    public bool Opaque => false;

    public string Text { get; }

    /// <param name="text">The characters to match. Must not be empty.</param>
    /// <param name="ignoreCase">Whether to compare by invariant lower case.</param>
    /// <param name="combinator">The public combinator's name, for the error message.</param>
    public StringNode(string text, bool ignoreCase, string combinator)
    {
        Text = Guard.NotNull(text);

        if (text.Length == 0)
        {
            throw new GrammarException($"{combinator}(\"\") matches nothing. To succeed without consuming input, use Return(\"\").");
        }

        IgnoreCase = ignoreCase;
        First = FirstSet.OfChars(CharactersAt(0));
    }

    internal override Plan<char, string> Lower(PlanBuilder builder) =>
        PlanBuilder.String(Text, IgnoreCase);

    internal override MatchValue<string> ValueOfMatch()
    {
        var text = Text;

        return IgnoreCase
            ? static (ref Cursor<char> cursor, long start) => cursor.Since(start).ToString()
            : (ref Cursor<char> _, long _) => text;
    }

    /// <summary>
    /// The input characters that match the text's character at <paramref name="index"/>.
    /// </summary>
    public CharSet CharactersAt(int index) =>
        IgnoreCase ? CaseVariants.Of(Text[index]) : CharSet.Of(Text[index]);
}

/// <summary>
/// The characters that match a given character under invariant lower-case comparison.
/// </summary>
file static class CaseVariants
{
    private static readonly Dictionary<char, List<char>> _byLower = Build();

    private static Dictionary<char, List<char>> Build()
    {
        var byLower = new Dictionary<char, List<char>>();

        for (var code = 0; code <= char.MaxValue; code++)
        {
            var c = (char)code;
            var lower = char.ToLowerInvariant(c);

            if (lower != c)
            {
                if (!byLower.TryGetValue(lower, out var variants))
                {
                    byLower[lower] = variants = [];
                }

                variants.Add(c);
            }
        }

        return byLower;
    }

    /// <summary>
    /// Every character whose invariant lower case equals that of <paramref name="c"/>.
    /// </summary>
    public static CharSet Of(char c)
    {
        var lower = char.ToLowerInvariant(c);

        return _byLower.TryGetValue(lower, out var variants)
            ? CharSet.Of([lower, .. variants])
            : CharSet.Of(lower);
    }
}
