# ERR-02: Furthest-failure tracking and labels

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

Report the furthest failure with merged expectations, without allocating until a failure is actually reported.

## Scope

- Per-layer furthest failure stored as a buffer index plus an interned expectation-set handle.
- Merge expected sets at equal positions; build `ParseError` lazily.
- `Named` replaces the expected set only when `p` fails without consuming.
- `Context` stack; `Describe(TKind)` for display.

## Acceptance criteria

- [ ] The whitepaper's rendered JSON error is reproduced exactly.
- [ ] Failure paths allocate nothing in benchmarks.

## Blocked by

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ERR-01 Error and result types](../04-errors-and-recovery/ERR-01-error-and-result-types.md)

## Unblocks

- [INT-08 Single-layer Parse API](../03-interpreter/INT-08-single-layer-parse-api.md)
- [ERR-05 Recover combinator and strategies](../04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md)
- [ERR-06 OrInsert for missing items](../04-errors-and-recovery/ERR-06-orinsert-for-missing-items.md)
- [LAY-06 Buffer size limit](../06-layers-and-pipelines/LAY-06-buffer-size-limit.md)
- [GEN-02 Emit the core combinators](../11-source-generator/GEN-02-emit-the-core-combinators.md)
- [TOOL-03 Explain](../12-grammar-author-tooling/TOOL-03-explain.md)
- [BENCH-01 Phase 0 speed gate](../13-benchmarks/BENCH-01-phase-0-speed-gate.md)

## Whitepaper references

- [§ The error model](../../../whitepaper.md#the-error-model)
- [§ Failure positions](../../../whitepaper.md#failure-positions)
