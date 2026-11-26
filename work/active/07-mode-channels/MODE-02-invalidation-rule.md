# MODE-02: Invalidation rule

| | |
| --- | --- |
| Epic | [Mode channels](../../README.md#mode--mode-channels) |
| Phase | Phase 1 — Layers |
| Status | Building |

## Summary

A mode change discards items lexed under the old mode and re-produces them correctly.

## Scope

- A write is a change only when its new top differs from its old top by `EqualityComparer<TMode>.Default`. Every `SetMode`, `PushMode` and `PopMode` appends (index *i*, previous mode stack) to the mode log, but only a change discards, re-runs and lowers ([ADR-0042](../../docs/decisions/0042-a-write-discards-only-when-it-changes-the-top-mode-and-the-mode-log-keeps-only-what-a-rewind-can-reach.md) point 1).
- Discard buffered items at index i or later, and withdraw the diagnostics raised by the steps that are run again (ERR-04, [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 4).
- Re-run item i's producing step from `stepStart` with `stateBefore`. The reader is a `Layer.Modal`, which adds one output per step, so the slot's `groupIndex` is 0 and nothing is dropped; assert it. Dropping outputs, CRL0008 and ADR-0040 point 7 belong to [MODE-04](MODE-04-multi-layer-invalidation-cascade.md) (ADR-0042 points 2 and 6).
- Under `ParseMany`, starting an item resets the parser's channels to their initial modes. Where a channel's top differs from its initial mode, that is a change at the item's first index, so it discards what the last item lexed past its end under another mode ([ADR-0041](../../docs/decisions/0041-a-channel-has-one-reader-below-one-writer-and-a-write-nothing-reads-or-a-pop-of-the-last-mode-throws.md) point 9, ADR-0042 point 4).
- Lower the furthest failure at *i*: a failure past *i* becomes a bare failure at *i*, and a failure whose span reaches past *i* loses that end and a rejection's or chained operator's message. Saves note the mode log's length and are lowered when restored, by the changes still in effect ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 3).
- This is the first caller of `FailureLog.WithdrawFromStep`. Test that `Tokens` and `Transduce` tag each diagnostic with its step's start, so the withdrawal takes exactly the re-run steps' diagnostics ([ERR-04](../04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md) Outcome).
- When the writing layer's input buffer releases segments, drop the mode-log entries below the horizon it states. Marks keep absolute log lengths. Before dropping, each open save that a dropped entry follows keeps the index of the first dropped entry that changed the top, and its restore lowers there ([ADR-0042](../../docs/decisions/0042-a-write-discards-only-when-it-changes-the-top-mode-and-the-mode-log-keeps-only-what-a-rewind-can-reach.md) point 5, option C2, tried against the record's criteria 3 to 6).
- A write whose layer reads the channel's reader through another layer discards nothing until MODE-04 builds cascades (ADR-0042 point 7).

## Acceptance criteria

- [ ] Example C lexes `/` correctly in both `(a) / 2` and `if (ok) /x/.test(s)`.
- [ ] A filter and a `Context` around Example C's `Operand`, which starts with `SetMode`, each report the item lexed under the new mode ([ADR-0028](../../docs/decisions/0028-an-errors-items-are-copied-when-the-buffer-releases-them.md)).
- [ ] Tests from [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 11: counterexample 3, and a rejection whose span crossed a later mode change no longer widens the error or supplies its message.

## Blocked by

- [ERR-04 Diagnostic withdrawal](../04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md)
- [LAY-02 Marks and retention](../06-layers-and-pipelines/LAY-02-marks-and-retention.md)
- [LAY-08 Per-layer parse state](../06-layers-and-pipelines/LAY-08-per-layer-parse-state.md)
- [MODE-01 Mode channels and Layer.Modal](../07-mode-channels/MODE-01-mode-channels-and-layermodal.md)

## Unblocks

- [ERR-05 Recover combinator and strategies](../04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md)
- [MODE-03 Rollback rule](../07-mode-channels/MODE-03-rollback-rule.md)
- [MODE-04 Multi-layer invalidation cascade](../07-mode-channels/MODE-04-multi-layer-invalidation-cascade.md)

## Whitepaper references

- [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels)
- [§ The stream buffer](../../../whitepaper.md#the-stream-buffer)

## Artifacts

- [ADR-0042](../../docs/decisions/0042-a-write-discards-only-when-it-changes-the-top-mode-and-the-mode-log-keeps-only-what-a-rewind-can-reach.md) (Proposed): a write discards only when it changes the top mode (A1, chosen); CRL0008 and ADR-0040 point 7 move to MODE-04 (B1, chosen); the mode log drops entries below the writing layer's horizon as its input releases (C2, recommended).

## Progress notes

- 2026-11-26 (Design): Started. The scope leaves open how CRL0008's check reaches consumed outputs the buffer may have released (ADR-0040 point 7); MODE-01 left open whether a `SetMode` that leaves the top mode unchanged is a change; and the whitepaper does not say what "match" means for CRL0008's comparison or how CRL0008 surfaces at run time. Next: a decision record via `/decide`.
- 2026-11-26 (Design): Drafted ADR-0042 (Proposed). Reading the layer code showed that a channel's reader, a `Layer.Modal`, adds one output per step, so a single boundary never drops outputs and CRL0008 first becomes reachable in a MODE-04 cascade. The maintainer chose A1 (a write discards only when it changes the top) and B1 (CRL0008 and ADR-0040 point 7 move to MODE-04); scope moved accordingly. Drafting found that nothing bounds the mode log, which grows by one entry per write over a whole document; Fork C recommends C2. Next: the maintainer approves the options, criteria and trial plan, especially Fork C; then Build.
- 2026-11-26 (Build): The maintainer approved ADR-0042's options, criteria and trial plan (A1, B1, C2), and committed the draft as `fb86fe7`. Scope gained C2's line. Next: plan the build against the criteria and ADR-0042's criteria 1 to 6.
- 2026-11-26 (Build): Plan approved: four increments (lowering in the failure log; the mode log, with changes that lower; discarding, with Example C and the criteria tests; bounding the log under C2, with the measurements). Calls: a change lowers the writing layer's failures wherever it happens; until MODE-03, restores and `Longest`'s reset lower at the first change since their save; the `ParseMany` reset discards but neither logs nor lowers; a `PipelineRun.Log<T>()` seam lets tests read a lexer's diagnostics; Example C lives in the tests, not `samples/`. Increment 1 done: `FailureLog.LowerAt` under ADR-0030 point 3. A failure past the index becomes a bare failure there, marked by a context sentinel that marks and diagnostic copies carry, so the next failure there supplies the context; lowering bumps the generation and records nothing. A failure whose span reaches past the index loses its end and a chained operator's message, and a filter's message, which a new flag on the log, `FailureMark` and diagnostic copies tracks. A given-back failure where one was lowered supplies its context. No callers yet. `FailureLoweringTests` (16); ten mutations each failed a test. `dotnet build` 0 warnings; `dotnet test` 2,021 passed. Next: increment 2, the mode log and changes that lower.
