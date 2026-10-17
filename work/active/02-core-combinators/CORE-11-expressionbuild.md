# CORE-11: Expression.Build

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

An operator-precedence node, so grammars never need left recursion for operators.

## Scope

- `Expression.Build(atom, table)` with prefix, postfix, left-, right- and non-associative infix operators per level.
- Specify precedence-climbing semantics and error behavior.
- Expose the table so analysis treats the node as one well-understood unit.

## Acceptance criteria

- [ ] Arithmetic grammar tests cover associativity, mixed prefix and infix use of the same token, and non-associative errors.

## Blocked by

- [CORE-01 Grammar node model](../02-core-combinators/CORE-01-grammar-node-model.md)

## Unblocks

- [INT-04 Execute Bind, Expression.Build and Custom](../03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [RSLN-01 Roslyn grammar reader](../10-roslyn-analyzers/RSLN-01-roslyn-grammar-reader.md)
- [TOOL-05 EBNF and railroad diagram export](../12-grammar-author-tooling/TOOL-05-ebnf-and-railroad-diagram-export.md)
- [REL-01 Worked-example samples](../14-release/REL-01-worked-example-samples.md)

## Whitepaper references

- [§ Expressions](../../../whitepaper.md#expressions)
