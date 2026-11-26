# ADR-0040: A producing layer keeps its input only for the outputs its reader can still go back to, and the reader states its horizon when it pulls

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-11-24 |
| Cards | [LAY-09](../../active/06-layers-and-pipelines/LAY-09-hold-only-the-input-the-reader-can-still-go-back-to.md), [MODE-02](../../active/07-mode-channels/MODE-02-invalidation-rule.md), [MODE-04](../../active/07-mode-channels/MODE-04-multi-layer-invalidation-cascade.md), [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md) |
| Whitepaper | [§ The stream buffer](../../../whitepaper.md#the-stream-buffer), [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention), [§ Guarantees](../../../whitepaper.md#guarantees) |
| Research | — |
| Supersedes | — (refines [ADR-0031](0031-constructs-that-read-their-input-back-pin-their-start.md) points 2 and 5) |

## Context

[ADR-0031](0031-constructs-that-read-their-input-back-pin-their-start.md) point 2 says a producing layer pins its input at the start of the step it is running. When the step ends, the pin moves to the `stepStart` of the oldest output the layer still holds. LAY-02 built "still holds" as `StreamBuffer.OldestStepStart`: the `stepStart` of the first item in the oldest segment the output buffer holds ([StreamBuffer.cs](../../../src/Creole/StreamBuffer.cs)). `Tokens`, `Transduce`, `Select`, `Where` and `Utf8` each take their floor from it.

The output buffer keeps the segment holding the item its reader read last ([ADR-0028](0028-an-errors-items-are-copied-when-the-buffer-releases-them.md) point 3). So the pin never passes that item's step, and every later step that emits nothing is held as well. LAY-02's completion review found this, and ADR-0031 point 5 put it in guarantee 2: "a filter that drops a long run holds the whole run". Some cases:

| Layer | Input | Held today |
| --- | --- | --- |
| `Layer.Where(line => line.Contains("ERROR"))` | A log whose matches are far apart | Every line between two matches |
| `Layer.Where` that drops comment tokens | A 10,000-line commented-out block | The block's tokens, and its characters in the buffer below |
| A `Transduce` layout layer | A long run of blank lines, which emit nothing | The whole run |

That breaks G2's measure, "peak memory tracks the largest backtracking window, not input size" (§ Design goals and non-goals). The reader has no window open over the dropped run. It has read one item and wants the next.

**Why the pin exists.** A slot's `stepStart`, `stateBefore` and `groupIndex` let its item be produced again (§ The stream buffer). Only invalidation produces an item again, and it reaches only items at or above the reader's horizon. The horizon is the minimum of the reader's cursor, its floor, its own pin and the restart bookmark (§ Bookmarks and retention).

| What produces an item again | Where it starts | Why that is at or above the horizon |
| --- | --- | --- |
| A mode change (§ Mode channels, invalidation rule) | The reader's cursor | The horizon is at or below the cursor |
| Rollback | A mode-log entry's index, at or past the position the rewind returns to | A rewind returns to a pin's start, which is at or above the floor, or to a light mark, which rewinds only when nothing was consumed |
| A restart (§ The restart protocol) | The restart bookmark, then rollback | The restart bookmark is one of the horizon's inputs |
| A cascade | The `stepStart` of the re-run output, in the buffer below | By induction down the layers (Decision, point 4) |

An output below the horizon has been consumed and lies outside every pin. Nothing produces it again, so its step's input need not be held.

**What reads a step's input after the step ends.** These were checked so that the narrower pin frees nothing that is read later:

| What | Reads | Needs the producing layer's input? |
| --- | --- | --- |
| An output's span | Its slot, written when the item is added. That includes a synthetic output's span after its step ([TransduceLayer.cs](../../../src/Creole/Layers/TransduceLayer.cs)) | No |
| ADR-0028's copies and [ADR-0035](0035-span-combinators-commit-like-sequences-and-hold-their-spans.md)'s span holds | The reader's own buffer, copied when it releases | No |
| The producing layer's failure log, across its steps | Its input, copied when that input is released, since `Release` passes the log | No. Release copies first, as it does now |
| Trivia ([ADR-0036](0036-trivia-is-the-source-between-items-read-with-a-zero-width-parser.md)) | The spans of the item before the reader's cursor and the item at it, in the reader's buffer | No |
| Diagnostics in streaming mode | The root layer's line counter, updated as it releases | No |
| An incremental session (§ Retained parses) | Every buffer, which it keeps whole | Not affected |

**The producer does not know its reader's horizon.** A producing layer runs inside a pull: a cursor's `TryGetSegment` fills the buffer up to the position it asked for. A horizon crosses the boundary only when the cursor releases. It releases after the pull returns, and only on entering a new segment (`LoadSegment` in [Cursor.cs](../../../src/Creole/Cursor.cs)). A reader at the producer's front gets a segment that ends at the last item filled, so it pulls again within the same segment. A producer that used the last horizon released would still hold the dropped run in that case, which is the common one.

**Found while drafting: CRL0008 needs outputs that were consumed.** The invalidation rule's step 2 runs item *i*'s step again and checks the first `groupIndex` outputs against the ones the reader consumed. Those outputs lie below the horizon. Their step is item *i*'s, so the narrower pin still holds its input. But the output buffer keeps only the segment holding item *i* − 1. When a step emits more outputs than a segment holds, the earlier ones may already have been released, under either pin. That is MODE-02's to settle, and this record does not change it.

Constraints:

- **Speed.** BENCH-01 measures single-layer parses over input held in memory. Those have no cursor source, so they never pull. No gate measures producing layers yet (BENCH-02).
- **Engines.** Generated code must pin as the interpreter does (G7, GEN-06).
- **Simplicity:** the maintainer's standing preference.

## Options

### Fork 1. What the layer pins

#### 1A. The oldest output the buffer holds (ADR-0031 point 2, as built)

- Pros:
  - No change.
  - The buffer alone decides it, so no reader can get it wrong.
- Cons:
  - A run of input that produces nothing is held whole while an earlier output is held. G2 fails for filters.
  - The pin is set by the first item of the oldest held segment, not by the item the reader read last. So a producing layer also holds the input of up to a segment of outputs its reader has passed. This is bounded, but it is waste.

#### 1B. The oldest output at or above the reader's horizon (recommended)

The pin is the `stepStart` of the output at the reader's horizon, if the buffer has produced it. Otherwise it is the start of the running step. Outputs are produced in order, and a step's outputs share its `stepStart`, so this is the lowest `stepStart` of any output the reader can still go back to.

- Pros:
  - A dropped run is released as it is dropped, so G2 holds for filters.
  - It is what the pin is for. The pin exists so an output can be produced again, and only outputs at or above the horizon can be.
  - Same mechanism, one changed input.
- Cons:
  - The producer needs the reader's horizon (Fork 2).
  - A reader that overstates its horizon lets the layer below release input that a later invalidation needs. Under 1A, a pull cannot get this wrong.
  - An output the buffer still holds may have lost its step's input. LAY-02's `LayerRetentionTests` checks every held output today, and has to change.

#### 1C. 1B, and no layer pin where nothing can invalidate

If no mode channel reaches a layer, nothing produces its outputs again, so it pins only the step it is running.

- Pros:
  - It holds the least. It drops the input of the outputs the reader can go back to.
- Cons:
  - It needs analysis to know which layers a channel reaches (MODE-01, ANA-01), and a second rule.
  - It saves only the input behind outputs the reader can still go back to. For a deterministic parser that is one token. For a pinned window, the window is held in the layer above anyway, so it is a constant factor on memory G2 already allows.

### Fork 2. How the producer learns the reader's horizon

#### 2A. The reader states its horizon with every pull (recommended)

`TryGetSegment` takes the reader's horizon. The buffer keeps the highest one stated, and the producing layer reads it as each step begins. A cursor passes the minimum of its position and its floor. A layer that reads without a cursor, such as `Select`, `Where` and `Utf8`, passes its own pin.

- Pros:
  - The signature means no pull can leave out a horizon.
  - The horizon is current at every pull.
  - Release stays where LAY-02 put it.
- Cons:
  - An internal interface changes, with its two test implementations (`SegmentedSource`, and `ReleasingSource` in `RejectionFailureTests`).

#### 2B. The reader releases before it pulls

The cursor calls `Release` before `TryGetSegment` rather than after entering a segment. The buffer remembers the horizon even when it frees nothing.

- Pros:
  - No new parameter. One exchange carries both release and horizon.
- Cons:
  - Release runs on every load, which at the producer's front is about once per item.
  - It moves the step that LAY-02 had to fix ("releasing on every segment load freed pooled arrays early", LAY-02's Outcome).
  - A layer that reads without a cursor must remember to do the same. Nothing makes it.

#### 2C. The buffer asks its reader

The buffer keeps a reference to its reader and asks for its horizon when a step begins.

- Pros:
  - Always exact.
- Cons:
  - A reader is a `Cursor<T>` struct, and each copy is an independent position ([cursor.md](../../../docs/engines/cursor.md)). There is no single object to ask. A reader that is a layer could register, but the last layer's cursor cannot.

## Decision

Options **1B and 2A**, as recommended. Accepted by the maintainer on 2026-11-24.

1. **The layer's pin.** A producing layer pins its input at the `stepStart` of the oldest output its reader can still go back to: the output at the reader's horizon, if the buffer has produced it. Otherwise it pins the start of the step it is running, from the moment the step begins. A restart keeps the pin. This replaces "the oldest output the layer still holds" in ADR-0031 point 2.
2. **The reader states its horizon when it pulls.** Every read that may make the layer below produce carries the reader's horizon: the minimum of its cursor, its floor, its own pin and the restart bookmark. A cursor passes the minimum of its position and its floor, and the floor of a step's cursor starts at its layer's pin. A layer that reads without a cursor passes its own pin. The buffer keeps the highest horizon stated, and a layer computes its pin from it as each step begins.
   - A horizon never falls. A pin lowers the floor no further than the cursor. A rewind goes back no further than an open pin's start, and a light mark rewinds only when nothing was consumed. A restart goes back no further than its bookmark. So a horizon stated earlier is still a lower bound, and a layer may use it until the next pull. A read that states less than the reader's true horizon, such as a cursor copy that reads behind the floor to build an error, does not lower it.
   - A copy of the cursor reads ahead only inside a pin, and carries the floor (ADR-0031 points 1 and 3). So it states the same horizon as the original.
3. **What the reader may no longer ask for.** An output below its reader's horizon keeps its item and slot while the buffer holds it (ADR-0028 point 3), but its step's input may be gone. Nothing may produce it again.
4. **Cascades.** A cascade truncates layer *L*'s input at *j*, the `stepStart` of *L*'s re-run output *i*. Output *i* is at or above the horizon of *L*'s reader, so *j* is at or above *L*'s pin. *L*'s pin is one input of *L*'s horizon on its own input, so *j* is at or above that horizon too. The layer below *L* then sees an invalidation at *j*, at or above its reader's horizon, and the argument repeats one layer down. Two earlier arguments rest on the old pin, and both still hold:
   - ADR-0028's invariant that a cascade never truncates below a lower buffer's horizon;
   - ADR-0030's note that an intermediate layer's discarded input was never released, so never copied.
5. **§ Guarantees point 2 becomes:**

   > **Bounded memory.** Memory at a boundary is proportional to the longest window held open by a pin in the layer above. The layer below keeps its own input from where the step began that produced the oldest item the layer above can still go back to, or from the start of the step it is running if there is none, through that step. `Atomic`, `Lookahead`, `Not`, `Longest`, `Slice`, `Text`, `Custom` and a longer `String` or `CIString` pin (§ Bookmarks and retention). `Or` and repetition add nothing, so a large document keeps a constant number of items per boundary, however deeply its `Or`s nest, as long as no pin in the last layer spans it. Input that produces nothing is held only while the layer above can still go back to an item produced before it, so a filter that drops a long run holds one step of it at a time.

6. **Engines.** Generated code computes the pin the same way (GEN-06). ADR-0031 point 4 still lets an engine skip node pins inside a producing layer's step, because the narrower pin is still at or below the running step's start.
7. **Left to other cards.** [MODE-02](../../active/07-mode-channels/MODE-02-invalidation-rule.md): CRL0008's check needs consumed outputs that the output buffer may have released, under either pin.

Deciding reasons:

- **1B** gives G2 to filtering layers with the mechanism that exists. It narrows the pin to exactly what invalidation can reach. 1C saves a constant and needs analysis and a second rule.
- **2A** carries the horizon across the boundary before the producer runs, and its signature makes it impossible to leave out. It leaves release where LAY-02 put it.

What would change this:

- **A way to produce an output again from below its reader's horizon**: a release ahead of the cursor (ADR-0028's "what would change this"), a construct that rewinds without pinning, or an incremental session over streamed input. Each would need the old pin, or a pin of its own.
- **A measurement in LAY-05 or BENCH-02** showing that the per-step lookup costs time. That is unlikely: it reads one slot per step, as `OldestStepStart` does now.

## Consequences

**Easier:**

- A filter, or a step that emits nothing, holds a constant amount of input. Record streams such as logs keep G2.
- A producing layer no longer holds the input of up to a segment of outputs its reader has passed.

**Harder:**

- `ICursorSource.TryGetSegment` gains a parameter, and its two test implementations change. It is internal.
- Correctness now depends on each reader stating its horizon truthfully. An overstated horizon gives no error until an invalidation needs released input.
- The invariant "every output a buffer holds can be produced again" becomes "every output at or above its reader's horizon can". `LayerRetentionTests` asserts the old one.
- LAY-02's Outcome, [layers.md](../../../docs/concepts/layers.md) and the XML docs on the layers and on `StreamBuffer.OldestStepStart` describe the old pin.

**Ruled out:**

- Producing an output again from below its reader's horizon.

**Risks and how they are watched:**

- **An overstated horizon.** `LayerRetentionTests` changes to check, after each read and with and without a pinned reader, that every output at or above the reader's horizon still has its step's input. Overstating the horizon in the cursor, or in any layer that reads without one, must fail it.
- **A dropped run still held.** LAY-09's criteria: `Where` and `Transduce` over runs of N items that produce nothing hold an amount of input that does not grow with N, and `BoundedMemoryTests` gains a layer that drops most of its input.
- **An invalidation that needs released input.** MODE-02, MODE-03 and MODE-04 have no streamed tests yet. MODE-04's model-based tests run over segments of one and two items, with the overwriting pool LAY-02's tests use.

## Changes made

- **Whitepaper:**
  - [§ The stream buffer](../../../whitepaper.md#the-stream-buffer): the step fields let any item its reader can still go back to be produced again.
  - [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention): a producing layer pins at the `stepStart` of the output at its reader's horizon, or at the start of its running step if it has not produced that output. Only invalidation produces an output again, and only at or above the horizon. The reader states its horizon each time it pulls, and a horizon never falls (points 1 and 2). Links this record.
  - [§ Guarantees](../../../whitepaper.md#guarantees) point 2, as in point 5.
  - Checked and unchanged: § Feedback's invalidation, rollback and cascade rules, which already act only at or above the reader's horizon; § The restart protocol, whose bookmark is one of the horizon's inputs; § Trivia, which reads spans from the reader's own buffer; § Retained parses, which keeps every buffer.
- **Cards:**
  - [LAY-09](../../active/06-layers-and-pipelines/LAY-09-hold-only-the-input-the-reader-can-still-go-back-to.md): to Building, with the scope narrowed to points 1 and 2 and a criterion for `LayerRetentionTests`.
  - [MODE-02](../../active/07-mode-channels/MODE-02-invalidation-rule.md): a scope line for point 7.
  - [MODE-04](../../active/07-mode-channels/MODE-04-multi-layer-invalidation-cascade.md): the pin in its scope follows point 4, and its model-based tests also run over streamed input in small segments.
  - [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md): a producing layer's pin follows point 1, and every pull states the reader's horizon.
- **Decisions index:** a note that this record refines ADR-0031 points 2 and 5.

This record was edited in place before its branch merged. LAY-09's build found that a cursor copy reading behind the floor, as an error does with `ItemAt`, states a horizon below the reader's true one. If the buffer kept the latest horizon, that would lower the pin, and in a stack of layers the pin lookup below could land on an item already released. So Fork 2A and point 2 now say the buffer keeps the highest horizon stated, which equals the latest whenever every reader states its true horizon.
