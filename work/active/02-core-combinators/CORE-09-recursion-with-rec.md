# CORE-09: Recursion with Rec

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

`Rec(() => p)` defers a reference so static fields can refer to each other, and names the rule for diagnostics.

## Scope

- `Rec` node with lazy resolution.
- `[CallerArgumentExpression]` captures the rule name (for example `Value`).
- Rule names feed `GrammarException` rule paths such as `Document → Value → ObjectValue`.

## Acceptance criteria

- [ ] Mutually recursive grammars like Example A resolve correctly.

## Blocked by

- [CORE-02 Grammar base classes and authoring surface](../02-core-combinators/CORE-02-grammar-base-classes-and-authoring-surface.md)

## Unblocks

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [RSLN-04 CRL0004 forward field reference analyzer](../10-roslyn-analyzers/RSLN-04-crl0004-forward-field-reference-analyzer.md)

## Whitepaper references

- [§ LINQ query syntax](../../../whitepaper.md#linq-query-syntax)
- [§ Developer experience for grammar authors](../../../whitepaper.md#developer-experience-for-grammar-authors)
