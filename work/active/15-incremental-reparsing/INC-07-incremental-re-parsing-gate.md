# INC-07: Incremental re-parsing gate

| | |
| --- | --- |
| Epic | [Incremental re-parsing (1.x)](../../README.md#inc--incremental-re-parsing-1x) |
| Phase | Phase 4 — 1.x releases |
| Size | S |
| Status | Not started |
| Gate | Incremental gate |

## Summary

Ship only if re-parse cost tracks the size of the edit (G12).

## Scope

- Benchmark edit-size against re-parse cost on large files.
- Editor-style scenario test.

## Acceptance criteria

- [ ] Meets the incremental threshold from FND-06.

## Blocked by

- [FND-06 Fix numeric gate thresholds](../01-foundations/FND-06-fix-numeric-gate-thresholds.md)
- [INC-05 Session span table](../15-incremental-reparsing/INC-05-session-span-table.md)
- [INC-06 CRL0205 stale span analyzer](../15-incremental-reparsing/INC-06-crl0205-stale-span-analyzer.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Incremental re-parsing](../../../whitepaper.md#incremental-re-parsing)
