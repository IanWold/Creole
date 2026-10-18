# FND-08: Whitepaper editorial cleanup

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

Fix leftover inconsistencies in the whitepaper before it is shared more widely.

## Scope

- The abstract compares against Sprache, Superpower and Pidgin, and § Background says "all three libraries", but Parlot makes four.
- § Design goals says the goal not in 1.0 is general parsing; it is G12 (incremental re-parsing), and general parsing is not one of the twelve goals.
- § The stream buffer says each slot stores "three things" but the table lists five fields.
- § Design choices still says "two `int`s per buffered item"; reconcile with `long` public spans and segment-relative storage.
- References: reference 12 uses `12)`, which breaks the numbered list; reference 10 is never cited; the Libraries list mixes `-` and `*` bullets, which splits it in two.

## Acceptance criteria

- [x] Every listed issue is fixed.

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

## Progress notes

- 2026-10-17 (Building): Fixed every listed issue. The abstract no longer names the libraries (already fixed before this card), so only § Background needed the Parlot correction: it now says Sprache, Superpower and Pidgin cannot analyze or precompile grammars, and that Parlot precompiles but none of the four analyzes, matching § Comparison. § Design goals now names G12 as the 1.x goal and says general parsing is not a goal. § The stream buffer says five fields. § Design choices describes segment-relative 32-bit span offsets widened to the public `long` `SourceSpan`, as § Source spans does. References: 12 renumbered, Libraries bullets unified, and reference 10 (Willis and Wu 2021) now cited in § Expressions, checked against the paper's Pattern 1c (precedence tables). Next: `/complete FND-08`.

## Outcome

Every listed issue is fixed in [whitepaper.md](../../../whitepaper.md); the critic subagent confirmed each fix independently and found them editorial. Details:

- § Background: corrected, not just recounted. Parlot does precompile grammars, so the sentence now says Sprache, Superpower and Pidgin cannot analyze or precompile, and Parlot precompiles but none of the four analyzes, matching § Comparison. The abstract no longer named the libraries.
- § Design goals: G12 is the 1.x goal; general parsing is not a goal.
- § The stream buffer: "five fields".
- § Design choices: span storage now matches § Source spans.
- References: 12 renumbered, Libraries bullets unified, reference 10 cited in § Expressions after checking the paper (Pattern 1c).

Follow-up, not done here: the whitepaper presents Parlot's build-time generator as shipped, but it exists only in preview packages (`Parlot.SourceGenerator` 1.6.0 and 2.0.0 previews, checked on NuGet 2026-10-18). Proposed as a separate editorial change.

