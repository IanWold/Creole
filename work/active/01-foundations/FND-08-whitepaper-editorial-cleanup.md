# FND-08: Whitepaper editorial cleanup

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

Fix leftover inconsistencies in the whitepaper before it is shared more widely.

## Scope

- The abstract compares against Sprache, Superpower and Pidgin, and § Background says "all three libraries", but Parlot makes four.
- § Design goals says the goal not in 1.0 is general parsing; it is G12 (incremental re-parsing), and general parsing is not one of the twelve goals.
- § The stream buffer says each slot stores "three things" but the table lists five fields.
- § Design choices still says "two `int`s per buffered item"; reconcile with `long` public spans and segment-relative storage.
- References: reference 12 uses `12)`, which breaks the numbered list; reference 10 is never cited; the Libraries list mixes `-` and `*` bullets, which splits it in two.

## Acceptance criteria

- [ ] Every listed issue is fixed.

## Blocked by

Nothing. This item can start immediately.

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Abstract (opening section)](../../../whitepaper.md)
- [§ Design goals and non-goals](../../../whitepaper.md#design-goals-and-non-goals)
- [§ The stream buffer](../../../whitepaper.md#the-stream-buffer)
- [§ Design choices](../../../whitepaper.md#design-choices)
- [§ References](../../../whitepaper.md#references)
