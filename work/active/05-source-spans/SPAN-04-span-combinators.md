# SPAN-04: Span combinators

| | |
| --- | --- |
| Epic | [Source spans and provenance](../../README.md#span--source-spans-and-provenance) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

Let grammars capture and test locations.

## Scope

- `Spanned()` and `WithSpan((value, span) => node)`.
- `Adjacent(p, q)`, succeeding only when spans touch.
- `Position`, a zero-width parser returning the current offset.

## Acceptance criteria

- [ ] The `>>` split-and-merge example works with `Adjacent(Kind(Gt), Kind(Gt))`.

## Blocked by

- [SPAN-02 Out-of-band span storage](../05-source-spans/SPAN-02-out-of-band-span-storage.md)

## Unblocks

- [INC-05 Session span table](../15-incremental-reparsing/INC-05-session-span-table.md)

## Whitepaper references

- [§ Using spans in grammars](../../../whitepaper.md#using-spans-in-grammars)
- [§ 1. Split and merge](../../../whitepaper.md#1-split-and-merge)
