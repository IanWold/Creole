# LAY-02: Marks and retention

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

Pinning bookmarks keep what may be rewound; light marks pin nothing, so `Or` and repetition do not grow memory.

## Scope

- Pinning bookmarks taken by `Atomic`, `Lookahead`, `Not` and `Longest` over non-regular alternatives; reference-counted. An `Atomic` may release its bookmark once a cut inside it is permanent: no open attempt between the cut and the `Atomic` can still fail without consuming, and no `Lookahead`, `Not` or `Longest` alternative lies between them. The engine gets no call when `Cut` runs, so this needs the stack of open bookmarks and light marks; releasing at the `Atomic`'s exit is the conservative fallback ([ADR-0010](../../docs/decisions/0010-cuts-under-dispatch-and-across-layers.md)).
- Light marks taken by `Or`, `Optional`, repetition, `Expression.Build` operators and `Recover`: cursor position, mode log length, diagnostic count, layer-state reference, cut flag.
- Horizon is the minimum of the cursor and the oldest pin; each layer pins its own input at the `stepStart` of its oldest retained output.

## Acceptance criteria

- [ ] Property test of guarantee 2: deeply nested `Or` without `Atomic` keeps a constant number of items per boundary.

## Blocked by

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [LAY-01 StreamBuffer and pulling cursor](../06-layers-and-pipelines/LAY-01-streambuffer-and-pulling-cursor.md)

## Unblocks

- [ERR-04 Diagnostic withdrawal](../04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md)
- [LAY-04 Layer-local state](../06-layers-and-pipelines/LAY-04-layer-local-state.md)
- [LAY-06 Buffer size limit](../06-layers-and-pipelines/LAY-06-buffer-size-limit.md)
- [MODE-02 Invalidation rule](../07-mode-channels/MODE-02-invalidation-rule.md)
- [STRM-01 Starved outcome](../08-streaming/STRM-01-starved-outcome.md)
- [ANA-06 Deterministic mode](../09-static-analysis/ANA-06-deterministic-mode.md)

## Whitepaper references

- [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention)
- [§ Guarantees](../../../whitepaper.md#guarantees)
