# FND-09: Move to the .NET 11 GA SDK

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

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

- [ ] `global.json` pins a GA SDK, and on it `dotnet build` has 0 warnings, `dotnet test` passes and the AOT smoke publish has 0 IL warnings.
- [ ] `Microsoft.CodeAnalysis.CSharp` is at the GA compiler's Roslyn version.
- [ ] Each RC 2 finding listed in Scope is re-checked on GA, with the result recorded on this card. Any finding that changed gets a decision record or a card.
- [ ] CLAUDE.md and work/README.md no longer describe RC 2 as the pinned SDK.

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
