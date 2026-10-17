# LAY-01: StreamBuffer and pulling cursor

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Size | L |
| Status | Not started |

## Summary

Each layer boundary is a memoizing buffer, so a rewind is an index reset and upstream work never re-runs.

## Scope

- Segmented, `ArrayPool`-backed `StreamBuffer<T>` indexed by absolute position.
- Slot fields: `item`, `span`, `stepStart`, `stateBefore`, `groupIndex`.
- Reading past the filled region pulls the upstream layer for one more item.
- Release segments below the retention horizon back to the pool.
- Back `Cursor<TIn>` with the buffer, keeping the in-segment fast path.

## Acceptance criteria

- [ ] Each upstream item is produced exactly once per parse in tests without mode changes (guarantee 1).

## Blocked by

- [INT-01 Cursor and in-memory input](../03-interpreter/INT-01-cursor-and-in-memory-input.md)

## Unblocks

- [SPAN-02 Out-of-band span storage](../05-source-spans/SPAN-02-out-of-band-span-storage.md)
- [LAY-02 Marks and retention](../06-layers-and-pipelines/LAY-02-marks-and-retention.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)

## Whitepaper references

- [§ The stream buffer](../../../whitepaper.md#the-stream-buffer)
- [§ Guarantees](../../../whitepaper.md#guarantees)
