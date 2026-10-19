namespace Creole;

/// <summary>
/// Runs two parsers in order and combines their results with <see cref="Selector"/>.
/// Backs <c>Seq</c> and the <c>Select</c> that follows it, and <c>Before</c> and <c>Then</c>.
/// </summary>
/// <remarks>
/// Every arity has <see cref="NodeKind.Sequence"/>, so structural consumers see one kind with one child per operand.
/// </remarks>
internal sealed class SequenceNode<TIn, T1, T2, TOut>(Parser<TIn, T1> first, Parser<TIn, T2> second, Func<T1, T2, TOut> selector) : Parser<TIn, TOut>
{
    /// <summary>
    /// The selector <c>Seq</c> uses, which builds a tuple. <c>Select</c> replaces it when it finds it.
    /// </summary>
    public static readonly Func<T1, T2, (T1, T2)> TupleSelector = static (first, second) => (first, second);

    internal override IReadOnlyList<INode> Children => [First, Second];

    internal override NodeKind Kind => NodeKind.Sequence;

    public Parser<TIn, T1> First { get; } = first;

    public Parser<TIn, T2> Second { get; } = second;

    public Func<T1, T2, TOut> Selector { get; } = selector;

    internal override Plan<TIn, TOut> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, TOut>(Kind);
}

/// <summary>
/// Runs three parsers in order and combines their results with <see cref="Selector"/>.
/// Backs <c>Seq</c> and the <c>Select</c> that follows it, and <c>Between</c>.
/// </summary>
/// <remarks>
/// Every arity has <see cref="NodeKind.Sequence"/>, so structural consumers see one kind with one child per operand.
/// </remarks>
internal sealed class SequenceNode<TIn, T1, T2, T3, TOut>(Parser<TIn, T1> first, Parser<TIn, T2> second, Parser<TIn, T3> third, Func<T1, T2, T3, TOut> selector) : Parser<TIn, TOut>
{
    /// <summary>
    /// The selector <c>Seq</c> uses, which builds a tuple. <c>Select</c> replaces it when it finds it.
    /// </summary>
    public static readonly Func<T1, T2, T3, (T1, T2, T3)> TupleSelector = static (first, second, third) => (first, second, third);

    internal override IReadOnlyList<INode> Children => [First, Second, Third];

    internal override NodeKind Kind => NodeKind.Sequence;

    public Parser<TIn, T1> First { get; } = first;

    public Parser<TIn, T2> Second { get; } = second;

    public Func<T1, T2, T3, TOut> Selector { get; } = selector;

    public Parser<TIn, T3> Third { get; } = third;

    internal override Plan<TIn, TOut> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, TOut>(Kind);
}

/// <summary>
/// Runs four parsers in order and combines their results with <see cref="Selector"/>.
/// Backs <c>Seq</c> and the <c>Select</c> that follows it.
/// </summary>
/// <remarks>
/// Every arity has <see cref="NodeKind.Sequence"/>, so structural consumers see one kind with one child per operand.
/// </remarks>
internal sealed class SequenceNode<TIn, T1, T2, T3, T4, TOut>(Parser<TIn, T1> first, Parser<TIn, T2> second, Parser<TIn, T3> third, Parser<TIn, T4> fourth, Func<T1, T2, T3, T4, TOut> selector) : Parser<TIn, TOut>
{
    /// <summary>
    /// The selector <c>Seq</c> uses, which builds a tuple. <c>Select</c> replaces it when it finds it.
    /// </summary>
    public static readonly Func<T1, T2, T3, T4, (T1, T2, T3, T4)> TupleSelector = static (first, second, third, fourth) => (first, second, third, fourth);

    internal override IReadOnlyList<INode> Children => [First, Second, Third, Fourth];

    internal override NodeKind Kind => NodeKind.Sequence;

    public Parser<TIn, T1> First { get; } = first;

    public Parser<TIn, T4> Fourth { get; } = fourth;

    public Parser<TIn, T2> Second { get; } = second;

    public Func<T1, T2, T3, T4, TOut> Selector { get; } = selector;

    public Parser<TIn, T3> Third { get; } = third;

    internal override Plan<TIn, TOut> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, TOut>(Kind);
}

/// <summary>
/// Runs five parsers in order and combines their results with <see cref="Selector"/>.
/// Backs <c>Seq</c> and the <c>Select</c> that follows it.
/// </summary>
/// <remarks>
/// Every arity has <see cref="NodeKind.Sequence"/>, so structural consumers see one kind with one child per operand.
/// </remarks>
internal sealed class SequenceNode<TIn, T1, T2, T3, T4, T5, TOut>(Parser<TIn, T1> first, Parser<TIn, T2> second, Parser<TIn, T3> third, Parser<TIn, T4> fourth, Parser<TIn, T5> fifth, Func<T1, T2, T3, T4, T5, TOut> selector) : Parser<TIn, TOut>
{
    /// <summary>
    /// The selector <c>Seq</c> uses, which builds a tuple. <c>Select</c> replaces it when it finds it.
    /// </summary>
    public static readonly Func<T1, T2, T3, T4, T5, (T1, T2, T3, T4, T5)> TupleSelector = static (first, second, third, fourth, fifth) => (first, second, third, fourth, fifth);

    internal override IReadOnlyList<INode> Children => [First, Second, Third, Fourth, Fifth];

    internal override NodeKind Kind => NodeKind.Sequence;

    public Parser<TIn, T5> Fifth { get; } = fifth;

    public Parser<TIn, T1> First { get; } = first;

    public Parser<TIn, T4> Fourth { get; } = fourth;

    public Parser<TIn, T2> Second { get; } = second;

    public Func<T1, T2, T3, T4, T5, TOut> Selector { get; } = selector;

    public Parser<TIn, T3> Third { get; } = third;

    internal override Plan<TIn, TOut> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, TOut>(Kind);
}

/// <summary>
/// Runs six parsers in order and combines their results with <see cref="Selector"/>.
/// Backs <c>Seq</c> and the <c>Select</c> that follows it.
/// </summary>
/// <remarks>
/// Every arity has <see cref="NodeKind.Sequence"/>, so structural consumers see one kind with one child per operand.
/// </remarks>
internal sealed class SequenceNode<TIn, T1, T2, T3, T4, T5, T6, TOut>(Parser<TIn, T1> first, Parser<TIn, T2> second, Parser<TIn, T3> third, Parser<TIn, T4> fourth, Parser<TIn, T5> fifth, Parser<TIn, T6> sixth, Func<T1, T2, T3, T4, T5, T6, TOut> selector) : Parser<TIn, TOut>
{
    /// <summary>
    /// The selector <c>Seq</c> uses, which builds a tuple. <c>Select</c> replaces it when it finds it.
    /// </summary>
    public static readonly Func<T1, T2, T3, T4, T5, T6, (T1, T2, T3, T4, T5, T6)> TupleSelector = static (first, second, third, fourth, fifth, sixth) => (first, second, third, fourth, fifth, sixth);

    internal override IReadOnlyList<INode> Children => [First, Second, Third, Fourth, Fifth, Sixth];

    internal override NodeKind Kind => NodeKind.Sequence;

    public Parser<TIn, T5> Fifth { get; } = fifth;

    public Parser<TIn, T1> First { get; } = first;

    public Parser<TIn, T4> Fourth { get; } = fourth;

    public Parser<TIn, T2> Second { get; } = second;

    public Func<T1, T2, T3, T4, T5, T6, TOut> Selector { get; } = selector;

    public Parser<TIn, T6> Sixth { get; } = sixth;

    public Parser<TIn, T3> Third { get; } = third;

    internal override Plan<TIn, TOut> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, TOut>(Kind);
}

/// <summary>
/// Runs seven parsers in order and combines their results with <see cref="Selector"/>.
/// Backs <c>Seq</c> and the <c>Select</c> that follows it.
/// </summary>
/// <remarks>
/// Every arity has <see cref="NodeKind.Sequence"/>, so structural consumers see one kind with one child per operand.
/// </remarks>
internal sealed class SequenceNode<TIn, T1, T2, T3, T4, T5, T6, T7, TOut>(Parser<TIn, T1> first, Parser<TIn, T2> second, Parser<TIn, T3> third, Parser<TIn, T4> fourth, Parser<TIn, T5> fifth, Parser<TIn, T6> sixth, Parser<TIn, T7> seventh, Func<T1, T2, T3, T4, T5, T6, T7, TOut> selector) : Parser<TIn, TOut>
{
    /// <summary>
    /// The selector <c>Seq</c> uses, which builds a tuple. <c>Select</c> replaces it when it finds it.
    /// </summary>
    public static readonly Func<T1, T2, T3, T4, T5, T6, T7, (T1, T2, T3, T4, T5, T6, T7)> TupleSelector = static (first, second, third, fourth, fifth, sixth, seventh) => (first, second, third, fourth, fifth, sixth, seventh);

    internal override IReadOnlyList<INode> Children => [First, Second, Third, Fourth, Fifth, Sixth, Seventh];

    internal override NodeKind Kind => NodeKind.Sequence;

    public Parser<TIn, T5> Fifth { get; } = fifth;

    public Parser<TIn, T1> First { get; } = first;

    public Parser<TIn, T4> Fourth { get; } = fourth;

    public Parser<TIn, T2> Second { get; } = second;

    public Func<T1, T2, T3, T4, T5, T6, T7, TOut> Selector { get; } = selector;

    public Parser<TIn, T7> Seventh { get; } = seventh;

    public Parser<TIn, T6> Sixth { get; } = sixth;

    public Parser<TIn, T3> Third { get; } = third;

    internal override Plan<TIn, TOut> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, TOut>(Kind);
}

/// <summary>
/// Runs eight parsers in order and combines their results with <see cref="Selector"/>.
/// Backs <c>Seq</c> and the <c>Select</c> that follows it.
/// </summary>
/// <remarks>
/// Every arity has <see cref="NodeKind.Sequence"/>, so structural consumers see one kind with one child per operand.
/// </remarks>
internal sealed class SequenceNode<TIn, T1, T2, T3, T4, T5, T6, T7, T8, TOut>(Parser<TIn, T1> first, Parser<TIn, T2> second, Parser<TIn, T3> third, Parser<TIn, T4> fourth, Parser<TIn, T5> fifth, Parser<TIn, T6> sixth, Parser<TIn, T7> seventh, Parser<TIn, T8> eighth, Func<T1, T2, T3, T4, T5, T6, T7, T8, TOut> selector) : Parser<TIn, TOut>
{
    /// <summary>
    /// The selector <c>Seq</c> uses, which builds a tuple. <c>Select</c> replaces it when it finds it.
    /// </summary>
    public static readonly Func<T1, T2, T3, T4, T5, T6, T7, T8, (T1, T2, T3, T4, T5, T6, T7, T8)> TupleSelector = static (first, second, third, fourth, fifth, sixth, seventh, eighth) => (first, second, third, fourth, fifth, sixth, seventh, eighth);

    internal override IReadOnlyList<INode> Children => [First, Second, Third, Fourth, Fifth, Sixth, Seventh, Eighth];

    internal override NodeKind Kind => NodeKind.Sequence;

    public Parser<TIn, T8> Eighth { get; } = eighth;

    public Parser<TIn, T5> Fifth { get; } = fifth;

    public Parser<TIn, T1> First { get; } = first;

    public Parser<TIn, T4> Fourth { get; } = fourth;

    public Parser<TIn, T2> Second { get; } = second;

    public Func<T1, T2, T3, T4, T5, T6, T7, T8, TOut> Selector { get; } = selector;

    public Parser<TIn, T7> Seventh { get; } = seventh;

    public Parser<TIn, T6> Sixth { get; } = sixth;

    public Parser<TIn, T3> Third { get; } = third;

    internal override Plan<TIn, TOut> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, TOut>(Kind);
}
