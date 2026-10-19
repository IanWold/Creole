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
- `Named` replaces the expectations `p` recorded at its start position whenever `p` consumes nothing, on failure or success, and adds none if there were none ([ADR-0008](../../docs/decisions/0008-control-node-semantics.md) point 4).
- `Context` stack. Decide whether an engine appends the start position to a context line: the whitepaper's rendered example shows "in object starting at 2:1" from `Context("in object")`, but `ParseError.Context` holds finished strings.
- Decide (`/decide`), so both engines record the same expected sets (G7): whether expectations recorded inside `Not` and inside a successful `Lookahead` reach the furthest failure (Pidgin's `Not` and Megaparsec's `notFollowedBy` keep them out), what `Not` records when its parser succeeds, which context stack a merged failure keeps when failures at one position carry different stacks, and how `Named` treats an expectation that was already recorded at its start position before it ran. Also what an attempt that dispatch skips contributes: running it would have recorded its expectations at the current position, and FIRST sets over-approximate, so dispatch must still record the same expected set as running in order (G7; [ADR-0011](../../docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md) point 6). Raised by the CORE-08 review.
- `Describe(TKind)` on `TokenGrammar` for display (moved from CORE-02). Run `/decide` first on how the renderer reaches it: grammar classes hold static fields and are never instantiated, so an instance `virtual` has no caller, and a class cannot declare static virtual members. Options include passing a grammar instance to the pipeline, a self-typed `TokenGrammar<TSelf, TToken, TKind>`, or a static abstract interface member on the token or kind type.

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
