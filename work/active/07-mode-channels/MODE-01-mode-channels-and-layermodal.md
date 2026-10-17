# MODE-01: Mode channels and Layer.Modal

| | |
| --- | --- |
| Epic | [Mode channels](../../README.md#mode--mode-channels) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

The parser can switch an upstream layer's lexing mode through a typed channel.

## Scope

- `ModeChannel<TMode>` as a typed key with an initial mode; the mode stack lives in parse state.
- `Layer.Modal(channel, selector, skip:)`, calling the selector once per distinct mode case and caching the result.
- `SetMode`, `PushMode`, `PopMode` as zero-width parser nodes.

## Acceptance criteria

- [ ] Selector caching verified: each mode's item parser is planned once.

## Blocked by

- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)

## Unblocks

- [MODE-02 Invalidation rule](../07-mode-channels/MODE-02-invalidation-rule.md)
- [MODE-05 Mode payloads](../07-mode-channels/MODE-05-mode-payloads.md)
- [ANA-06 Deterministic mode](../09-static-analysis/ANA-06-deterministic-mode.md)
- [RSLN-01 Roslyn grammar reader](../10-roslyn-analyzers/RSLN-01-roslyn-grammar-reader.md)

## Whitepaper references

- [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels)
