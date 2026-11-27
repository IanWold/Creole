# MODE-05: Mode payloads

| | |
| --- | --- |
| Epic | [Mode channels](../../README.md#mode--mode-channels) |
| Phase | Phase 1 — Layers |
| Status | Building |

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
- 2026-11-26 (Build): Started; the maintainer approved ADR-0043's options, criteria and trials, and the build plan. Increment 1: added `ModalUnion` to `LayerBenchmarks` (a one-case union mode with no data) and checked it in `BaselineAgreementTests`. Trial T1 baseline, three default-job runs on 12e48bb plus that case: `ModalUnion` takes 1.44–1.46 times as long as `Modal` and allocates about 80 B more per token, so C1 fails criterion 3 (recorded in ADR-0043). Next: increment 2, the bounded cache with per-run reuse (A2, B2, C2), its tests and docs, then T1's three runs after.
- 2026-11-26 (Build): Increment 2. `Layer.Modal` keeps the plans of the 64 modes used most recently (new `RecentPlans`, keyed so a null mode still works). Each run reuses its plan while its stack is the same object and keeps its own map of up to 64 modes, so a mode change takes the lock only for a mode the run has not used (`ModalLayer.cs`). New `ModePayloadTests` (5 tests) cover criteria 1 to 3, B2 and a null mode; six mutations, each fails a test. Docs: XML docs on `Layer.Modal` and `ModeChannel`, the selector bullet in layers.md, a new "Modes that carry data" section in mode-channels.md (sample mirrored in the tests), and the docs index. 2,010 tests pass; the AOT smoke publishes and runs. T1 after: `ModalUnion` takes 0.999–1.022 times as long as `Modal`, and allocates exactly 104 B more per parse, counted per thread. ADR-0043 records the results, with A2, B2, C2, D1 and E1 chosen. The payload half of the criterion is met by `EachPayloadIsLexedWithItsOwnData`. Its analysis half waits on ANA-05, which the record moves there on acceptance. Next: `/complete`, which accepts ADR-0043 and makes its whitepaper and card changes.
- 2026-11-27 (Build): `/complete` stopped before acceptance. The critic's independent check found the payload half of the criterion met. It also found that ADR-0043's criterion 3 held only between writes: the step after any write compares modes by value, about 104 B for a union. It also found that a mode changed after it was cached broke the 64-plan bound. The maintainer chose to narrow criterion 3 and document `IEquatable<T>` for union modes (C4, a map keyed by stack object, ruled out), and to store each entry's hash. Increment 3: `RecentPlans` keys keep their mode's first hash. Three new tests: a mode changed after planning still leaves 64 plans; the layer's own order keeps `Plain` across 200 parses; a union mode that implements `IEquatable<T>` adds no more per write than an enum. Three mutations, each fails one of them. XML docs and mode-channels.md now say what "used" means and when to implement `IEquatable<T>` (sample mirrored in the tests). ADR-0043 revised: criterion 3 (dated), the per-write measurements under C2, C4, Decision points 2, 3, 6 and 8, 0810a0b cited, ANA-01 added to its cards. 2,013 tests pass; the AOT smoke publishes and runs; `ModalUnion` still allocates exactly 104 B per parse more than `Modal` on every input. Next: once committed, resume `/complete`: the maintainer accepts ADR-0043, then the whitepaper, card and index changes it lists.
