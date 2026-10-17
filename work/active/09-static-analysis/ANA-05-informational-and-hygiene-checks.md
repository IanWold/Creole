# ANA-05: Informational and hygiene checks

| | |
| --- | --- |
| Epic | [Static analysis](../../README.md#ana--static-analysis) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

Tell authors where analysis is limited and where error messages will be poor.

## Scope

- CRL0201: opaque node limits analysis.
- CRL0202: dependent query clause makes the rest of the rule opaque.
- CRL0204: mutable type used as layer state.
- CRL0501: unlabelled rule whose errors would list many raw tokens.

## Acceptance criteria

- [ ] Each check fires on its whitepaper example.

## Blocked by

- [ANA-02 Analysis report and Debug enforcement](../09-static-analysis/ANA-02-analysis-report-and-debug-enforcement.md)

## Unblocks

- [RSLN-03 Build-time diagnostic reporting](../10-roslyn-analyzers/RSLN-03-build-time-diagnostic-reporting.md)

## Whitepaper references

- [§ Diagnostics](../../../whitepaper.md#diagnostics)
