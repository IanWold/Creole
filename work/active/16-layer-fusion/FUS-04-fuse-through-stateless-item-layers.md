# FUS-04: Fuse through stateless item layers

| | |
| --- | --- |
| Epic | [Layer fusion (1.x)](../../README.md#fus--layer-fusion-1x) |
| Phase | Phase 4 — 1.x releases |
| Size | S |
| Status | Not started |

## Summary

`Where` and `Select` layers between lexer and parser inline instead of breaking fusion.

## Scope

- Inline stateless item functions into the fused loop.

## Acceptance criteria

- [ ] A lexer plus comment filter plus parser pipeline fuses.

## Blocked by

- [FUS-02 Fused code emission](../16-layer-fusion/FUS-02-fused-code-emission.md)

## Unblocks

- [FUS-05 Fusion gate](../16-layer-fusion/FUS-05-fusion-gate.md)

## Whitepaper references

- [§ Conditions](../../../whitepaper.md#conditions)
