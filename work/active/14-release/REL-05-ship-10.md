# REL-05: Ship 1.0

| | |
| --- | --- |
| Epic | [Samples, docs and 1.0 release](../../README.md#rel--samples-docs-and-10-release) |
| Phase | Phase 3 — Staging |
| Size | M |
| Status | Not started |
| Gate | 1.0 release |

## Summary

Package and release Creole 1.0.

## Scope

- `Creole` package with bundled analyzers and generator; `Creole.Testing`.
- SourceLink, symbols, release notes.
- Final check of every phase gate.

## Acceptance criteria

- [ ] 1.0 published on nuget.org.

## Blocked by

- [FND-01 Reserve the Creole NuGet package ID](../01-foundations/FND-01-reserve-the-creole-nuget-package-id.md)
- [MODE-06 Model-based tests for mode rollback](../07-mode-channels/MODE-06-model-based-tests-for-mode-rollback.md)
- [GEN-07 Debugging generated code](../11-source-generator/GEN-07-debugging-generated-code.md)
- [GEN-08 Differential engine-agreement tests](../11-source-generator/GEN-08-differential-engine-agreement-tests.md)
- [GEN-09 Native AOT and trimming validation](../11-source-generator/GEN-09-native-aot-and-trimming-validation.md)
- [GEN-10 Measure the interpreter fallback rate](../11-source-generator/GEN-10-measure-the-interpreter-fallback-rate.md)
- [TOOL-06 EngineAgreement.Check](../12-grammar-author-tooling/TOOL-06-engineagreementcheck.md)
- [BENCH-02 Full benchmark suite](../13-benchmarks/BENCH-02-full-benchmark-suite.md)
- [REL-02 Documentation](../14-release/REL-02-documentation.md)
- [REL-03 Migration guides](../14-release/REL-03-migration-guides.md)
- [REL-04 Public API review and freeze](../14-release/REL-04-public-api-review-and-freeze.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Roadmap](../../../whitepaper.md#roadmap)
