# CORE-03: Primitive nodes

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

The leaf nodes of every grammar.

## Scope

- `Token(x)`, `Kind(k)`, `Any`, `End`, `Satisfy(pred)`. The `Return` and `Fail` nodes are in CORE-01.
- `Case<T>()` on `TokenGrammar` for union tokens; analysis treats each case type as a kind ([§ Where unions are not used](../../../whitepaper.md#where-unions-are-not-used)).
- Character classes: `Char`, `Range`, `Digit`, `Letter`, `LetterOrDigit`, `Whitespace`, `NoneOf`.
- `String` and `CIString`, atomic by construction (they fail without consuming).

## Acceptance criteria

- [x] Each primitive's node exposes what analysis needs: its FIRST set, or that it is opaque (`Satisfy`).

## Blocked by

- [CORE-02 Grammar base classes and authoring surface](../02-core-combinators/CORE-02-grammar-base-classes-and-authoring-surface.md)

## Unblocks

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [TOOL-05 EBNF and railroad diagram export](../12-grammar-author-tooling/TOOL-05-ebnf-and-railroad-diagram-export.md)

## Whitepaper references

- [§ Types](../../../whitepaper.md#types)
- [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking)

## Artifacts

- Docs: [Primitives](../../../docs/concepts/primitives.md)

## Progress notes

- 2026-10-19 (Build): Started. Primitive nodes expose their FIRST set through one internal representation shared by character and token layers, with `CharSet` designed for reuse by CORE-12.
- 2026-10-19 (Build): Increment 1 done: internal `RangeSet`, `CharSet` (kept internal; CORE-12 makes it public) and `FirstSet` in `src/Creole/Analysis/`, and `IPrimitiveNode`. One `FirstSet` covers character and token layers: integer keys (character codes, enum kinds), union case types, literal values, plus any-item and end-of-input flags. Logged the union-token/`IKinded` concern as an idea. Build, 281 tests and the AOT publish pass. Next: increment 2, the `Grammar<TIn>` and `CharGrammar` primitives with `docs/concepts/primitives.md`.
- 2026-10-19 (Build): Increment 2 done: `AnyNode`, `EndNode`, `TokenNode`, `SatisfyNode`, `CharClassNode` and `StringNode`, each implementing `IPrimitiveNode`; `Any`, `End`, `Token` and `Satisfy` in `GrammarOfTIn.Primitives.cs`; character classes, `String` and `CIString` on `CharGrammar` (CA1720 suppressed on `Char` and `String`); `docs/concepts/primitives.md`. Build, all tests and the AOT publish pass. Next: increment 3, `Kind` and `Case<T>()` on `TokenGrammar`.
- 2026-10-19 (Build): Increment 3 done: `KindNode` and `CaseNode` with `Kind(k)` and `Case<T>()` on `TokenGrammar` (CA1000 suppressed as on `Grammar<TIn>`); `Case<T>()` throws `GrammarException` unless `TToken` is a union. Criterion met: all eight primitive node types implement `IPrimitiveNode` (`NodeModelTests.EveryPrimitiveNodeExposesItsFirstSet`), and `PrimitiveNodeTests` and `TokenPrimitiveTests` check each FIRST set and that `Satisfy` is opaque. `TokenPrimitiveTests.CaseOverloadsOnTokenGrammarAndGrammarBothResolve` skips its second half until CORE-05 adds `Grammar<TIn>.Case`; a scratch spike confirmed C# resolves both overloads. Build, 334 tests (1 skipped) and the AOT publish pass. Next: maintainer review, then `/complete`.
