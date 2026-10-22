# INT-04: Execute Bind, Expression.Build and Custom

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

Interpreter support for the dependent, operator-precedence and user-scanner nodes.

## Scope

- `Bind` with the once-compiled continuation, and the filter node behind `Where` and query `where` clauses (CORE-10): it fails committed if its source consumed input, and the cursor stays where its source left it ([ADR-0014](../../docs/decisions/0014-a-rejected-filter-reports-at-the-start-of-the-value.md) point 1). Add the overload `Where(predicate, message)`, which throws `GrammarException` for a null or empty message (point 4). ERR-02 records what a rejection reports and where (point 7).
- Precedence climbing for `Expression.Build`, with the semantics of [ADR-0004](../../docs/decisions/0004-expression-build-operator-table-and-semantics.md) points 3 to 5. A chained non-associative operator fails committed here; ERR-02 records its message (ADR-0014 point 7).
- Each operator attempt and each atom takes a light mark, so a cut in one that fails without consuming is dropped ([ADR-0011](../../docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md)). `Named` on an expression needs nothing here: ERR-02 applies [ADR-0008](../../docs/decisions/0008-control-node-semantics.md) point 4 to every node, which replaces the earlier rule in ADR-0004 point 5.
- docs/concepts/expressions.md gains the parsing rules and the behavioural differences from Pidgin (repeated prefix and postfix, `2 ^ -x`, non-associative chains), which CORE-11 left out because nothing ran them. docs/concepts/sequencing.md gains where a rejected filter is reported, the message overload, and when to prefer `Bind` + `Fail` (ADR-0014).
- `Custom` scanners invoked through `IScanner`. What a failed `Custom` records ([ADR-0012](../../docs/decisions/0012-custom-scanner-metadata.md) point 4) is ERR-02's (ADR-0014 point 7).
- Replace the remaining `PlanBuilder.NotYetExecutable` stubs, in `ExpressionNode`, `BindNode`, `FilterNode` and `CustomNode` (CORE-12), and remove `NotYetExecutable`. INT-03 left them, since these nodes are this card's.

## Acceptance criteria

- [ ] Example grammars using all three run and pass their tests.
- [ ] Arithmetic grammar tests cover left and right associativity, `-` as both prefix and infix (`a - -b`), `2 ^ -x` with `-` looser than `^`, repeated prefix and postfix operators, the non-associative chain error (as a committed failure at the second operator; ERR-02 checks its message), and a committed failure after an operator. Moved here from [CORE-11](../02-core-combinators/CORE-11-expressionbuild.md).

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
- [§ The error model](../../../whitepaper.md#the-error-model)

## Artifacts

- [ADR-0014](../../docs/decisions/0014-a-rejected-filter-reports-at-the-start-of-the-value.md) A rejected filter reports at the start of the rejected value, stays committed, and may carry a message (Accepted)

## Progress notes

- 2026-10-22 (Design): Started. Everything but the filter is pinned by ADR-0004, ADR-0008, ADR-0011, ADR-0012 and INT-03's nullable-repetition rule; the card says what a rejected filter reports is decided here, so `/decide` that first. Also settle how the reporting parts (filter, `Custom` expectations, the chain message, `Named` on an expression) relate to ERR-02, since the interpreter records no expectations yet. Then Build.
- 2026-10-22 (Design): Drafted [ADR-0014](../../docs/decisions/0014-a-rejected-filter-reports-at-the-start-of-the-value.md) as Proposed from a prior-art survey (Pidgin, Sprache, Superpower, Parlot, FParsec, Megaparsec, FastParse) and the maintainer's answers: report at the start of the rejected value, committed, with `p`'s own failures dropped; an optional `Where(predicate, message)`; INT-04 runs the nodes and ERR-02 records what they report. Next: the maintainer accepts, rejects or revises; on acceptance, fold it into the whitepaper and the INT-04, ERR-02, ERR-05 and GEN-06 cards, then `/build`.
- 2026-10-22 (Build): The maintainer accepted [ADR-0014](../../docs/decisions/0014-a-rejected-filter-reports-at-the-start-of-the-value.md). Folded into the whitepaper (§ LINQ query syntax, § The error model, § Failure positions) and cards (this card's scope and criterion 2, ERR-02, ERR-05, GEN-06). Moved to Build. Next: `/build`, which plans the increments (`Bind` and the filter with its message overload; `Expression.Build`; `Custom`; removing `NotYetExecutable`) before coding.
- 2026-10-22 (Build): Plan agreed with the maintainer: three increments (`Bind` and the filter; `Expression.Build`; `Custom` and removing `NotYetExecutable`). Criterion 1 is the whitepaper's own examples run end to end: `Blob`, `Calc` and the raw-string lexer. Calls agreed: a prefix operator's operand takes strictly tighter levels; an infix operator and its right operand are one attempt; operators that consume nothing throw `GrammarException` at parse time; `Bind` gets the chosen parser's plan from `PlanCache`.
- 2026-10-22 (Build): Increment 1 of 3 done. New `BindPlan` (the chosen parser's plan comes from `PlanCache`, so a rule field is lowered once; a null parser throws `GrammarException` naming the continuation, whose text `BindNode` now keeps) and `FilterPlan` (a rejection leaves the cursor where its source stopped). `Where` takes an optional `message`, stored on `FilterNode` for ERR-02. Deviation from ADR-0014 point 4, agreed in the plan: the message is an optional parameter, not a second overload, because a compile check showed C# binds `p.Where(pred, "msg")` to the existing overload's `[CallerArgumentExpression]` parameter. So an explicit null means no message; empty or white space throws. Tests: `QueryExecutionTests` (18), covering both nodes' outcomes, `Atomic` around a rejection, `where` clauses, `Blob` over bytes and plan reuse; `QueryNodeTests` gains the message checks. Mutants that rewind on a rejection, or lower the chosen parser without the cache, failed 6 tests and 1 test. Docs: the sequencing.md notice names the ERR-02 gap; the page covers where a rejection is reported, the message, `Where` versus `Bind` + `Fail`, and the Pidgin difference. Verified: build 0 warnings, `dotnet test` 1003 passed, AOT smoke publish clean and the binary prints OK. Next: increment 2, `Expression.Build`.
- 2026-10-22 (Build): Increment 2 of 3 done. `Expression.Build` runs by precedence climbing: `ExpressionPlan` with `PrefixOperatorPlan`, `PostfixOperatorPlan` and `InfixOperatorPlan` behind an abstract `OperatorPlan` that hides `TOp`; `ExpressionOperator` gained an internal abstract `Lower(builder, level)`, and the builder methods are in `PlanBuilder.Expression.cs`. Each prefix attempt, each postfix or infix attempt (operator and right operand together) and the atom take a light mark. A prefix or postfix operator that consumes nothing, or an infix operator and operand that together consume nothing, throws `GrammarException` (`NullableRepetition.Operator` and `InfixOperator`). A chained `InfixN` fails committed after the second operator. Only `CustomNode` still calls `NotYetExecutable`. Tests: `ExpressionExecutionTests` (51 cases), including the criterion 2 table: both associativities, `a - -b`, `2 ^ -x`, `- -x` and `x!!`, prefix before postfix at one level, the chain error (cursor after the second `<`), and committed failures after an operator. It also covers the whitepaper's calculator evaluating, cut handling, juxtaposition and the guards. Mutants that let a prefix's operand take its own level, keep a cut after an operator attempt or atom that failed empty, or skip the chain check failed 1 to 2 tests each. Docs: expressions.md gains the parsing rules, commit behaviour and the three differences from Pidgin; the XML docs of `Expression.Build` and `OperatorFactory.Prefix` were made precise. Found: ADR-0004 point 5 says wrapping an operator in `Atomic` makes `a -` stop before the `-`. It does not under commit semantics, because the operator succeeds and the missing operand after it is outside the `Atomic`. The whitepaper does not make this claim, and the docs leave it out; raised with the maintainer. Verified: build 0 warnings, `dotnet test` 1053 passed, AOT smoke publish clean (the smoke app still runs no plan). Next: increment 3, `Custom` and removing `NotYetExecutable`.
- 2026-10-22 (Build): With the maintainer's approval, corrected ADR-0004 point 5 in place: an operator that should not commit on a missing operand looks ahead for one (`Atomic(Char('-').Before(Lookahead(Atom)))`); wrapping the operator alone in `Atomic` does not help. `AnOperatorThatLooksAheadForAnOperandStopsBeforeAMissingOne` pins the pattern. Next: increment 3.
