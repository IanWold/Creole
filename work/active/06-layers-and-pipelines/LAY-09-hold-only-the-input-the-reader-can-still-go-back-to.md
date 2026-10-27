# LAY-09: Hold only the input the reader can still go back to

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

A producing layer keeps its input only from the step of the oldest output its reader can still go back to, so a run of input that produces nothing is not held behind an output the reader has passed.

## Scope

- Today a producing layer keeps its input from the `stepStart` of the oldest output its buffer holds ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 2). The buffer always keeps the item its reader read last ([ADR-0028](../../docs/decisions/0028-an-errors-items-are-copied-when-the-buffer-releases-them.md) point 3), so the input after that item that produces nothing is held in full. `Layer.Where` dropping a run, or `Transduce` steps that emit nothing, hold the whole run, and a filter that drops most of its input holds memory that grows with it (LAY-02's Outcome).
- Pin at the `stepStart` of the oldest output at or above the reader's horizon, or at the running step's start when there is none. The reader pulls when it needs an item it does not have, so it must make its horizon known before the pull. The cursor's release now comes after it.
- The narrower pin rests on an output below the reader's horizon never being produced again. Check that against mode invalidation and rollback ([MODE-02](../07-mode-channels/MODE-02-invalidation-rule.md), [MODE-03](../07-mode-channels/MODE-03-rollback-rule.md)), cascades ([MODE-04](../07-mode-channels/MODE-04-multi-layer-invalidation-cascade.md)), restarts ([STRM-02](../08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md)) and ADR-0028's copies. It changes ADR-0031 point 2 and § Guarantees point 2, so it needs a decision record before the build.

## Acceptance criteria

- [ ] `Layer.Where` dropping a run of N items, and `Transduce` steps that emit nothing over N items, hold a number of input items that does not grow with N.
- [ ] LAY-02's `BoundedMemoryTests` gains a layer that drops most of its input, and its bounds still hold.

## Blocked by

- [LAY-02 Marks and retention](LAY-02-marks-and-retention.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention)
- [§ Guarantees](../../../whitepaper.md#guarantees)

## Artifacts

- [ADR-0031: Constructs that read their input back pin their start, and a producing layer pins the step it is running](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) The pin this card narrows, and the guarantee 2 wording it would strengthen
