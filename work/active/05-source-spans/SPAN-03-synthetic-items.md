# SPAN-03: Synthetic items

| | |
| --- | --- |
| Epic | [Source spans and provenance](../../README.md#span--source-spans-and-provenance) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

Items produced without consuming input, such as INDENT, DEDENT or an inserted semicolon.

## Scope

- Zero-width span at the start of the next input item.
- `IsSynthetic` flag.

## Acceptance criteria

- [ ] Example B's "expected statement" error after a block points at the dedented line.

## Blocked by

- [SPAN-02 Out-of-band span storage](../05-source-spans/SPAN-02-out-of-band-span-storage.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)

## Unblocks

- [ERR-06 OrInsert for missing items](../04-errors-and-recovery/ERR-06-orinsert-for-missing-items.md)
- [LAY-07 Three-layer demo and bounded-memory check](../06-layers-and-pipelines/LAY-07-three-layer-demo-and-bounded-memory-check.md)

## Whitepaper references

- [§ Span rules](../../../whitepaper.md#span-rules)
