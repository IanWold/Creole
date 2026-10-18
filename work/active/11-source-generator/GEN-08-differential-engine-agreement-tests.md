# GEN-08: Differential engine-agreement tests

| | |
| --- | --- |
| Epic | [Source-generated engine](../../README.md#gen--source-generated-engine) |
| Phase | Phase 3 — Staging |
| Status | Not started |
| Gate | Phase 3 gate |

## Summary

The Phase 3 release gate: both engines agree on value, consumed position, every diagnostic span and every expected set.

## Scope

- Random grammar generator plus input generator (valid inputs and near misses, reusing TOOL-04).
- Generated-engine compilation of random grammars inside tests.
- Run in CI as a release gate, not a nice-to-have: 10,000 fixed-seed grammar and input pairs on every run, plus a nightly fresh-seed run that files issues without blocking.

## Acceptance criteria

- [ ] Zero mismatches over 10,000 fixed-seed random grammar and input pairs on every CI run ([ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md)).

## Blocked by

- [FND-04 Test infrastructure](../01-foundations/FND-04-test-infrastructure.md)
- [GEN-03 Lambda and helper inlining](../11-source-generator/GEN-03-lambda-and-helper-inlining.md)
- [GEN-04 Interpreter fallback and CRL0301](../11-source-generator/GEN-04-interpreter-fallback-and-crl0301.md)
- [GEN-05 Generated DFAs, scans and jump tables](../11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md)
- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)
- [TOOL-04 Grammar fuzzing](../12-grammar-author-tooling/TOOL-04-grammar-fuzzing.md)

## Unblocks

- [TOOL-06 EngineAgreement.Check](../12-grammar-author-tooling/TOOL-06-engineagreementcheck.md)
- [REL-05 Ship 1.0](../14-release/REL-05-ship-10.md)
- [INC-01 ParseSession and retained buffers](../15-incremental-reparsing/INC-01-parsesession-and-retained-buffers.md)
- [FUS-05 Fusion gate](../16-layer-fusion/FUS-05-fusion-gate.md)

## Whitepaper references

- [§ Equivalence](../../../whitepaper.md#equivalence)
- [§ Risks](../../../whitepaper.md#risks)
