# CORE-08: Control nodes

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

Nodes that control backtracking and error labelling.

## Scope

- `Atomic`, `Lookahead`, `Not`, `Cut`.
- `Named(label)` and `Context(text)`.

## Acceptance criteria

- [ ] Node semantics are specified precisely enough for both engines to implement (including `Cut` inside `Atomic`).

## Blocked by

- [CORE-01 Grammar node model](../02-core-combinators/CORE-01-grammar-node-model.md)

## Unblocks

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [TOOL-05 EBNF and railroad diagram export](../12-grammar-author-tooling/TOOL-05-ebnf-and-railroad-diagram-export.md)

## Whitepaper references

- [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking)
- [§ The error model](../../../whitepaper.md#the-error-model)
