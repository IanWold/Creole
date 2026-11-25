# Mode channels

Some tokens can be told apart only by what the parser knows. In JavaScript-like syntax, `/` after `)` starts a regex in `if (ok) /x/.test(s)` but divides in `(a) / 2`. A mode channel carries that knowledge: the parser keeps a stack of modes on the channel and changes it as it parses. The design is in the [whitepaper's § 3. Mode channels](../../whitepaper.md#3-mode-channels) and [ADR-0041](../../work/docs/decisions/0041-a-channel-has-one-reader-below-one-writer-and-a-write-nothing-reads-or-a-pop-of-the-last-mode-throws.md).

## A channel

A `ModeChannel<TMode>` is a typed key with an initial mode. A channel is data, as a parser is: it holds its initial mode and nothing else. Each parse keeps its own stack of modes for each channel, which starts as the initial mode alone, so one channel serves any number of parses at once, on any thread.

A channel is known by its instance, not by its type or its initial mode. Two `new ModeChannel<LexMode>(LexMode.Operand)` are two channels. Keep each channel in a `static readonly` field and use that field everywhere.

## Changing the mode

`SetMode`, `PushMode` and `PopMode` are members of `Grammar<TIn>`, so a grammar class calls them unqualified. Each consumes nothing and produces `Unit`, so it composes like any other parser:

| Combinator | Does |
| --- | --- |
| `SetMode(channel, mode)` | Replaces the top of the stack with `mode`. The stack keeps its depth. |
| `PushMode(channel, mode)` | Pushes `mode`, which is current until a `PopMode` returns to the mode below it. |
| `PopMode(channel)` | Pops the top of the stack. |

The current mode is the top of the stack. `SetMode` suits modes that follow one another, such as operand and operator. `PushMode` and `PopMode` suit modes that nest, such as a block:

```csharp
public enum LexMode { Operand, Operator, Block }

public sealed class Script : TokenGrammar<Tok, K>
{
    public static readonly ModeChannel<LexMode> Modes = new(LexMode.Operand);

    public static readonly Parser<Tok, Tok> Operand =
        Kind(K.Number).Between(SetMode(Modes, LexMode.Operand), SetMode(Modes, LexMode.Operator));

    public static readonly Parser<Tok, Unit> Block =
        Kind(K.LBrace).Then(PushMode(Modes, LexMode.Block)).Then(Operand.SkipMany()).Before(Kind(K.RBrace)).Before(PopMode(Modes));
}
```

Pair each `PopMode` with a `PushMode` through the grammar's structure, as `Block` does. A path that pops more than it pushed is a mistake in the grammar, not in the input, so popping the last mode throws `GrammarException` rather than failing the parse:

```text
The parser layer pops the last mode of a ModeChannel<LexMode>. Each PopMode needs a PushMode on the same channel before it.
```

## A parser on its own

A parser run on its own, with `Parse` over text or over a list of tokens, keeps its channels' stacks, so `PopMode` still checks its pairs, but nothing reads the modes. So a parser that changes modes can be tested over tokens that are already lexed: `Script.Block` parses `{ 1 2 }` given as the tokens `LBrace`, `Number`, `Number` and `RBrace`.

Each parse starts every channel at its initial mode.
