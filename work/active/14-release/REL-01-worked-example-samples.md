# REL-01: Worked-example samples

| | |
| --- | --- |
| Epic | [Samples, docs and 1.0 release](../../README.md#rel--samples-docs-and-10-release) |
| Phase | Phase 2 — Analysis |
| Size | M |
| Status | Not started |

## Summary

Examples A, B and C as runnable samples with tests.

## Scope

- JSON in two layers (deterministic).
- Indentation-sensitive language in three layers.
- Script language whose parser steers its lexer.

## Acceptance criteria

- [ ] All three samples build, run and pass their tests.

## Blocked by

- [CORE-11 Expression.Build](../02-core-combinators/CORE-11-expressionbuild.md)
- [INT-04 Execute Bind, Expression.Build and Custom](../03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md)
- [LAY-07 Three-layer demo and bounded-memory check](../06-layers-and-pipelines/LAY-07-three-layer-demo-and-bounded-memory-check.md)
- [MODE-03 Rollback rule](../07-mode-channels/MODE-03-rollback-rule.md)
- [ANA-06 Deterministic mode](../09-static-analysis/ANA-06-deterministic-mode.md)

## Unblocks

- [GEN-10 Measure the interpreter fallback rate](../11-source-generator/GEN-10-measure-the-interpreter-fallback-rate.md)

## Whitepaper references

- [§ Worked examples](../../../whitepaper.md#worked-examples)
