# FUS-03: Fusion with modes and custom scanners

| | |
| --- | --- |
| Epic | [Layer fusion (1.x)](../../README.md#fus--layer-fusion-1x) |
| Phase | Phase 4 — 1.x releases |
| Size | M |
| Status | Not started |

## Summary

Extend fusion to finite mode channels and lexers with `Custom` tokens.

## Scope

- One DFA per mode, selected by the current mode.
- Run a `Custom` scanner alongside the DFA when the next character is in its FIRST set.

## Acceptance criteria

- [ ] Example C fuses.

## Blocked by

- [CORE-12 Custom primitives](../02-core-combinators/CORE-12-custom-primitives.md)
- [MODE-04 Multi-layer invalidation cascade](../07-mode-channels/MODE-04-multi-layer-invalidation-cascade.md)
- [FUS-02 Fused code emission](../16-layer-fusion/FUS-02-fused-code-emission.md)

## Unblocks

- [FUS-05 Fusion gate](../16-layer-fusion/FUS-05-fusion-gate.md)

## Whitepaper references

- [§ Conditions](../../../whitepaper.md#conditions)
- [§ Custom primitives](../../../whitepaper.md#custom-primitives)
