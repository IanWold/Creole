# INT-04: Execute Bind, Expression.Build and Custom

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

Interpreter support for the dependent, operator-precedence and user-scanner nodes.

## Scope

- `Bind` with the once-compiled continuation.
- Precedence climbing for `Expression.Build`, with the semantics of [ADR-0004](../../docs/decisions/0004-expression-build-operator-table-and-semantics.md) points 3 to 5.
- `Custom` scanners invoked through `IScanner`.

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
