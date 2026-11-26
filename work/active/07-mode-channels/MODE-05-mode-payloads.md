# MODE-05: Mode payloads

| | |
| --- | --- |
| Epic | [Mode channels](../../README.md#mode--mode-channels) |
| Phase | Phase 1 — Layers |
| Status | Designing |

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

## Artifacts

- [ADR-0043](../../docs/decisions/0043-layer-modal-plans-each-mode-value-and-keeps-the-64-most-recent.md): `Layer.Modal` plans each distinct mode value, data included, keeps the 64 most recently used, and needs no `ModeData<T>()` (Proposed)

## Progress notes

- 2026-11-26 (Design): Started. Payload modes conflict with ADR-0041 point 6, which caches the selector by distinct mode value, so a decision record settles how payload modes are cached, how `ModeData<T>()` finds and types its data, and what analysis marks before any code.
- 2026-11-26 (Design): Drafted ADR-0043 (Proposed). Spikes on SDK 11.0.100: a union mode compares by case value but boxes, 80 B per comparison; a `Bind` continuation that builds a parser lowers it at each match. The maintainer chose per-value caching with a bound over `ModeData<T>()`. Recommends A2, B2, C2, D1 and E1. Next: the maintainer approves the options, criteria and trials (T1 and the tests), then Build.
