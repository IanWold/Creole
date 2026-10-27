# GEN-06: Generated layers, modes, recovery and streaming

| | |
| --- | --- |
| Epic | [Source-generated engine](../../README.md#gen--source-generated-engine) |
| Phase | Phase 3 — Staging |
| Status | Not started |

## Summary

Full feature parity: everything the interpreter supports must run in generated code.

## Scope

- Layers, layer state and marks over stream buffers.
- Mode channels, invalidation, rollback and cascades.
- `Recover`, `OrInsert`, lexer recovery, Starved propagation, depth limits.
- Count depth and probe the stack at the same points as the interpreter, sharing the cursor's counter with interpreted subtrees ([ADR-0020](../../docs/decisions/0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md)).
- `Bind`, the filter behind `Where` ([ADR-0014](../../docs/decisions/0014-a-rejected-filter-reports-at-the-start-of-the-value.md)), `Expression.Build` ([ADR-0015](../../docs/decisions/0015-a-prefix-operand-takes-only-tighter-levels.md)) and `Custom` nodes.
- Register the start of each open filter, chained operator and custom scanner with the failure log, as the interpreter does, so the buffer can copy what an error needs before it releases a segment ([ADR-0028](../../docs/decisions/0028-an-errors-items-are-copied-when-the-buffer-releases-them.md)).
- Pin as the interpreter does: `Atomic`, `Lookahead`, `Not`, `Longest`, `Slice`, `Text`, `Custom` and a longer `String` or `CIString`, and each producing layer's running step. Generated code may skip node pins inside a producing layer's step and over input held whole in memory ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) points 1, 2 and 4).
- Lower failures at mode changes, undos and restores, keep the rewind order, run a `Longest` winner again when needed, and clear and give back failures for `Recover` and `OrInsert`, as the interpreter does ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) points 2, 3, 6 and 8).

## Acceptance criteria

- [ ] All interpreter test suites pass when run against the generated engine, with each suite's grammars emitted as source in `partial` grammar classes and staged with zero CRL0301. A suite that builds its grammars at run time passes through interpreter fallback and does not count ([Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) finding 13).

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

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
