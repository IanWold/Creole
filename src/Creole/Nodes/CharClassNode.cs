namespace Creole;

/// <summary>
/// Consumes one character in <see cref="Set"/> and produces it. Fails without consuming on any other character.
/// Backs <c>Char</c>, <c>Range</c>, <c>NoneOf</c> and the named classes such as <c>Digit</c>.
/// </summary>
internal sealed class CharClassNode(CharSet set, string? name) : Parser<char, char>, IPrimitiveNode
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.CharClass;

    public FirstSet First { get; } = FirstSet.OfChars(set);

    /// <summary>
    /// The class's name for messages and grammar export, such as "digit", or null for a class made from its characters.
    /// </summary>
    public string? Name { get; } = name;

    public bool Nullable => false;

    public bool Opaque => false;

    public CharSet Set { get; } = set;

    internal override Plan<char, char> Lower(PlanBuilder builder) => PlanBuilder.NotYetExecutable<char, char>(Kind);
}
