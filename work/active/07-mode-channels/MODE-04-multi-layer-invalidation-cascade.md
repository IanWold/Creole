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
