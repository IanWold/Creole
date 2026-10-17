# CORE-10: Bind and LINQ query syntax

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Size | L |
| Status | Not started |

## Summary

Query syntax stays first-class. Each `from` clause is classified as an analyzable sequence step or a genuine bind.

## Scope

- `Bind` node (opaque to analysis).
- `SelectMany` taking `Expression<Func<A, Parser<TIn, B>>>`.
- Per-clause classification at construction: if the parameter is unused, evaluate once and build `Seq`; if used, compile once and build `Bind`.
- Handle transparent identifiers in queries with three or more clauses.
- `where` as a filter that fails committed after consuming; `let` as a map.
- Verify behavior under Native AOT (expressions are interpreted) and trimming.

## Acceptance criteria

- [ ] The whitepaper's `Member` query builds a `Seq` node and `Blob` builds a `Bind`.
- [ ] No expression-tree work happens during parsing.
- [ ] The AOT smoke app (FND-03) exercises a query grammar without warnings.

## Blocked by

- [CORE-04 Sequencing and mapping nodes](../02-core-combinators/CORE-04-sequencing-and-mapping-nodes.md)

## Unblocks

- [INT-04 Execute Bind, Expression.Build and Custom](../03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [RSLN-01 Roslyn grammar reader](../10-roslyn-analyzers/RSLN-01-roslyn-grammar-reader.md)
- [GEN-03 Lambda and helper inlining](../11-source-generator/GEN-03-lambda-and-helper-inlining.md)
- [GEN-09 Native AOT and trimming validation](../11-source-generator/GEN-09-native-aot-and-trimming-validation.md)

## Whitepaper references

- [§ LINQ query syntax](../../../whitepaper.md#linq-query-syntax)
