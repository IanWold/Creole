# CORE-03: Primitive nodes

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Done |

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

- [CORE-12 Custom primitives](../02-core-combinators/CORE-12-custom-primitives.md)
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

## Outcome

- **FIRST-set representation** in `src/Creole/Analysis/`, all internal:
  - `RangeSet`: sorted, disjoint integer key ranges.
  - `CharSet`: a typed view over UTF-16 code units, with `Digit`, `Letter`, `LetterOrDigit` and `Whitespace` built from `char.IsX`. [CORE-12](CORE-12-custom-primitives.md) makes it public for `ScannerInfo`.
  - `FirstSet`: one representation for character and token layers. It holds integer keys (character codes, enum kinds), union case types, literal values, and any-item and end-of-input flags. Overlap is exact within a part and conservative across parts.
- **`IPrimitiveNode`** ([src/Creole/IPrimitiveNode.cs](../../../src/Creole/IPrimitiveNode.cs)) exposes `First`, `Nullable` and `Opaque` without type arguments, for ANA-01.
- **Nodes** in `src/Creole/Nodes/`: `AnyNode`, `EndNode`, `TokenNode`, `SatisfyNode` (opaque), `CharClassNode`, `StringNode`, `KindNode` and `CaseNode`. Their `NodeKind` members are one group at the end of the enum. Each `Lower` returns `PlanBuilder.NotYetExecutable` until INT-03.
- **Entry points:**
  - `Any`, `End`, `Token` and `Satisfy` in [GrammarOfTIn.Primitives.cs](../../../src/Creole/GrammarOfTIn.Primitives.cs).
  - Character classes, `String` and `CIString` on [CharGrammar](../../../src/Creole/CharGrammar.cs). CA1720 is suppressed on `Char` and `String`, with a justification.
  - `Kind(k)` and `Case<T>()` on [TokenGrammar](../../../src/Creole/TokenGrammar.cs). CA1000 is suppressed, as on `Grammar<TIn>`.
- **Choices the whitepaper left open:**
  - Named classes are Unicode, as in .NET and Pidgin.
  - `CIString` compares by `ToLowerInvariant` and returns the input as written. This matches Pidgin 3.5.1, measured.
  - `String("")` and `CIString("")` are rejected.
  - `Case<T>()` requires `TToken` to be a union.
- **Tests:** `RangeSetTests` (CsCheck model-based), `CharSetTests` (every code unit against `char.IsX`), `FirstSetTests`, `PrimitiveNodeTests`, `TokenPrimitiveTests`, and `NodeModelTests.EveryPrimitiveNodeExposesItsFirstSet`.
- **Evidence, 2026-10-19, commit 99f3eda:** `dotnet build` with 0 warnings; `dotnet test` with 334 total, 0 failed, 1 skipped; the AOT smoke publish with 0 warnings.
- **The skipped test:** `CaseOverloadsOnTokenGrammarAndGrammarBothResolve` skips its second half until CORE-05 adds `Grammar<TIn>.Case`. After both are merged, it must run and pass.
- **Docs:** [Primitives](../../../docs/concepts/primitives.md).
- **Follow-ups:**
  - Idea [Union tokens and `IKinded`](../../ideas/union-tokens-and-ikinded.md), for a research spike.
  - INT-03 must test that `String`, `CIString`, `Kind` and the other primitives fail without consuming input; this is now in its scope. Only their XML docs state it so far.
