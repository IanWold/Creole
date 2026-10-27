# ADR-0032: `Emit<T>` is a list of outputs, each derived or synthetic, not a union

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-27 |
| Cards | [LAY-03](../../active/06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md), [SPAN-03](../../active/05-source-spans/SPAN-03-synthetic-items.md), [LAY-04](../../active/06-layers-and-pipelines/LAY-04-layer-local-state.md), [LAY-07](../../active/06-layers-and-pipelines/LAY-07-three-layer-demo-and-bounded-memory-check.md), [ANA-03](../../active/09-static-analysis/ANA-03-loop-and-recursion-checks.md), [MODE-04](../../active/07-mode-channels/MODE-04-multi-layer-invalidation-cascade.md) |
| Whitepaper | [§ Defining layers](../../../whitepaper.md#defining-layers), [§ Where unions are not used](../../../whitepaper.md#where-unions-are-not-used), [§ Span rules](../../../whitepaper.md#span-rules), [§ B. An indentation-sensitive language in three layers](../../../whitepaper.md#b-an-indentation-sensitive-language-in-three-layers) |
| Research | LAY-03's RC 2 spike, recorded below (2026-10-27, scratchpad, not checked in). FND-07's spike and [ADR-0002](0002-parseresult-as-a-plain-union.md) § Consequences for the earlier finding. |
| Supersedes | — |

## Context

`Layer.Transduce(initialState, step, atEnd)` runs a step parser repeatedly. Each run produces an `Emit<TOut>`: zero, one or several outputs. The whitepaper describes `Emit<T>` in two places:

- § Defining layers: "a small struct holding zero, one or many outputs; synthetic outputs such as INDENT and DEDENT are emitted this way".
- § Where unions are not used: "a hand-written `[Union]` struct using C# 15's non-boxing access pattern (`HasValue` plus `TryGetValue`), so it keeps exhaustive matching without allocating".

Four problems sit in that sketch.

1. **The empty state.** FND-07's spike built an `Emit<T>`-like `[Union]` struct with cases `T` and `T[]`. It matched through `TryGetValue` without allocating, but its empty state was a null `Value`, which exhaustiveness does not see. A switch without a `null` arm compiled and then threw on an empty `Emit`. ADR-0002 § Consequences asked LAY-03 to consider a case type for "no output".
2. **Who reads an `Emit`.** The layer that ran the step reads every output in order and adds each to its buffer. Users mostly build `Emit`s. A user who reads one, in a helper that combines steps or in a test, also loops over its outputs. Nobody needs to handle "one" differently from "many".
3. **Synthetic outputs.** Span rule 3 gives "an item produced without consuming input" a zero-width span and flags it `IsSynthetic`. Example B's step consumes a newline token and emits NEWLINE, which came from that token, then INDENT or DEDENTs, which did not. A `Transduce` step that succeeds without consuming would loop forever, as a `Layer.Tokens` item would (CRL0003). So synthetic outputs come from steps that did consume input, and the layer cannot tell which outputs those are unless the step says so. Rule 2 is also written for one item per run of an item parser, not for several outputs.
4. **State.** `Transduce` takes `initialState`, and Example B's step reads it with `Scan`. `Scan`, `GetState` and `SetState` belong to LAY-04, which is blocked by LAY-03. LAY-03's first criterion asks for "a lexer composed with a layout pass", and an indentation pass needs its stack.

### The spike

LAY-03 checked the list form below on SDK 11.0.100-rc.2.26504.105 in Release: a `readonly struct Emit<T>` with `[CollectionBuilder]`, an element type `Emitted<T>(T Item, bool IsSynthetic)`, implicit conversions from `T` to both, and `Emit.Synthetic(item)`. Allocation is bytes per build and `foreach`, measured with `GC.GetAllocatedBytesForCurrentThread` over 1,000 iterations after a warm-up.

| Check | Result |
| --- | --- |
| `[newline, Emit.Synthetic(indent)]` | Compiles. Two outputs, the second synthetic |
| `[]` and `default` | No outputs |
| `Emit<Tok> e = tok;` and `Func<Tok, Emit<Tok>> f = t => t;` | One derived output, through the implicit conversion |
| `[newline, .. Enumerable.Repeat(Emit.Synthetic(dedent), 3)]` | Four outputs; spreads work |
| `Emit<IItem> e = item;` and `[item]` with `IItem` an interface | CS0029. The conversion does not apply, so such items need a factory. `[Emit.Synthetic(item)]` compiles |
| Allocation, zero or one output | 0 bytes |
| Allocation, two outputs | 40 bytes: one array |

## Options

### A. The whitepaper's `[Union]` struct, cases `T` and `T[]`, empty as a null `Value`

- Pros: as designed. Exhaustive over its two cases and allocation-free (FND-07).
- Cons: ADR-0002's finding: a switch without a `null` arm throws on an empty `Emit`. The cases overlap, since a one-element array means the same as a single output. There is no room for a per-output synthetic mark without more cases. `HasValue` is also the member ADR-0007 removed from `Maybe<T>` because the union pattern gives it a different meaning.

### B. A `[Union]` struct with cases `Nothing`, `Emitted<T>` and `Emitted<T>[]`

ADR-0007's pattern, as `Maybe<T>` uses it, with the mark carried in the element.

- Pros: exhaustive, safe for `default`, and allocation-free for zero or one output. It follows § Using C# 15 unions literally.
- Cons: the three cases are lengths of one list, not distinct shapes. An array of length 0 or 1 means the same as `Nothing` or a single output, so a switch that treats them differently is a bug the compiler cannot see. Every consumer loops anyway. The pattern needs a public boxing `object? Value`, and FND-09 would have a second type to re-check on the GA compiler.

### C. A list struct that is not a union (recommended)

An ordered list of outputs, each derived or synthetic, read with `Count` and `foreach`, built with conversions, two factories and collection expressions.

- Pros: it is what an `Emit` is. There is nothing to switch on, so the empty state and `default` raise no question. Each output carries its own mark. Zero or one output allocates nothing (spike). It does not depend on the `[Union]` pattern.
- Cons: it departs from the rule of thumb that fixed-shape values are unions, so § Where unions are not used must say why. It adds a public element type, `Emitted<T>`. Several outputs allocate one array per step.

### How a step marks synthetic outputs

Under B or C:

- **Per output (recommended).** The step marks each synthetic output.
- **From consumption.** Only outputs of a step that consumed nothing are synthetic. Steps must consume, so only `atEnd` could emit synthetic outputs, and Example B's step could not.
- **Explicit spans.** The step gives each output its span. Users never handle spans elsewhere (§ Source spans and provenance), and a wrong span would break the span rules silently.

### Where a synthetic output sits in a step that consumed input

- **At the start of the next input item (recommended).** Rule 3's wording, read per output. A DEDENT after a NEWLINE sits at the start of the first token on the dedented line, as Example B says. It costs one input item of lookahead past the step.
- **At the end of the step's input.** It needs no lookahead and depends only on consumed input. A DEDENT then sits wherever the newline token ends, before any indentation the lexer skipped.

### State before LAY-04

- **A stateless pass now (recommended).** LAY-03 carries the state to `atEnd`. LAY-04 lets the step read and change it. Criterion 1 composes a lexer with a stateless pass, and Example B's indentation pass stays with LAY-07, which already builds it.
- **Pull `Scan`, `GetState` and `SetState` into LAY-03.** Criterion 1 could then be the indentation pass. But marks would not restore state until LAY-02 and LAY-04 exist, so an `Atomic`, `Lookahead` or `Longest` that rewinds over a state change would leak it.
- **Move criterion 1 to LAY-04.** It works, but it moves the check of layer composition away from the card that builds it.

## Decision

Option C, with per-output marks, synthetic outputs at the start of the next input item, and a stateless pass for LAY-03. The maintainer chose each fork and accepted the record on 2026-10-27.

1. **Shape.** `Emit<T>` is a public `readonly struct` holding an ordered list of outputs. Each output is an `Emitted<T>`, a `readonly record struct` of `Item` and `IsSynthetic`. `Emit<T>` is not a union: it has no `[Union]` attribute, no `IUnion`, and no `Value` or `TryGetValue`.
2. **Building.**
   - `default` and `[]` hold no outputs.
   - An item converts implicitly to an `Emit<T>` of one derived output, and to an `Emitted<T>` that is derived.
   - `Emit.One(item)` makes one derived output. It is needed where the conversion does not apply, such as an item typed as an interface.
   - `Emit.Synthetic(item)` makes one synthetic output. It returns an `Emitted<T>`, which converts to `Emit<T>`.
   - A collection expression builds several outputs from items, `Emit.Synthetic(...)` outputs and spreads: `[newline, Emit.Synthetic(indent)]`.
3. **Reading.** `Count`, and `foreach` over `Emitted<T>` through a struct enumerator. Two emits are equal when they hold equal outputs in the same order.
4. **Storage.** Zero or one output is held inline. Several are copied once into an array. Building and reading zero or one output allocates nothing.
5. **Steps consume.** A `Transduce` step that succeeds without consuming input is an error at run time, as a `Layer.Tokens` item is. CRL0003 covers both when ANA-03 adds it.
6. **Derived spans.** Every derived output of a `Transduce` step gets the step's span under rule 2: from the start of the first input item the step consumed to the end of the last. A step that emits several derived outputs gives each the whole span. A grammar that wants finer spans emits one output per step.
7. **Synthetic spans.** A synthetic output gets a zero-width span at the start of the next input item (rule 3). In a step, that is the step's first input item if no derived output comes before it in the step, and otherwise the first input item after the step. At the end of the input, it is the end of the last input item. So a step that emits a synthetic output after a derived one reads one input item past its own input before it adds its outputs.
8. **`atEnd`.** `atEnd` is optional. It takes the layer's final state and returns an `Emit<TOut>`, and runs once, when the step loop reaches the end of input. Its outputs are synthetic whether marked or not, since it consumes nothing. It takes no span, because rule 3 places its outputs. It counts as one step that starts at the end of the input.
9. **Group index.** A slot's `groupIndex` counts every output of its step, synthetic ones included.
10. **State before LAY-04.** `Layer.Transduce(initialState, step, atEnd)` keeps the whitepaper's parameters. In LAY-03 the state reaches `atEnd` unchanged. The step reads and changes it once LAY-04 adds `Scan`, `GetState` and `SetState`, and LAY-04 records `stateBefore`. LAY-03's first criterion composes a lexer with a stateless `Transduce` pass, such as one that turns each newline token into a synthetic `;`. LAY-07 builds Example B's indentation pass.
11. **Cards.** LAY-03 builds `Emit<T>`, the marks, and the span of every `Transduce` output, since its loop computes them. SPAN-03 keeps the `IsSynthetic` flag that downstream layers read, and its Example B criterion.

Deciding reason: an `Emit` is a list whose length varies, not one of a fixed set of shapes, and the layer reading it loops over every output. A union over its lengths has overlapping cases, gives the compiler nothing useful to check, and has no room for the per-output mark that Example B's step needs.

What would change this:

- A consumer that must treat zero, one and several outputs differently, such as generated layer code (GEN-06). `Count` may be enough for it.
- A measured cost from the array in a real layout pass, such as LAY-07's memory check. That would argue for holding two outputs inline.

## Consequences

- **Easier.** Steps build outputs with collection expressions. There is no null arm or `default` question. `Emit<T>` no longer relies on the `[Union]` member pattern, so FND-09 re-checks only `Maybe<T>`.
- **Harder.** One more public type, `Emitted<T>`. A step with several outputs allocates an array. In Example B that happens only on lines whose indentation changes, since every other NEWLINE step emits one output.
- **Lookahead.** A streamed layer cannot hand a trailing synthetic output downstream until the next input item exists or the input ends. For Example B that is the first token on the next line.
- **Mode cascades.** A synthetic output's span reads the start of an item its step did not consume. If a cascade re-lexes that item (MODE-04), an unconsumed synthetic output is produced again with the new start. A consumed one keeps the old start, since consumed items are never invalidated (§ 3. Mode channels). The two differ only if the new mode skips differently before that item. MODE-04 should test this case.
- **Ruled out.** Switching on an `Emit`'s length as union cases, and steps that give outputs explicit spans.
- **Watch.** LAY-03's tests pin the allocation figures above. The interface-typed item case stays covered by `Emit.One`.

## Changes made

- Whitepaper:
  - § Where unions are not used: `Maybe<T>` keeps the `[Union]` pattern; a new bullet says why `Emit<T>` is not a union.
  - § Defining layers: `atEnd` takes only the state in the sample. `Transduce`'s step is held to CRL0003's rule. A new paragraph describes `Emit<T>`, its marks and `atEnd`, linking this record.
  - § Span rules: rule 2 gives several derived outputs the whole step span; rule 3 covers outputs marked synthetic and says which input item is next.
  - § Diagnostics: CRL0003 reads "A layer's item parser or step is nullable".
  - § B. An indentation-sensitive language in three layers: `Adjust` takes the newline token and marks DEDENTs synthetic; `atEnd` takes no span.
- Cards:
  - [LAY-03](../../active/06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md): scope follows this record; criterion 1 names a stateless pass; a new criterion pins zero allocation for zero or one output. Moved to Building.
  - [SPAN-03](../../active/05-source-spans/SPAN-03-synthetic-items.md): placement of `Transduce` outputs moves to LAY-03; the card keeps the `IsSynthetic` flag and its Example B criterion.
  - [LAY-04](../../active/06-layers-and-pipelines/LAY-04-layer-local-state.md): `Transduce`'s step reads and changes the state LAY-03 hands to `atEnd`.
  - [LAY-07](../../active/06-layers-and-pipelines/LAY-07-three-layer-demo-and-bounded-memory-check.md): builds Example B's indentation pass.
  - [ANA-03](../../active/09-static-analysis/ANA-03-loop-and-recursion-checks.md): CRL0003 covers `Transduce` steps.
  - [MODE-04](../../active/07-mode-channels/MODE-04-multi-layer-invalidation-cascade.md): tests a consumed synthetic output's span across a re-lex.
