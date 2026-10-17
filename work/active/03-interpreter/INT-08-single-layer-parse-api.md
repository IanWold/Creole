# INT-08: Single-layer Parse API

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Size | S |
| Status | Not started |

## Summary

Phase 0 on its own is a modern single-layer library, so parsers need a public entry point before pipelines exist.

## Scope

- `parser.Parse(input, options)` returning `ParseResult<T>`.
- `ParseOptions` type, shared later with pipelines.

## Acceptance criteria

- [ ] A character-level JSON parser can be parsed and its result switched on exhaustively.

## Blocked by

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ERR-02 Furthest-failure tracking and labels](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)

## Unblocks

- [BENCH-01 Phase 0 speed gate](../13-benchmarks/BENCH-01-phase-0-speed-gate.md)

## Whitepaper references

- [§ Roadmap](../../../whitepaper.md#roadmap)
