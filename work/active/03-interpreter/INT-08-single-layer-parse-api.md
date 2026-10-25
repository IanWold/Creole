# INT-08: Single-layer Parse API

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

Phase 0 on its own is a modern single-layer library, so parsers need a public entry point before pipelines exist.

## Scope

- `parser.Parse(input, source, options)` returning `ParseResult<T>`. `source` is an optional name, and spans use `SourceId.Unnamed` ([ADR-0024](../../docs/decisions/0024-a-parses-diagnostics-hold-their-source-text.md)).
- `ParseOptions` type, shared later with pipelines.
- Attach a `FailureLog<T>` to the cursor and build the reported error from it with `ToError`, naming the layer and mapping positions to spans; ERR-02 built both. Reuse the log between parses, so that a parse allocates nothing for it.
- A `Recovered` or `Failed` result's `Diagnostics` holds a `SourceText` for the input, without copying it, so `Render()` shows the block in [§ The error model](../../../whitepaper.md#the-error-model) ([ADR-0024](../../docs/decisions/0024-a-parses-diagnostics-hold-their-source-text.md) points 1 to 4). `SourceText` gains a constructor over `ReadOnlyMemory<char>`. `Render()` writes one line per error when the collection lacks a source, and equality ignores held sources. Update [docs/concepts/errors.md](../../../docs/concepts/errors.md#one-line-per-error).
- `ParseOptions.MaxDepth`, a positive `int` that defaults to 1,000 ([ADR-0020](../../docs/decisions/0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md)). `Parse` catches INT-07's depth abort and returns `Failed`. The depth diagnostic covers the item where the level would have started (zero-width at the end of input) and names the layer. Its `CustomMessage` names `MaxDepth` and its value, or the thread's stack, and it has no expectations.
- Build the cursor for `string` and `ReadOnlyMemory<char>` input over the caller's memory (`Cursor<T>.FromMemory`), so `Slice()` returns views of it ([ADR-0013](../../docs/decisions/0013-slices-view-in-memory-input-and-copy-streamed-input.md)).
- Add each public member that returns or exposes `ParseResult<T>` to `_coveredMembers` in `tests/Creole.Tests/ParseResultTests.cs`, with a test that it never returns `default(ParseResult<T>)` ([ADR-0002](../../docs/decisions/0002-parseresult-as-a-plain-union.md); ERR-01's Outcome).

## Acceptance criteria

- [ ] A character-level JSON parser can be parsed and its result switched on exhaustively.
- [ ] A parse that exceeds `MaxDepth` returns `Failed` with one depth diagnostic at the offending position.
- [ ] A failed parse's `Diagnostics.Render()` shows the block in [§ The error model](../../../whitepaper.md#the-error-model), naming the source given as `source:`, and copies nothing from `ReadOnlyMemory<char>` input.

## Blocked by

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [INT-07 Recursion depth limit](../03-interpreter/INT-07-recursion-depth-limit.md)
- [ERR-02 Furthest-failure tracking and labels](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)

## Unblocks

- [BENCH-01 Phase 0 speed gate](../13-benchmarks/BENCH-01-phase-0-speed-gate.md)

## Whitepaper references

- [§ Roadmap](../../../whitepaper.md#roadmap)
- [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input)
- [§ Results](../../../whitepaper.md#results-1)

## Artifacts

- [ADR-0024](../../docs/decisions/0024-a-parses-diagnostics-hold-their-source-text.md) A parse's diagnostics hold the source text they point into, and `Parse` names its source with a `source:` parameter (Accepted)

## Progress notes

- 2026-10-25 (Design): Started. Blockers INT-03, INT-07 and ERR-02 are Done. Design first: the scope asks to settle how a parse's errors render against their source, which changes either the result's shape or § A. JSON in two layers, so it needs a decision record. The rest (`ParseOptions`, the depth diagnostic, cursors over the caller's memory) is pinned by ADR-0020 and ADR-0013 and goes to the Build plan.
- 2026-10-25 (Design): Drafted ADR-0024 (Proposed). The maintainer chose 1A, so the result holds its source and `Render()` shows the block, and 2A, so `Parse(input, source, options)` takes the name as a parameter and spans use `SourceId.Unnamed`. Prior art: megaparsec and Roslyn reach the source from the error; ariadne takes it from the caller. Next: the maintainer accepts or revises the record; on acceptance, edit the whitepaper's § Results and § Streaming and async input, settle the scope lines in INT-08, LAY-05 and STRM-03, then move to Build.
- 2026-10-25 (Build): ADR-0024 accepted. Whitepaper § Results and § Streaming and async input updated. Scope gains the held source and the `source` parameter, and a third criterion covers rendering. LAY-05 and STRM-03 scope lines settled. Next: `/build` plans the increments.
