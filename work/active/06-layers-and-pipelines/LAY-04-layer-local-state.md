# LAY-04: Layer-local state

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Done |

## Summary

Layers decide from their own history with immutable state that backtracking restores automatically.

## Scope

- `GetState<S>()`, `SetState<S>(s)` and `p.Scan<S, R>(...)`.
- `Layer.Tokens(initialState, item, skip, name)`, with no form that takes `initialState` and `item` alone ([ADR-0037](../../docs/decisions/0037-lexers-take-an-initial-state-parsers-set-theirs-and-a-mistyped-read-throws.md) point 2). `item` and `skip` share the state.
- Every layer that runs a grammar, and every parser, has one state. It starts as the layer's `initialState`, or as no state. A parser sets its own with `SetState` at its root (ADR-0037 points 1 and 3).
- `GetState` and `Scan` throw `GrammarException` when there is no state or it is not an `S`. A layer given an `initialState` throws when a step leaves a state that is not a `TState` (ADR-0037 points 4 and 5).
- Compare benchmark runs before and after with `scripts/bench-compare.py`, since the state adds a field to each layer's parse state (ADR-0037 § Consequences).
- Record `stateBefore` per slot; marks snapshot state by reference.
- `Transduce`'s step reads and changes its layer state through these. LAY-03 only hands `initialState` to `atEnd` ([ADR-0032](../../docs/decisions/0032-emit-is-a-list-of-outputs-each-derived-or-synthetic.md) point 10).
- `p.Scan`'s function returns layer state, so it is a decision function: engines run it wherever the state could change the outcome ([ADR-0026](../../docs/decisions/0026-engines-may-skip-unused-values-and-repeat-user-functions.md)). Layer state is the supported place for state a parse needs, since functions in grammars must not rely on side effects.
- Marks save the layer-state reference only in a layer whose grammar contains `SetState`, `Scan` or `Bind`. A `Longest` whose winner can change layer state runs the winner again when another alternative ran after it ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) points 6 and 9).

## Acceptance criteria

- [x] A template-string brace-depth lexer backtracks correctly in property tests.
- [x] A parser that sets its state with `SetState` is tested, as are the `GrammarException` cases of ADR-0037 points 4 and 5.

## Blocked by

- [LAY-02 Marks and retention](../06-layers-and-pipelines/LAY-02-marks-and-retention.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)

## Unblocks

- [ERR-05 Recover combinator and strategies](../04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md)
- [LAY-07 Three-layer demo and bounded-memory check](../06-layers-and-pipelines/LAY-07-three-layer-demo-and-bounded-memory-check.md)
- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)

## Whitepaper references

- [§ 2. Layer-local state](../../../whitepaper.md#2-layer-local-state)
- [§ The stream buffer](../../../whitepaper.md#the-stream-buffer)

## Artifacts

- [ADR-0032: `Emit<T>` is a list of outputs, each derived or synthetic, not a union](../../docs/decisions/0032-emit-is-a-list-of-outputs-each-derived-or-synthetic.md)
- [ADR-0037](../../docs/decisions/0037-lexers-take-an-initial-state-parsers-set-theirs-and-a-mistyped-read-throws.md) Accepted: `Layer.Tokens(initialState, item, skip)` for lexers, a parser sets its own state, and reading state of the wrong type throws

## Progress notes

- 2026-11-24 (Design): Started. The whitepaper does not say how a `Layer.Tokens` lexer, such as the criterion's template-string lexer, gets an initial state, whether a parser has state, or what `GetState<S>` does when the state is missing or of another type. Next: a decision record on those three points, then Build.
- 2026-11-24 (Design): Drafted ADR-0037 (Proposed). A spike showed that a `Layer.Tokens(initialState, item)` overload makes `Layer.Tokens(item, skip)` ambiguous (CS0121), so the stateful form always takes `skip`. The maintainer chose 1A, 2A and 3A. Next: the maintainer accepts, rejects or revises; on acceptance, update the whitepaper, MODE-01 and ANA-05, then Build.
- 2026-11-24 (Design): The maintainer accepted ADR-0037. The whitepaper's § 2. Layer-local state and § Defining layers, this card's scope and criteria, MODE-01 and ANA-05 were updated as its Changes made lists. Moved to Building. Next: /build, planning against both criteria.
- 2026-11-24 (Build): Increment 1 done: state in a parser run on its own. `GetState`, `SetState` and `p.Scan` (an instance method, because explicit type arguments on an extension member must include the extension block's own, so `Scan<IndentStack, Emit<Tok>>` would not compile as one), held on the cursor as one reference. `FullMark` saves it, a grammar with `SetState`, `Scan` or `Bind` takes full marks, and `Longest` runs its winner again whenever another alternative ran after it. A wrong read throws `GrammarException` naming the layer. Tests: a model-based test that the state after every control shape matches the diagnostics `ControlModel` keeps, with and without a failure log, plus examples, errors and allocation. Docs: docs/concepts/layer-state.md. Next: increment 2, state in layers (`Layer.Tokens(initialState, item, skip)`, threading across steps, `stateBefore`, the end-of-step type check).
- 2026-11-24 (Build): Increment 2 done: state in layers. `Layer.Tokens(initialState, item, skip, name)`; `Tokens` and `Transduce` carry the state from step to step, record each slot's `stateBefore`, and pass `atEnd` the state the last step left. A layer given an `initialState` checks at the end of each step that its state is still of that type, and state errors name the layer. `LayerStateTests` and new `TransduceTests` cover these; LAY-03's slot test now expects `stateBefore` 0, not null. Docs: layers.md and layer-state.md. The gate benchmarks run no layers, so they were not re-run. Next: increment 3, the template-string brace-depth lexer property test (criterion 1).
- 2026-11-24 (Build): Increment 3 done: criterion 1's test. `TemplateLexerTests` lexes identifiers, braces and JavaScript-style template literals with a `Layer.Tokens(ImmutableStack<Open>.Empty, …)` lexer, whose grammar changes state inside a `Longest` loser, an `Atomic` that rewinds, a `Lookahead`, a `Not` and an `Optional`. A CsCheck property generates programs as trees, writes them out with the tokens and stacks they must lex to, and checks the tokens and each slot's `stateBefore` across segment sizes 1, 2, 3 and 1024, with and without a failure log (5,000 programs a run). It fails if `Longest` stops running its winner again without a failure log, or if marks stop restoring the state. Both criteria now appear met. Next: /complete.
- 2026-11-24 (Build): Completed. Both criteria verified on e2a0a71, as the Outcome records.

## Outcome

- **API**, as the whitepaper's § 2. Layer-local state and [ADR-0037](../../docs/decisions/0037-lexers-take-an-initial-state-parsers-set-theirs-and-a-mistyped-read-throws.md) define it:
  - `GetState<TState>()` and `SetState<TState>(state)` in [GrammarOfTIn.State.cs](../../../src/Creole/GrammarOfTIn.State.cs). `SetState`'s value is boxed once, when the grammar is lowered.
  - `p.Scan<TState, TResult>((value, state, span) => (result, newState))` in [Parser.State.cs](../../../src/Creole/Parser.State.cs). It is an instance method, as `Select` is: on SDK 11.0.100, explicit type arguments on an extension member must include the extension block's own, so Example B's `Scan<IndentStack, Emit<Tok>>(…)` would not compile as one.
  - `Layer.Tokens(initialState, item, skip, name)` in [Layer.cs](../../../src/Creole/Layer.cs), with no `(initialState, item)` form (ADR-0037 point 2).
- **Engine:**
  - The state is one reference on the cursor ([Cursor.cs](../../../src/Creole/Cursor.cs), [StateBox.cs](../../../src/Creole/StateBox.cs)): null for no state, and a sentinel for a state set to null.
  - [FullMark](../../../src/Creole/Plans/FullMark.cs) saves and restores it. A grammar takes full marks when it contains `SetState`, `Scan` or `Bind` (ADR-0030 point 9).
  - [LongestPlan](../../../src/Creole/Plans/LongestPlan.cs) runs its winner again whenever another alternative ran after it, with or without a failure log (ADR-0030 point 6).
  - None of the three nodes is Regular. For dispatch, `Scan` takes its source's keys, and `GetState` and `SetState` have none.
  - `Tokens` and `Transduce` carry the state from step to step, record each slot's `stateBefore`, and pass `atEnd` the last state. A layer given an `initialState` checks each step's state against its type ([InitialState.cs](../../../src/Creole/InitialState.cs)).
  - A wrong read throws `GrammarException` naming the layer, through [LayerStateException](../../../src/Creole/LayerStateException.cs), which `Parse` and each layer translate.
- **Tests** (40 new, plus 3 `RegularTests` cases):
  - [StateNodeTests.cs](../../../tests/Creole.Tests/StateNodeTests.cs) (7) and [StateExecutionTests.cs](../../../tests/Creole.Tests/StateExecutionTests.cs) (17). The second includes a model-based test: with `Raise` leaves as `Scan`s that append to the state, the state after every control shape to depth 3, and after 20,000 random deeper shapes, equals the diagnostics `ControlModel` keeps, with and without a failure log.
  - [LayerStateTests.cs](../../../tests/Creole.Tests/LayerStateTests.cs) (10) and 4 in [TransduceTests.cs](../../../tests/Creole.Tests/TransduceTests.cs).
  - [TemplateLexerTests.cs](../../../tests/Creole.Tests/TemplateLexerTests.cs) (2, one a theory of 5 cases): criterion 1.
  - Mutations: restoring the old `Longest` condition failed both model-based tests and the template property; dropping the state restore from `FullMark` failed the property and 3 template cases.
  - The AOT smoke program parses with `SetState`, `Scan` and a `Longest`.
- **Docs:** [layer-state.md](../../../docs/concepts/layer-state.md) (new); [layers.md](../../../docs/concepts/layers.md), [sequencing.md](../../../docs/concepts/sequencing.md), [interpreter.md](../../../docs/engines/interpreter.md) and the [docs index](../../../docs/README.md).
- **Cost.** Three runs of the gate benchmarks before and after increment 1, `--job short`: allocations identical; median times of Creole's benchmarks 0.94–1.14 of the baseline, while the unchanged Parlot benchmarks moved 0.93–1.15, so no change shows above the noise.
- **Follow-ups**, already on cards: MODE-01's `Layer.Modal` takes an `initialState` (ADR-0037 point 2); ANA-05 adds the state-type check (point 6) and CRL0204; LAY-07 builds Example B's indentation pass.
- **Evidence, 2026-11-24, on e2a0a71:** `dotnet build` with 0 warnings; `dotnet test` with 1,810 passed and 0 failed.

  | Criterion | Evidence |
  | --- | --- |
  | 1. A template-string brace-depth lexer backtracks correctly in property tests | `TemplateLexerTests.TheLexerProducesTheTokensOfRandomProgramsWithTheStackBeforeEach`: 5,000 generated programs a run, each checked for its tokens and every slot's `stateBefore` at segment sizes 1, 2, 3 and 1,024, with and without a failure log. The grammar changes state in a `Longest` loser, an `Atomic` that rewinds, a `Lookahead`, a `Not` and an `Optional`. The two mutations above failed it |
  | 2. A parser that sets its state with `SetState` is tested, as are the `GrammarException` cases of ADR-0037 points 4 and 5 | `StateExecutionTests.AParserSetsItsStateAtItsRootAndReadsItBack`; point 4: `ReadingStateAParserNeverSetThrows`, `ReadingStateAsAnotherTypeThrows`, `ANullStateReadsAsNullOnlyAsATypeThatAdmitsNull`, and `ReadingTheStateAsAnotherTypeNamesTheLayer` in `LayerStateTests` and `TransduceTests`; point 5: `AStepThatLeavesAStateOfAnotherTypeThrows` in both |
