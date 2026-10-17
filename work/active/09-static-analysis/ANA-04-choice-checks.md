# ANA-04: Choice checks

| | |
| --- | --- |
| Epic | [Static analysis](../../README.md#ana--static-analysis) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

Warn about alternatives that can never succeed or never be tried.

## Scope

- CRL0101: shadowed alternative, exact via DFA product when both are regular, otherwise from FIRST sets.
- CRL0102: overlapping FIRST in `Or` without `Atomic`.
- CRL0103: two `Longest` alternatives match the same string.
- CRL0105: `Match` leaves a union case without a branch.

## Acceptance criteria

- [ ] Each check fires on its whitepaper example with no false positives on Examples A to C.

## Blocked by

- [INT-06 DFA compiler for regular subtrees](../03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md)
- [ANA-02 Analysis report and Debug enforcement](../09-static-analysis/ANA-02-analysis-report-and-debug-enforcement.md)

## Unblocks

- [RSLN-03 Build-time diagnostic reporting](../10-roslyn-analyzers/RSLN-03-build-time-diagnostic-reporting.md)

## Whitepaper references

- [§ Diagnostics](../../../whitepaper.md#diagnostics)
