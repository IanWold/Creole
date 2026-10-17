# INT-05: Interpreter lowering rewrites

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

Rewrites that make the interpreter competitive with Pidgin and Parlot.

## Scope

- Collapse chains of `Select` into one composed function.
- Lower `Or` over primitives with disjoint FIRST sets to a `switch` on token kind or character.
- Lower runs of a character class to a `SearchValues<char>` scan with `IndexOfAnyExcept`.
- Value-type results flow through generic plan nodes without boxing; skipped results use `Unit`.

## Acceptance criteria

- [ ] Benchmarks show each rewrite helps or the rewrite is dropped.

## Blocked by

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)

## Unblocks

- [ANA-06 Deterministic mode](../09-static-analysis/ANA-06-deterministic-mode.md)
- [BENCH-01 Phase 0 speed gate](../13-benchmarks/BENCH-01-phase-0-speed-gate.md)

## Whitepaper references

- [§ Interpreted engine](../../../whitepaper.md#interpreted-engine)
- [§ Design choices](../../../whitepaper.md#design-choices)
