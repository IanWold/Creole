# CORE-03: Primitive nodes

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Size | M |
| Status | Not started |

## Summary

The leaf nodes of every grammar.

## Scope

- `Token(x)`, `Kind(k)`, `Any`, `End`, `Satisfy(pred)`, `Return`, `Fail`.
- Character classes: `Char`, `Range`, `Digit`, `Letter`, `LetterOrDigit`, `Whitespace`, `NoneOf`.
- `String` and `CIString`, atomic by construction (they fail without consuming).

## Acceptance criteria

- [ ] Each primitive's node exposes what analysis needs: its FIRST set, or that it is opaque (`Satisfy`).

## Blocked by

- [CORE-02 Grammar base classes and authoring surface](../02-core-combinators/CORE-02-grammar-base-classes-and-authoring-surface.md)

## Unblocks

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [TOOL-05 EBNF and railroad diagram export](../12-grammar-author-tooling/TOOL-05-ebnf-and-railroad-diagram-export.md)

## Whitepaper references

- [§ Types](../../../whitepaper.md#types)
- [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking)
