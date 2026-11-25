# MODE-05: Mode payloads

| | |
| --- | --- |
| Epic | [Mode channels](../../README.md#mode--mode-channels) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

Support the rare data-carrying mode, at the cost of analysis precision and fusion.

## Scope

- Union-typed `TMode` with exhaustive `Layer.Modal` switches.
- `ModeData<T>()` for reading a payload; the node is opaque to analysis.
- `Layer.Modal` caches the selector's result per distinct mode value ([ADR-0041](../../docs/decisions/0041-a-channel-has-one-reader-below-one-writer-and-a-write-nothing-reads-or-a-pop-of-the-last-mode-throws.md) point 6). A case that carries data gets one entry per value, so bound the cache or key it by case.
- Document that lexer-decidable data belongs in layer state.

## Acceptance criteria

- [ ] A payload mode works and is reported as opaque by analysis once ANA-05 exists.

## Blocked by

- [MODE-01 Mode channels and Layer.Modal](../07-mode-channels/MODE-01-mode-channels-and-layermodal.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels)
