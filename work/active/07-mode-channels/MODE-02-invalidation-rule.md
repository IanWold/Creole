# MODE-02: Invalidation rule

| | |
| --- | --- |
| Epic | [Mode channels](../../README.md#mode--mode-channels) |
| Phase | Phase 1 — Layers |
| Size | L |
| Status | Not started |

## Summary

A mode change discards items lexed under the old mode and re-produces them correctly, including inside multi-output steps.

## Scope

- Discard buffered items at index i or later, and withdraw their diagnostics (ERR-04).
- Re-run item i's producing step from `stepStart` with `stateBefore`, dropping the first `groupIndex` outputs.
- Check dropped outputs equal the consumed ones; report CRL0008 at run time if not.
- Append (index i, previous mode stack) to the mode log.

## Acceptance criteria

- [ ] Example C lexes `/` correctly in both `(a) / 2` and `if (ok) /x/.test(s)`.

## Blocked by

- [ERR-04 Diagnostic withdrawal](../04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md)
- [LAY-02 Marks and retention](../06-layers-and-pipelines/LAY-02-marks-and-retention.md)
- [MODE-01 Mode channels and Layer.Modal](../07-mode-channels/MODE-01-mode-channels-and-layermodal.md)

## Unblocks

- [ERR-05 Recover combinator and strategies](../04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md)
- [MODE-03 Rollback rule](../07-mode-channels/MODE-03-rollback-rule.md)
- [MODE-04 Multi-layer invalidation cascade](../07-mode-channels/MODE-04-multi-layer-invalidation-cascade.md)

## Whitepaper references

- [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels)
- [§ The stream buffer](../../../whitepaper.md#the-stream-buffer)
