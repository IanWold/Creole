# SPAN-03: Synthetic items

| | |
| --- | --- |
| Epic | [Source spans and provenance](../../README.md#span--source-spans-and-provenance) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

Items produced without consuming input, such as INDENT, DEDENT or an inserted semicolon.

## Scope

- `IsSynthetic` flag, which downstream layers read.
- LAY-03 gives synthetic `Transduce` outputs their zero-width spans, since its loop computes every span ([ADR-0032](../../docs/decisions/0032-emit-is-a-list-of-outputs-each-derived-or-synthetic.md) points 7 and 8). This card checks the placement end to end on Example B.

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

## Artifacts

- [ADR-0032: `Emit<T>` is a list of outputs, each derived or synthetic, not a union](../../docs/decisions/0032-emit-is-a-list-of-outputs-each-derived-or-synthetic.md)
