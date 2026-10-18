# CORE-05: Choice nodes

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

Ordered choice with commit semantics, plus the selective forms that keep every branch visible to analysis.

## Scope

- `Or` and `OneOf`.
- `Switch` over a scrutinee with statically known branches and `@default`.
- `Match(Case<A>(...), Case<B>(...))` over a union-valued parser.

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
