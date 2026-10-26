# LAY-04: Layer-local state

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

Layers decide from their own history with immutable state that backtracking restores automatically.

## Scope

- `GetState<S>()`, `SetState<S>(s)` and `p.Scan<S, R>(...)`.
- Record `stateBefore` per slot; marks snapshot state by reference.
- `p.Scan`'s function returns layer state, so it is a decision function: engines run it wherever the state could change the outcome ([ADR-0026](../../docs/decisions/0026-engines-may-skip-unused-values-and-repeat-user-functions.md)). Layer state is the supported place for state a parse needs, since functions in grammars must not rely on side effects.

## Acceptance criteria

- [ ] A template-string brace-depth lexer backtracks correctly in property tests.

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
