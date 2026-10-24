# FND-10: Move the Parlot baselines to 2.0.0

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

Parlot 2.0.0 is a stable release, published 2026-10-20, and so is `Parlot.SourceGenerator`. It also removes `Compile()`. The harness still pins Parlot 1.5.12, and the whitepaper still calls Parlot's generator preview-only ([research note](../../docs/research/2026-10-23-dfa-compilation-under-commit-semantics.md), findings 21–22). This card brings the baselines and the whitepaper's facts about Parlot up to date before the Phase 0 gate measures against them.

## Scope

- **Measure.** Run Parlot 1.5.12's and 2.0.0's interpreted parsers on the FND-05 JSON and arithmetic corpora. Pin the faster in `Directory.Packages.props`: [ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md) sets the baseline as the fastest stable release's interpreted parser.
  - If 2.0.0 is pinned, remove the `ParlotCompiled` baselines.
  - If 1.5.12 is faster, record that, and propose how the harness can also carry 2.0.0, which BENCH-03 needs for Parlot's generated parsers.
- **Re-check FND-05's Parlot findings on 2.0.0** and record the results: the string literal rejects `\/`, and `Separated` fails on empty lists. The corpora stay as they are.
- **Update the facts.**
  - The whitepaper's statements about Parlot's versions, its generator and `Compile()`. They are in § Background and motivation, § Source-generated engine (including Considered and deferred), § Comparison with existing .NET libraries, § Roadmap, § Risks, and the build-time execution question under Questions raised in review.
  - The Parlot row of benchmarks/README.md.

  Accepted decision records and dated research notes stay as they are.
- **Re-check the comparisons.** 2.0.0 puts grammars for generation in `.parlot.cs` files. Check how its generator now reads a grammar. The whitepaper contrasts Parlot, whose generator executes `[GenerateParser]` methods, with Creole, whose generator reads syntax only. If that contrast no longer holds, rewording it changes an argument rather than a fact, so it goes through `/decide`. The same applies to § Risks' assessment of Parlot.
- **Out of scope.** Parlot's generated JSON and arithmetic baselines belong to [BENCH-03](../13-benchmarks/BENCH-03-phase-3-speed-gate.md).

## Acceptance criteria

- [ ] Parlot 1.5.12's and 2.0.0's interpreted parsers are measured on the FND-05 corpora, median of three runs, with results on this card, and `Directory.Packages.props` pins the faster.
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
