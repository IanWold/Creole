# INT-07: Recursion depth limit

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Size | S |
| Status | Not started |

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
