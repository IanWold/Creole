# ANA-03: Loop and recursion checks

| | |
| --- | --- |
| Epic | [Static analysis](../../README.md#ana--static-analysis) |
| Phase | Phase 2 — Analysis |
| Size | M |
| Status | Not started |

## Summary

Turn the classic infinite-loop and left-recursion bugs into errors.

## Scope

- CRL0001: repetition of a nullable parser.
- CRL0002: left recursion, reported with the cycle path.
- CRL0003: nullable layer item parser.

## Acceptance criteria

- [ ] Each check fires on its whitepaper example and not on Examples A to C.

## Blocked by

- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)
- [ANA-02 Analysis report and Debug enforcement](../09-static-analysis/ANA-02-analysis-report-and-debug-enforcement.md)

## Unblocks

- [RSLN-03 Build-time diagnostic reporting](../10-roslyn-analyzers/RSLN-03-build-time-diagnostic-reporting.md)

## Whitepaper references

- [§ Diagnostics](../../../whitepaper.md#diagnostics)
