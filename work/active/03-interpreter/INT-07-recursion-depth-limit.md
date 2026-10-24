# INT-07: Recursion depth limit

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Designing |

## Summary

Untrusted input must never cause a stack overflow.

## Scope

- `ParseOptions.MaxDepth` bounds recursion depth.
- Exceeding it produces an ordinary diagnostic at the offending span.

## Acceptance criteria

- [ ] A deeply nested input returns a `Failed` result instead of crashing the process.

## Blocked by

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ERR-01 Error and result types](../04-errors-and-recovery/ERR-01-error-and-result-types.md)

## Unblocks

- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)

## Whitepaper references

- [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input)

## Artifacts

- [ADR-0020](../../docs/decisions/0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md) Depth counts each `Rec`, `Bind` continuation and recursive operand, and exceeding it or the stack ends the parse (Proposed)

## Progress notes

- 2026-10-24 (Designing): Started. The whitepaper fixes the feature but not what counts as a level, how exceeding the limit stops the parse, the default, whether a stack probe backs the limit, or how the card divides work with INT-08, which owns `ParseOptions` and the `Parse` API that returns `Failed`. Parlot (the stated model) counts `Deferred` and `Unary` entries, is off by default and throws `ParseException`. Next: a decision record via `/decide`.
- 2026-10-24 (Designing): Drafted ADR-0020 (Proposed). It is based on measurements of stack use per level, probe cost and unwinding cost on the current interpreter, plus prior art (Parlot, Roslyn `StackGuard`, System.Text.Json, Pidgin, Superpower). The maintainer chose a default of 1,000, and that INT-07 builds the mechanism while INT-08 exposes `ParseOptions.MaxDepth` and returns `Failed`. Next: the maintainer accepts, rejects or revises ADR-0020; on acceptance, update the whitepaper and the INT-07, INT-08, BENCH-01, ERR-05 and GEN-06 cards, then move to Building.
