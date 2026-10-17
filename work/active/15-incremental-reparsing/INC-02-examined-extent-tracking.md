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

## Acceptance criteria

- [ ] Examined extents are correct on property-generated grammars.

## Blocked by

- [INC-01 ParseSession and retained buffers](../15-incremental-reparsing/INC-01-parsesession-and-retained-buffers.md)

## Unblocks

- [INC-03 Layer item reuse](../15-incremental-reparsing/INC-03-layer-item-reuse.md)

## Whitepaper references

- [§ Reusing layer items](../../../whitepaper.md#reusing-layer-items)
