# GEN-06: Generated layers, modes, recovery and streaming

| | |
| --- | --- |
| Epic | [Source-generated engine](../../README.md#gen--source-generated-engine) |
| Phase | Phase 3 — Staging |
| Size | XL |
| Status | Not started |

## Summary

Full feature parity: everything the interpreter supports must run in generated code.

## Scope

- Layers, layer state and marks over stream buffers.
- Mode channels, invalidation, rollback and cascades.
- `Recover`, `OrInsert`, lexer recovery, Starved propagation, depth limits.
- `Bind`, `Expression.Build` and `Custom` nodes.

## Acceptance criteria

- [ ] All interpreter test suites pass when run against the generated engine.

## Blocked by

- [INT-04 Execute Bind, Expression.Build and Custom](../03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md)
- [INT-07 Recursion depth limit](../03-interpreter/INT-07-recursion-depth-limit.md)
- [ERR-05 Recover combinator and strategies](../04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md)
- [ERR-06 OrInsert for missing items](../04-errors-and-recovery/ERR-06-orinsert-for-missing-items.md)
- [ERR-07 Lexer recovery and cascade suppression](../04-errors-and-recovery/ERR-07-lexer-recovery-and-cascade-suppression.md)
- [LAY-04 Layer-local state](../06-layers-and-pipelines/LAY-04-layer-local-state.md)
- [MODE-04 Multi-layer invalidation cascade](../07-mode-channels/MODE-04-multi-layer-invalidation-cascade.md)
- [STRM-01 Starved outcome](../08-streaming/STRM-01-starved-outcome.md)
- [GEN-02 Emit the core combinators](../11-source-generator/GEN-02-emit-the-core-combinators.md)

## Unblocks

- [GEN-08 Differential engine-agreement tests](../11-source-generator/GEN-08-differential-engine-agreement-tests.md)
- [GEN-09 Native AOT and trimming validation](../11-source-generator/GEN-09-native-aot-and-trimming-validation.md)
- [BENCH-02 Full benchmark suite](../13-benchmarks/BENCH-02-full-benchmark-suite.md)
- [REL-04 Public API review and freeze](../14-release/REL-04-public-api-review-and-freeze.md)
- [RES-02 Resumable generated parsers](../17-resumable-async/RES-02-resumable-generated-parsers.md)

## Whitepaper references

- [§ Execution engines](../../../whitepaper.md#execution-engines)
