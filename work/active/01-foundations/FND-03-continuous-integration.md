# FND-03: Continuous integration

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Size | S |
| Status | Not started |

## Summary

Build, test and package on every change, and keep Native AOT and trimming safety (G8) enforced from day one.

## Scope

- Build and test on Linux, Windows and macOS.
- Publish a small Native AOT smoke app that uses the library; fail on any trim or AOT warning.
- Pack job that uploads `.nupkg` files as artifacts.
- On-demand benchmark job using the harness from FND-05.

## Acceptance criteria

- [ ] Pull requests run CI automatically.
- [ ] An introduced IL2xxx or IL3xxx warning fails the build.

## Blocked by

- [FND-02 Repository and solution scaffolding](../01-foundations/FND-02-repository-and-solution-scaffolding.md)

## Unblocks

- [GEN-09 Native AOT and trimming validation](../11-source-generator/GEN-09-native-aot-and-trimming-validation.md)

## Whitepaper references

- [§ Design goals and non-goals](../../../whitepaper.md#design-goals-and-non-goals)
