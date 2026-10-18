# FND-04: Test infrastructure

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

Shared testing tools that later epics rely on: property-based and model-based tests for semantics and engine agreement.

## Scope

- xUnit test projects.
- Pick and wire up one property-based testing library (CsCheck or FsCheck) and record the choice.
- Out: snapshot testing. Dropped by the maintainer on 2026-10-17; see the progress notes.
- Out: helpers for building small throwaway grammars. They need the grammar model, so they moved to [CORE-01](../02-core-combinators/CORE-01-grammar-node-model.md).

## Acceptance criteria

- [x] A sample property test runs in CI.

## Blocked by

- [FND-02 Repository and solution scaffolding](../01-foundations/FND-02-repository-and-solution-scaffolding.md)

## Unblocks

- [MODE-06 Model-based tests for mode rollback](../07-mode-channels/MODE-06-model-based-tests-for-mode-rollback.md)
- [GEN-08 Differential engine-agreement tests](../11-source-generator/GEN-08-differential-engine-agreement-tests.md)
- [TOOL-01 Creole.Testing assertions](../12-grammar-author-tooling/TOOL-01-creoletesting-assertions.md)

## Progress notes

- 2026-10-17 (Building): Chose CsCheck over FsCheck: it is written in C# for C#, has no FSharp.Core dependency, and has built-in model-based (`SampleModelBased`) and parallel testing, which MODE-06 and GEN-08 need. Added CsCheck 4.9.1 to every test project through `tests/Directory.Build.props`, and two reference samples in `tests/Creole.Tests/Samples/PropertyTestSamples.cs`: a plain property (UTF-8 length is the sum of rune lengths) and a model-based one (`Stack<T>` against a list model). Breaking the model on purpose produced a shrunk failing sequence and a replay seed. Documented CsCheck and seed replay in CONTRIBUTING.md § Code.
- 2026-10-17 (Building): Snapshot testing dropped, with the maintainer's agreement. Verify 33 replaced its MIT license with an Open Source Maintenance Fee EULA, and its build fails (SC021) until the consuming repository declares a sponsorship, license or exemption. Snapshot tests give little value before ERR-03 renders diagnostics. TOOL-01 still plans snapshot tests of rendered diagnostics and must choose an approach that does not pass Verify's terms on to grammar authors. The throwaway-grammar helpers moved to CORE-01, because nothing can build a grammar before the node model exists. Next: `/complete FND-04`.

## Outcome

- CsCheck 4.9.1 is referenced by every test project through `tests/Directory.Build.props`. Reference samples are in `tests/Creole.Tests/Samples/PropertyTestSamples.cs`: one plain property and one model-based. Usage and seed replay are documented in [CONTRIBUTING.md § Code](../../../CONTRIBUTING.md#code).
- Evidence: CI run 39128253968 (Build, PR #3, commit 7286fef) passed on Ubuntu, macOS and Windows. In that run Creole.Tests ran 4 tests: the 2 scaffolding cases and the 2 samples (names matched by local test discovery). The critic confirmed this independently.
- Deviations from the original card, agreed with the maintainer and recorded in the progress notes:
  - Snapshot testing was dropped because of Verify 33's licence terms; TOOL-01 records the constraint.
  - The throwaway-grammar helpers moved to CORE-01.
- No public API changed, so `docs/` is unchanged.

