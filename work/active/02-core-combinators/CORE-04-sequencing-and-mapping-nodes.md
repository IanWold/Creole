# CORE-04: Sequencing and mapping nodes

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

Sequencing and maps: the shapes that make up most grammar code.

## Scope

- `Seq` overloads up to at least 8 operands (Example B uses 5).
- `Select` and `As` as instance methods, allowing an explicit result type so union conversions apply.
- `Before`, `Then`, `Between`, `Ignore`, `Optional`.
- `Slice()` and `Text()` capture nodes.

## Acceptance criteria

- [ ] `Seq(p, q, r).Select((a, b, c) => ...)` builds a single analyzable sequence node.

## Blocked by

- [CORE-01 Grammar node model](../02-core-combinators/CORE-01-grammar-node-model.md)

## Unblocks

- [CORE-10 Bind and LINQ query syntax](../02-core-combinators/CORE-10-bind-and-linq-query-syntax.md)
- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [TOOL-05 EBNF and railroad diagram export](../12-grammar-author-tooling/TOOL-05-ebnf-and-railroad-diagram-export.md)

## Whitepaper references

- [§ Types](../../../whitepaper.md#types)
- [§ Small API consequences](../../../whitepaper.md#small-api-consequences)
- [§ Text capture](../../../whitepaper.md#text-capture)
