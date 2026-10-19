# CORE-13: Extension members guard their receiver

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

A combinator written as an extension member, such as `p.Many()`, can be called on a null parser, usually a field declared later in the class. Every such combinator should reject it with a `GrammarException` naming the caller's expression, as it does for its other parser arguments.

## Scope

- Each extension member in `Repetition` takes a `[CallerArgumentExpression]` parameter naming its receiver and passes it to `Guard.NotNull`. CORE-04's `Optional`, `Slice`, `Text` and N-ary `Select` already do this (measured on RC 2: the attribute can name an extension block's receiver).
- State the rule in CONTRIBUTING.md § Code and `.claude/rules/dotnet.md`, and in the null-argument section of `docs/concepts/grammars-as-data.md`.
- A reflection test over the `Creole` assembly finds every public extension member whose receiver is a parser and checks that it has a `[CallerArgumentExpression]` parameter naming the receiver. Combinators added by cards built in parallel (CORE-03, CORE-05 and others) are covered once they merge.
- Instance methods cannot do this: C# throws `NullReferenceException` before the method runs. CRL0004 covers them at build time.

## Acceptance criteria

- [ ] Each `Repetition` member rejects a null receiver with a `GrammarException` whose message names the caller's expression and suggests `Rec`, shown by a test per member.
- [ ] The reflection test passes, and fails when the receiver parameter is removed from any one member.
- [ ] The rule is stated in CONTRIBUTING.md § Code, `.claude/rules/dotnet.md` and `docs/concepts/grammars-as-data.md`.

## Blocked by

- [CORE-07 Repetition nodes](../02-core-combinators/CORE-07-repetition-nodes.md)

## Unblocks

- [REL-04 Public API review and freeze](../14-release/REL-04-public-api-review-and-freeze.md)

## Whitepaper references

- [§ Diagnostics](../../../whitepaper.md#diagnostics)
