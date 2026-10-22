# INT-04: Execute Bind, Expression.Build and Custom

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

Interpreter support for the dependent, operator-precedence and user-scanner nodes.

## Scope

- `Bind` with the once-compiled continuation, and the filter node behind `Where` and query `where` clauses (CORE-10): it fails committed if its source consumed input. What a rejected filter reports is decided here.
- Precedence climbing for `Expression.Build`, with the semantics of [ADR-0004](../../docs/decisions/0004-expression-build-operator-table-and-semantics.md) points 3 to 5.
- `Named` on an expression follows [ADR-0008](../../docs/decisions/0008-control-node-semantics.md) point 4: it also applies when the expression succeeds without consuming. ADR-0004 point 5 states the earlier rule. Each operator attempt and each atom takes a light mark, so a cut in one that fails without consuming is dropped ([ADR-0011](../../docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md)).
- docs/concepts/expressions.md gains the parsing rules and the behavioural differences from Pidgin (repeated prefix and postfix, `2 ^ -x`, non-associative chains), which CORE-11 left out because nothing ran them.
- `Custom` scanners invoked through `IScanner`. A failed `Custom` records `Label(Name)` at its start position, or the characters of `First` when it has no name, or nothing when it has neither ([ADR-0012](../../docs/decisions/0012-custom-scanner-metadata.md) point 4).
- Replace the remaining `PlanBuilder.NotYetExecutable` stubs, in `ExpressionNode`, `BindNode`, `FilterNode` and `CustomNode` (CORE-12), and remove `NotYetExecutable`. INT-03 left them, since these nodes are this card's.

## Acceptance criteria

- [ ] Example grammars using all three run and pass their tests.
- [ ] Arithmetic grammar tests cover left and right associativity, `-` as both prefix and infix (`a - -b`), `2 ^ -x` with `-` looser than `^`, repeated prefix and postfix operators, the non-associative chain error, and a committed failure after an operator. Moved here from [CORE-11](../02-core-combinators/CORE-11-expressionbuild.md).

## Blocked by

- [CORE-10 Bind and LINQ query syntax](../02-core-combinators/CORE-10-bind-and-linq-query-syntax.md)
- [CORE-11 Expression.Build](../02-core-combinators/CORE-11-expressionbuild.md)
- [CORE-12 Custom primitives](../02-core-combinators/CORE-12-custom-primitives.md)
- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)

## Unblocks

- [ANA-08 Custom scanner contract checks](../09-static-analysis/ANA-08-custom-scanner-contract-checks.md)
- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)
- [REL-01 Worked-example samples](../14-release/REL-01-worked-example-samples.md)

## Whitepaper references

- [§ Expressions](../../../whitepaper.md#expressions)
- [§ Custom primitives](../../../whitepaper.md#custom-primitives)
- [§ LINQ query syntax](../../../whitepaper.md#linq-query-syntax)
