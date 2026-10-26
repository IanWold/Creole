# INC-02: Examined-extent tracking

| | |
| --- | --- |
| Epic | [Incremental re-parsing (1.x)](../../README.md#inc--incremental-re-parsing-1x) |
| Phase | Phase 4 — 1.x releases |
| Status | Not started |

## Summary

Each buffered item records how far its producer examined input, not just what it consumed.

## Scope

- Track examined extent including lookahead and failed `Longest` alternatives.
- Implement in both engines.
- Decide how a custom scanner reports its examined extent before REL-04 freezes `IScanner`. Reads through `Cursor.Available` are otherwise invisible, so a lexer with one `Custom` token must be assumed to examine to the end of the input ([Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) finding 19).

## Acceptance criteria

- [ ] Examined extents are correct on property-generated grammars.

## Blocked by

- [INC-01 ParseSession and retained buffers](../15-incremental-reparsing/INC-01-parsesession-and-retained-buffers.md)

## Unblocks

- [INC-03 Layer item reuse](../15-incremental-reparsing/INC-03-layer-item-reuse.md)

## Whitepaper references

- [§ Reusing layer items](../../../whitepaper.md#reusing-layer-items)

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
