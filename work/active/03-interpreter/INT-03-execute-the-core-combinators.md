# INT-03: Execute the core combinators

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

The interpreter runs every analyzable node family with Parsec-style commit semantics. This is the reference semantics for Creole.

## Scope

- Four outcomes as `bool` plus cursor movement.
- `Or` and `OneOf` commit; `Atomic` rewinds; `Lookahead`, `Not`; `Cut` overrides an enclosing `Atomic` up to the nearest `Lookahead`, `Not` or `Longest` alternative, and is dropped when the attempt that passed it fails without consuming. Each light mark (every `Or` alternative including the last, `Optional`, every repetition item including mandatory ones, separators, terminators) saves the cut flag and restores it on that attempt's empty failure ([ADR-0011](../../docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md)). Compare against CORE-08's reference model.
- `Longest` by running each alternative atomically (the DFA path is INT-06).
- CORE-03's primitives fail without consuming input. `String` and `CIString` are atomic, and `CIString` compares by `ToLowerInvariant`. Test both.
- Repetition, sequencing, `Switch` and `Match`.
- Run-time guard against nullable repetition until analysis exists.
- Replace every `PlanBuilder.NotYetExecutable` stub with a real plan, except those of `Expression.Build` and of CORE-10's `Bind` and filter nodes, which [INT-04](INT-04-execute-bind-expressionbuild-and-custom.md) executes. INT-02 added the stubs so nodes merged before INT-03 compile against the abstract `Lower` member.

## Acceptance criteria

- [x] A semantic test suite covering the four-outcome table, including property tests, passes.
- [x] The grammar classes in Worked Example A compile against the API, apart from `Layer.Tokens` (LAY-03). Moved here from CORE-02.
- [x] The interpreter passes every row of the CORE-07 repetition table, and handles the rows where Pidgin throws on a repeated parser that consumes nothing. Moved here from [CORE-07](../02-core-combinators/CORE-07-repetition-nodes.md).
- [x] The interpreter passes every row of the CORE-06 `Longest` table: `<=` beats `<`, `iffy` beats `if`, and ties go to the earliest alternative. Moved here from [CORE-06](../02-core-combinators/CORE-06-longest.md).
- [x] The interpreter passes every row of the CORE-08 control table, including `Cut` inside nested `Atomic`s and a cut dropped with an abandoned alternative. Moved here from [CORE-08](../02-core-combinators/CORE-08-control-nodes.md).
- [x] A plan lowered from a grammar with `Rec` cycles parses recursive input. Moved here from [CORE-09](../02-core-combinators/CORE-09-recursion-with-rec.md).

## Blocked by

- [CORE-03 Primitive nodes](../02-core-combinators/CORE-03-primitive-nodes.md)
- [CORE-04 Sequencing and mapping nodes](../02-core-combinators/CORE-04-sequencing-and-mapping-nodes.md)
- [CORE-05 Choice nodes](../02-core-combinators/CORE-05-choice-nodes.md)
- [CORE-06 Longest](../02-core-combinators/CORE-06-longest.md)
- [CORE-07 Repetition nodes](../02-core-combinators/CORE-07-repetition-nodes.md)
- [CORE-08 Control nodes](../02-core-combinators/CORE-08-control-nodes.md)
- [CORE-09 Recursion with Rec](../02-core-combinators/CORE-09-recursion-with-rec.md)
- [INT-02 Plan lowering and cache](../03-interpreter/INT-02-plan-lowering-and-cache.md)

## Unblocks

- [INT-04 Execute Bind, Expression.Build and Custom](../03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md)
- [INT-05 Interpreter lowering rewrites](../03-interpreter/INT-05-interpreter-lowering-rewrites.md)
- [INT-06 DFA compiler for regular subtrees](../03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md)
- [INT-07 Recursion depth limit](../03-interpreter/INT-07-recursion-depth-limit.md)
- [INT-08 Single-layer Parse API](../03-interpreter/INT-08-single-layer-parse-api.md)
- [ERR-02 Furthest-failure tracking and labels](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)
- [LAY-02 Marks and retention](../06-layers-and-pipelines/LAY-02-marks-and-retention.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)
- [GEN-02 Emit the core combinators](../11-source-generator/GEN-02-emit-the-core-combinators.md)
- [TOOL-02 Tracing](../12-grammar-author-tooling/TOOL-02-tracing.md)

## Whitepaper references

- [§ Combinator semantics](../../../whitepaper.md#combinator-semantics)
- [§ Results](../../../whitepaper.md#results)
- [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking)

## Artifacts

- [ADR-0013](../../docs/decisions/0013-slices-view-in-memory-input-and-copy-streamed-input.md) Slices are views of in-memory input and copies of streamed input (Accepted)

## Progress notes

- 2026-10-22 (Build): Started. Replace every `PlanBuilder.NotYetExecutable` stub with a real plan node, node family by node family, and test each against the CORE-06, CORE-07 and CORE-08 tables and the four-outcome model.
- 2026-10-22 (Build): Plan agreed with the maintainer: four increments (primitives and sequencing; repetition; choice and control; `Rec`, Worked Example A and cleanup). Calls agreed: the cut flag is an internal `Cursor<T>.Cut`; the nullable-repetition guard throws `GrammarException` at parse time for unbounded repetition only; `Slice` is a view inside one memory-backed segment and a copy otherwise; `Named` and `Context` lower to their item's plan until ERR-02; `Switch` scans keys linearly; `MatchCase` gains an internal lowering member. `Expression.Build` stays a stub for INT-04, so the scope line "replace every stub" is corrected in increment 4.
- 2026-10-22 (Build): Increment 1 of 4 done. `Cursor<T>` gained internal `Cut`, `FromMemory` and `Since(start)`. New plans in `src/Creole/Plans/`: `ItemPlan` (`Any`, `Token`, `Satisfy`, character classes, `Kind`), `CasePlan`, `EndPlan`, `StringPlan` (atomic; `CIString` by `ToLowerInvariant`), `MapPlan`, `SequencePlan` (2 to 8), `OptionalPlan`, `SlicePlan`, `TextPlan`, and `LightMark`, which restores the cut flag on an attempt's empty failure. `PlanBuilder` is partial, with `.Primitives`, `.Sequencing` and `.Control` files. Tests: `Execution` helper, `PrimitiveExecutionTests` (with CsCheck properties for `String`, `CIString` and single-item primitives), `SequencingExecutionTests`, test-only `CutThenNode`. Mutants that drop `StringPlan`'s rewind or `LightMark`'s cut restore failed 7 tests. Verified: build clean, `dotnet test` 783 passed, AOT smoke publish clean. Docs: primitives.md and sequencing.md banners and the slice copy rule. Open: a slice that views a pooled stream-buffer segment would dangle once LAY-01 releases it, raised with the maintainer. Next: increment 2, repetition.
- 2026-10-22 (Build): Increment 2 of 4 done. Repetition runs: `RepeatPlan`, `SkipRepeatPlan`, `SeparatedPlan` and `UntilPlan`, each taking a `LightMark` per item, separator and terminator, wired through `PlanBuilder.Repetition.cs`. `NullableRepetition` throws `GrammarException` when an unbounded repetition's round consumes nothing; bounded `Repeat` does not check. `GrammarException`'s remarks now say it can be thrown at first use or during a parse. Tests: `RepetitionTableTests` runs Creole against all 52 rows of `RepetitionTable`, including the four where Pidgin throws (the evidence for criterion 3); `RepetitionExecutionTests` covers the guard and which attempts keep or drop a cut; `Execution` reports a `GrammarException` as `Outcome.Throws`. Mutants that make a separator always need an item after it, or keep an `Until` item's cut, failed 3 tests. Verified: build clean, `dotnet test` 846 passed, AOT smoke publish clean. Docs: repetition.md gains how repetition stops and items that consume nothing. Next: increment 3, choice and control.
- 2026-10-22 (Build): Increment 3 of 4 done. Choice and control run: `OrPlan` (a light mark per alternative), `AtomicPlan`, `LookaheadPlan`, `NotPlan`, `CutPlan`, `LongestPlan` (each alternative atomic from the same start, ties to the earliest), `SwitchPlan` (linear key scan) and `MatchPlan`. `MatchCase` gained an internal abstract `Lower`, and `TypedMatchCase` lowers to `TypedMatchCasePlan`. Builder methods are in `PlanBuilder.Choice.cs` and `PlanBuilder.Control.cs`. Only `ExpressionNode` still calls `NotYetExecutable` (INT-04). Tests: `ControlTableTests` and `LongestTableTests` run the interpreter on every row (evidence for criteria 5 and 4); `ControlModelExecutionTests` compares outcome and cut-in-force with `ControlModel.InOrder` on every shape to depth 3 over every input up to length 3 (over 30,000 grammars), and on 20,000 random shapes to depth 5 with CsCheck; `ChoiceExecutionTests` covers `Or`, `OneOf`, `Switch` and `Match`. `ControlModel.RepeatsNullable` became internal for the generator. Mutants that make `Atomic` drop an inner cut on exit or let a cut leave `Not` failed 4 to 5 tests each. A mutant that stops `Longest` clearing the cut per alternative passed, and is equivalent: only `Atomic` reads the flag and it clears it on entry, and `Longest` restores the outer flag on exit. Verified: build clean, `dotnet test` 926 passed, AOT smoke publish clean. Docs: choice.md and control.md banners (control.md notes that labels and context wait for ERR-02). Next: increment 4, `Rec` execution, Worked Example A, the INT-03 and INT-04 scope fix for `Expression.Build`, and criterion 1's evidence summary.
- 2026-10-22 (Build): Increment 4 of 4 done. `RecTests` runs lowered `Rec` cycles on recursive input: a self-cycle (`Lists`), a cycle between two rules (`Parens`, including 500 levels of nesting) and the token-level JSON grammar (evidence for criterion 6). `WorkedExampleATests.cs` holds Worked Example A's grammar classes as the whitepaper writes them, with four recorded changes (file-local types, `private` where IDE0040 requires it, the lexer's item and skip as fields in place of `Layer.Tokens`, and `!` on the later rules inside `Value`'s `Rec` lambda for CS8604). It lexes and parses JSON end to end (evidence for criterion 2). Scope fix agreed in the plan: this card's scope now excludes `Expression.Build`'s stub, and INT-04's scope gains replacing it and removing `NotYetExecutable`, whose doc comment says so. Verified: `dotnet build` 0 warnings, `dotnet test` 937 passed, AOT smoke publish clean; `grep NotYetExecutable src` finds only `ExpressionNode` and the method itself. Evidence for criterion 1: the four outcomes are checked per family (`PrimitiveExecutionTests`, `SequencingExecutionTests`, `RepetitionExecutionTests`, `ChoiceExecutionTests`), with CsCheck properties in `PrimitiveExecutionTests` and `ControlModelExecutionTests`, which compares outcome and cut against `ControlModel` exhaustively to depth 3 and on 20,000 random shapes to depth 5. Every criterion appears met. Open for the maintainer: slices of pooled stream-buffer segments (raised in increment 1) and CLAUDE.md's "the interpreter (INT-03) is not" built line, for `/complete`. Next: `/complete`.
- 2026-10-22 (Design): Moved back from Build for `/complete`, which an independent critic review ran. All six criteria verified met (937 tests; the critic added 13 adversarial cases, all correct). Blocking: `Slice` copies in cases § Text capture says it does not, with no decision, and views into LAY-01's pooled segments would dangle. The maintainer chose option B (view over in-memory input, copy over streamed input); drafted [ADR-0013](../../docs/decisions/0013-slices-view-in-memory-input-and-copy-streamed-input.md) as Proposed. Also merged origin/main (CORE-10), which adds `NotYetExecutable` stubs in `BindNode` and `FilterNode`. Next: on acceptance, fold ADR-0013 into the whitepaper and cards, then a Build change: make `Cursor.Since` copy for every cursor not built over caller-owned memory, fix the three scope texts the merge made false, replace `RecLoweringTests`' `AltNode` cycles (CORE-09 follow-up), add the guard to the repetition XML docs, fix sequencing.md and the `WorkedExampleATests` header, and extend RSLN-04's criterion; then `/complete`.
- 2026-10-22 (Build): The maintainer accepted [ADR-0013](../../docs/decisions/0013-slices-view-in-memory-input-and-copy-streamed-input.md) (the draft was renumbered from 0012, which a concurrent change uses). Folded into the whitepaper (§ Text capture, § The stream buffer, § Design choices) and cards (INT-08, LAY-05, LAY-01 scope and a new criterion, CORE-12). Back to Build. Next: the follow-up Build change listed in the previous note, then `/complete`.
- 2026-10-22 (Build): Follow-up increment done. `Cursor.Since` returns a view only for a `FromMemory` cursor and copies for every other, per ADR-0013; `SliceOverASourceKeepsItsTextWhenTheSourceMemoryIsReused` pins it (a mutant that views source segments fails it). `RecLoweringTests` builds its cycles from real `Or` nodes wrapped in `CountingNode` (the CORE-09 follow-up); `OrPlan` exposes `Alternatives` for that, and `AltNode`/`AltPlan` are gone from `TestNodes.cs`. Texts made false by merging CORE-10 now name `BindNode` and `FilterNode` too: this card's scope, INT-04's scope and the `NotYetExecutable` doc comment. The repetition XML docs state the parse-time `GrammarException`. sequencing.md states the ADR-0013 rule, and its banner excludes `Bind` and `where`, which run from INT-04. RSLN-04's criterion also covers the `!` in `WorkedExampleATests` (the header comment there was removed by the maintainer in 843e644). Verified: build clean, `dotnet test` 951 passed, AOT smoke publish clean. Next: `/complete`.

## Outcome

Delivered the interpreter's reference semantics: every analyzable node family runs with Parsec-style commit semantics and ADR-0011's cut rule, except `Expression.Build`, `Bind` and the filter node, which INT-04 executes.

| Path | Contents |
| --- | --- |
| `src/Creole/Cursor.cs` | Internal `Cut` (one cut flag per layer), `FromMemory` and `Since(start)` for text capture: a view only over caller-owned memory, otherwise a copy (ADR-0013). |
| `src/Creole/Plans/LightMark.cs` | The light mark: saves position and cut flag before an attempt, restores the flag on the attempt's empty failure. |
| `src/Creole/Plans/` primitive plans | `ItemPlan` (`Any`, `Token`, `Satisfy`, character classes, `Kind`), `CasePlan`, `EndPlan`, `StringPlan` (atomic; `CIString` by `ToLowerInvariant`). |
| `src/Creole/Plans/` sequencing plans | `MapPlan`, `SequencePlan` (2 to 8), `OptionalPlan`, `SlicePlan`, `TextPlan`. `Named` and `Context` lower to their item's plan until ERR-02. |
| `src/Creole/Plans/` repetition plans | `RepeatPlan`, `SkipRepeatPlan`, `SeparatedPlan`, `UntilPlan`; `NullableRepetition` throws `GrammarException` when an unbounded round consumes nothing. |
| `src/Creole/Plans/` choice and control plans | `OrPlan`, `AtomicPlan`, `LookaheadPlan`, `NotPlan`, `CutPlan`, `LongestPlan`, `SwitchPlan`, `MatchPlan`, `MatchCasePlan` and `TypedMatchCasePlan`; `MatchCase` gained an internal abstract `Lower`. |
| `src/Creole/Plans/PlanBuilder.*.cs` | `PlanBuilder` is partial, with `Primitives`, `Sequencing`, `Repetition`, `Choice` and `Control` files. |
| `tests/Creole.Tests/` | `Execution` helper; `PrimitiveExecutionTests`, `SequencingExecutionTests`, `RepetitionExecutionTests`, `ChoiceExecutionTests`; `RepetitionTableTests`, and interpreter theories in `ControlTableTests` and `LongestTableTests`; `ControlModelExecutionTests`; execution tests in `RecTests`; `WorkedExampleATests`; `RecLoweringTests` on real `Or` cycles. |
| `docs/concepts/` | primitives.md, sequencing.md, repetition.md, choice.md and control.md describe the executed behaviour; repetition.md gains how repetition stops and the nullable-item guard. |

Evidence (2026-10-22, commit 7c9a9e8): `dotnet build` 0 warnings; `dotnet test` 951 passed, 0 failed; AOT smoke publish clean (the smoke app runs no plan until INT-08's `Parse`).

1. Four-outcome suite with property tests: per-family execution tests, CsCheck properties in `PrimitiveExecutionTests` (3) and `ControlModelExecutionTests`, which matches outcome and cut-in-force with `ControlModel` on every shape to depth 3 over every input up to length 3 (over 30,000 grammars) and on 20,000 random shapes to depth 5.
2. Worked Example A: `WorkedExampleATests` (7 tests) compiles the grammar classes and lexes and parses JSON end to end. Changes from the whitepaper text: file-local types, `private` and `_camelCase` names for private members as the repository's style requires, the lexer's item and skip as fields in place of `Layer.Tokens` (LAY-03), and `!` on `_arrayValue` and `_objectValue` inside `Value`'s `Rec` lambda for CS8604, which [RSLN-04](../10-roslyn-analyzers/RSLN-04-crl0004-forward-field-reference-analyzer.md) will remove.
3. Repetition table: `RepetitionTableTests` 52 of 52, including the 5 rows where Pidgin throws; Creole throws `GrammarException` there.
4. `Longest` table: `LongestTableTests` 11 of 11 rows.
5. Control table: `ControlTableTests` 56 of 56 rows.
6. `Rec` cycles: `APlanWithACycleParsesRecursiveInput`, `APlanWithACycleBetweenTwoRulesParsesRecursiveInput` (with 500 levels in `APlanWithACycleParsesDeeplyNestedInput`) and `ExampleAParsesNestedTokens`.

Mutation checks: mutants that drop `StringPlan`'s rewind, `LightMark`'s cut restore, a separator's need for an item only when it consumed, an `Until` item's cut drop, `Atomic`'s cut merge, `Not`'s cut restore, or `Since`'s copy over a source each failed tests. A mutant that stops `Longest` clearing the cut per alternative passed and is equivalent. An independent critic review verified all six criteria and added 13 adversarial cases, all correct.

Deviations and decisions:

- [ADR-0013](../../docs/decisions/0013-slices-view-in-memory-input-and-copy-streamed-input.md) (Accepted): `Slice` is a view of in-memory input and a copy of streamed input, replacing § Text capture's unconditional "without copying"; whitepaper, INT-08, LAY-01, LAY-05 and CORE-12 updated.
- Scope agreed with the maintainer: `Expression.Build` and CORE-10's `Bind` and filter stay `NotYetExecutable` for [INT-04](INT-04-execute-bind-expressionbuild-and-custom.md), whose scope now replaces them and removes the method.
- Calls made in Build: the cut flag is an internal `Cursor<T>.Cut`, so plan and staged-code signatures are unchanged; `Switch` scans keys linearly (faster dispatch is [INT-05](INT-05-interpreter-lowering-rewrites.md)); `Longest` runs each alternative in turn until [INT-06](INT-06-dfa-compiler-for-regular-subtrees.md); `GrammarException` may now be thrown during a parse, which its remarks state.

Follow-ups: [ERR-02](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md) gives `Named`, `Context` and `Fail` their reporting; [INT-08](INT-08-single-layer-parse-api.md) builds in-memory cursors with `FromMemory`; [LAY-01](../06-layers-and-pipelines/LAY-01-streambuffer-and-pulling-cursor.md) gains a criterion that slices survive segment reuse; [RSLN-04](../10-roslyn-analyzers/RSLN-04-crl0004-forward-field-reference-analyzer.md) also removes the `!` in `WorkedExampleATests`.
