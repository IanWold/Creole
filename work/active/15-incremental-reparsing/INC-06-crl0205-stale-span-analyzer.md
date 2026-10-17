# INC-06: CRL0205 stale span analyzer

| | |
| --- | --- |
| Epic | [Incremental re-parsing (1.x)](../../README.md#inc--incremental-re-parsing-1x) |
| Phase | Phase 4 — 1.x releases |
| Status | Not started |

## Summary

Warn when a `Reusable` rule's result type stores a `SourceSpan`.

## Scope

- Build-time check over result types of reusable rules.

## Acceptance criteria

- [ ] `record Stmt(SourceSpan Span, ...)` in a reusable rule is flagged.

## Blocked by

- [RSLN-03 Build-time diagnostic reporting](../10-roslyn-analyzers/RSLN-03-build-time-diagnostic-reporting.md)
- [INC-04 Reusable parser rules](../15-incremental-reparsing/INC-04-reusable-parser-rules.md)

## Unblocks

- [INC-07 Incremental re-parsing gate](../15-incremental-reparsing/INC-07-incremental-re-parsing-gate.md)

## Whitepaper references

- [§ Reusing parser results](../../../whitepaper.md#reusing-parser-results)
- [§ Diagnostics](../../../whitepaper.md#diagnostics)
