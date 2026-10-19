# CORE-13: Extension members guard their receiver

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

A combinator written as an extension member, such as `p.Many()`, can be called on a null parser, usually a field declared later in the class. Every such combinator should reject it with a `GrammarException` naming the caller's expression, as it does for its other parser arguments.

## Scope

- Each extension member in `Repetition` takes a `[CallerArgumentExpression]` parameter naming its receiver and passes it to `Guard.NotNull`. CORE-04's `Optional`, `Slice`, `Text` and N-ary `Select` already do this (measured on RC 2: the attribute can name an extension block's receiver).
- State the rule in CONTRIBUTING.md § Code and `.claude/rules/dotnet.md`, and in the null-argument section of `docs/concepts/grammars-as-data.md`.
- A reflection test over the `Creole` assembly finds every public extension member whose receiver is a parser and checks that it has a `[CallerArgumentExpression]` parameter naming the receiver. Combinators added by cards built in parallel (CORE-03, CORE-05 and others) are covered once they merge.
- Instance methods cannot do this: C# throws `NullReferenceException` before the method runs. CRL0004 covers them at build time.

## Acceptance criteria

- [x] Each `Repetition` member rejects a null receiver with a `GrammarException` whose message names the caller's expression and suggests `Rec`, shown by a test per member.
- [x] The reflection test passes, and fails when the receiver parameter is removed from any one member.
- [x] The rule is stated in CONTRIBUTING.md § Code, `.claude/rules/dotnet.md` and `docs/concepts/grammars-as-data.md`.

## Blocked by

- [CORE-07 Repetition nodes](../02-core-combinators/CORE-07-repetition-nodes.md)

## Unblocks

- [REL-04 Public API review and freeze](../14-release/REL-04-public-api-review-and-freeze.md)

## Whitepaper references

- [§ Diagnostics](../../../whitepaper.md#diagnostics)

## Artifacts

- Docs: [Grammars as data](../../../docs/concepts/grammars-as-data.md), [Repetition](../../../docs/concepts/repetition.md)

## Progress notes

- 2026-10-19 (Building): Started. Blocker CORE-07 is Done and the card fixes the approach, so it enters at Build.
- 2026-10-19 (Building): Built in one increment; this card merges before CORE-06, CORE-08 and CORE-09. Code: each of the 10 `Repetition` members takes `parserExpression` and calls `Guard.NotNull`, with the receiver's parameter first as in `Choice` (agreed with the maintainer); `Repeat(count)` passes it on. Tests: `RepetitionNodeTests.NullReceiverNamesTheExpression`, one row per member; `ExtensionReceiverTests`, which finds every public static `[Extension]` method in `Creole` whose first parameter is a parser, plus a fixture test showing the check reports only the unguarded member. Measured on RC 2: extension methods compile to such methods; extension properties and static extension members carry no `[Extension]`, so they are not checked. Removing the receiver parameter from `SkipAtLeastOnce`, `SepBy1` and `Maybe.Optional` in turn failed the test, naming exactly that member (built with warnings not as errors, because the leftover `<param>` tag is CS1572). Rule text in CONTRIBUTING.md § Conventions and `.claude/rules/dotnet.md`; docs in grammars-as-data.md and repetition.md. Verified: `dotnet build` 0 warnings; `dotnet test` 541 passed, 0 failed; AOT smoke publish clean. All criteria appear met. Next: `/complete CORE-13`.

## Outcome

Each of the 10 `Repetition` members takes a `[CallerArgumentExpression]` parameter naming its receiver and passes it to `Guard.NotNull`, so a null receiver throws a `GrammarException` that names the caller's expression and suggests `Rec` ([Repetition.cs](../../../src/Creole/Repetition.cs)). The receiver's parameter comes before any other expression parameter, as in `Choice`. [ExtensionReceiverTests](../../../tests/Creole.Tests/ExtensionReceiverTests.cs) checks every public static `[Extension]` method in `Creole` whose first parameter is a parser, so combinators added by later cards are covered without changing the test. Extension properties and static extension members are not checked: measured on RC 2, they carry no `[Extension]`, and they cannot take the parameter or have no receiver. Instance methods are left to CRL0004 ([RSLN-04](../10-roslyn-analyzers/RSLN-04-crl0004-forward-field-reference-analyzer.md)). Verification: all 10 null-receiver rows pass; stripping the receiver's attribute from each `Repetition` member in turn failed the reflection test naming exactly that member. The rule is in CONTRIBUTING.md § Conventions and `.claude/rules/dotnet.md`; user docs are in [Grammars as data](../../../docs/concepts/grammars-as-data.md) and [Repetition](../../../docs/concepts/repetition.md). No deviations from the card or whitepaper, and no follow-up cards.
