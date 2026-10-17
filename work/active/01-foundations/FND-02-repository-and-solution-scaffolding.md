# FND-02: Repository and solution scaffolding

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Size | S |
| Status | Not started |

## Summary

Create the solution layout. Creole targets .NET 11 and C# 15 only; there is no `net10.0` target.

## Scope

- Projects: `Creole` (net11.0), `Creole.Generators` (netstandard2.0, Roslyn analyzers and generator, packed into `Creole`), `Creole.Testing`, test projects, `Creole.Benchmarks`, `samples/`.
- `Directory.Build.props`: nullable, warnings as errors, `IsAotCompatible`, `IsTrimmable`, deterministic builds, SourceLink.
- `global.json` pinning the .NET 11 SDK (preview until GA).
- License, `.editorconfig`, and the code sections of [CONTRIBUTING.md](../../../CONTRIBUTING.md) (CONTRIBUTING.md already covers the work process).
- Replace the "Build and test" section of [CLAUDE.md](../../../CLAUDE.md) with the real commands. Put C# conventions in `.claude/rules/dotnet.md` (scoped to `**/*.cs`), and add a formatting hook (`dotnet format` after edits) to `.claude/settings.json`.

## Acceptance criteria

- [ ] `dotnet build` and `dotnet test` succeed on the empty solution.
- [ ] Every runtime library project targets only `net11.0`.

## Blocked by

Nothing. This item can start immediately.

## Unblocks

- [FND-03 Continuous integration](../01-foundations/FND-03-continuous-integration.md)
- [FND-04 Test infrastructure](../01-foundations/FND-04-test-infrastructure.md)
- [FND-05 Benchmark harness](../01-foundations/FND-05-benchmark-harness.md)
- [FND-07 Validate C# 15 features and decide `ParseResult<T>`](../01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md)
- [CORE-01 Grammar node model](../02-core-combinators/CORE-01-grammar-node-model.md)
- [INT-01 Cursor and in-memory input](../03-interpreter/INT-01-cursor-and-in-memory-input.md)
- [SPAN-01 SourceSpan, SourceId and LineMap](../05-source-spans/SPAN-01-sourcespan-sourceid-and-linemap.md)
- [RSLN-01 Roslyn grammar reader](../10-roslyn-analyzers/RSLN-01-roslyn-grammar-reader.md)
- [RSLN-04 CRL0004 forward field reference analyzer](../10-roslyn-analyzers/RSLN-04-crl0004-forward-field-reference-analyzer.md)

## Whitepaper references

- [§ Design goals and non-goals](../../../whitepaper.md#design-goals-and-non-goals)
