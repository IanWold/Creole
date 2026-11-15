# FND-09: Move to the .NET 11 GA SDK

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 2 — Analysis |
| Status | Done |

## Summary

Creole is built and tested on the .NET 11 RC 2 SDK. When .NET 11 and C# 15 reach GA, move to the GA SDK and re-check every finding that was measured only on RC 2.

## Scope

- Pin the GA SDK in `global.json`, and make every CI job use it.
- Raise `Microsoft.CodeAnalysis.*` to the GA compiler's Roslyn version, keeping it at or below the SDK's compiler. `SnippetCompiler` tests compile with this package, not the SDK's compiler, so they check GA behaviour only after the bump.
- Re-run FND-07's checks (unions, hand-written `[Union]` structs, `closed` for the `ParseResult<T>` fallback), or the tests that rely on them, such as ERR-01's.
- Re-check CORE-04's finding that an N-ary `Select<T>((a, b) => …)` does not compile (CS1593), pinned by `SequencingNodeTests.NaryResultTypeCannotBeGivenExplicitly`, and that `[CallerArgumentExpression]` can name an extension block's receiver.
- Re-check the `[Union]` member-provider pattern `Maybe<T>` relies on ([ADR-0007](../../docs/decisions/0007-maybe-as-a-non-boxing-union.md)): a public `object? Value` is required, `TryGetValue` alone drives matching, `default` matches `Nothing`, and creating and matching a `Maybe<int>` allocates nothing.
- Re-check CORE-07's finding that instance methods returning a type built from `TOut` fail the AOT smoke publish (IL3054).
- Re-check CORE-05's findings behind [ADR-0005](../../docs/decisions/0005-match-cases-infer-every-type-from-a-typed-lambda.md) and [ADR-0006](../../docs/decisions/0006-switch-and-match-take-a-collection-beyond-eight-cases.md), pinned in `MatchNodeTests`: `Case<V1Header>(branch, (h, body) => …)` does not compile (CS1501); the typed-lambda `Case(...)` compiles beside `TokenGrammar.Case<T>()`; `Match` on a non-union is CS0311; declared unions implement `IUnion`. Also that a collection expression of tuples infers `Switch`'s `TOut` (`SwitchNodeTests.ACollectionOfCasesBuildsTheSameNode`).
- Re-check [ADR-0009](../../docs/decisions/0009-where-rule-names-come-from.md)'s finding that a `[CallerMemberName]` parameter called from a static field initializer receives the field's name, pinned by CORE-09's `RecTests`. If it changed, a `Rec` at a rule's definition is unnamed until GEN-01 names it.
- Point CLAUDE.md's project facts and the work README's FND-07 note at the GA results.

## Acceptance criteria

- [x] `global.json` pins a GA SDK, and on it `dotnet build` has 0 warnings, `dotnet test` passes and the AOT smoke publish has 0 IL warnings.
- [x] `Microsoft.CodeAnalysis.CSharp` is at the GA compiler's Roslyn version.
- [x] Each RC 2 finding listed in Scope is re-checked on GA, with the result recorded on this card. Any finding that changed gets a decision record or a card.
- [x] CLAUDE.md and work/README.md no longer describe RC 2 as the pinned SDK.

## Blocked by

- [FND-07 Validate C# 15 features and decide `ParseResult<T>`](../01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md)
- [CORE-04 Sequencing and mapping nodes](../02-core-combinators/CORE-04-sequencing-and-mapping-nodes.md)
- External: .NET 11 / C# 15 GA (expected November 2026)

## Unblocks

- [RSLN-04 CRL0004 forward field reference analyzer](../10-roslyn-analyzers/RSLN-04-crl0004-forward-field-reference-analyzer.md)
- [REL-04 Public API review and freeze](../14-release/REL-04-public-api-review-and-freeze.md)

## Whitepaper references

- [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions)
- [§ Small API consequences](../../../whitepaper.md#small-api-consequences)

## Progress notes

- 2026-10-19 (Not started): Moved from Phase 0 to Phase 1, wave 1, by the maintainer: GA is unlikely before Phase 0 ends, and no Phase 0 card depends on this one. Phase 0 work continues on RC 2. Next: start when .NET 11 GA ships.
- 2026-10-19 (Not started): Correction to the note above: the card is in Phase 1, wave 3, not wave 1. Waves count dependency depth across phases, and CORE-04 puts this card at the same depth as SPAN-04. Next: start when .NET 11 GA ships.
- 2026-10-26 (Not started): Moved from Phase 1 to Phase 2, wave 1, by the maintainer. Its depth (5) matches CORE-14, the shallowest Phase 2 card. It now blocks RSLN-04, so the first Roslyn analyzer is written against the GA Roslyn packages, not RC 2. REL-04 in Phase 3 still depends on it. Next: start when .NET 11 GA ships.
- 2026-11-15 (Building): Started. .NET 11 GA shipped on 2026-11-10 (dotnet/core discussion #10711; Learn "What's new in C# 15" and ".NET 11 overview", updated 2026-11-11/12), so the external blocker is met. SDK 11.0.100 is installed locally. Approach: pin 11.0.100, raise Roslyn and the other packages, build and test, re-check each RC 2 finding, then look at which GA C# 15 and .NET 11 features fit.
- 2026-11-15 (Building): Moved to GA. `global.json` pins 11.0.100 (no prerelease); both CI workflows set up .NET from `global.json` instead of `11.0.x`. `LangVersion` is now `15`, not `preview`: C# 15 is the default for `net11.0` at GA, and `preview` now also turns on the memory-safety preview, which Creole does not use. `SnippetCompiler` parses at `LanguageVersion.CSharp15`. Packages: `Microsoft.CodeAnalysis.CSharp` and `.Analyzers` 5.9.0 → 5.12.0 (package build 5.12.0-1.26502.2, the SDK compiler is 5.12.0-1.26529.118, both assembly version 5.12.0.0, so the package stays at or below the compiler); CsCheck 5.0.1, xunit.v3 4.0.3, Parlot 2.3.1. Unchanged because no newer stable exists: BenchmarkDotNet 0.16.0-preview.2, Pidgin 3.5.1, Sprache 2.3.1, Superpower 3.2.1. Evidence: `dotnet build` 0 warnings, 0 errors; `dotnet test` 1637 passed, 0 failed; AOT smoke publish has no IL warnings and prints "Creole AOT smoke: OK"; `dotnet pack src/Creole -c Release` succeeds.
- 2026-11-15 (Building): RC 2 findings re-checked on GA (SDK 11.0.100, compiler 5.12.0-1.26529.118). All held. Pinned by tests that pass on GA: ERR-01's `ParseResultTests` (default throws `SwitchExpressionException`, null arm), `SequencingNodeTests.NaryResultTypeCannotBeGivenExplicitly` (CS1593), `ExtensionReceiverTests` (`[CallerArgumentExpression]` names an extension block's receiver), `MatchNodeTests` (CS1501, CS0311, the typed-lambda `Case` beside `TokenGrammar.Case<T>()`, declared unions implement `IUnion`), `SwitchNodeTests.ACollectionOfCasesBuildsTheSameNode`, `RecTests` (`[CallerMemberName]` from a static field initializer), and `MaybeTests` (default is `Nothing`, 0 bytes to create and match). Scratch spike (not checked in): a declared `ParseResult<T>` union switch needs no null arm, and a missing case is CS8509; `ParseResult<T>?` without a null arm is CS8655; `default` throws `SwitchExpressionException` and matches an explicit `null` arm; a `Maybe<T>`-shaped `[Union]` struct whose `Value` throws still matches correctly, so `TryGetValue` alone drives matching; without a public `Value` it fails with CS0535 and CS9386 ("an instance 'Value' property of type 'object?' or 'object'"), so `object Value` would also do; `Maybe<int>` creates and matches with 0 bytes, a declared `union Boxy(int, string)` with 24; `closed Result<T>` with `Ok<T>`, `Fixed<T>`, `Fail<T>` is exhaustive in generic code, a missing case is CS8509, and `closed sealed` is CS9381. Scratch copy of the repo with an instance `ManyInstance()` on `Parser<TIn, TOut>`: the AOT smoke publish fails with IL3054 (5 errors), so CORE-04's extension-member rule still stands. Learn says `UnionAttribute` and `IUnion` are "not directly user-facing yet", but the GA compiler honours the hand-written pattern as above; ADR-0007 stands. Next: `/complete FND-09`.

## Outcome

- The repository builds on the .NET 11 GA SDK: `global.json` pins 11.0.100, and every CI job in `.github/workflows/build.yml` and `publish.yml` sets up .NET from `global.json`.
- `Directory.Build.props` sets `LangVersion` 15 instead of `preview`, so the build no longer picks up C# 15's memory-safety preview or later preview features. `tests/Creole.Tests/Utilities/SnippetCompiler.cs` compiles at `LanguageVersion.CSharp15`.
- `Directory.Packages.props`: Roslyn packages 5.12.0, CsCheck 5.0.1, xunit.v3 4.0.3, Parlot 2.3.1. BenchmarkDotNet stays on 0.16.0-preview.2 because no stable 0.16 exists.
- Every RC 2 finding held on GA (see the progress notes), so no decision record or follow-up card was needed. ADR-0002, ADR-0005, ADR-0006, ADR-0007 and ADR-0009 stand, as does CORE-04's extension-member rule for combinators whose result type is built from `TOut`.
- README.md, CONTRIBUTING.md, CLAUDE.md, work/README.md and the whitepaper's § Risks and open questions entry on `ParseResult<T>` now describe GA, not RC 2.
- Parlot's move from 2.1.0 to 2.3.1 changes a competitor baseline. BENCH-01's recorded ratios were measured against 2.0.0/2.1.0 and were not re-run here.

