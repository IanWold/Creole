# LAY-01: StreamBuffer and pulling cursor

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Building |

## Summary

Each layer boundary is a memoizing buffer, so a rewind is an index reset and upstream work never re-runs.

## Scope

- Segmented, `ArrayPool`-backed `StreamBuffer<T>` indexed by absolute position.
- Slot fields: `item`, `span`, `stepStart`, `stateBefore`, `groupIndex`.
- Reading past the filled region pulls the upstream layer for one more item.
- Release segments below the retention horizon back to the pool.
- Back `Cursor<TIn>` with the buffer, keeping the in-segment fast path.
- A slice taken over the buffer is a copy, never a view of a pooled segment ([ADR-0013](../../docs/decisions/0013-slices-view-in-memory-input-and-copy-streamed-input.md)).
- Before a segment is released, copy the item and span at each position the reading layer's failure log still refers to in it: the start of each open filter, chained operator and custom scanner, each `Context` frame's start, and the position and end of the furthest failure and of each saved failure ([ADR-0028](../../docs/decisions/0028-an-errors-items-are-copied-when-the-buffer-releases-them.md)). Keep the segment holding the item just below the horizon, so a cursor never holds a returned segment. Over in-memory input nothing is released, so nothing is copied.
- Constructs record positions only. `FilterPlan`, `ExpressionPlan` and `CustomPlan` register their start with the failure log instead of peeking at it. `FailureLog<T>` keeps the copies taken at release, `FailureMark` no longer carries an item, and a `Context` frame gains room for a copied span. This changes ERR-02's code: behaviour over in-memory input does not change, and its tests keep their assertions.

## Acceptance criteria

- [ ] Each upstream item is produced exactly once per parse in tests without mode changes (guarantee 1).
- [ ] A slice taken over the buffer keeps its text after its segment is released to the pool and reused (ADR-0013).
- [ ] Over a buffer that has released the items involved, a rejected filter, a chained operator, a `Context` line and the furthest failure each report the same error as over in-memory input, including the furthest failure an `Atomic` rewound past in `Atomic(p).Or(q.Where(pred))` (ADR-0028).

## Blocked by

- [INT-01 Cursor and in-memory input](../03-interpreter/INT-01-cursor-and-in-memory-input.md)

## Unblocks

- [SPAN-02 Out-of-band span storage](../05-source-spans/SPAN-02-out-of-band-span-storage.md)
- [LAY-02 Marks and retention](../06-layers-and-pipelines/LAY-02-marks-and-retention.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)

## Whitepaper references

- [§ The stream buffer](../../../whitepaper.md#the-stream-buffer)
- [§ Guarantees](../../../whitepaper.md#guarantees)
- [§ Failure positions](../../../whitepaper.md#failure-positions)

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
- [ADR-0028](../../docs/decisions/0028-an-errors-items-are-copied-when-the-buffer-releases-them.md) The items an error needs behind the cursor are copied when the buffer releases them, not when a construct starts

## Progress notes

- 2026-10-26 (Design): Started. The capture of a construct's first item and span (filters, chained operators, `Context`) needs a decision record that obeys mode invalidation before the buffer is built (Phase 0 review finding 16). Next: `/decide` on when and how the capture is taken; then Build.
- 2026-10-26 (Design): Drafted ADR-0028 (Proposed). It recommends recording positions only and copying items and spans when the buffer releases their segment, which no mode change can make stale. Drafting also found that the furthest failure's own item has no rule on streamed input: `Atomic(p).Or(q.Where(pred))` reads a released item when the error is built. The record covers that case too. Next: the maintainer accepts, rejects or revises; on acceptance, update the whitepaper and LAY-01, MODE-02, LAY-08 and GEN-06, then Build.
- 2026-10-26 (Design): ADR-0028 accepted. Whitepaper § Failure positions, § The stream buffer and § Mode channels updated; scope and criteria of LAY-01, MODE-02, LAY-08 and GEN-06 updated.
- 2026-10-26 (Build): Started. Next: `/build` plans the buffer, the pulling cursor and the release-time copies. Before LAY-02 adds pins, the release horizon is the cursor, so the `Atomic` case of the new criterion needs `p` to stay within one segment or a horizon the test controls.
- 2026-10-26 (Build): Increment 1 done. Added `StreamBuffer<T>`, `StreamSlot` and `IUpstream<T>`; `Cursor<T>` reads the buffer through `ICursorSource<T>` unchanged, so its in-segment fast path stays. `Release(horizon)` returns whole segments below the horizon; nothing in `src` calls it until LAY-02 computes the horizon. `StreamBufferTests` (8 tests) cover every control shape to depth 3 over segments of 1 and 2 matching in-memory results with each item produced once, a token buffer over a character buffer, one-step-at-a-time pulls, slot fields, release, spans, a slice surviving its segment's reuse, and no allocation per item when reading and releasing. A mutation that exposes unfilled slots fails 7 of the 8. Full suite: 1,531 passed. No `docs/` change, since nothing public changed. Criteria 1 and 2 appear met. Next: increment 2, ADR-0028's copies, with saved failure records added to ADR-0028 point 1 in place.
- 2026-10-26 (Build): Increment 2 done. `FailureLog<T>` records positions only and, through `Preserve`, copies the item and span at each position an error may still read before `StreamBuffer<T>.Release(horizon, failures)` frees it: the furthest failure and its end, every saved failure (a new stack pushed by `Save` and popped by `Keep` and `Restore`), every context frame's start, and the starts that filters and chained operators now hold with `HoldStart` instead of peeking. It also keeps the last released item's span, for a rejection whose value ends at a segment boundary. `FailureMark` loses its item fields. Edited ADR-0028 in place (unmerged) for saved failures, the last released span and `FailureMark`. `ReleasedInputFailureTests` (6 tests) release the buffer during and after the parse, and each case gives the same error as in memory, including `Atomic(p).Or(q.Where(pred))`. Disabling any one copy path fails at least one test. `BuildingTheErrorsReadsNothingBehindTheCursor` now releases through the stream buffer, with its assertions unchanged. docs/concepts/errors.md no longer says an item is captured when it is recorded. Full suite: 1,537 passed. All three criteria appear met. Next: the maintainer opens the PR and runs `/complete`.
- 2026-10-27 (Build): `/complete` stopped before ticking. The critic found no blocker, but: (1) the copy of a saved failure's end span has no test, so the 2026-10-26 claim that every copy path is tested was wrong; (2) a cursor at the end of a segment keeps that segment after `Release` returns it to the pool, and `Reset`, `ItemAt` and `Since` then read reused memory instead of throwing; (3) a `Custom` scanner that fails after moving records at its start, a position ADR-0028 point 1 does not list; (4) the Scope still says `FailureMark` keeps its item fields, and the card and ADR-0028 say ERR-02's tests pass "unchanged" although one harness changed; (5) ADR-0028's line links are stale, and ERR-02 has no note pointing to it; (6) `Release`'s failure log is optional, so a caller can skip the copies by mistake. Next: increment 3 fixes these: keep the segment holding the item below the horizon, which also removes the last-released span; hold `Custom`'s start; add the missing tests; make the log argument required; fix the wording. Then run `/complete` again.
- 2026-10-27 (Build): Increment 3 done, fixing the critic's findings. `Release` keeps the segment holding the item just below the horizon, so a cursor never holds a returned segment; this replaced the log's copy of the last released span. Its failure log is now a required argument. `CustomPlan` holds its start, and any failure recorded at the innermost held start takes that copy. New tests: a cursor that has read a segment's last item keeps it (fails under the old rule), a custom scanner that fails after moving, a saved failure's end span, and a rejection an `Atomic` rewound past. The allocation test now measures steady-state reading on one buffer, since each buffer keeps its last segment when a parse ends. A sweep of 10 mutations (every copy, the transfer, `Custom`'s hold, the kept segment) each fails at least one test. Card Scope and ADR-0028 (in place) updated to match; ERR-02 has a dated note pointing to ADR-0028. Full suite: 1,541 passed; AOT smoke publish and run OK. The speed gate was not re-run: its benchmarks use neither `Custom` nor `Where` and parse successfully, so they never reach the changed failure paths. Next: run `/complete` again, with the critic re-checking.
