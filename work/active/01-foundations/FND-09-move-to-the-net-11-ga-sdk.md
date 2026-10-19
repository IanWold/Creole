# FND-09: Move to the .NET 11 GA SDK

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
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

- [REL-04 Public API review and freeze](../14-release/REL-04-public-api-review-and-freeze.md)

## Whitepaper references

- [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions)
- [§ Small API consequences](../../../whitepaper.md#small-api-consequences)
