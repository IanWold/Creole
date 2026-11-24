# LAY-04: Layer-local state

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Building |

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

- [ ] A template-string brace-depth lexer backtracks correctly in property tests.
- [ ] A parser that sets its state with `SetState` is tested, as are the `GrammarException` cases of ADR-0037 points 4 and 5.

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
