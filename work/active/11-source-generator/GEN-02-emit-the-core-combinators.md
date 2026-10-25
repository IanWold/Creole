# GEN-02: Emit the core combinators

| | |
| --- | --- |
| Epic | [Source-generated engine](../../README.md#gen--source-generated-engine) |
| Phase | Phase 3 — Staging |
| Status | Not started |

## Summary

Generated code for every core node, with semantics and error tracking identical to the interpreter.

## Scope

- `Seq` as straight-line code, `Or` as a `switch`, `Many` as a loop, `Rec` as direct (possibly mutually recursive) calls.
- Control nodes, `Switch`, `Match` and commit semantics. A dispatched `Or`, `Optional` or loop saves and restores the cut flag as the interpreter's light marks do, so dispatch is exact. Generated code does not fail early on any other node that can reach a `Cut` without consuming inside an `Atomic` ([ADR-0011](../../docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md)). Compare against CORE-08's reference model.
- Expectation-set tracking identical to ERR-02, as [ADR-0021](../../docs/decisions/0021-how-failures-are-recorded-and-merged.md) says. That includes the per-gap records for alternatives a dispatched `Or` skips, the restores in `Lookahead` and `Not`, and `Named`'s relabelling. Display strings come from `IKinded<TKind>.Describe` and `Detail`, called only when an error is built, and tokens show through them by the interpreter's rule, once a token grammar over the type is initialised ([ADR-0023](../../docs/decisions/0023-token-types-describe-their-kinds.md) point 3).

## Acceptance criteria

- [ ] Examples A to C produce identical results and diagnostics on both engines for a fixed test corpus.

## Blocked by

- [INT-03 Execute the core combinators](../../complete/03-interpreter/INT-03-execute-the-core-combinators.md)
- [ERR-02 Furthest-failure tracking and labels](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)
- [GEN-01 Generator skeleton and registration](../11-source-generator/GEN-01-generator-skeleton-and-registration.md)

## Unblocks

- [GEN-03 Lambda and helper inlining](../11-source-generator/GEN-03-lambda-and-helper-inlining.md)
- [GEN-04 Interpreter fallback and CRL0301](../11-source-generator/GEN-04-interpreter-fallback-and-crl0301.md)
- [GEN-05 Generated DFAs, scans and jump tables](../11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md)
- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)
- [GEN-07 Debugging generated code](../11-source-generator/GEN-07-debugging-generated-code.md)

## Whitepaper references

- [§ Source-generated engine](../../../whitepaper.md#source-generated-engine)
- [§ Equivalence](../../../whitepaper.md#equivalence)
