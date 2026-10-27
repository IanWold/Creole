# ERR-04: Diagnostic withdrawal

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

Diagnostics raised on paths that are later abandoned must disappear, so every error is reported once.

## Scope

- Every diagnostic records the layer and the step that raised it.
- Marks record their own layer's diagnostic count. Rewinding a mark withdraws the diagnostics its own layer raised after it, never another layer's ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 4).
- A hook for mode invalidation and restarts to withdraw the diagnostics raised by steps that are run again or whose outputs are discarded (used by MODE-02 and STRM-02).
- A withdrawn diagnostic that was built from the furthest failure gives that failure back, before the rewind undoes mode changes ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 8).

## Acceptance criteria

- [ ] Property test: no diagnostic survives from an abandoned alternative.

## Blocked by

- [ERR-01 Error and result types](../04-errors-and-recovery/ERR-01-error-and-result-types.md)
- [LAY-02 Marks and retention](../06-layers-and-pipelines/LAY-02-marks-and-retention.md)

## Unblocks

- [ERR-05 Recover combinator and strategies](../04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md)
- [MODE-02 Invalidation rule](../07-mode-channels/MODE-02-invalidation-rule.md)

## Whitepaper references

- [§ Recovery](../../../whitepaper.md#recovery)
