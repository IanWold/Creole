# ERR-04: Diagnostic withdrawal

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 1 — Layers |
| Size | M |
| Status | Not started |

## Summary

Diagnostics raised on paths that are later abandoned must disappear, so every error is reported once.

## Scope

- Every diagnostic records the layer and item index that raised it.
- Marks record the diagnostic count; rewinding a mark withdraws later diagnostics.
- A hook for mode invalidation to withdraw diagnostics raised while producing discarded items (used by MODE-02).

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
