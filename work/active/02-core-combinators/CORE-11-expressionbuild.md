# CORE-11: Expression.Build

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

An operator-precedence node, so grammars never need left recursion for operators.

## Scope

- `Grammar<TIn>.Expression.Build(atom, table)` and `OperatorFactory<TIn, T>` with `Prefix`, `Postfix`, `InfixL`, `InfixR` and `InfixN`, each taking an operator parser and a combine function ([ADR-0004](../../docs/decisions/0004-expression-build-operator-table-and-semantics.md) point 1).
- Construction-time checks: empty table, empty level, mixed infix associativities at one level (point 2).
- `ExpressionNode<TIn, T>` with `NodeKind.Expression`, its children, and an internal table view of levels, fixities and operator nodes for analysis and export (point 6). `Lower` calls `PlanBuilder.NotYetExecutable` until INT-04.
- XML docs and a docs page stating the semantics of ADR-0004 points 3 to 5, including the differences from Pidgin.
- Each public combinator null-checks its parser arguments by taking its own `[CallerArgumentExpression]` parameter per parser argument and passing it to `Guard.NotNull`, so the `GrammarException` names the caller's expression (see `Grammar.Deterministic`, CORE-02).
- Out: executing the node. Precedence climbing and its parsing tests are INT-04.

## Acceptance criteria

- [ ] Tests show that an arithmetic table builds one `Expression` node whose table view lists every level, fixity and operator in order, that each construction-time check throws `GrammarException`, and that null arguments name the caller's expression.
- [ ] The AOT smoke publish passes with the factory methods in place (ADR-0004, Consequences).

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

## Artifacts

- [ADR-0004: Expression.Build operator table and semantics](../../docs/decisions/0004-expression-build-operator-table-and-semantics.md) (Accepted)

## Progress notes

- 2026-10-18 (Design): Started. The whitepaper gives one paragraph, so decide the operator-table API, precedence-climbing semantics, mixed prefix/infix tokens and non-associative error behaviour in a decision record before building; also fix the acceptance criterion, which needs execution that INT-04 owns.
- 2026-10-18 (Design): Read Pidgin's ExpressionParser, Parlot's associative and unary nodes, and Willis and Wu § 2.3. The maintainer chose: operator parser plus combine function, repeating prefix and postfix, a committed error for non-associative chains, and one result type. Spiked the factory-lambda API on RC 2 (T and TOp inferred). Drafted ADR-0004 (Proposed). Next: maintainer accepts or revises it, then the whitepaper, CORE-11, INT-04 and ANA-01 are updated and the card moves to Building.
- 2026-10-18 (Design): The maintainer accepted ADR-0004. Rewrote § Expressions, rewrote this card's scope and criteria to construction (parsing tests moved to INT-04), and updated INT-04 and ANA-01. Moved to Building. Next: `/build` plan.
