# FND-04: Test infrastructure

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

Shared testing tools that later epics rely on: property-based tests for semantics and engine agreement, snapshot tests for rendered diagnostics.

## Scope

- xUnit test projects.
- Pick and wire up one property-based testing library (CsCheck or FsCheck) and record the choice.
- Snapshot testing (for example Verify) for rendered diagnostics and traces.
- Helpers for building small throwaway grammars in tests.

## Acceptance criteria

- [ ] A sample property test and a sample snapshot test run in CI.

## Blocked by

- [FND-02 Repository and solution scaffolding](../01-foundations/FND-02-repository-and-solution-scaffolding.md)

## Unblocks

- [MODE-06 Model-based tests for mode rollback](../07-mode-channels/MODE-06-model-based-tests-for-mode-rollback.md)
- [GEN-08 Differential engine-agreement tests](../11-source-generator/GEN-08-differential-engine-agreement-tests.md)
- [TOOL-01 Creole.Testing assertions](../12-grammar-author-tooling/TOOL-01-creoletesting-assertions.md)
