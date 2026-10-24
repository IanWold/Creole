namespace Creole;

/// <summary>
/// What a regular node matches under commit semantics, with values, labels and contexts stripped (ADR-0017).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Regular.Of{T}"/> builds terms from nodes. <c>Seq</c> and <c>Or</c> become right-nested pairs, <c>Optional(p)</c> becomes <c>Choice(p, Empty)</c>,
/// <c>AtLeastOnce(p)</c> becomes <c>Sequence(p, Many(p))</c>, and a <c>String</c> of two or more characters becomes an <see cref="Atomic"/> sequence of classes.
/// Each rewrite matches exactly what the node does.
/// </para>
/// <para>
/// Every term caches the structural properties that fragment E′ and the automaton need, so asking for them is constant time.
/// </para>
/// </remarks>
internal abstract record RegularTerm
{
    /// <summary>
    /// Runs <see cref="Item"/>; a failure after consuming becomes a failure without consuming.
    /// </summary>
    public sealed record Atomic(RegularTerm Item) : RegularTerm
    {
        public override bool Consumes { get; } = Item.Consumes;

        public override CharSet FirstChars { get; } = Item.FirstChars;

        public override bool NeverFails { get; } = Item.NeverFails;

        public override bool NeverFailsAfterConsuming => true;

        public override bool Nullable { get; } = Item.Nullable;
    }

    /// <summary>
    /// One character from <see cref="Set"/>.
    /// </summary>
    public sealed record Chars(CharSet Set) : RegularTerm
    {
        public override bool Consumes { get; } = !Set.IsEmpty;

        public override CharSet FirstChars => Set;

        public override bool NeverFails => false;

        public override bool NeverFailsAfterConsuming => true;

        public override bool Nullable => false;
    }

    /// <summary>
    /// Runs <see cref="First"/>, and <see cref="Second"/> only if <see cref="First"/> fails without consuming.
    /// </summary>
    public sealed record Choice(RegularTerm First, RegularTerm Second) : RegularTerm
    {
        public override bool Consumes { get; } = First.Consumes || Second.Consumes;

        public override CharSet FirstChars { get; } = First.FirstChars.Union(Second.FirstChars);

        public override bool NeverFails { get; } = First.NeverFails || (First.NeverFailsAfterConsuming && Second.NeverFails);

        public override bool NeverFailsAfterConsuming { get; } = First.NeverFailsAfterConsuming && Second.NeverFailsAfterConsuming;

        public override bool Nullable { get; } = First.Nullable || Second.Nullable;
    }

    /// <summary>
    /// Succeeds without consuming.
    /// </summary>
    public sealed record Empty : RegularTerm
    {
        public override bool Consumes => false;

        public override CharSet FirstChars => CharSet.Empty;

        public static Empty Instance { get; } = new();

        public override bool NeverFails => true;

        public override bool NeverFailsAfterConsuming => true;

        public override bool Nullable => true;

        private Empty()
        {
        }
    }

    /// <summary>
    /// Runs <see cref="Item"/> until it fails, greedily; the item is never nullable.
    /// A failure of the item after consuming fails the repetition.
    /// </summary>
    public sealed record Many(RegularTerm Item) : RegularTerm
    {
        public override bool Consumes { get; } = Item.Consumes;

        public override CharSet FirstChars { get; } = Item.FirstChars;

        public override bool NeverFails { get; } = Item.NeverFailsAfterConsuming;

        public override bool NeverFailsAfterConsuming { get; } = Item.NeverFailsAfterConsuming;

        public override bool Nullable => true;
    }

    /// <summary>
    /// Runs <see cref="First"/>, then <see cref="Second"/>.
    /// </summary>
    public sealed record Sequence(RegularTerm First, RegularTerm Second) : RegularTerm
    {
        public override bool Consumes { get; } = First.Consumes || Second.Consumes;

        public override CharSet FirstChars { get; } = First.Nullable ? First.FirstChars.Union(Second.FirstChars) : First.FirstChars;

        public override bool NeverFails { get; } = First.NeverFails && Second.NeverFails;

        public override bool NeverFailsAfterConsuming { get; } =
            First.NeverFailsAfterConsuming && (Second.NeverFails || (!First.Consumes && Second.NeverFailsAfterConsuming));

        public override bool Nullable { get; } = First.Nullable && Second.Nullable;
    }

    /// <summary>
    /// Whether the term can consume input. Over-approximated: a part that can never match still counts.
    /// </summary>
    public abstract bool Consumes { get; }

    /// <summary>
    /// The characters that can start a match: the language FIRST set, which over-approximates what commit semantics can start with.
    /// </summary>
    public abstract CharSet FirstChars { get; }

    /// <summary>
    /// Whether the term succeeds on every input. Under-approximated.
    /// </summary>
    public abstract bool NeverFails { get; }

    /// <summary>
    /// Whether every failure of the term is a failure without consuming. Under-approximated.
    /// </summary>
    public abstract bool NeverFailsAfterConsuming { get; }

    /// <summary>
    /// Whether the term can succeed without consuming. The language property, which over-approximates commit semantics.
    /// </summary>
    public abstract bool Nullable { get; }

    /// <summary>
    /// <paramref name="first"/> followed by <paramref name="second"/>, leaving out an <see cref="Empty"/> on either side.
    /// </summary>
    public static RegularTerm Then(RegularTerm first, RegularTerm second) =>
        (first, second) switch
        {
            (Empty, _) => second,
            (_, Empty) => first,
            _ => new Sequence(first, second),
        };
}
