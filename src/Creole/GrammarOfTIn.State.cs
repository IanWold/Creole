namespace Creole;

public abstract partial class Grammar<TIn>
{
    /// <summary>
    /// A parser that succeeds without consuming input and produces the layer's state.
    /// </summary>
    /// <typeparam name="TState">The type to read the state as. The state must be a <typeparamref name="TState"/>: of that type, or of one derived from it.</typeparam>
    /// <returns>A parser that produces the layer's state.</returns>
    /// <exception cref="GrammarException">When it runs: the layer has no state, or its state is not a <typeparamref name="TState"/>. A state set to null reads as null when <typeparamref name="TState"/> admits null.</exception>
    /// <remarks>
    /// <para>
    /// A layer's state starts as the <c>initialState</c> it was given, which <c>Layer.Transduce</c> and one overload of <c>Layer.Tokens</c> take.
    /// Another layer, and a parser run on its own, start with no state until <see cref="SetState{TState}"/> or <c>Scan</c> sets one, so a parser sets its own at its root: <c>SetState(0).Then(Root)</c>.
    /// </para>
    /// <para>
    /// Every use with the same <typeparamref name="TState"/> refers to the same parser.
    /// </para>
    /// </remarks>
    public static Parser<TIn, TState> GetState<TState>() => GetStateNode<TIn, TState>.Instance;

    /// <summary>
    /// A parser that succeeds without consuming input and replaces the layer's state with <paramref name="state"/>.
    /// </summary>
    /// <typeparam name="TState">The type of the new state. In a layer given an <c>initialState</c>, each step must leave a state of that state's type.</typeparam>
    /// <param name="state">The new state. It should be immutable, because backtracking restores the state by keeping a reference to it.</param>
    /// <returns>A parser that sets the state and produces <see cref="Unit"/>.</returns>
    /// <remarks>
    /// <para>
    /// The state lasts until something sets it again, or until the parse goes back past this parser, and in a layer it carries on into the steps after.
    /// An attempt that fails without consuming, and every rewind of <see cref="Atomic"/>, <see cref="Lookahead"/>, <see cref="Not"/> and <c>Longest</c>, puts back the state from before it.
    /// </para>
    /// <para>
    /// To set a state computed from the input or from the state before, use <c>Scan</c>.
    /// </para>
    /// </remarks>
    public static Parser<TIn, Unit> SetState<TState>(TState state) => new SetStateNode<TIn, TState>(state);
}
