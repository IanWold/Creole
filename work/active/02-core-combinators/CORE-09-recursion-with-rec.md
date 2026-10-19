# CORE-09: Recursion with Rec

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

`Rec(() => p)` defers a reference so static fields can refer to each other, and names the rule for diagnostics.

## Scope

- `Rec` node with lazy resolution.
- Rule names per [ADR-0009](../../docs/decisions/0009-where-rule-names-come-from.md): `[CallerArgumentExpression]` names the rule when the reference is a plain member reference (`Rec(() => Value)` is `Value`); otherwise `[CallerMemberName]` does (`Value = Rec(() => OneOf(...))` is `Value`).
- Rule names feed `GrammarException` rule paths such as `Document → Value → ObjectValue`. Lowering errors name the path of `Rec`s being lowered; field names for rules outside `Rec` come with GEN-01.
- Interpreter lowering of `Rec` cycles: the lowering context from INT-02 lowers each node once and resolves a cycle through a placeholder plan node that is filled when the target is lowered. Moved here from INT-02 with the maintainer.

## Acceptance criteria

- [ ] Mutually recursive grammars like Example A resolve correctly.
- [ ] Lowering a grammar with `Rec` cycles terminates and lowers each node once. Parsing recursive input with the lowered plan moved to [INT-03](../03-interpreter/INT-03-execute-the-core-combinators.md), the first card that executes the nodes a recursive grammar needs (agreed with the maintainer on 2026-10-19).

## Blocked by

- [CORE-02 Grammar base classes and authoring surface](../02-core-combinators/CORE-02-grammar-base-classes-and-authoring-surface.md)
- [INT-02 Plan lowering and cache](../03-interpreter/INT-02-plan-lowering-and-cache.md)

## Unblocks

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [RSLN-04 CRL0004 forward field reference analyzer](../10-roslyn-analyzers/RSLN-04-crl0004-forward-field-reference-analyzer.md)

## Whitepaper references

- [§ LINQ query syntax](../../../whitepaper.md#linq-query-syntax)
- [§ Developer experience for grammar authors](../../../whitepaper.md#developer-experience-for-grammar-authors)
- [§ Interpreted engine](../../../whitepaper.md#interpreted-engine)

## Artifacts

- [ADR-0009: Rule names come from `Rec`, the enclosing field, and a generated name table](../../docs/decisions/0009-where-rule-names-come-from.md) (Accepted)

## Progress notes

- 2026-10-19 (Designing): Started. Blockers CORE-02 and INT-02 are done. Build plan agreed with the maintainer in two increments: (1) the `Rec` node, `Grammar<TIn>.Rec` in `GrammarOfTIn.Rec.cs`, `NodeKind.Rec` at the end of the enum, lazy resolution cached in a `PublicationOnly` `Lazy` (null results not cached), an Example A structure test, docs in `docs/concepts/grammars-as-data.md`; (2) cycle lowering in `PlanBuilder`: in-progress nodes reached again get a `ForwardPlan` placeholder filled when the node finishes, `Rec` lowers to its target's plan, lowering errors name the path of named `Rec`s, a `Rec`-only cycle is a `GrammarException`, tested with a counting test node (no parse results; INT-03). Planning showed `Rec` alone cannot name most rules at run time, so drafted ADR-0009 (Proposed). Next: maintainer accepts or revises ADR-0009, then fold it in and build increment 1.
- 2026-10-19 (Building): ADR-0009 accepted with option C; the name table goes in GEN-01, and TOOL-05 moved to Phase 3 behind it. Folded into the whitepaper (§ LINQ query syntax, § Developer experience for grammar authors, § Source-generated engine) and cards (GEN-01, TOOL-05, ANA-02, TOOL-02, FND-09, this card's scope). Next: build increment 1 (the `Rec` node).
- 2026-10-19 (Building): Increment 1 built. Code: `Grammar<TIn>.Rec` (`GrammarOfTIn.Rec.cs`) with `[CallerArgumentExpression]` and `[CallerMemberName]`; `RecNode` (resolves on first read through a `PublicationOnly` `Lazy`, so a null result throws a `GrammarException` and is not kept); `RuleName.From` (ADR-0009's rule); `NodeKind.Rec` at the end of the enum; `INode` remarks on cycles. `RecNode.Lower` is still `NotYetExecutable` until increment 2. Call made during Build: the reference is `Func<Parser<TIn, TOut>?>`, because on RC 2 with nullable enabled `Rec(() => LaterField)` warns CS8603 with a non-nullable return (measured in a scratch program). Finding not fixed here: a later field used inside a larger `Rec` lambda, as in Example A's `Rec(() => OneOf(..., ArrayValue, ObjectValue))`, warns CS8604 whatever `Rec`'s signature is; `RecTests` pins Example A with a justified pragma and the docs tell users to write `!`. Tests: `tests/Creole.Tests/RecTests.cs` (names from both sources and the edge cases, forward reference, null reference and null target, lazy single resolution, Example A built from the real combinators with cycles through `Value`, and the docs example). Docs: "Recursion with `Rec`" and "Rule names" in `docs/concepts/grammars-as-data.md`, docs index row. Verified: `dotnet build` 0 warnings; `dotnet test` 553 passed, 0 failed; AOT smoke publish clean. Next: maintainer reviews and commits, then increment 2 (cycle lowering).
