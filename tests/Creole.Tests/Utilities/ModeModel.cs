using System.Collections.Immutable;

namespace Creole.Tests;

/// <summary>
/// A reference model of mode rollback over <see cref="ModeShape"/> grammars, which lexes each token from scratch.
/// </summary>
/// <remarks>
/// <para>
/// Its state is how many tokens the parse has consumed, where in the text the next token starts, and the mode stack, which is an immutable value.
/// Reading a token lexes it from scratch where the next token starts, in the mode on top of the stack, so no token read in another mode survives a change.
/// Going back is going back to an earlier state, so it undoes every change made since by construction.
/// </para>
/// <para>
/// It follows Creole's commit semantics.
/// A choice tries its next alternative only when the last failed without consuming, and so do <c>Optional</c> and <c>Many</c>.
/// <c>Atomic</c> turns a failure after consuming into one without.
/// <c>Lookahead</c> goes back when its item succeeds, and <c>Not</c> always goes back.
/// <c>Longest</c> runs each alternative from the same state and keeps the state its winner left.
/// </para>
/// </remarks>
/// <param name="input">The text, of <c>a</c> and <c>b</c>.</param>
internal class ModeModel(string input)
{
    /// <summary>
    /// Where a parse is.
    /// </summary>
    /// <param name="Consumed">How many tokens it has consumed.</param>
    /// <param name="Offset">Where in the text the next token starts.</param>
    /// <param name="Modes">The mode stack, whose top is the mode the next token is read in.</param>
    private readonly record struct State(int Consumed, int Offset, ImmutableStack<RunMode> Modes);

    /// <summary>
    /// What a shape did: on success its value and the state after it, and on failure how many tokens the parse had consumed where it failed.
    /// </summary>
    private readonly record struct Result(bool Ok, string Value, State State);

    private readonly string _input = input;

    private Result Atomic(ModeShape item, State state)
    {
        var result = Evaluate(item, state);

        return result.Ok
            ? result
            : Failed(state.Consumed);
    }

    private Result Evaluate(ModeShape shape, State state) => shape switch
    {
        ModeShape.Atomic atomic => Atomic(atomic.Item, state),
        ModeShape.Lookahead lookahead => Lookahead(lookahead.Item, state),
        ModeShape.Longest longest => Longest(longest.First, longest.Second, state),
        ModeShape.Many many => Many(many.Item, state),
        ModeShape.Not not => Not(not.Item, state),
        ModeShape.Optional optional => Optional(optional.Item, state),
        ModeShape.Or or => Or(or.First, or.Second, state),
        ModeShape.Push push => Push(push.Mode, push.Item, state),
        ModeShape.Seq seq => Seq(seq.First, seq.Second, state),
        ModeShape.Set set => new(true, "", state with { Modes = state.Modes.Pop().Push(set.Mode) }),
        ModeShape.Tok tok => Tok(tok.Kind, state),
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
    };

    private static Result Failed(int consumed) =>
        new(false, "", new State(consumed, 0, []));

    private Result Lookahead(ModeShape item, State state)
    {
        var result = Evaluate(item, state);

        return result.Ok
            ? result with { State = state }
            : result;
    }

    private Result Longest(ModeShape first, ModeShape second, State state)
    {
        Result? best = null;

        foreach (var alternative in (ModeShape[])[first, second])
        {
            var result = Evaluate(alternative, state);

            if (result.Ok && (best is null || result.State.Consumed > best.Value.State.Consumed))
            {
                best = result;
            }
        }

        return best ?? Failed(state.Consumed);
    }

    private Result Many(ModeShape item, State state)
    {
        var values = "";
        var current = state;

        while (true)
        {
            var result = Evaluate(item, current);

            if (!result.Ok)
            {
                return result.State.Consumed > current.Consumed
                    ? result
                    : new(true, values, current);
            }

            values += result.Value;
            current = result.State;
        }
    }

    private Result Not(ModeShape item, State state) =>
        Evaluate(item, state).Ok
            ? Failed(state.Consumed)
            : new(true, "", state);

    private Result Optional(ModeShape item, State state)
    {
        var result = Evaluate(item, state);

        return result.Ok || result.State.Consumed > state.Consumed
            ? result
            : new(true, "", state);
    }

    private Result Or(ModeShape first, ModeShape second, State state)
    {
        var result = Evaluate(first, state);

        return result.Ok || result.State.Consumed > state.Consumed
            ? result
            : Evaluate(second, state);
    }

    private Result Push(RunMode mode, ModeShape item, State state)
    {
        var result = Evaluate(item, state with { Modes = state.Modes.Push(mode) });

        return result.Ok
            ? result with { State = result.State with { Modes = result.State.Modes.Pop() } }
            : result;
    }

    private Result Seq(ModeShape first, ModeShape second, State state)
    {
        var head = Evaluate(first, state);

        if (!head.Ok)
        {
            return head;
        }

        var tail = Evaluate(second, head.State);

        return tail.Ok
            ? tail with { Value = head.Value + tail.Value }
            : tail;
    }

    private Result Tok(RunKind kind, State state)
    {
        if (state.Offset >= _input.Length)
        {
            return Failed(state.Consumed);
        }

        var character = _input[state.Offset];
        var end = state.Offset + 1;

        if (state.Modes.Peek() == RunMode.Runs)
        {
            while (end < _input.Length && _input[end] == character)
            {
                end++;
            }
        }

        var token = new RunToken(
            character == 'a'
                ? RunKind.A
                : RunKind.B,
            _input[state.Offset..end]
        );

        return token.Kind == kind
            ? new(true, $"[{token.Text}]", new State(state.Consumed + 1, end, state.Modes))
            : Failed(state.Consumed);
    }

    /// <summary>
    /// The outcome of <paramref name="shape"/> on <paramref name="input"/>, and whether a success read the text to its end.
    /// </summary>
    public static (Outcome Outcome, bool AtEnd) Run(ModeShape shape, string input)
    {
        var result = new ModeModel(input).Evaluate(shape, new State(0, 0, [RunMode.Single]));

        return result.Ok
            ? (new Outcome.Ok(result.Value, result.State.Consumed), result.State.Offset == input.Length)
            : (
                result.State.Consumed > 0
                    ? new Outcome.FailConsumed()
                    : new Outcome.FailEmpty(),
                false
            );
    }
}
