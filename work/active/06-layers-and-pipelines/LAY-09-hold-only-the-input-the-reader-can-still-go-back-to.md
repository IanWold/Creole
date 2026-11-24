# LAY-09: Hold only the input the reader can still go back to

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Done |

## Summary

A producing layer keeps its input only from the step of the oldest output its reader can still go back to, so a run of input that produces nothing is not held behind an output the reader has passed.

## Scope

- Today a producing layer keeps its input from the `stepStart` of the oldest output its buffer holds ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 2). The buffer always keeps the item its reader read last ([ADR-0028](../../docs/decisions/0028-an-errors-items-are-copied-when-the-buffer-releases-them.md) point 3), so the input after that item that produces nothing is held in full. `Layer.Where` dropping a run, or `Transduce` steps that emit nothing, hold the whole run, and a filter that drops most of its input holds memory that grows with it (LAY-02's Outcome).
- Pin at the `stepStart` of the output at the reader's horizon, or at the running step's start if the layer has not produced it ([ADR-0040](../../docs/decisions/0040-a-producing-layer-keeps-input-only-for-outputs-its-reader-can-go-back-to.md) point 1). `Tokens`, `Transduce`, `Select`, `Where` and `Utf8` all take their pin this way.
- `ICursorSource.TryGetSegment` takes the reader's horizon, and the buffer keeps the latest one stated. A cursor passes the minimum of its position and its floor. A layer that reads without a cursor passes its own pin (point 2). Release stays where it is.
- ADR-0040 checked the narrower pin against invalidation, rollback, cascades, restarts, ADR-0028's copies, span holds and trivia. An output below the reader's horizon is never produced again, so its step's input may go.
- Update [layers.md](../../../docs/concepts/layers.md) and the XML docs that describe the old pin.

## Acceptance criteria

- [x] `Layer.Where` dropping a run of N items, and `Transduce` steps that emit nothing over N items, hold a number of input items that does not grow with N.
- [x] LAY-02's `BoundedMemoryTests` gains a layer that drops most of its input, and its bounds still hold.
- [x] `LayerRetentionTests` checks, after each read and with and without a pinned reader, that every output at or above the reader's horizon still has its step's input. Overstating the horizon in the cursor, or in any layer that reads without one, fails it.

## Blocked by

- [LAY-02 Marks and retention](LAY-02-marks-and-retention.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention)
- [§ Guarantees](../../../whitepaper.md#guarantees)

## Artifacts

- [ADR-0031: Constructs that read their input back pin their start, and a producing layer pins the step it is running](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) The pin this card narrows, and the guarantee 2 wording it would strengthen
- [ADR-0040: A producing layer keeps its input only for the outputs its reader can still go back to, and the reader states its horizon when it pulls](../../docs/decisions/0040-a-producing-layer-keeps-input-only-for-outputs-its-reader-can-go-back-to.md) The narrower pin, and how the reader's horizon reaches the layer below

## Progress notes

- 2026-11-24 (Design): Started. Draft a decision record narrowing ADR-0031 point 2's layer pin to outputs at or above the reader's horizon, checked against mode invalidation, rollback, cascades, restarts and ADR-0028's copies. Next: `/decide`.
- 2026-11-24 (Design): Drafted ADR-0040 (Proposed): 1B, pin at the `stepStart` of the output at the reader's horizon, or the running step's start; 2A, `TryGetSegment` carries the reader's horizon. Checked invalidation, rollback, restarts, cascades, ADR-0028 copies, span holds and trivia; none reads input below the horizon. Found CRL0008's check needs consumed outputs the output buffer may have released, under either pin, which goes to MODE-02. Next: maintainer accepts, rejects or revises ADR-0040.
- 2026-11-24 (Design): ADR-0040 accepted as written. Whitepaper, MODE-02, MODE-04 and GEN-06 updated; scope narrowed to the decision and a criterion added for `LayerRetentionTests`. Next: `/build`.
- 2026-11-24 (Building): Built in one increment. `ICursorSource.TryGetSegment` takes the reader's horizon: the cursor passes `Math.Min(position, Floor)`, and `Select`, `Where` and `Utf8` pass their pin. `StreamBuffer` keeps the highest horizon given by a pull or a release, and `OldestStepStart` is now the step start of the item at that horizon. ADR-0040 was edited in place from "latest" to "highest", since a cursor copy that reads behind the floor states less. Tests: `LayerRetentionTests` runs each layer over a producing layer and checks both boundaries; `BoundedMemoryTests` gains a run-length fact and a property with dropping layers, and every boundary's bound is now 2S + 1, the lexer's input included (LAY-02 had 3S + 2); a `StreamBufferTests` case pins the highest horizon. Eight mutations each fail a test: the old pin, a pin at only the running step, keeping the latest horizon, and overstating the horizon in `Cursor.LoadSegment`, `Cursor.Reset`, `Select`, `Where` and `Utf8`. `dotnet build` 0 warnings; `dotnet test` 1,766 passed. Docs: layers.md. Next: maintainer review and commit, then `/complete`.
- 2026-11-24 (Building): Verified for completion at fb4c40e. Every criterion met; see Outcome.

## Outcome

A producing layer now keeps its input only from the step of the oldest output its reader can still go back to, as [ADR-0040](../../docs/decisions/0040-a-producing-layer-keeps-input-only-for-outputs-its-reader-can-go-back-to.md) decides. A run of input that produces nothing is let go as it is read. The design stage produced ADR-0040; the build was one increment.

- **The horizon crosses each boundary.** `ICursorSource.TryGetSegment` takes the reader's horizon ([ICursorSource.cs](../../../src/Creole/ICursorSource.cs)):
  - The cursor passes `Math.Min(position, Floor)` from `LoadSegment`, `Reset` and `Since` ([Cursor.cs](../../../src/Creole/Cursor.cs)). It is computed only past the `_source is null` check, so in-memory parses are untouched.
  - `Select`, `Where` and `Utf8` pass their own pin ([src/Creole/Layers/](../../../src/Creole/Layers/)).
- **The pin.** `StreamBuffer` keeps the highest horizon it is given, by a pull or by `Release`. `OldestStepStart` is the step start of the output at that horizon, or null if it is not yet produced ([StreamBuffer.cs](../../../src/Creole/StreamBuffer.cs)). `Tokens` and `Transduce` already took their floor from it, so only their docs changed.
- **Tests:**
  - [BoundedMemoryTests.cs](../../../tests/Creole.Tests/BoundedMemoryTests.cs), 2 new:
    - `ALayerThatDropsALongRunHoldsTheSameWhateverTheRunsLength`. It runs runs of 10 to 10,000 dropped characters through `Where`, a `Transduce` whose steps emit nothing, and `Tokens(Any)` then `Where`, in segments of 1, 2 and 16.
    - `LayersThatDropMostOfTheirInput…`, a CsCheck property over LAY-02's nested-choice grammars. About one character in sixteen is followed by 1 to 256 noise characters, so about 89% of the input is dropped.
    - Every boundary now holds at most 2S + 1 items, S being the segment size. That includes the lexer's input in LAY-02's test, which allowed 3S + 2 under the old pin. Measured: S + 1 below a step-cursor layer, 2S + 1 below `Where` and at the top. A one-off run of 1,000 cases per property also passed.
  - [LayerRetentionTests.cs](../../../tests/Creole.Tests/LayerRetentionTests.cs) (1, rewritten):
    - Each of `Tokens`, `Transduce`, `Select`, `Where` and `Utf8` runs over a producing layer, with and without a pinned reader.
    - After each read it checks that every output at or above the reader's horizon still has its step's input. It also checks every input item at or above the layer's own pin.
  - [StreamBufferTests.cs](../../../tests/Creole.Tests/StreamBufferTests.cs) (1 new): the buffer keeps the highest horizon stated.
- **Mutation checks, on fresh builds, at fb4c40e.** Each fails at least one of these tests:
  - `OldestStepStart` as the oldest held output, which was the old pin;
  - a pin at only the running step;
  - keeping the latest horizon rather than the highest;
  - overstating the horizon in `Cursor.LoadSegment`, `Cursor.Reset`, `Select`, `Where` or `Utf8`. Each of these fails only the layers that read through the mutated code.
- **Evidence, 2026-11-24, at fb4c40e.** `dotnet build`: 0 warnings. `dotnet test`: 1,766 passed, 0 failed.

  | Criterion | Evidence |
  | --- | --- |
  | 1. A dropped run of N holds input that does not grow with N | `ALayerThatDropsALongRunHoldsTheSameWhateverTheRunsLength`: 2S + 1 at every boundary for N from 10 to 10,000. The old pin fails it |
  | 2. `BoundedMemoryTests` gains a layer that drops most of its input | `LayersThatDropMostOfTheirInputUnderAParserOfDeeplyNestedChoicesHoldAConstantNumberOfItemsAtEachBoundary`. The old pin fails it |
  | 3. `LayerRetentionTests` fails on an overstated horizon | `EveryProducingLayerKeepsTheInputOfEveryItemItsReaderCanStillGoBackTo` fails under each of the five overstating mutations |
- **Docs:** [layers.md](../../../docs/concepts/layers.md): what `Then` keeps, and that `Where` lets a dropped item go as it drops it. XML docs on `ICursorSource.TryGetSegment`, `StreamBuffer`, `Cursor.Floor` and the five layers. No public API changed.
- **Deviations:**
  - ADR-0040 was edited in place before merge: the buffer keeps the highest horizon stated, not the latest. A cursor copy that reads behind the floor to build an error states less than the reader's true horizon. The whitepaper's sentence in § Bookmarks and retention says the same. The record's closing note gives the reason.
  - LAY-02's lexer bound in `BoundedMemoryTests` tightened from 3S + 2 to 2S + 1, which the new pin achieves.
- **Not run:** the speed gate, whose benchmarks are in memory and never pull, and the AOT smoke publish, which this change adds nothing to.
- **Follow-ups:** none new. MODE-02 already carries ADR-0040 point 7.
