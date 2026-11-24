# SPAN-04: Span combinators

| | |
| --- | --- |
| Epic | [Source spans and provenance](../../README.md#span--source-spans-and-provenance) |
| Phase | Phase 1 — Layers |
| Status | Building |

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

- [ ] The `>>` split-and-merge example works with `Atomic(Adjacent(Kind(Gt), Kind(Gt)))` over a lexer layer: `a >> b` parses as a shift, and `a > b` as a comparison.
- [ ] `Spanned` and `Adjacent` give the same results with a segment size of 1, where their input is released while they run, as with segments that release nothing, with and without a failure log.

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
