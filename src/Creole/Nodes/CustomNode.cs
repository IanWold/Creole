namespace Creole;

/// <summary>
/// Runs a hand-written <see cref="IScanner{TIn, TOut}"/> and produces its value.
/// Backs <c>Custom</c>, the only node that runs user code to read input.
/// </summary>
/// <remarks>
/// The scanner's <see cref="ScannerInfo"/> is read once, here. With a declared <see cref="ScannerInfo.First"/>, analysis sees the node like any other primitive.
/// Without one, the node is opaque and its FIRST set is any item (ADR-0012).
/// </remarks>
internal sealed class CustomNode<TIn, TOut> : Parser<TIn, TOut>, IPrimitiveNode
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.Custom;

    public Expected? Expected { get; }

    public FirstSet First { get; }

    /// <summary>
    /// The declaration as it was when the node was built.
    /// </summary>
    public ScannerInfo Info { get; }

    /// <summary>
    /// The token's name for messages, or null to expect the characters of <see cref="ScannerInfo.First"/>.
    /// </summary>
    public string? Name => Info.Name;

    public bool Nullable => Info.Nullable;

    public bool Opaque => Info.First is null;

    public IScanner<TIn, TOut> Scanner { get; }

    public CustomNode(IScanner<TIn, TOut>? scanner, string scannerExpression)
    {
        Scanner = Guard.ScannerNotNull(scanner, scannerExpression);
        Info = scanner.Info ?? throw new GrammarException($"The Info of '{scannerExpression}' is null.");

        if (Info.First is not null && typeof(TIn) != typeof(char))
        {
            throw new GrammarException($"'{scannerExpression}' declares First characters, but it reads {typeof(TIn).Name}, not char. Leave First null for a scanner over other items.");
        }

        if (Info.Name is not null && string.IsNullOrWhiteSpace(Info.Name))
        {
            throw new GrammarException($"'{scannerExpression}' has an empty Name. Give the token a name for error messages, or leave Name null.");
        }

        First = Info.First is null ? FirstSet.AnyItem : FirstSet.OfChars(Info.First);
        Expected = Info.Name is { } name
            ? Expected.Labelled(name)
            : Info.First is { } first
                ? Expected.CharacterSet(first)
                : null;
    }

    internal override Plan<TIn, TOut> Lower(PlanBuilder builder) =>
        PlanBuilder.Custom(Scanner, Expected);
}
