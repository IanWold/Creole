# Layer state

A grammar can keep a value as it parses and decide from it later, such as how deeply brackets nest or which blocks are open. That value is its state. Each layer has its own, and so does a parser run on its own. `GetState` and `SetState` are members of `Grammar<TIn>`, so a grammar class calls them unqualified, and `p.Scan(...)` is a method of `Parser<TIn, TOut>`. Backtracking puts the state back, so a grammar never sees a change made on a path the parse abandoned. The design is in the [whitepaper's § 2. Layer-local state](../../whitepaper.md#2-layer-local-state) and [ADR-0037](../../work/docs/decisions/0037-lexers-take-an-initial-state-parsers-set-theirs-and-a-mistyped-read-throws.md).

## Reading and setting the state

| Combinator | Consumes | Produces | Does |
| --- | --- | --- | --- |
| `GetState<TState>()` | nothing | the state | Reads the state |
| `SetState(state)` | nothing | `Unit` | Replaces the state with `state` |
| `p.Scan<TState, TResult>((value, state, span) => (result, newState))` | what `p` consumes | `result` | Runs `p`, then replaces the state with `newState` |

`Scan`'s function gets `p`'s value, the state as `p` left it, and the [span](spans.md) of what `p` consumed. It returns the value to produce and the new state. Use it whenever the new state depends on the input or on the state before. `SetState` only sets a value fixed when the grammar is built.

## Where the state starts

| Where | Starts as |
| --- | --- |
| `Layer.Transduce(initialState, step, atEnd)` | `initialState` |
| `Layer.Tokens(initialState, item, skip)` and `Layer.Modal(initialState, channel, selector, skip)` | `initialState` |
| `Layer.Tokens(item)`, `Layer.Tokens(item, skip)`, `Layer.Modal(channel, selector)` and `Layer.Modal(channel, selector, skip)` | no state, until the grammar sets one |
| A parser run with `Parse`, alone or at the end of a [pipeline](pipelines.md) | no state, so the grammar sets its own at its root: `SetState(0).Then(Root)`. Under `ParseMany`, each item starts with no state |

A layer runs in steps, and each step starts with the state the step before left, so a lexer can carry what it has seen from one token to the next. `skip` and `item` share it. `atEnd` receives the state the last step left. A `SetState` at the start of a layer's `item` would run on every step, so a layer that needs a state to start with takes an `initialState` instead.

This lexer numbers its words:

```csharp
Layer<char, string> numbered = Layer.Tokens(
    initialState: 0,
    item: Letter.AtLeastOnce().Text().Scan<int, string>(static (word, count, _) => ($"{count}:{word}", count + 1)),
    skip: Whitespace.SkipMany()
);
```

Over `ab cd e` it produces `0:ab`, `1:cd` and `2:e`.

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

## A layout pass

A layer's state can carry what a language's layout means. This pass turns indentation into INDENT and DEDENT tokens, as Python's tokenizer does. Its lexer ends each line with a newline token whose `Width` is the next line's indentation, and the pass keeps the indentation of each open block:

```csharp
public sealed record IndentLevels(int Width, IndentLevels? Enclosing)
{
    private static readonly Token _dedent = new(TokenKind.Dedent, ReadOnlyMemory<char>.Empty);

    private static readonly Token _inconsistentDedent = new(TokenKind.Error, ReadOnlyMemory<char>.Empty);

    private static readonly Token _indent = new(TokenKind.Indent, ReadOnlyMemory<char>.Empty);

    public static IndentLevels Root { get; } = new(0, null);

    public (Emit<Token> Tokens, IndentLevels Levels) Adjust(Token newline)
    {
        if (newline.Width == Width)
        {
            return (newline, this);
        }

        if (newline.Width > Width)
        {
            return ([newline, Emit.Synthetic(_indent)], new IndentLevels(newline.Width, this));
        }

        List<Emitted<Token>> tokens = [newline];
        var levels = this;

        while (newline.Width < levels.Width)
        {
            tokens.Add(Emit.Synthetic(_dedent));
            levels = levels.Enclosing!;
        }

        if (newline.Width != levels.Width)
        {
            tokens.Add(Emit.Synthetic(_inconsistentDedent));
        }

        return ([.. tokens], levels);
    }

    public Emit<Token> CloseAll()
    {
        List<Emitted<Token>> dedents = [];

        for (var levels = this; levels.Enclosing is not null; levels = levels.Enclosing)
        {
            dedents.Add(Emit.Synthetic(_dedent));
        }

        return [.. dedents];
    }
}

public sealed class Layout : TokenGrammar<Token, TokenKind>
{
    public static readonly Layer<Token, Token> Pass = Layer.Transduce(
        initialState: IndentLevels.Root,
        step: OneOf(
            Kind(TokenKind.Newline).Scan<IndentLevels, Emit<Token>>(static (newline, levels, _) => levels.Adjust(newline)),
            Any.Select(static token => Emit.One(token))
        ),
        atEnd: static levels => levels.CloseAll()
    );
}
```

Each step either reads a newline and emits it with the INDENT or DEDENTs it stands for, or passes another token on. At the end of the input, `atEnd` closes the blocks still open. The state is immutable, so a parser above the pass can backtrack freely. A line indented less than the line before, but not as far back as any open block, gets an error token after its DEDENTs, which the parser rejects.

INDENT and DEDENT are [synthetic](layers.md#a-steps-outputs-emitt), so each is zero-width at the start of the first token on the next line. An error at a DEDENT shows on the dedented line. Here the block after `if b:` is missing:

```text
while a:
    if b:
c = 1
```

```text
error[parser]: expected indent
 --> 3:1
  |
3 | c = 1
  | ^ unexpected dedent
```

The whole language, with its lexer and parser, is the sample [Creole.Samples.Indentation](../../samples/Creole.Samples.Indentation/).

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

`GetState<TState>()` reads the state when it is a `TState`: of that type, or of a type derived from it. A state set to null reads as null when `TState` allows null, such as `string?` or `int?`. `Scan` reads the state the same way. A layer given an `initialState` also checks, at the end of each step, that the state is still of `initialState`'s type, because later steps and `atEnd` read it as one.

Anything else is a mistake in the grammar, not in the input, so the parse throws `GrammarException` rather than returning an error. The message names the layer: `parser` for a parser, whether run on its own or at the end of a pipeline, and otherwise the layer's name.

| Mistake | Message |
| --- | --- |
| Reading the state before anything sets it | The parser layer has no state, but its grammar reads it as Int32. Give the layer an initialState, or set the state with SetState before reading it. |
| Reading it as another type | The parser layer's state is of type String, but its grammar reads it as Int32. |
| Reading null as a type that does not allow it | The parser layer's state is null, but its grammar reads it as Int32, which cannot be null. |
| A layer's step leaving a state of another type | A step of the lexer layer left its state of type String, but its initialState is of type Int32. |

## Cost

A grammar that contains no `SetState`, `Scan`, `Bind` or query with a dependent `from` runs as if state did not exist. In one that does, each place a parse may go back to also saves the state, which is one reference. A `Longest` in such a grammar runs its winner again when another alternative ran after it, so that the state is the winner's own ([Interpreter](../engines/interpreter.md#when-a-longest-compiles)). `SetState` allocates nothing when it runs. `Scan` with a value-type state, such as `int` or `Depth` above, boxes the new state each time it runs.
