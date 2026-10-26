# CORE-14: Choices beyond 16 alternatives

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

Decide how a choice takes more than 16 alternatives, and whether the fixed-arity overloads of `OneOf`, `Longest`, `Match` and `Switch` stay, before the grammar reader and the API freeze depend on their shape.

## Scope

- **Size today.** `OneOf` and `Longest` have 15 overloads each (2 to 16 alternatives): about 2,700 lines, 1,730 of them XML docs. `Match` and `Switch` have 8 fixed overloads each beside their collection forms, about 1,280 lines.
  - [ADR-0006](../../docs/decisions/0006-switch-and-match-take-a-collection-beyond-eight-cases.md)'s arity table decides `OneOf`'s 2–16 and `Match`'s and `Switch`'s fixed forms.
  - `Longest`'s arity comes from CORE-06.
- **The cap bites.** Building the 138-rule lexer needs a 15-way `Flat` switch and nested `Level` helpers, both in tests/Creole.Tests/Utilities/LexerSamples.cs and in benchmarks/Creole.Benchmarks/LongestLexers.cs. It also shapes a sentence in § Properties.
- **Options:**
  - (a) `params ReadOnlySpan<Parser<TIn, TOut>>` only, deleting about 2,600 lines. A `params` parameter must come last, so no caller-info parameter can follow it, and a null alternative is named by its index until CRL0004 (RSLN-04) catches it at build time. That is the forward-reference bug RSLN-04 calls the most common one.
  - (b) Fixed overloads plus one `params` overload, which lifts the cap and keeps the named messages.
  - (c) Keep the fixed overloads and add collection overloads to `OneOf` and `Longest`, as `Match` and `Switch` have.
- **`Match`.** Its per-case null guard is nearly unreachable, because `Case(...)` always returns a new object, so a collection-only `Match` loses almost nothing.
- **Error messages.** Collection-form errors name a case's index but not the key ADR-0006 promises. Fix that whichever option is chosen.
- **Records and docs.** The decision supersedes ADR-0006 and the CORE-02 convention. It updates whitepaper § Selective choice, § Properties and § Diagnostics, and docs/concepts/choice.md, as the record decides.
- From the [Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md), finding 21.

## Acceptance criteria

- [ ] An accepted decision record supersedes ADR-0006's arity rules.
- [ ] A `Longest` of 138 alternatives builds without nesting helpers, and the `Flat` helpers are gone; or the record says why nesting stays.
- [ ] A test shows a collection-form error naming the case's key, as ADR-0006 promises.

## Blocked by

- [CORE-05 Choice nodes](CORE-05-choice-nodes.md)
- [CORE-06 Longest](CORE-06-longest.md)

## Unblocks

- [RSLN-01 Roslyn grammar reader](../10-roslyn-analyzers/RSLN-01-roslyn-grammar-reader.md)
- [REL-04 Public API review and freeze](../14-release/REL-04-public-api-review-and-freeze.md)

## Whitepaper references

- [§ Selective choice](../../../whitepaper.md#selective-choice)
- [§ Properties](../../../whitepaper.md#properties)
- [§ Diagnostics](../../../whitepaper.md#diagnostics)

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
