# INT-08: Single-layer Parse API

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

Phase 0 on its own is a modern single-layer library, so parsers need a public entry point before pipelines exist.

## Scope

- `parser.Parse(input, options)` returning `ParseResult<T>`.
- `ParseOptions` type, shared later with pipelines.
- `ParseOptions.MaxDepth`, a positive `int` that defaults to 1,000 ([ADR-0020](../../docs/decisions/0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md)). `Parse` catches INT-07's depth abort and returns `Failed`. The depth diagnostic covers the item where the level would have started (zero-width at the end of input) and names the layer. Its `CustomMessage` names `MaxDepth` and its value, or the thread's stack, and it has no expectations.
- Build the cursor for `string` and `ReadOnlyMemory<char>` input over the caller's memory (`Cursor<T>.FromMemory`), so `Slice()` returns views of it ([ADR-0013](../../docs/decisions/0013-slices-view-in-memory-input-and-copy-streamed-input.md)).
- Add each public member that returns or exposes `ParseResult<T>` to `_coveredMembers` in `tests/Creole.Tests/ParseResultTests.cs`, with a test that it never returns `default(ParseResult<T>)` ([ADR-0002](../../docs/decisions/0002-parseresult-as-a-plain-union.md); ERR-01's Outcome).

## Acceptance criteria

- [ ] A character-level JSON parser can be parsed and its result switched on exhaustively.
- [ ] A parse that exceeds `MaxDepth` returns `Failed` with one depth diagnostic at the offending position.

## Blocked by

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [INT-07 Recursion depth limit](../03-interpreter/INT-07-recursion-depth-limit.md)
- [ERR-02 Furthest-failure tracking and labels](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)

## Unblocks

- [BENCH-01 Phase 0 speed gate](../13-benchmarks/BENCH-01-phase-0-speed-gate.md)

## Whitepaper references

- [§ Roadmap](../../../whitepaper.md#roadmap)
- [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input)
