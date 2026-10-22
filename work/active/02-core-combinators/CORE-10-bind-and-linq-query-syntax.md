# CORE-10: Bind and LINQ query syntax

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

Query syntax stays first-class. Each `from` clause is classified as an analyzable sequence step or a genuine bind.

## Scope

- `Bind` node (opaque to analysis).
- `SelectMany` taking `Expression<Func<A, Parser<TIn, B>>>`.
- Per-clause classification at construction: if the parameter is unused, evaluate once and build `Seq`; if used, compile once and build `Bind`.
- Handle transparent identifiers in queries with three or more clauses.
- `where` as a filter that fails committed after consuming; `let` as a map.
- Verify behavior under Native AOT (expressions are interpreted) and trimming.
- Each public combinator null-checks its parser arguments by taking its own `[CallerArgumentExpression]` parameter per parser argument and passing it to `Guard.NotNull`, so the `GrammarException` names the caller's expression (see `Grammar.Deterministic`, CORE-02).

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

## Progress notes

- 2026-10-22 (Build): Started. `Bind` node plus expression-taking `SelectMany`, classifying each clause once at construction into `SequenceNode` or `Bind`; `where` and `let` on top.
- 2026-10-22 (Build): Increment 1 done. `BindNode` and `FilterNode` (new `NodeKind.Bind` and `NodeKind.Filter`); `Parser.SelectMany` taking `Expression<Func<…>>`, classified once with an `ExpressionVisitor`; fluent `Bind(Func)` and `Where`; `let` uses `Select`. Queries of three or more clauses build nested two-step sequences, not flattened. `QueryNodeTests` (13) cover criteria 1 and 2; 731 tests pass. Docs: query syntax section in docs/concepts/sequencing.md. Whitepaper gained one cost bullet (AOT bind clauses run interpreted); INT-04 scope gained the filter node. Next: increment 2, a query grammar in the AOT smoke app (criterion 3).
- 2026-10-22 (Build): Increment 2 done. `tests/Creole.AotSmoke` gained a `Queries` grammar: one query whose clauses are all independent, with `where` and `let`, and one whose last clause is a bind. Its static initializer runs the classification, the one-time interpreted evaluation and the bind's `Compile()` in the Native AOT binary. `dotnet publish tests/Creole.AotSmoke -c Release` reports no warnings, and the binary prints "Creole AOT smoke: OK" and exits 0. All three criteria appear met; next: `/complete`.
