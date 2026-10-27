# MODE-02: Invalidation rule

| | |
| --- | --- |
| Epic | [Mode channels](../../README.md#mode--mode-channels) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

A mode change discards items lexed under the old mode and re-produces them correctly, including inside multi-output steps.

## Scope

- Discard buffered items at index i or later, and withdraw the diagnostics raised by the steps that are run again (ERR-04, [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 4).
- Re-run item i's producing step from `stepStart` with `stateBefore`, dropping the first `groupIndex` outputs.
- Check dropped outputs equal the consumed ones; report CRL0008 at run time if not.
- Append (index i, previous mode stack) to the mode log.
- Lower the furthest failure at *i*: a failure past *i* becomes a bare failure at *i*, and a failure whose span reaches past *i* loses that end and a rejection's or chained operator's message. Saves note the mode log's length and are lowered when restored, by the changes still in effect ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 3).

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
