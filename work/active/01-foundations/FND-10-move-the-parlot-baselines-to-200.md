# FND-10: Move the Parlot baselines to 2.0.0

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

Parlot 2.0.0 is a stable release, published 2026-10-20, and so is `Parlot.SourceGenerator`. It also removes `Compile()`. The harness still pins Parlot 1.5.12, and the whitepaper still calls Parlot's generator preview-only ([research note](../../docs/research/2026-10-23-dfa-compilation-under-commit-semantics.md), findings 21–22). This card brings the baselines and the whitepaper's facts about Parlot up to date before the Phase 0 gate measures against them.

## Scope

- **Measure.** Run Parlot 1.5.12's, 1.6.0's and 2.0.0's interpreted parsers on the FND-05 JSON and arithmetic corpora. 1.6.0 is a stable 1.x release, published the same day as 2.0.0. Pin the fastest in `Directory.Packages.props`: [ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md) sets the baseline as the fastest stable release's interpreted parser.
  - If 2.0.0 is pinned, remove the `ParlotCompiled` baselines.
  - If a 1.x release is faster, record that, and propose how the harness can also carry 2.0.0, which BENCH-03 needs for Parlot's generated parsers.
- **Re-check FND-05's Parlot findings on 2.0.0** and record the results: the string literal rejects `\/`, and `Separated` fails on empty lists. The corpora stay as they are.
- **Update the facts.**
  - The whitepaper's statements about Parlot's versions, its generator and `Compile()`. They are in § Background and motivation, § Source-generated engine (including Considered and deferred), § Comparison with existing .NET libraries, § Roadmap, § Risks, and the build-time execution question under Questions raised in review.
  - The Parlot row of benchmarks/README.md.

  Accepted decision records and dated research notes stay as they are.
- **Re-check the comparisons.** 2.0.0 puts grammars for generation in `.parlot.cs` files. Check how its generator now reads a grammar. The whitepaper contrasts Parlot, whose generator executes `[GenerateParser]` methods, with Creole, whose generator reads syntax only. If that contrast no longer holds, rewording it changes an argument rather than a fact, so it goes through `/decide`. The same applies to § Risks' assessment of Parlot.
- **Out of scope.** Parlot's generated JSON and arithmetic baselines belong to [BENCH-03](../13-benchmarks/BENCH-03-phase-3-speed-gate.md).

## Acceptance criteria

- [ ] Parlot 1.5.12's, 1.6.0's and 2.0.0's interpreted parsers are measured on the FND-05 corpora, median of three runs, with results on this card, and `Directory.Packages.props` pins the fastest.
- [ ] On the pinned version, the benchmarks build with 0 warnings and `tests/Creole.Benchmarks.Tests` passes. `ParlotCompiled` is gone if 2.0.0 is pinned.
- [ ] Every statement about Parlot's versions, generator or `Compile()` in the whitepaper and benchmarks/README.md matches Parlot 2.0.0's release notes or source, checked with `/check-sources`.
- [ ] The whitepaper's comparisons with Parlot's generator (§ Source-generated engine, § Risks) are re-checked against 2.0.0. Any change to their argument has a decision record.

## Blocked by

- [FND-05 Benchmark harness](../01-foundations/FND-05-benchmark-harness.md)

## Unblocks

- [BENCH-01 Phase 0 speed gate](../13-benchmarks/BENCH-01-phase-0-speed-gate.md)
- [BENCH-03 Phase 3 speed gate](../13-benchmarks/BENCH-03-phase-3-speed-gate.md)

## Whitepaper references

- [§ Background and motivation](../../../whitepaper.md#background-and-motivation)
- [§ Source-generated engine](../../../whitepaper.md#source-generated-engine)
- [§ Comparison with existing .NET libraries](../../../whitepaper.md#comparison-with-existing-net-libraries)
- [§ Roadmap](../../../whitepaper.md#roadmap)
- [§ Risks](../../../whitepaper.md#risks)

## Artifacts

- [Research: Parlot 2.0 baselines and generator](../../docs/research/2026-10-24-parlot-2-0-baselines-and-generator.md) Speed of 1.5.12, 1.6.0 and 2.0.0, harness changes, FND-05's findings, how the generator reads a grammar, Parlot's streaming, and the stale statements
- [ADR-0018](../../docs/decisions/0018-the-generator-keeps-reading-syntax-only.md) The generator keeps reading syntax only; § Considered and deferred restates why (Accepted)
- [ADR-0019](../../docs/decisions/0019-parlot-streaming-is-prior-art-and-creoles-case-rests-on-layers.md) Parlot 2.0's streaming is prior art; a § Comparison streaming row and § Streaming citation (Accepted)

## Progress notes

- 2026-10-24 (Research): Started. Approach: benchmark Parlot 1.5.12 and 2.0.0 interpreted on the FND-05 corpora, re-check FND-05's two Parlot findings, and read 2.0.0's generator source and docs to see how it reads `.parlot.cs` grammars. Next: `/research`.
- 2026-10-24 (Research): Research note written. Parlot 1.6.0 is also stable (2026-10-20), so it was measured too. Medians over two interleaved loops of three runs: 2.0.0 / 1.5.12 = 0.92 (JSON 0.86–0.93, arithmetic 1.02); 2.0.0 / 1.6.0 = 0.99, within noise. 2.0.0 needs `Separated(comma, x, 0, 0)` because `ZeroOrOne` now returns a list; it then builds with 0 warnings and passes 22/22 agreement tests. `\/` and empty `Separated` still fail on all three versions. The generator still executes factories, so the contrast holds, but the `[IncludeFiles]` clause and "preview packages" are stale; Parlot 2.0.0 also streams `TextReader` input. Next: maintainer to agree the scope change (1.6.0) and the pin (2.0.0); then `/decide` on line 766's IDE argument and a § Comparison streaming row, or go straight to Build for the fact edits.
- 2026-10-24 (Design): Maintainer agreed to add 1.6.0 to the scope (Measure bullet and first criterion edited) and to pin 2.0.0. Drafted ADR-0018 (restate why the generator reads syntax only; options: keep text, restate, add a build-time execution tier) and ADR-0019 (record Parlot's streaming as prior art; options: leave silent, record, narrow G2). Next: maintainer accepts, rejects or revises both; on acceptance edit the whitepaper, then Build (pin 2.0.0, remove `ParlotCompiled`, fact edits, `/check-sources`).
- 2026-10-24 (Design): Maintainer accepted ADR-0018 and ADR-0019 with the recommended options; whitepaper edited per both (§ Background, § Considered and deferred, § Streaming and async input, § Comparison, § Risks, § Questions raised in review, § References). Moved to Build. Next: `/build` — pin Parlot 2.0.0, port `ParlotJson` to `Separated(comma, x, 0, 0)`, remove `ParlotCompiled`, update benchmarks/README.md, fix the remaining stale facts (whitepaper lines 24, 1072, 1082, 1117), then `/check-sources`.
- 2026-10-24 (Build): Increment 1. `Directory.Packages.props` pins Parlot 2.0.0. `ParlotJson` uses `Separated(comma, x, min: 0)` in place of `ZeroOrOne(Separated(...))`; the `ParlotCompiled` benchmarks, parser fields and agreement cases are removed; benchmarks/README.md's measured-baselines rows and Parlot notes are updated. `dotnet build`: 0 warnings, 0 errors. `tests/Creole.Benchmarks.Tests`: 22/22 passed. `--job dry` over `JsonBenchmarks` and `ArithmeticBenchmarks` ran all 19 benchmarks with no issues. Next: increment 2, the remaining whitepaper facts (lines 24, 1072, 1084, 1118), then `/check-sources`.
- 2026-10-24 (Build): Increment 2. Whitepaper facts brought up to Parlot 2.0.0: "preview packages" dropped from § Background, § Comparison's opening paragraph and its build-time generation row; § Roadmap's baseline rule restated in the present tense; § Risks' opening sentence adds single-layer streaming, in line with ADR-0019; Parlot's References entry links its source generation guide. `/check-sources` over every Parlot statement in the whitepaper and benchmarks/README.md, first-hand against 2a34f3d, the v2.0.0 release notes, the v1.6.0 README and NuGet: all supported after these edits. PR #427 confirms that 2.0's "left recursion is detected" is a run-time check, so § Background's "none of the four analyzes a grammar" holds. Next: every criterion appears met; the maintainer runs `/complete`.
