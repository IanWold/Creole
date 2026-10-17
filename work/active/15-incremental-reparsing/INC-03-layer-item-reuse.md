# INC-03: Layer item reuse

| | |
| --- | --- |
| Epic | [Incremental re-parsing (1.x)](../../README.md#inc--incremental-re-parsing-1x) |
| Phase | Phase 4 — 1.x releases |
| Status | Not started |

## Summary

Re-run only the layer work an edit can affect.

## Scope

- Reuse items whose examined range ends before the edit.
- Re-lex from the first touched item; stop when a new item ends where an old one did, with the same state and mode.
- Shift spans lazily through an offset table.

## Acceptance criteria

- [ ] A one-character edit inside a string literal re-lexes one token.

## Blocked by

- [MODE-03 Rollback rule](../07-mode-channels/MODE-03-rollback-rule.md)
- [INC-02 Examined-extent tracking](../15-incremental-reparsing/INC-02-examined-extent-tracking.md)

## Unblocks

- [INC-04 Reusable parser rules](../15-incremental-reparsing/INC-04-reusable-parser-rules.md)

## Whitepaper references

- [§ Reusing layer items](../../../whitepaper.md#reusing-layer-items)
