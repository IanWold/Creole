# MODE-04: Multi-layer invalidation cascade

| | |
| --- | --- |
| Epic | [Mode channels](../../README.md#mode--mode-channels) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

A channel may skip intermediate layers, such as a parser steering the lexer beneath a layout layer.

## Scope

- Truncate each buffer using the slot's `stepStart`, down to the channel's producer.
- Intermediate layers restore `stateBefore` and re-run over re-lexed items.
- No storage beyond existing slot fields.
- A synthetic output's span reads the start of the item after its step. Test a cascade that re-lexes that item under a mode that skips differently: an unconsumed synthetic output is produced again with the new start, and a consumed one keeps the old start ([ADR-0032](../../docs/decisions/0032-emit-is-a-list-of-outputs-each-derived-or-synthetic.md) Consequences).
- Each layer whose input is truncated at *j* lowers its failures at *j*, and a producing layer that re-runs a step from `stepStart` *s* lowers its own at *s*. An intermediate layer discards input it had consumed; its pin at the `stepStart` of the oldest output its reader can still go back to kept that input from being released, so its copies stay correct ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 3, [ADR-0040](../../docs/decisions/0040-a-producing-layer-keeps-input-only-for-outputs-its-reader-can-go-back-to.md) point 4).
- Run the model-based tests over streamed input as well, in segments of one and two items with the overwriting pool LAY-02's tests use, so a cascade that needs released input fails (ADR-0040 Consequences).

## Acceptance criteria

- [ ] A three-layer grammar with a mode channel crossing the layout layer passes model-based tests (MODE-06).

## Blocked by

- [MODE-02 Invalidation rule](../07-mode-channels/MODE-02-invalidation-rule.md)

## Unblocks

- [MODE-06 Model-based tests for mode rollback](../07-mode-channels/MODE-06-model-based-tests-for-mode-rollback.md)
- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)
- [FUS-03 Fusion with modes and custom scanners](../16-layer-fusion/FUS-03-fusion-with-modes-and-custom-scanners.md)

## Whitepaper references

- [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels)

## Artifacts

- [ADR-0032: `Emit<T>` is a list of outputs, each derived or synthetic, not a union](../../docs/decisions/0032-emit-is-a-list-of-outputs-each-derived-or-synthetic.md)
- [ADR-0040: A producing layer keeps its input only for the outputs its reader can still go back to](../../docs/decisions/0040-a-producing-layer-keeps-input-only-for-outputs-its-reader-can-go-back-to.md) Why a cascade never truncates input a layer below has released
