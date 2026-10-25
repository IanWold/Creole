# INT-04: Execute Bind, Expression.Build and Custom

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

Interpreter support for the dependent, operator-precedence and user-scanner nodes.

## Scope

- `Bind` with the once-compiled continuation, and the filter node behind `Where` and query `where` clauses (CORE-10): it fails committed if its source consumed input, and the cursor stays where its source left it ([ADR-0014](../../docs/decisions/0014-a-rejected-filter-reports-at-the-start-of-the-value.md) point 1). Give `Where` an optional `message`, which throws `GrammarException` when it is empty or white space (point 4). ERR-02 records what a rejection reports and where (point 7).
- Precedence climbing for `Expression.Build`, with the semantics of [ADR-0004](../../docs/decisions/0004-expression-build-operator-table-and-semantics.md) points 3 to 5. A chained non-associative operator fails committed here; ERR-02 records its message (ADR-0014 point 7).
- Each operator attempt and each atom takes a light mark, so a cut in one that fails without consuming is dropped ([ADR-0011](../../docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md)). `Named` on an expression needs nothing here: ERR-02 applies [ADR-0008](../../docs/decisions/0008-control-node-semantics.md) point 4 to every node, which replaces the earlier rule in ADR-0004 point 5.
- docs/concepts/expressions.md gains the parsing rules and the behavioural differences from Pidgin (repeated prefix and postfix, `2 ^ -x`, non-associative chains), which CORE-11 left out because nothing ran them. docs/concepts/sequencing.md gains where a rejected filter is reported, the optional message, and when to prefer `Bind` + `Fail` (ADR-0014).
- `Custom` scanners invoked through `IScanner`. What a failed `Custom` records ([ADR-0012](../../docs/decisions/0012-custom-scanner-metadata.md) point 4) is ERR-02's (ADR-0014 point 7).
- Replace the remaining `PlanBuilder.NotYetExecutable` stubs, in `ExpressionNode`, `BindNode`, `FilterNode` and `CustomNode` (CORE-12), and remove `NotYetExecutable`. INT-03 left them, since these nodes are this card's.

## Acceptance criteria

- [x] Example grammars using all three run and pass their tests.
- [x] Arithmetic grammar tests cover left and right associativity, `-` as both prefix and infix (`a - -b`), `2 ^ -x` with `-` looser than `^`, repeated prefix and postfix operators, the non-associative chain error (as a committed failure at the second operator; ERR-02 checks its message), and a committed failure after an operator. Moved here from [CORE-11](../../active/02-core-combinators/CORE-11-expressionbuild.md).

## Blocked by

- [CORE-10 Bind and LINQ query syntax](../../active/02-core-combinators/CORE-10-bind-and-linq-query-syntax.md)
- [CORE-11 Expression.Build](../../active/02-core-combinators/CORE-11-expressionbuild.md)
- [CORE-12 Custom primitives](../../active/02-core-combinators/CORE-12-custom-primitives.md)
- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)

## Unblocks

- [ANA-08 Custom scanner contract checks](../../active/09-static-analysis/ANA-08-custom-scanner-contract-checks.md)
- [GEN-06 Generated layers, modes, recovery and streaming](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)
- [REL-01 Worked-example samples](../../active/14-release/REL-01-worked-example-samples.md)

## Whitepaper references

- [§ Expressions](../../../whitepaper.md#expressions)
- [§ Custom primitives](../../../whitepaper.md#custom-primitives)
- [§ LINQ query syntax](../../../whitepaper.md#linq-query-syntax)
- [§ The error model](../../../whitepaper.md#the-error-model)

## Artifacts

- [ADR-0014](../../docs/decisions/0014-a-rejected-filter-reports-at-the-start-of-the-value.md) A rejected filter reports at the start of the rejected value, stays committed, and may carry a message (Accepted)
- [ADR-0015](../../docs/decisions/0015-a-prefix-operand-takes-only-tighter-levels.md) A prefix operator's operand takes only tighter levels, and an operator looks ahead to avoid committing on a missing operand (Accepted)

## Progress notes

- 2026-10-22 (Design): Started. Everything but the filter is pinned by ADR-0004, ADR-0008, ADR-0011, ADR-0012 and INT-03's nullable-repetition rule; the card says what a rejected filter reports is decided here, so `/decide` that first. Also settle how the reporting parts (filter, `Custom` expectations, the chain message, `Named` on an expression) relate to ERR-02, since the interpreter records no expectations yet. Then Build.
- 2026-10-22 (Design): Drafted [ADR-0014](../../docs/decisions/0014-a-rejected-filter-reports-at-the-start-of-the-value.md) as Proposed from a prior-art survey (Pidgin, Sprache, Superpower, Parlot, FParsec, Megaparsec, FastParse) and the maintainer's answers: report at the start of the rejected value, committed, with `p`'s own failures dropped; an optional `Where(predicate, message)`; INT-04 runs the nodes and ERR-02 records what they report. Next: the maintainer accepts, rejects or revises; on acceptance, fold it into the whitepaper and the INT-04, ERR-02, ERR-05 and GEN-06 cards, then `/build`.
- 2026-10-22 (Build): The maintainer accepted [ADR-0014](../../docs/decisions/0014-a-rejected-filter-reports-at-the-start-of-the-value.md). Folded into the whitepaper (§ LINQ query syntax, § The error model, § Failure positions) and cards (this card's scope and criterion 2, ERR-02, ERR-05, GEN-06). Moved to Build. Next: `/build`, which plans the increments (`Bind` and the filter with its message overload; `Expression.Build`; `Custom`; removing `NotYetExecutable`) before coding.
- 2026-10-22 (Build): Plan agreed with the maintainer: three increments (`Bind` and the filter; `Expression.Build`; `Custom` and removing `NotYetExecutable`). Criterion 1 is the whitepaper's own examples run end to end: `Blob`, `Calc` and the raw-string lexer. Calls agreed: a prefix operator's operand takes strictly tighter levels; an infix operator and its right operand are one attempt; operators that consume nothing throw `GrammarException` at parse time; `Bind` gets the chosen parser's plan from `PlanCache`.
- 2026-10-22 (Build): Increment 1 of 3 done. New `BindPlan` (the chosen parser's plan comes from `PlanCache`, so a rule field is lowered once; a null parser throws `GrammarException` naming the continuation, whose text `BindNode` now keeps) and `FilterPlan` (a rejection leaves the cursor where its source stopped). `Where` takes an optional `message`, stored on `FilterNode` for ERR-02. Deviation from ADR-0014 point 4, agreed in the plan: the message is an optional parameter, not a second overload, because a compile check showed C# binds `p.Where(pred, "msg")` to the existing overload's `[CallerArgumentExpression]` parameter. So an explicit null means no message; empty or white space throws. Tests: `QueryExecutionTests` (18), covering both nodes' outcomes, `Atomic` around a rejection, `where` clauses, `Blob` over bytes and plan reuse; `QueryNodeTests` gains the message checks. Mutants that rewind on a rejection, or lower the chosen parser without the cache, failed 6 tests and 1 test. Docs: the sequencing.md notice names the ERR-02 gap; the page covers where a rejection is reported, the message, `Where` versus `Bind` + `Fail`, and the Pidgin difference. Verified: build 0 warnings, `dotnet test` 1003 passed, AOT smoke publish clean and the binary prints OK. Next: increment 2, `Expression.Build`.
- 2026-10-22 (Build): Increment 2 of 3 done. `Expression.Build` runs by precedence climbing: `ExpressionPlan` with `PrefixOperatorPlan`, `PostfixOperatorPlan` and `InfixOperatorPlan` behind an abstract `OperatorPlan` that hides `TOp`; `ExpressionOperator` gained an internal abstract `Lower(builder, level)`, and the builder methods are in `PlanBuilder.Expression.cs`. Each prefix attempt, each postfix or infix attempt (operator and right operand together) and the atom take a light mark. A prefix or postfix operator that consumes nothing, or an infix operator and operand that together consume nothing, throws `GrammarException` (`NullableRepetition.Operator` and `InfixOperator`). A chained `InfixN` fails committed after the second operator. Only `CustomNode` still calls `NotYetExecutable`. Tests: `ExpressionExecutionTests` (51 cases), including the criterion 2 table: both associativities, `a - -b`, `2 ^ -x`, `- -x` and `x!!`, prefix before postfix at one level, the chain error (cursor after the second `<`), and committed failures after an operator. It also covers the whitepaper's calculator evaluating, cut handling, juxtaposition and the guards. Mutants that let a prefix's operand take its own level, keep a cut after an operator attempt or atom that failed empty, or skip the chain check failed 1 to 2 tests each. Docs: expressions.md gains the parsing rules, commit behaviour and the three differences from Pidgin; the XML docs of `Expression.Build` and `OperatorFactory.Prefix` were made precise. Found: ADR-0004 point 5 says wrapping an operator in `Atomic` makes `a -` stop before the `-`. It does not under commit semantics, because the operator succeeds and the missing operand after it is outside the `Atomic`. The whitepaper does not make this claim, and the docs leave it out; raised with the maintainer. Verified: build 0 warnings, `dotnet test` 1053 passed, AOT smoke publish clean (the smoke app still runs no plan). Next: increment 3, `Custom` and removing `NotYetExecutable`.
- 2026-10-22 (Build): With the maintainer's approval, corrected ADR-0004 point 5 in place: an operator that should not commit on a missing operand looks ahead for one (`Atomic(Char('-').Before(Lookahead(Atom)))`); wrapping the operator alone in `Atomic` does not help. `AnOperatorThatLooksAheadForAnOperandStopsBeforeAMissingOne` pins the pattern. Next: increment 3.
- 2026-10-22 (Build): Increment 3 of 3 done. `CustomPlan` runs a scanner through `IScanner.Scan` and trusts it to fail without consuming (ADR-0012); a scanner that fails after moving the cursor fails committed. `PlanBuilder.NotYetExecutable` is removed; no code refers to it. Tests: `RawStringSampleTests` runs the docs' raw-string lexer (`Custom` inside `Longest`, then `Bind` and `Fail`) on the interpreter, including an unterminated string failing committed and input in segments of 1, 2, 3 and 5 items; new `CustomExecutionTests` covers a scanner over `int` items (no `First`): its value, failing without consuming, an enclosing choice, and repetition. Docs: the custom-scanners.md notice names the ERR-02 gap. Verified: build 0 warnings, `dotnet test` 1069 passed, AOT smoke publish clean. Evidence for criterion 1: `Blob` (`QueryExecutionTests.BlobReadsALengthThenThatManyBytes`), the whitepaper's calculator (`ExpressionExecutionTests.TheWhitepapersCalculatorEvaluates`) and the raw-string lexer (`RawStringSampleTests.TheLexerReadsATokenOnTheInterpreter`, `TheLexerFailsCommittedAtAnUnterminatedString`, `TheLexerRunsAcrossSegments`). Evidence for criterion 2: the table in `ExpressionExecutionTests` (increment 2). Every criterion appears met. Open for `/complete`: CLAUDE.md's opening paragraph still lists INT-04 as not built. Next: `/complete`.
- 2026-10-22 (Design): Moved back from Build for `/complete`. Both criteria verified met (`dotnet test` 1069 passed; the four test classes behind them, 102 tests). Two Build choices differed from accepted records. (1) `Where`'s optional message: ADR-0014 point 4 was edited in place with the maintainer's approval, since it is not on `main`; this card's scope now says the same. (2) A prefix operator's operand takes strictly tighter levels. ADR-0004 point 3 says "at that operator's level (so a looser operator cannot be its operand)", and the whitepaper says "parsed at its own level". Both conflict with their own rule that `-x!` is `(-x)!` at one level. ADR-0004 cannot be edited, so this needs a new record. The in-place edit of ADR-0004 point 5 in 15b2131 predates that ruling; raised with the maintainer. Next: `/decide` on the prefix operand rule, then finish `/complete`.
- 2026-10-22 (Design): The maintainer chose to revert the in-place edit of ADR-0004 point 5 (ADR-0004 is on `main` and is not edited) and to correct points 3 and 5 in a new record. ADR-0004 is restored to its text before 15b2131. Drafted [ADR-0015](../../docs/decisions/0015-a-prefix-operand-takes-only-tighter-levels.md) as Proposed: a prefix operand takes only tighter levels (as built), and an operator looks ahead to avoid committing on a missing operand. Next: on acceptance, the whitepaper's § Expressions wording, an index note, the same-level infix test (`-a*b` is `((-a)*b)`), GEN-06's scope, then finish `/complete`.
- 2026-10-22 (Design): The maintainer accepted [ADR-0015](../../docs/decisions/0015-a-prefix-operand-takes-only-tighter-levels.md). Folded into the whitepaper (§ Expressions, Operands), a note in the decisions index, and GEN-06's scope. `APrefixAtTheSameLevelAsAnInfixOperatorAppliesFirst` pins `-a*b` as `((-a)*b)`; a mutant whose prefix operand includes its own level fails it and the postfix test. Verified: build 0 warnings, `dotnet test` 1070 passed, AOT smoke publish clean. Completed.

## Outcome

Delivered interpreter execution of `Bind`, the filter behind `Where`, `Expression.Build` and `Custom`. No node lowers to a stub any more, and `PlanBuilder.NotYetExecutable` is removed.

| Path | Contents |
| --- | --- |
| `src/Creole/Plans/BindPlan.cs`; `Nodes/BindNode.cs` | Runs the source, then the parser the continuation chooses. That parser's plan comes from `PlanCache`, so a rule field is lowered once and a parser built afresh per match is lowered per match. A null parser throws `GrammarException` naming the continuation, whose text `BindNode` keeps. |
| `src/Creole/Plans/FilterPlan.cs`; `Nodes/FilterNode.cs`; `Parser.Query.cs` | A rejection leaves the cursor where the source stopped, so it is committed if the source consumed input. `Where` gains an optional `message`, kept on `FilterNode` for ERR-02; an empty or white-space message throws. |
| `src/Creole/Plans/ExpressionPlan.cs`, `OperatorPlan.cs`, `PrefixOperatorPlan.cs`, `PostfixOperatorPlan.cs`, `InfixOperatorPlan.cs`, `PlanBuilder.Expression.cs`; `ExpressionOperator.cs` | Precedence climbing (ADR-0004, ADR-0015). `ExpressionOperator` gained an internal abstract `Lower(builder, level)`, and `OperatorPlan` hides each operator's `TOp`. Each prefix attempt, each postfix or infix attempt (operator and right operand together) and the atom take a light mark. A chained `InfixN` fails committed after the second operator. |
| `src/Creole/Plans/NullableRepetition.cs` | `Operator()` and `InfixOperator()`: a prefix or postfix operator that consumes nothing, or an infix operator and operand that together consume nothing, throw `GrammarException` during the parse. |
| `src/Creole/Plans/CustomPlan.cs` | Runs `IScanner.Scan`, trusting the scanner to fail without consuming (ADR-0012). |
| `tests/Creole.Tests/` | `QueryExecutionTests` (18); message checks in `QueryNodeTests` (3); `ExpressionExecutionTests` (52); interpreter runs of the raw-string lexer in `RawStringSampleTests` (9); `CustomExecutionTests` (6). |
| `docs/concepts/` | sequencing.md: `Bind` at run time, where a rejection is reported, the message, and `Where` versus `Bind` + `Fail`. expressions.md: how an expression is parsed, commit, operators that consume nothing, and the differences from Pidgin. custom-scanners.md: the notice. Each notice names the ERR-02 gap. |

Evidence (2026-10-22, on fbd212c plus the completion change): `dotnet build` 0 warnings; `dotnet test` 1070 passed, 0 failed; AOT smoke publish clean, and the binary prints OK (it runs no plan until INT-08).

1. Example grammars: `Blob` (`QueryExecutionTests.BlobReadsALengthThenThatManyBytes`); the whitepaper's calculator (`ExpressionExecutionTests.TheWhitepapersCalculatorEvaluates`, 11 inputs); the raw-string lexer, using `Custom` inside `Longest` and then `Bind` and `Fail` (`RawStringSampleTests.TheLexerReadsATokenOnTheInterpreter`, `TheLexerFailsCommittedAtAnUnterminatedString`, `TheLexerRunsAcrossSegments`).
2. Arithmetic: `ExpressionExecutionTests.OperatorsGroupByLevelAndAssociativity` (22 rows: `a+b+c`, `a^b^c`, `a--b`, `2^-x` with prefix `-` looser than `^`, `--x`, `x!!`), the chain error as a committed failure with the cursor after the second `<`, and `a+` and four other incomplete inputs failing committed.

Mutation checks failed tests for each of these mutants:

- rewinding on a filter rejection;
- lowering `Bind`'s parser without the cache;
- a prefix operand that includes its own level;
- keeping a cut after a failed operator attempt or atom;
- skipping the chain check.

Deviations and decisions:

- [ADR-0014](../../docs/decisions/0014-a-rejected-filter-reports-at-the-start-of-the-value.md) (Accepted): a rejected filter is reported at the start of the rejected value, stays committed, and may carry a message. Point 4 was edited in place before reaching `main`, with the maintainer's approval: the message is an optional parameter, because a second overload would bind `p.Where(pred, "msg")` to the caller-expression parameter.
- [ADR-0015](../../docs/decisions/0015-a-prefix-operand-takes-only-tighter-levels.md) (Accepted): a prefix operand takes only tighter levels, and an operator looks ahead to avoid committing on a missing operand. It refines ADR-0004 points 3 and 5, which contradicted their own example and commit semantics. ADR-0004 is unedited, and an in-place edit made during the build was reverted.
- Calls made in Build: `Bind` gets plans through `PlanCache`; an infix attempt is the operator and its right operand, so an empty juxtaposition operator works; operators that consume nothing throw at parse time, following INT-03's rule.

Follow-ups:

- [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md) records the filter's report, the chain message, `Custom`'s expectations and `Named` on expressions (ADR-0014 point 7).
- [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md) repairs from the cursor.
- [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md) emits the same behaviour.
- [INT-07](INT-07-recursion-depth-limit.md) bounds expression recursion.
- [INT-08](INT-08-single-layer-parse-api.md) lets the AOT smoke app run a plan.

