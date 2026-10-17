# INC-04: Reusable parser rules

| | |
| --- | --- |
| Epic | [Incremental re-parsing (1.x)](../../README.md#inc--incremental-re-parsing-1x) |
| Phase | Phase 4 — 1.x releases |
| Size | L |
| Status | Not started |

## Summary

Reuse whole rule results, incremental-packrat style, for rules that opt in.

## Scope

- `.Reusable()` memoizes result, indexes, examined extent and entry state.
- Reuse requires matching entry mode stack; `Bind` disables reuse; recovered items are never reused.

## Acceptance criteria

- [ ] Editing one statement in a large file re-parses only that statement's rule.

## Blocked by

- [INC-03 Layer item reuse](../15-incremental-reparsing/INC-03-layer-item-reuse.md)

## Unblocks

- [INC-05 Session span table](../15-incremental-reparsing/INC-05-session-span-table.md)
- [INC-06 CRL0205 stale span analyzer](../15-incremental-reparsing/INC-06-crl0205-stale-span-analyzer.md)

## Whitepaper references

- [§ Reusing parser results](../../../whitepaper.md#reusing-parser-results)
- [§ Limits](../../../whitepaper.md#limits)
