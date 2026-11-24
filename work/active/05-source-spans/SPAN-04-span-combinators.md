# SPAN-04: Span combinators

| | |
| --- | --- |
| Epic | [Source spans and provenance](../../README.md#span--source-spans-and-provenance) |
| Phase | Phase 1 — Layers |
| Status | Done |

## Summary

Let grammars capture and test locations.

## Scope

- `p.Spanned()`, producing `Spanned<T>(Value, Span)`, and `p.WithSpan((value, span) => node)` as a node of its own ([ADR-0035](../../docs/decisions/0035-span-combinators-commit-like-sequences-and-hold-their-spans.md) points 1 and 2).
- `Adjacent(p, q)`, a sequence that checks after `q` that the spans touch and otherwise fails as a rejected filter over the pair, committed like `Seq` (point 3).
- `Position`, a zero-width parser returning a zero-width `SourceSpan` at the start of the next item (point 4).
- Cursors read spans from their source: item offsets in `SourceId.Unnamed` over in-memory input, and source spans over a stream buffer (point 5).
- A register of span holds that a layer keeps whether or not it has a failure log, which the buffer copies from before it releases a segment (point 6).
- The `Longest` automaton's regular predicate: `Spanned`, `WithSpan` and `Position` are regular when their parts are, and `Adjacent` is not (point 7).
- User docs for the four combinators.
- Out: a message parameter on `Adjacent`, and tokens with spans for `Parse` over a token array.

## Acceptance criteria

- [x] The `>>` split-and-merge example works with `Atomic(Adjacent(Kind(Gt), Kind(Gt)))` over a lexer layer: `a >> b` parses as a shift, and `a > b` as a comparison.
- [x] `Spanned` and `Adjacent` give the same results with a segment size of 1, where their input is released while they run, as with segments that release nothing, with and without a failure log.

## Blocked by

- [SPAN-02 Out-of-band span storage](../05-source-spans/SPAN-02-out-of-band-span-storage.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)

## Unblocks

- [INC-05 Session span table](../15-incremental-reparsing/INC-05-session-span-table.md)

## Whitepaper references

- [§ Using spans in grammars](../../../whitepaper.md#using-spans-in-grammars)
- [§ 1. Split and merge](../../../whitepaper.md#1-split-and-merge)

## Artifacts

- [ADR-0035: `Adjacent` commits like a sequence, `Position` is a zero-width span, and span combinators hold their spans without the failure log](../../docs/decisions/0035-span-combinators-commit-like-sequences-and-hold-their-spans.md)

## Progress notes

- 2026-11-24 (Design): Started. The whitepaper names `Spanned`, `WithSpan`, `Adjacent` and `Position` but leaves their failure behaviour, result units and span retention open, and ADR-0031 says `Spanned` holds its start under ADR-0028, whose copy-at-release lives in the failure log that a run may not have. Decisions to make: `Adjacent`'s failure (committed like `Seq` and `Where`, or atomic like a literal), where it is reported and whether the check runs before or after `q`; `Position`'s result (a source offset in root units, the layer's item index, or a zero-width `SourceSpan`); `Spanned<T>`'s shape and the span of a parser that consumed nothing; how `Spanned` and `Adjacent` keep the spans they need behind the cursor over a streamed layer with no failure log; how a cursor reads spans over in-memory and buffered input. Next: `/decide`.
- 2026-11-24 (Design): Drafted ADR-0035 (Proposed). The maintainer chose `Adjacent` committing like `Seq` and `Where`, so the `>>` example becomes `Atomic(Adjacent(Kind(Gt), Kind(Gt)))`, and `Position` as a zero-width `SourceSpan` at the start of the next item. The record also proposes: `Spanned<T>` as `(Value, Span)` with an empty `p` zero-width where `Position` would be; `WithSpan` as its own node for INC-05; `Adjacent` checking after `q` and failing as a rejected filter over the pair; span holds in a register of their own, copied at release whether or not the layer has a failure log, refining ADR-0028 point 2; in-memory spans as item offsets, so `Adjacent` always passes over a token array. Next: the maintainer accepts, rejects or revises ADR-0035; on acceptance, update the whitepaper (§ Using spans in grammars, § 1. Split and merge, § Functions in grammars, § Bookmarks and retention, § Failure positions) and SPAN-04, INC-05, ANA-01 and GEN-06, then move to Build.
- 2026-11-24 (Build): ADR-0035 accepted as written. Whitepaper updated (§ Using spans in grammars, § 1. Split and merge, § Functions in grammars, § Bookmarks and retention, § Failure positions, § Properties). Scope now follows the record's points 1 to 7. Criterion 1 names `Atomic(Adjacent(…))` and both `a >> b` and `a > b`, and criterion 2 covers spans read after release, with and without a failure log. INC-05, ANA-01 and GEN-06 gained scope lines. The criterion tests can run before LAY-05 through the internal `LayerRun` runner. Next: `/build SPAN-04`.
- 2026-11-24 (Build): Plan approved: three increments (the cursor's spans and the span-hold register; `Spanned`, `WithSpan` and `Position`; `Adjacent`). Calls: the register lives in the `StreamBuffer` the layer reads; `Adjacent`'s message names both parsers by the rule chained operators use (`'>' must come right after '>', with nothing between them`, or `these items must touch, with nothing between them`), built once per plan when a rejection is first recorded; holds take no `try/finally`, as `HoldStart` does; empty streamed input gives `Position` `(Unnamed, 0, 0)` until LAY-05; no public span API on `Cursor<T>`. Increment 1 done: `ICursorSource<T>` gains `SpanOf`, `HoldSpan` and `DropSpan`. `StreamBuffer<T>` keeps a stack of held positions, copies the span at each one in `Release` whether or not a failure log is passed, and `SpanOf` starts from that copy for a released item. `Cursor<T>` gains `SpanSince(start)` (item offsets in `SourceId.Unnamed` over memory, the source's spans otherwise, peeking for an empty range), `HoldSpan()` and `DropSpan()`. The two test cursor sources implement the new members. Tests (6): a held span read after release, with and without a failure log; a dropped hold keeps nothing; nested holds; `SpanSince` zero-width at the next item, at the end and in an empty buffer; `SpanSince` over memory; holding a span at every item allocates nothing per item. Mutations each failed tests: no copy at release (3), no peek for an empty range (1), a lookup that ignores position (3), a drop that does not pop (1). No `docs/` change: the register is internal. `dotnet build` 0 warnings; `dotnet test` 1,670 passed; AOT smoke publishes and prints OK. Next: increment 2, `Spanned`, `WithSpan` and `Position`.
- 2026-11-24 (Build): Increment 2 done. Public: `Spanned<T>(Value, Span)`; `SpanCapture` with `Spanned()` and `WithSpan(selector)` as extension members (an instance `Spanned()` would trip IL3054); `Grammar<TIn>.Position`, one shared instance. Nodes `SpannedNode`, `WithSpanNode` and `PositionNode` with new `NodeKind`s; `Spanned` and `WithSpan` lower to one `SpanPlan`, which holds its start, reads `SpanSince(start)` and drops the hold; `PositionPlan` reads `SpanSince` at the cursor. `Regular` passes `Spanned` and `WithSpan` through and treats `Position` as the empty match; dispatch passes `Spanned` and `WithSpan` through and stops at `Position`. Tests: `SpanNodeTests` (4) and `SpanExecutionTests` (12 tests, 18 cases): in-memory spans, an empty `p`, failures and errors unchanged, `WithSpan`, `Position` at the start, middle and end and on empty input, token arrays, and over a lexer layer (skipped input left out, `Position` after it, no tokens). Criterion 2's `Spanned` half: segment size 1 against 1024, with and without a failure log, and the buffer shown to have released. Also the docs sample, a `Spanned` at every item allocating nothing per item (one measured run, since a doubling leak evades the fewest-of-several measure), and the new combinators in the regular-cases table and the dispatch and DFA agreement properties. AOT smoke parses a grammar using all three. Mutations each failed tests: span read from the cursor instead of the start (8), no hold (2), no drop after success (1, the allocation test), `Position` as an item index (1), `Spanned`/`WithSpan` not regular (2), `Position` not regular (2). Docs: spans.md gains § Spans in grammars; docs index updated. Notes added to LAY-05 (clear holds in a reused buffer; empty-input `Position`'s source) and STRM-01 (a restart clears span holds). `dotnet build` 0 warnings; `dotnet test` 1,694 passed; AOT smoke publishes and prints OK. Next: increment 3, `Adjacent`.
- 2026-11-24 (Build): Increment 3 done. `Grammar<TIn>.Adjacent(first, second)`, `AdjacentNode` and `NodeKind.Adjacent`; `AdjacentPlan` mirrors `FilterPlan`'s failure handling: it holds `p`'s start in the failure log, reads the end of `p`'s span from the item before the cursor as soon as `p` succeeds, holds `q`'s start in the span register, and checks after `q`. A `p` that consumed nothing touches. A mismatch restores the failures saved at the start and records a rejection at `p`'s start; the message names both parsers by the dispatch rule (`PlanBuilder.NameOf`, built on `Keyed`), is cached in the plan, and is built only in a run that records failures. Not regular; dispatch passes through to `p`. Tests: `AdjacentTests` (15 tests, 20 cases) and one node test. Criterion 1 over a `Layer.Tokens` lexer of single `<` and `>` tokens: `a >> b` gives `(a >> b)`, `a > b` gives `(a > b)`, `a >> b > c` gives `((a >> b) > c)`, `a > > b` fails at the second `>`, `List<List<int>>` parses, and without `Atomic` `a > b` fails committed. Criterion 2's `Adjacent` half: a long `q` and a long `p`, touching and not, at segment sizes 1 and 1024, with and without a failure log. Also pinned messages (named, names in order, unnamed), the rejection's error at both segment sizes, an empty `p`, in-memory text and tokens always touching, and failures as `Seq`'s. `Adjacent` joins the regular-cases table and the dispatch agreement property; the AOT smoke parses with it. Mutations each failed tests: `p`'s first span instead of its last (2), no hold on `q`'s start (3), no empty-`p` rule (1), failures not restored before the rejection (2), no failure-log hold on `p`'s start (1), names swapped (1), touching off by one (8), `Adjacent` regular (1). Two cannot fail yet: checking before `q` differs only under a mode change (MODE-01), and ignoring the source id needs more than one source. Docs: spans.md gains the `Adjacent` row and § Joining split tokens, whose sample is the tested `Expr` rule; docs index updated. `dotnet build` 0 warnings; `dotnet test` 1,716 passed; AOT smoke publishes and prints OK. Both criteria appear met; neither ticked until `/complete`. Next: maintainer review, then `/complete SPAN-04`.
- 2026-11-24 (Done): `/complete` on 8d9a1d2. Both criteria met on evidence: criterion 1's six tests and criterion 2's eight cases pass run alone; `dotnet build` 0 warnings; `dotnet test` 1,716 passed; AOT smoke prints OK.

## Outcome

- **Decision.** [ADR-0035](../../docs/decisions/0035-span-combinators-commit-like-sequences-and-hold-their-spans.md) settled what the whitepaper left open:
  - `Adjacent` commits like `Seq`, checks after `q`, and fails as a rejected filter over the pair.
  - `Position` returns a zero-width `SourceSpan`.
  - `Spanned<T>` is `(Value, Span)`, and `WithSpan` is a node of its own.
  - Span holds are kept apart from the failure log, refining ADR-0028 point 2.

  The whitepaper changed in six sections, and INC-05, ANA-01 and GEN-06 gained scope lines.
- **Public API**:
  - [Spanned.cs](../../../src/Creole/Spanned.cs): `Spanned<T>`.
  - [SpanCapture.cs](../../../src/Creole/SpanCapture.cs): `Spanned()` and `WithSpan(selector)` as extension members. An instance `Spanned()` would trip IL3054.
  - [GrammarOfTIn.Spans.cs](../../../src/Creole/GrammarOfTIn.Spans.cs): `Position`, one shared instance, and `Adjacent(first, second)`.
- **Engine**:
  - **Cursor and buffer.** `ICursorSource<T>` gains `SpanOf`, `HoldSpan` and `DropSpan`. [StreamBuffer.cs](../../../src/Creole/StreamBuffer.cs) keeps a stack of held positions, copies the span at each one in `Release` whether or not a failure log is passed, and starts a span from that copy. [Cursor.cs](../../../src/Creole/Cursor.cs) gains `SpanSince`, which gives item offsets over memory and peeks for an empty range.
  - **Nodes.** `SpannedNode`, `WithSpanNode`, `PositionNode` and `AdjacentNode`, with four new `NodeKind`s.
  - **Plans.** [SpanPlan.cs](../../../src/Creole/Plans/SpanPlan.cs), shared by `Spanned` and `WithSpan`; [PositionPlan.cs](../../../src/Creole/Plans/PositionPlan.cs); [AdjacentPlan.cs](../../../src/Creole/Plans/AdjacentPlan.cs), which handles failures as `FilterPlan` does.
  - **Analysis.** `Regular` passes `Spanned` and `WithSpan` through, treats `Position` as the empty match, and treats `Adjacent` as not regular. Dispatch passes `Spanned`, `WithSpan` and `Adjacent` through, and stops at `Position`.
- **Calls made in the build plan**, none of which changes the design:
  - The span register lives in the `StreamBuffer` the layer reads.
  - `Adjacent`'s message names each parser as a choice names an alternative it skips: `'>' must come right after '>', with nothing between them`. It falls back to `these items must touch, with nothing between them` when either parser has no name. It is cached in the plan and built only in a run that records failures.
  - Holds take no `try/finally`, as `HoldStart` takes none.
  - `Position` over empty streamed input gives `(Unnamed, 0, 0)`.
- **Tests** (53 cases), with a mutation sweep in each increment: every mutation failed at least one test.
  - `StreamBufferTests` (+7) and `CursorTests` (+1).
  - [SpanNodeTests.cs](../../../tests/Creole.Tests/SpanNodeTests.cs) (5).
  - [SpanExecutionTests.cs](../../../tests/Creole.Tests/SpanExecutionTests.cs) (18 cases).
  - [AdjacentTests.cs](../../../tests/Creole.Tests/AdjacentTests.cs) (20 cases). Criterion 1 runs over a `Layer.Tokens` lexer through `LayerRun.Run`, and criterion 2 at segment sizes 1 and 1024, with and without a failure log.
  - Rows in the regular-cases table, the dispatch and DFA agreement properties, and the AOT smoke.

  Two mutations cannot fail yet: checking `Adjacent` before `q` (it differs only under a mode change, MODE-01) and ignoring the source id (it needs a second source).
- **Docs.** [spans.md](../../../docs/concepts/spans.md) gains § Spans in grammars and § Joining split tokens, whose samples are compiled and run by the tests. The [docs index](../../../docs/README.md) is updated.
- **Follow-ups**, recorded on the cards:
  - LAY-05: clear span holds in a buffer that a second run reuses, and give empty-input `Position` a root source if pipelines get one.
  - STRM-01: a restart clears span holds.
  - INC-05: `WithSpan` is its own node.
  - ANA-01: the span combinators' properties.
  - GEN-06: generated code holds span positions and reports `Adjacent`'s rejection.
