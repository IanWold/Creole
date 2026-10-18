# FND-06: Fix numeric gate thresholds

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

The roadmap's gates ("near Pidgin speed", "measured win") become numbers once the harness has baselines.

## Scope

- Measure Pidgin and Parlot baselines on the FND-05 corpora.
- Decide whether "near Parlot" thresholds need Parlot's source-generated parsers, which ship only as preview `Parlot.SourceGenerator` packages and are not yet in the harness ([FND-05](FND-05-benchmark-harness.md) Outcome).
- Propose a threshold for each gate: Phase 0 speed, Phase 1 bounded memory, fusion win, resumable-async win, incremental re-parse cost.
- Update the Roadmap table in the whitepaper.

## Acceptance criteria

- [x] Every gate in the whitepaper's Roadmap has a numeric, measurable threshold.

## Blocked by

- [FND-05 Benchmark harness](../01-foundations/FND-05-benchmark-harness.md)

## Unblocks

- [BENCH-01 Phase 0 speed gate](../13-benchmarks/BENCH-01-phase-0-speed-gate.md)
- [BENCH-03 Phase 3 speed gate](../13-benchmarks/BENCH-03-phase-3-speed-gate.md)
- [INC-07 Incremental re-parsing gate](../15-incremental-reparsing/INC-07-incremental-re-parsing-gate.md)
- [FUS-01 Fusion feasibility measurement](../16-layer-fusion/FUS-01-fusion-feasibility-measurement.md)
- [RES-01 Resumable async measurement](../17-resumable-async/RES-01-resumable-async-measurement.md)

## Whitepaper references

- [§ Roadmap](../../../whitepaper.md#roadmap)

## Artifacts

- Research: [What numbers should the roadmap gates use?](../../docs/research/2026-10-18-numeric-gate-thresholds.md)
- Decision: [ADR-0003 Numeric gate thresholds](../../docs/decisions/0003-numeric-gate-thresholds.md) (Accepted)

## Progress notes

- 2026-10-18 (Research): Started. Measure Pidgin and Parlot baselines on the FND-05 corpora, settle whether Parlot's preview source generator belongs in the baseline, then propose per-gate thresholds through a decision record.
- 2026-10-18 (Research): Ran the FND-05 suite three times (default job, RC 2, i9-13900H laptop): time varies up to 1.3× per benchmark between runs, allocations are exact. Five research questions answered in the note, with proposed thresholds for every gate (Phases 0–3 and the three 1.x gates). Found two whitepaper errors about Parlot (the "20%" is about `Compile()`; the 2.0 generator does not intercept) for a separate editorial fix. Next: `/decide` on the thresholds, starting with whether Phase 0 is set against Pidgin, Parlot or both.
- 2026-10-18 (Design): Maintainer chose speed gates against both Pidgin and Parlot (interpreter ≤ 1.5× Parlot, generated engine ≤ 1.0× Parlot), one record for all gates, and the laptop with pinned settings as gate machine. Drafted ADR-0003 (Proposed) with thresholds for every gate. Also corrected the whitepaper's Parlot claims as a separate editorial change. Next: maintainer accepts or revises ADR-0003, then `/decide accept 0003` updates the whitepaper and gate cards.
- 2026-10-18 (Design): Maintainer accepted ADR-0003. Whitepaper § Roadmap, § Performance design and benchmark plan, § Layer fusion, § Why not async combinators and § Risks updated; nine gate cards now state their thresholds; BENCH-03 added for the Phase 3 speed gate. Next: complete.

## Outcome

- Delivered: [ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md) (Accepted), backed by the research note [What numbers should the roadmap gates use?](../../docs/research/2026-10-18-numeric-gate-thresholds.md). [§ Roadmap](../../../whitepaper.md#roadmap) now gives each phase's gate as a number and has a table of every threshold, including the three 1.x gates, plus how speed is measured.
- Evidence: inspected § Roadmap after the change. Every phase row and every 1.x gate has a measurable threshold. The baselines come from three full runs of the FND-05 suite (default job, .NET 11 RC 2, maintainer's laptop). Those runs showed up to 1.3× time noise per benchmark and identical allocations, which set the margins.
- Scope decisions: speed is gated against both Pidgin and Parlot (maintainer's choice). Parlot's preview source generator is not a baseline until it ships stable; the Phase 3 speed gate switches to it then.
- Beyond the card's scope list: Phase 2 and Phase 3 gates were also given numbers, as the criterion requires. A new Phase 3 speed gate card, [BENCH-03](../13-benchmarks/BENCH-03-phase-3-speed-gate.md), was added.
- Related editorial change, separate commit: the whitepaper's Parlot claims ("about 20%" was about `Compile()`; the 2.0 generator does not intercept calls; the generator is preview-only).
- Gate cards updated: BENCH-01, LAY-07, RSLN-07, GEN-08, INC-07, FUS-01, FUS-05, RES-01, RES-02. benchmarks/README.md has a "Gate runs" section.
