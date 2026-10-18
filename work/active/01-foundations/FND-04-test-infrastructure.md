# FND-04: Test infrastructure

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

Shared testing tools that later epics rely on: property-based and model-based tests for semantics and engine agreement.

## Scope

- xUnit test projects.
- Pick and wire up one property-based testing library (CsCheck or FsCheck) and record the choice.
- Out: snapshot testing. Dropped by the maintainer on 2026-10-17; see the progress notes.
- Out: helpers for building small throwaway grammars. They need the grammar model, so they moved to [CORE-01](../02-core-combinators/CORE-01-grammar-node-model.md).

## Acceptance criteria

- [ ] A sample property test runs in CI.

## Blocked by

- [FND-02 Repository and solution scaffolding](../01-foundations/FND-02-repository-and-solution-scaffolding.md)

## Unblocks

- [MODE-06 Model-based tests for mode rollback](../07-mode-channels/MODE-06-model-based-tests-for-mode-rollback.md)
- [GEN-08 Differential engine-agreement tests](../11-source-generator/GEN-08-differential-engine-agreement-tests.md)
- [TOOL-01 Creole.Testing assertions](../12-grammar-author-tooling/TOOL-01-creoletesting-assertions.md)

## Progress notes

- 2026-10-17 (Building): Chose CsCheck over FsCheck: it is written in C# for C#, has no FSharp.Core dependency, and has built-in model-based (`SampleModelBased`) and parallel testing, which MODE-06 and GEN-08 need. Added CsCheck 4.9.1 to every test project through `tests/Directory.Build.props`, and two reference samples in `tests/Creole.Tests/Samples/PropertyTestSamples.cs`: a plain property (UTF-8 length is the sum of rune lengths) and a model-based one (`Stack<T>` against a list model). Breaking the model on purpose produced a shrunk failing sequence and a replay seed. Documented CsCheck and seed replay in CONTRIBUTING.md § Code.
- 2026-10-17 (Building): Snapshot testing dropped, with the maintainer's agreement. Verify 33 replaced its MIT license with an Open Source Maintenance Fee EULA, and its build fails (SC021) until the consuming repository declares a sponsorship, license or exemption. Snapshot tests give little value before ERR-03 renders diagnostics. TOOL-01 still plans snapshot tests of rendered diagnostics and must choose an approach that does not pass Verify's terms on to grammar authors. The throwaway-grammar helpers moved to CORE-01, because nothing can build a grammar before the node model exists. Next: `/complete FND-04`.
