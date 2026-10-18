# INC-07: Incremental re-parsing gate

| | |
| --- | --- |
| Epic | [Incremental re-parsing (1.x)](../../README.md#inc--incremental-re-parsing-1x) |
| Phase | Phase 4 — 1.x releases |
| Status | Not started |
| Gate | Incremental gate |

## Summary

Ship only if re-parse cost tracks the size of the edit (G12).

## Scope

- Replay at least 500 single-character edits on JSON and Example B files of about 300 KB and on 10× copies.
- Editor-style scenario test.

## Acceptance criteria

- [ ] Median re-parse ≤ 2% of a full parse, p99 ≤ 100 ms, and median on the 10× file ≤ 2× the base file ([ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md)).

## Blocked by

- [FND-06 Fix numeric gate thresholds](../01-foundations/FND-06-fix-numeric-gate-thresholds.md)
- [INC-05 Session span table](../15-incremental-reparsing/INC-05-session-span-table.md)
- [INC-06 CRL0205 stale span analyzer](../15-incremental-reparsing/INC-06-crl0205-stale-span-analyzer.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Incremental re-parsing](../../../whitepaper.md#incremental-re-parsing)
