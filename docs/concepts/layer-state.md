# Layer state

A grammar can keep a value as it parses and decide from it later, such as how deeply brackets nest or which blocks are open. That value is the parse's state. `GetState` and `SetState` are members of `Grammar<TIn>`, so a grammar class calls them unqualified, and `p.Scan(...)` is a method of `Parser<TIn, TOut>`. Backtracking puts the state back, so a grammar never sees a change made on a path the parse abandoned. The design is in the [whitepaper's § 2. Layer-local state](../../whitepaper.md#2-layer-local-state) and [ADR-0037](../../work/docs/decisions/0037-lexers-take-an-initial-state-parsers-set-theirs-and-a-mistyped-read-throws.md).

> In this version a parser run with `Parse` keeps its state for the whole parse. Each step of a layer starts with no state.

## Reading and setting the state

| Combinator | Consumes | Produces | Does |
| --- | --- | --- | --- |
| `GetState<TState>()` | nothing | the state | Reads the state |
| `SetState(state)` | nothing | `Unit` | Replaces the state with `state` |
| `p.Scan<TState, TResult>((value, state, span) => (result, newState))` | what `p` consumes | `result` | Runs `p`, then replaces the state with `newState` |

`Scan`'s function gets `p`'s value, the state as `p` left it, and the [span](spans.md) of what `p` consumed. It returns the value to produce and the new state. Use it whenever the new state depends on the input or on the state before. `SetState` only sets a value fixed when the grammar is built.

A parser starts with no state, so a grammar sets its own at its root before anything reads it: `SetState(0).Then(Root)`.

## An example

This grammar reads parentheses and produces how deeply they nest. Its state is the number of parentheses still open and the most that were open at once. A `)` with nothing open is rejected, which only the state can tell:

```csharp
public readonly record struct Depth(int Open, int Deepest);

public class Nesting : CharGrammar
{
    private static readonly Parser<char, char> _close =
        GetState<Depth>().Where(static depth => depth.Open > 0, "no '(' to close")
            .Then(Char(')').Scan<Depth, char>(static (paren, depth, _) => (paren, depth with { Open = depth.Open - 1 })));

    private static readonly Parser<char, char> _open =
        Char('(').Scan<Depth, char>(static (paren, depth, _) => (paren, new Depth(depth.Open + 1, Math.Max(depth.Deepest, depth.Open + 1))));

    public static readonly Parser<char, int> Deepest =
        SetState(new Depth(0, 0)).Then(_open.Or(_close).SkipMany()).Before(End).Then(GetState<Depth>()).Select(static depth => depth.Deepest);
}
```

`Nesting.Deepest.Parse("(()(()))")` produces 3. On `())`, the second `)` has nothing to close:

```text
error[parser]: expected '(' or end of input
 --> 1:3
  |
1 | ())
  |   ^ no '(' to close
```

## Backtracking puts the state back

Wherever a parse goes back, the state goes back with it, to what it was at that point:

- **An attempt that fails without consuming.** Each alternative of `Or` and `OneOf`, the item of `Optional`, each item, separator and terminator of a repetition, and each operator and atom of `Expression.Build`.
- **`Atomic`, when it rewinds.** `Atomic(_open.Then(Char('x'))).Or(_open)` on `(y` leaves one parenthesis open, not two.
- **`Lookahead` and `Not`.** Neither leaves a change to the state behind.
- **`Longest`.** The state is what the winning alternative leaves when it runs alone, whatever the other alternatives did.

A change on the path the parse keeps lasts until something changes the state again.

The state should be immutable: a value type, a record, or an immutable collection such as `ImmutableStack<T>`. Backtracking puts it back by keeping a reference to the state from before, so a state changed in place would not be put back.

`Scan`'s function decides how the rest of the input is parsed, so Creole runs it every time its parser succeeds, even when the value is not used, and may run it more than once (see [How often your functions run](sequencing.md#how-often-your-functions-run)). It must give the same result for the same arguments.

## Types

`GetState<TState>()` reads the state when it is a `TState`: of that type, or of a type derived from it. A state set to null reads as null when `TState` allows null, such as `string?` or `int?`. Anything else is a mistake in the grammar, not in the input, so `Parse` throws `GrammarException` rather than returning an error. `Scan` reads the state the same way.

| Mistake | Message |
| --- | --- |
| Reading the state before anything sets it | The parser layer has no state, but its grammar reads it as Int32. Give the layer an initialState, or set the state with SetState before reading it. |
| Reading it as another type | The parser layer's state is of type String, but its grammar reads it as Int32. |
| Reading null as a type that does not allow it | The parser layer's state is null, but its grammar reads it as Int32, which cannot be null. |

## Cost

A grammar that contains no `SetState`, `Scan`, `Bind` or query with a dependent `from` runs as if state did not exist. In one that does, each place a parse may go back to also saves the state, which is one reference. A `Longest` in such a grammar runs its winner again when another alternative ran after it, so that the state is the winner's own ([Interpreter](../engines/interpreter.md#when-a-longest-compiles)). `SetState` allocates nothing when it runs. `Scan` with a value-type state, such as `int` or `Depth` above, boxes the new state each time it runs.
