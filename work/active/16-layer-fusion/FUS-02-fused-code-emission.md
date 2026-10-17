# FUS-02: Fused code emission

| | |
| --- | --- |
| Epic | [Layer fusion (1.x)](../../README.md#fus--layer-fusion-1x) |
| Phase | Phase 4 — 1.x releases |
| Status | Not started |

## Summary

Emit one loop where parser decisions read characters directly and tokens are never stored.

## Scope

- Merge the lexer DFA's accept dispatch with the parser's `switch`.
- Build token payloads only where a `Select` uses them, as source slices; spans as locals.
- Check fusion conditions and report CRL0302 naming the failing one.

## Acceptance criteria

- [ ] Fused JSON parser agrees with unfused output on property-generated inputs.

## Blocked by

- [FUS-01 Fusion feasibility measurement](../16-layer-fusion/FUS-01-fusion-feasibility-measurement.md)

## Unblocks

- [FUS-03 Fusion with modes and custom scanners](../16-layer-fusion/FUS-03-fusion-with-modes-and-custom-scanners.md)
- [FUS-04 Fuse through stateless item layers](../16-layer-fusion/FUS-04-fuse-through-stateless-item-layers.md)

## Whitepaper references

- [§ How fusion works](../../../whitepaper.md#how-fusion-works)
- [§ Conditions](../../../whitepaper.md#conditions)
