# FND-03: Continuous integration

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

Build, test and package on every change, and keep Native AOT and trimming safety (G8) enforced from day one.

## Scope

- Build and test on Linux, Windows and macOS.
- Publish a small Native AOT smoke app that uses the library; fail on any trim or AOT warning.
- Pack job that checks the packages build on every pull request.

## Acceptance criteria

- [x] Pull requests run CI automatically.
- [x] An introduced IL2xxx or IL3xxx warning fails the build.

## Blocked by

- [FND-02 Repository and solution scaffolding](../01-foundations/FND-02-repository-and-solution-scaffolding.md)

## Unblocks

- [GEN-09 Native AOT and trimming validation](../11-source-generator/GEN-09-native-aot-and-trimming-validation.md)

## Whitepaper references

- [§ Design goals and non-goals](../../../whitepaper.md#design-goals-and-non-goals)

## Progress notes

- 2026-10-17 (Build): CI workflow in `.github/workflows/build.yml` (maintainer). Added `tests/Creole.AotSmoke`: a Native AOT app that references Creole and Creole.Testing and roots both assemblies with `TrimmerRootAssembly`, so ILC analyzes all their code. CI job `aot-smoke` publishes and runs it. Verified locally on .NET 11 RC 2: publish succeeds and the binary exits 0; a probe `Type.GetType(string)` in Creole failed publish with IL2057, both with the trim analyzer on (build-time) and off (ILC in the smoke publish). Next: the `build` job's `dotnet-version: 10.0.x` cannot satisfy `global.json` (11 RC 2); switch it to `global-json-file`, add the Windows/macOS matrix and the pack job, then confirm on a PR run.
- 2026-10-17 (Build): `build` now runs on a matrix of `ubuntu-latest`, `windows-latest` and `macos-latest` (`fail-fast: false`). New `pack` job runs `dotnet pack -c Release` on the solution and uploads the `.nupkg` and `.snupkg` files as the `packages` artifact; it fails if no packages are produced. Verified locally: pack produces Creole and Creole.Testing packages, and the Creole package contains `analyzers/dotnet/cs/Creole.Generators.dll`. The `build` job still uses `dotnet-version: 11.0.x` (maintainer's choice); `aot-smoke` and `pack` use `global-json-file`. Next: push `feature/ci`, open a PR, confirm all jobs pass, and optionally push an IL2057 probe to see CI fail; then complete.
- 2026-10-17 (Build): Removed the upload step from `pack` at the maintainer's request; the job now only runs `dotnet pack -c Release`, so a packaging break fails the PR. Scope line updated. Next step unchanged: open a PR and confirm all jobs pass.
- 2026-10-17 (Build): Scope changes recorded: the on-demand benchmark job moved to [FND-05](FND-05-benchmark-harness.md), which builds the harness it runs; uploading `.nupkg` files as CI artifacts was dropped by the maintainer, since nobody consumes PR-built packages before 1.0. Next: open a PR and confirm all jobs pass.
- 2026-10-17 (Build): PR #2 (`feature/ci`, head `abdc88d`) ran all five checks on `pull_request` and all passed. Next: complete.

## Outcome

Delivered:

- [.github/workflows/build.yml](../../../.github/workflows/build.yml), run on every pull request to `main`:
  - `build`: restore, build and test on `ubuntu-latest`, `windows-latest` and `macos-latest`.
  - `aot-smoke`: publishes [tests/Creole.AotSmoke](../../../tests/Creole.AotSmoke/) with Native AOT and runs the binary. The app roots `Creole` and `Creole.Testing` with `TrimmerRootAssembly`, so ILC analyzes all their code, and warnings are errors, so any IL2xxx or IL3xxx warning fails the job.
  - `pack`: runs `dotnet pack -c Release` on the solution so packaging breaks fail the PR. Packages are not uploaded.
- The AOT smoke command is in [CONTRIBUTING.md](../../../CONTRIBUTING.md) and CLAUDE.md.

Evidence:

- Pull requests run CI: PR #2 triggered the workflow on `pull_request` (runs 39091758225 and 39091893119). All five checks succeeded. The CI log shows `Generating native code`, then `Creole AOT smoke: OK`, and both packages created.
- Warnings fail the build: checked locally on .NET 11 RC 2, not in CI. A probe `Type.GetType(string)` in `Creole`, with nothing calling it, made `dotnet publish tests/Creole.AotSmoke -c Release` exit 1 with IL2057. This happened with the trim analyzers on (at build time, so the `build` job catches it too) and off (ILC in the smoke publish). CI runs the same command with the same `Directory.Build.props`.

Scope changes, agreed with the maintainer and with no whitepaper impact:

- The on-demand benchmark job moved to [FND-05](FND-05-benchmark-harness.md).
- Uploading `.nupkg` files as artifacts was dropped.

Not covered here:

- The `build` job installs `dotnet-version: 11.0.x`, while `aot-smoke` and `pack` read `global.json`. The `build` job can drift to a newer SDK than the pinned one.
- [publish.yml](../../../.github/workflows/publish.yml), the release job, was not exercised.
- `Program.cs` in the smoke app is a placeholder. It should call real API once CORE-01 lands. [GEN-09](../11-source-generator/GEN-09-native-aot-and-trimming-validation.md) extends AOT validation to generated parsers.
