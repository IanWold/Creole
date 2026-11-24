# LAY-09: Hold only the input the reader can still go back to

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Building |

## Summary

A producing layer keeps its input only from the step of the oldest output its reader can still go back to, so a run of input that produces nothing is not held behind an output the reader has passed.

## Scope

- Today a producing layer keeps its input from the `stepStart` of the oldest output its buffer holds ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 2). The buffer always keeps the item its reader read last ([ADR-0028](../../docs/decisions/0028-an-errors-items-are-copied-when-the-buffer-releases-them.md) point 3), so the input after that item that produces nothing is held in full. `Layer.Where` dropping a run, or `Transduce` steps that emit nothing, hold the whole run, and a filter that drops most of its input holds memory that grows with it (LAY-02's Outcome).
- Pin at the `stepStart` of the output at the reader's horizon, or at the running step's start if the layer has not produced it ([ADR-0040](../../docs/decisions/0040-a-producing-layer-keeps-input-only-for-outputs-its-reader-can-go-back-to.md) point 1). `Tokens`, `Transduce`, `Select`, `Where` and `Utf8` all take their pin this way.
- `ICursorSource.TryGetSegment` takes the reader's horizon, and the buffer keeps the latest one stated. A cursor passes the minimum of its position and its floor. A layer that reads without a cursor passes its own pin (point 2). Release stays where it is.
- ADR-0040 checked the narrower pin against invalidation, rollback, cascades, restarts, ADR-0028's copies, span holds and trivia. An output below the reader's horizon is never produced again, so its step's input may go.
- Update [layers.md](../../../docs/concepts/layers.md) and the XML docs that describe the old pin.

## Acceptance criteria

- [ ] `Layer.Where` dropping a run of N items, and `Transduce` steps that emit nothing over N items, hold a number of input items that does not grow with N.
- [ ] LAY-02's `BoundedMemoryTests` gains a layer that drops most of its input, and its bounds still hold.
- [ ] `LayerRetentionTests` checks, after each read and with and without a pinned reader, that every output at or above the reader's horizon still has its step's input. Overstating the horizon in the cursor, or in any layer that reads without one, fails it.

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
