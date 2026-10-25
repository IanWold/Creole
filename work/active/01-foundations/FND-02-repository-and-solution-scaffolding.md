# FND-02: Repository and solution scaffolding

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

Create the solution layout. Creole targets .NET 11 and C# 15 only; there is no `net10.0` target.

## Scope

- Projects: `Creole` (net11.0), `Creole.Generators` (netstandard2.0, Roslyn analyzers and generator, packed into `Creole`), `Creole.Testing`, test projects, `Creole.Benchmarks`, `samples/`.
- `Directory.Build.props`: nullable, warnings as errors, `IsAotCompatible`, `IsTrimmable`, deterministic builds, SourceLink.
- `global.json` pinning the .NET 11 SDK (preview until GA).
- License, `.editorconfig`, and the code sections of [CONTRIBUTING.md](../../../CONTRIBUTING.md) (CONTRIBUTING.md already covers the work process).
- Replace the "Build and test" section of [CLAUDE.md](../../../CLAUDE.md) with the real commands. Put C# conventions in `.claude/rules/dotnet.md` (scoped to `**/*.cs`), and add a formatting hook (`dotnet format` after edits) to `.claude/settings.json`.

## Acceptance criteria

- [x] `dotnet build` and `dotnet test` succeed on the empty solution.
- [x] Every runtime library project targets only `net11.0`.

## Blocked by

Nothing. This item can start immediately.

## Unblocks

- [FND-03 Continuous integration](../01-foundations/FND-03-continuous-integration.md)
- [FND-04 Test infrastructure](../01-foundations/FND-04-test-infrastructure.md)
- [FND-05 Benchmark harness](../01-foundations/FND-05-benchmark-harness.md)
- [FND-07 Validate C# 15 features and decide `ParseResult<T>`](../01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md)
- [CORE-01 Grammar node model](../02-core-combinators/CORE-01-grammar-node-model.md)
- [INT-01 Cursor and in-memory input](../../complete/03-interpreter/INT-01-cursor-and-in-memory-input.md)
- [SPAN-01 SourceSpan, SourceId and LineMap](../05-source-spans/SPAN-01-sourcespan-sourceid-and-linemap.md)
- [RSLN-01 Roslyn grammar reader](../10-roslyn-analyzers/RSLN-01-roslyn-grammar-reader.md)
- [RSLN-04 CRL0004 forward field reference analyzer](../10-roslyn-analyzers/RSLN-04-crl0004-forward-field-reference-analyzer.md)

## Whitepaper references

- [§ Design goals and non-goals](../../../whitepaper.md#design-goals-and-non-goals)

## Progress notes

- 2026-10-17 (Building): Started. Plan approved: `Creole.slnx` with src/, tests/, benchmarks/, samples/; .NET 11 SDK 11.0.100-rc.2 installed to ~/.dotnet; MIT license.
- 2026-10-17 (Building): Solution, projects, build props, central packages, global.json, LICENSE (MIT), .editorconfig, CONTRIBUTING § Code, CLAUDE.md commands and `.claude/rules/dotnet.md` in place. `dotnet build` is clean (0 warnings) and `dotnet test` passes 3 of 3. Tests run on Microsoft.Testing.Platform because xUnit v3 4.x does not support VSTest under the .NET 10+ SDK. `scripts/format-hook.py` is written and tested, but the maintainer must register it in `.claude/settings.json` (Claude was not permitted to edit that file). Next: register the hook, then `/complete`.
- 2026-10-17 (Building): Maintainer registered the format hook in `.claude/settings.json`. Criteria verified; card closed.

## Outcome

Delivered the empty solution `Creole.slnx`:

| Path | Target |
| --- | --- |
| `src/Creole` (packs `Creole.Generators.dll` under `analyzers/dotnet/cs`) | net11.0 |
| `src/Creole.Generators` | netstandard2.0 |
| `src/Creole.Testing` | net11.0 |
| `tests/Creole.Tests`, `tests/Creole.Generators.Tests` (xUnit v3) | net11.0 |
| `benchmarks/Creole.Benchmarks` (placeholder console app) | net11.0 |
| `samples/` (README only) | — |

Shared settings are in `Directory.Build.props`: nullable, warnings as errors, code style enforced at build, XML docs, deterministic builds, SourceLink. `Creole` and `Creole.Testing` set `IsAotCompatible` and `IsTrimmable`. Package versions are central in `Directory.Packages.props`. `global.json` pins SDK `11.0.100-rc.2.26504.105` (rolls forward to later feature bands, prerelease allowed). Also added: `LICENSE` (MIT), `.editorconfig`, `.gitignore`, [CONTRIBUTING.md § Code](../../../CONTRIBUTING.md#code), the Build and test section of CLAUDE.md, `.claude/rules/dotnet.md`, and the `scripts/format-hook.py` PostToolUse hook (whitespace-only `dotnet format` on the edited file, so it skips a slow solution load).

Evidence, 2026-10-17, SDK 11.0.100-rc.2:
- `dotnet build`: 0 warnings, 0 errors. `dotnet test`: 3 of 3 passed. `dotnet format --verify-no-changes`: exit 0.
- The `RuntimeLibrariesTargetNet11` test asserts `.NETCoreApp,Version=v11.0` for `Creole` and `Creole.Testing`. No project file uses `TargetFrameworks`.

Deviations: none from the whitepaper. Implementation note: tests run on Microsoft.Testing.Platform, which `global.json` selects, because xUnit v3 4.x does not support VSTest under the .NET 10+ SDK. FND-04 builds on this. No follow-up cards.
