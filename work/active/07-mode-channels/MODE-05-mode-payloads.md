# MODE-05: Mode payloads

| | |
| --- | --- |
| Epic | [Mode channels](../../README.md#mode--mode-channels) |
| Phase | Phase 1 — Layers |
| Size | S |
| Status | Not started |

## Summary

Support the rare data-carrying mode, at the cost of analysis precision and fusion.

## Scope

- Union-typed `TMode` with exhaustive `Layer.Modal` switches.
- `ModeData<T>()` for reading a payload; the node is opaque to analysis.
- Document that lexer-decidable data belongs in layer state.

## Acceptance criteria

- [ ] A payload mode works and is reported as opaque by analysis once ANA-05 exists.

## Blocked by

- [MODE-01 Mode channels and Layer.Modal](../07-mode-channels/MODE-01-mode-channels-and-layermodal.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels)
