# SPAN-05: Trivia from the gaps between spans

| | |
| --- | --- |
| Epic | [Source spans and provenance](../../README.md#span--source-spans-and-provenance) |
| Phase | Phase 1 — Layers |
| Status | Done |

## Summary

Let grammars read the source between items, so formatters and refactoring tools can reproduce the source exactly.

## Scope

Follows [ADR-0036](../../docs/decisions/0036-trivia-is-the-source-between-items-read-with-a-zero-width-parser.md).

- An item's leading trivia is the source between the end of the item before it and its own start. The end trivia runs from the end of the last item to where the input ends. Each gap belongs whole to the item after it. A trivia value is one `SourceSpan`, and nothing is stored per item.
- Each stream buffer records where its layer's input starts and ends in the source. The root sets them from its source, and each layer takes its input's.
- `LeadingTrivia`, a zero-width `Parser<TIn, SourceSpan>` on `Grammar<TIn>`, returns the leading trivia of the item at the cursor, or the end trivia at the end of input. It reads the spans of the items at and before the cursor through SPAN-04's span access.
- An item that starts before the previous one ends, or comes from another source, has zero-width leading trivia at its start. Synthetic items take the gap before them, as adjacent items.
- XML docs, and user docs in `docs/` for `LeadingTrivia`.
- Out: trailing trivia, typed trivia pieces, a `trivia:` parameter or `TriviaMode`, and a listing of items with their trivia on a pipeline run (ADR-0036 § Options).

## Acceptance criteria

- [x] A round-trip test reproduces the source exactly from tokens and trivia, including an input with no tokens.
- [x] Tests pin `LeadingTrivia` above a `Layer.Where` that drops tokens, and before a run of synthetic items.
- [x] Trivia costs nothing per item: `PackedSlot` stays 32 bytes.

## Blocked by

- [SPAN-02 Out-of-band span storage](../05-source-spans/SPAN-02-out-of-band-span-storage.md)
- [SPAN-04 Span combinators](../05-source-spans/SPAN-04-span-combinators.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Trivia](../../../whitepaper.md#trivia)
- [§ Using spans in grammars](../../../whitepaper.md#using-spans-in-grammars)

## Artifacts

- [ADR-0036](../../docs/decisions/0036-trivia-is-the-source-between-items-read-with-a-zero-width-parser.md): trivia is the source between items' spans, stored nowhere, all of it leading, and read with a zero-width `LeadingTrivia` parser (Accepted)

## Progress notes

- 2026-11-24 (Design): Started. § Trivia (optional) fixes the API shape but not the leading/trailing split rule, trivia granularity, where end-of-input trivia goes, how trivia passes through later layers, or how a grammar or tool reads it; settle these in a decision record before building.
- 2026-11-24 (Design): Drafted ADR-0036 after a prior-art pass (Roslyn, SwiftSyntax, rust-analyzer, TypeScript, Prettier) and the maintainer's choices on four forks: derive trivia from span gaps, leading only, one range per gap, read with a zero-width `LeadingTrivia` parser. Next: maintainer accepts, rejects or revises; on acceptance the card becomes blocked by SPAN-04.
- 2026-11-24 (Design): ADR-0036 accepted. Whitepaper § Trivia, this card's title, scope and criteria follow it. The card is now blocked by SPAN-04, which gives the cursor span access, so it returns to Not started with design done. Next: once SPAN-04 is Done, `/start SPAN-05` at Build.
- 2026-11-24 (Build): Started. SPAN-04 is done (branch rebased on feature/span-04). ADR-0034 and ADR-0035 checked against ADR-0036: no conflict. Plan: increment 1 gives every buffer the source bounds of its input through `IUpstream<T>`; increment 2 adds `LeadingTrivia`, its tests and docs.
- 2026-11-24 (Build): Increment 1 done. `IUpstream<T>`, `ICursorSource<T>` and `StreamBuffer<T>` have `InputStart` and `InputEnd`; the four layer upstreams take their input's, and test roots report their own. [InputBoundsTests.cs](../../../tests/Creole.Tests/InputBoundsTests.cs) (8 tests) covers Tokens, Where, Select, Identity, Transduce and Then over `"  ab  cd  "`, an empty input and an input that is only whitespace; full suite 1,729 passed. LAY-05 and SPAN-07 got scope notes for their roots. A branch that adds an `IUpstream<T>` (such as SPAN-06's decoding layer) must add both members when it merges. Next: increment 2, `LeadingTrivia`.
- 2026-11-24 (Build): Increment 2 done. `Grammar<TIn>.LeadingTrivia` (`LeadingTriviaNode`, `LeadingTriviaPlan`, `NodeKind.LeadingTrivia`, `Cursor<T>.LeadingTrivia()`), regular as `Empty` and ending the dispatch key walk, as `Position` does. [TriviaTests.cs](../../../tests/Creole.Tests/TriviaTests.cs) (8): a CsCheck round trip over random tokens, whitespace, line and block comments at segment sizes 1–5; an empty input and one of only trivia; comment tokens dropped by `Layer.Where`; a run of synthetic items and an `atEnd` output; overlapping derived outputs; input held in memory; the docs example. `LeadingTrivia` added to the node, regular, DFA and dispatch tests and the AOT smoke. Full suite 1,739 passed; AOT smoke publishes and runs. `PackedSlot` unchanged (`ABufferedItemsSpanTakesTwo32BitOffsetsAndItsSlot32Bytes` passes). Docs: [spans.md § Trivia](../../../docs/concepts/spans.md#trivia). ANA-01 and GEN-06 notes added. Next: all three criteria appear met; maintainer reviews, then `/complete`.
- 2026-11-24 (Build): Completed. All three criteria verified by running their tests; a mutation check (gaps forced to zero width) fails 6 of the 8 trivia tests, including the round-trip property.

## Outcome

Trivia is the source between items' spans, stored nowhere and read with a zero-width parser, as [ADR-0036](../../docs/decisions/0036-trivia-is-the-source-between-items-read-with-a-zero-width-parser.md) decided. That replaced the whitepaper's `TriviaMode.Attach`, with its stored leading and trailing ranges, and retitled this card.

- **Input bounds.** `IUpstream<T>`, `ICursorSource<T>` and `StreamBuffer<T>` have `InputStart` and `InputEnd`: zero-width spans where a layer's input starts and ends in the source ([IUpstream.cs](../../../src/Creole/IUpstream.cs), [ICursorSource.cs](../../../src/Creole/ICursorSource.cs), [StreamBuffer.cs](../../../src/Creole/StreamBuffer.cs)). The Tokens, Transduce, Select and Where upstreams take their input's; Identity and `Then` inherit them. A root reports its source's.
- **`LeadingTrivia`.** `Grammar<TIn>.LeadingTrivia`, a `Parser<TIn, SourceSpan>` in [GrammarOfTIn.Spans.cs](../../../src/Creole/GrammarOfTIn.Spans.cs), backed by `LeadingTriviaNode`, `LeadingTriviaPlan`, `NodeKind.LeadingTrivia` and `Cursor<T>.LeadingTrivia()` ([Cursor.cs](../../../src/Creole/Cursor.cs)).
  - It runs from the end of the item before the cursor, or `InputStart`, to the start of the next item, or `InputEnd`.
  - It is zero-width at the next item when that range would run backwards or cross sources, and over input held in memory.
  - Analysis treats it as `Position`: regular as `Empty`, and it ends the dispatch key walk.
- **Tests.**
  - [InputBoundsTests.cs](../../../tests/Creole.Tests/InputBoundsTests.cs) (8): bounds through every layer kind, an empty input, and one of only whitespace.
  - [TriviaTests.cs](../../../tests/Creole.Tests/TriviaTests.cs) (8): a CsCheck round trip over random tokens, whitespace, line and block comments at segment sizes 1–5; inputs with no tokens; `Layer.Where`; synthetic items and `atEnd`; overlapping derived outputs; input held in memory; the docs example.
  - `LeadingTrivia` also runs in the node, regular, DFA and dispatch tests and the AOT smoke. Full suite 1,739 passed.
- **Docs.** [spans.md § Trivia](../../../docs/concepts/spans.md#trivia) and the [docs index](../../../docs/README.md).
- **Implementation notes.**
  - The gap is also zero-width when the character offsets would run backwards while the root offsets do not. That can happen only over byte sources, and it keeps `SourceSpan`'s constructor from throwing.
  - The "another source" case in ADR-0036 point 2 is handled but untested: no stream can mix sources before SPAN-07.
- **Follow-ups, recorded on the cards.** [LAY-05](../06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md)'s roots report their bounds, and [SPAN-07](SPAN-07-embedded-languages.md)'s embedded root reports its mapped range. [GEN-06](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md) generates `LeadingTrivia` and [ANA-01](../09-static-analysis/ANA-01-grammar-property-analysis.md) gives it `Position`'s properties. A branch that adds an `IUpstream<T>`, such as SPAN-06's decoding layer, must add both members when it merges. No new cards.
