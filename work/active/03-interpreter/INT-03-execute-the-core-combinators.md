# INT-03: Execute the core combinators

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

The interpreter runs every analyzable node family with Parsec-style commit semantics. This is the reference semantics for Creole.

## Scope

- Four outcomes as `bool` plus cursor movement.
- `Or` and `OneOf` commit; `Atomic` rewinds; `Lookahead`, `Not`; `Cut` overrides an enclosing `Atomic`.
- `Longest` by running each alternative atomically (the DFA path is INT-06).
- Repetition, sequencing, `Switch` and `Match`.
- Run-time guard against nullable repetition until analysis exists.

## Acceptance criteria

- [ ] A semantic test suite covering the four-outcome table, including property tests, passes.
- [ ] The grammar classes in Worked Example A compile against the API, apart from `Layer.Tokens` (LAY-03). Moved here from CORE-02.

## Blocked by

- [CORE-03 Primitive nodes](../02-core-combinators/CORE-03-primitive-nodes.md)
- [CORE-04 Sequencing and mapping nodes](../02-core-combinators/CORE-04-sequencing-and-mapping-nodes.md)
- [CORE-05 Choice nodes](../02-core-combinators/CORE-05-choice-nodes.md)
- [CORE-06 Longest](../02-core-combinators/CORE-06-longest.md)
- [CORE-07 Repetition nodes](../02-core-combinators/CORE-07-repetition-nodes.md)
- [CORE-08 Control nodes](../02-core-combinators/CORE-08-control-nodes.md)
- [CORE-09 Recursion with Rec](../02-core-combinators/CORE-09-recursion-with-rec.md)
- [INT-02 Plan lowering and cache](../03-interpreter/INT-02-plan-lowering-and-cache.md)

## Unblocks

- [INT-04 Execute Bind, Expression.Build and Custom](../03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md)
- [INT-05 Interpreter lowering rewrites](../03-interpreter/INT-05-interpreter-lowering-rewrites.md)
- [INT-06 DFA compiler for regular subtrees](../03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md)
- [INT-07 Recursion depth limit](../03-interpreter/INT-07-recursion-depth-limit.md)
- [INT-08 Single-layer Parse API](../03-interpreter/INT-08-single-layer-parse-api.md)
- [ERR-02 Furthest-failure tracking and labels](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)
- [LAY-02 Marks and retention](../06-layers-and-pipelines/LAY-02-marks-and-retention.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)
- [GEN-02 Emit the core combinators](../11-source-generator/GEN-02-emit-the-core-combinators.md)
- [TOOL-02 Tracing](../12-grammar-author-tooling/TOOL-02-tracing.md)

## Whitepaper references

- [§ Combinator semantics](../../../whitepaper.md#combinator-semantics)
- [§ Results](../../../whitepaper.md#results)
- [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking)
