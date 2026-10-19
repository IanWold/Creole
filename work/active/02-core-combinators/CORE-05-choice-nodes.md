# CORE-05: Choice nodes

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

Ordered choice with commit semantics, plus the selective forms that keep every branch visible to analysis.

## Scope

- `Or` and `OneOf`, with 2 to 16 alternatives.
- `Switch` over a scrutinee with statically known branches and `@default`: 1 to 8 cases as separate arguments, or any number as a collection ([ADR-0006](../../docs/decisions/0006-switch-and-match-take-a-collection-beyond-eight-cases.md)).
- `Match(Case(branch, (A a, B b) => …), …)` over a parser whose value implements `IUnion`: 1 to 8 cases as separate arguments, or any number as a collection ([ADR-0005](../../docs/decisions/0005-match-cases-infer-every-type-from-a-typed-lambda.md), [ADR-0006](../../docs/decisions/0006-switch-and-match-take-a-collection-beyond-eight-cases.md)).
- Each public combinator null-checks its parser arguments by taking its own `[CallerArgumentExpression]` parameter per parser argument and passing it to `Guard.NotNull`, so the `GrammarException` names the caller's expression (see `Grammar.Deterministic`, CORE-02). A `params` array cannot capture each element's expression, so `OneOf` and `Match` take fixed-arity overloads, each with one expression parameter per operand, rather than `params`.

## Acceptance criteria

- [ ] Branches of `Switch` and `Match` are reachable from the node for analysis.

## Blocked by

- [CORE-02 Grammar base classes and authoring surface](../02-core-combinators/CORE-02-grammar-base-classes-and-authoring-surface.md)

## Unblocks

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [TOOL-05 EBNF and railroad diagram export](../12-grammar-author-tooling/TOOL-05-ebnf-and-railroad-diagram-export.md)

## Whitepaper references

- [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking)
- [§ Selective choice](../../../whitepaper.md#selective-choice)
- [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions)

## Artifacts

- [ADR-0005: `Match` cases infer every type from a typed lambda](../../docs/decisions/0005-match-cases-infer-every-type-from-a-typed-lambda.md) (Superseded by 0006)
- [ADR-0006: `Switch` and `Match` take a collection beyond eight cases](../../docs/decisions/0006-switch-and-match-take-a-collection-beyond-eight-cases.md) (Accepted)

## Progress notes

- 2026-10-19 (Designing): Started. Blocker CORE-02 is done. Spike on SDK 11.0.100-rc.2.26504.105 (scratchpad, not checked in): the whitepaper's `Case<V1Header>(BodyV1, static (h, body) => …)` does not compile, because C# needs all type arguments or none; from a `TokenGrammar` class declaring `Case<T>()` it fails with CS1501. `Case(BodyV1, static (V1Header h, Body body) => …)` compiles there. Declared unions implement `System.Runtime.CompilerServices.IUnion`. `[CallerArgumentExpression]` can name an extension-block receiver. `Version.Switch((1, RecordV1), …)` fails inference (CS0411) when `Version` produces `byte`; it works when the key type matches. Maintainer chose the typed-lambda form and caps of 16 for `OneOf` and 8 for `Switch` and `Match`; drafted ADR-0005. Plan approved: increment 1 `Or`/`OneOf`, 2 `Switch`, 3 `Match`/`Case`. Next: maintainer accepts ADR-0005, then whitepaper sample update and Build.
- 2026-10-19 (Designing): ADR-0005 accepted by the maintainer. Whitepaper § Using C# 15 unions and § Selective choice updated; scope updated. Card moves to Building. Next: increment 1, `Or` and `OneOf`.
- 2026-10-19 (Building): Increment 1 done: `OrNode` and `NodeKind.Or`; `Or` as an extension member in `Choice` (names a null receiver too); `OneOf` with 2 to 16 alternatives in `GrammarOfTIn.Choice.cs`; `ChoiceNodeTests` (reflection checks every overload pairs each alternative with its own expression); docs/concepts/choice.md. `dotnet test`: 295 passed. AOT smoke publish clean. Next: increment 2, `Switch`.
- 2026-10-19 (Designing): During increment 2, found that ADR-0005's "Larger choices nest" is false for `Switch` and `Match`: a nested one re-parses the scrutinee, so more than 8 cases could not be written. Spike on RC 2: `Version.Switch([(1, A), (2, B)], @default: A)` and an array both bind to an `IReadOnlyList` overload and infer `TOut`; separate tuple arguments still bind to the fixed overload. Maintainer chose a collection overload; drafted ADR-0006, superseding ADR-0005. Increment 2 is uncommitted in the working tree: `SwitchNode`, `NodeKind.Switch`, `Guard.BranchNotNull`, fixed `Switch` overloads in `Choice.Switch.cs`; no tests or docs yet. Next: maintainer accepts ADR-0006, then finish increment 2 with the collection overload.
- 2026-10-19 (Designing): ADR-0006 accepted by the maintainer; ADR-0005 superseded. Whitepaper § Selective choice and § Using C# 15 unions updated; scope updated. Card back to Building. Next: finish increment 2 with the `Switch` collection overload, tests and docs.
- 2026-10-19 (Building): Increment 2 done: `SwitchNode` (children: scrutinee, branches in order, default) and `NodeKind.Switch`; `Switch` with 1 to 8 cases as separate arguments and an `IReadOnlyList` collection overload, both through one builder in `Choice.Switch.cs`; `Guard.BranchNotNull`; duplicate keys and empty collections rejected; `SwitchNodeTests`; docs/concepts/choice.md § Selective choice with `Switch`. No structural view interface for `Switch`: analysis tells scrutinee, branches and default apart by position in `Children`. `dotnet test`: 329 passed. AOT smoke publish clean. Next: increment 3, `Match` and `Case`.
