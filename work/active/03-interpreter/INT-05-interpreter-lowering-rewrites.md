# INT-05: Interpreter lowering rewrites

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

Rewrites that make the interpreter competitive with Pidgin and Parlot.

## Scope

- Collapse chains of `Select` into one composed function.
- Lower `Or` over primitives with disjoint FIRST sets to a `switch` on token kind or character.
- Lower runs of a character class to a `SearchValues<char>` scan with `IndexOfAnyExcept`.
- Value-type results flow through generic plan nodes without boxing; skipped results use `Unit`.

## Acceptance criteria

- [ ] Benchmarks show each rewrite helps or the rewrite is dropped.

## Blocked by

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)

## Unblocks

- [ANA-06 Deterministic mode](../09-static-analysis/ANA-06-deterministic-mode.md)
- [BENCH-01 Phase 0 speed gate](../13-benchmarks/BENCH-01-phase-0-speed-gate.md)

## Whitepaper references

- [§ Interpreted engine](../../../whitepaper.md#interpreted-engine)
- [§ Design choices](../../../whitepaper.md#design-choices)

## Progress notes

- 2026-10-22 (Build): Started. Build, because the whitepaper (§ Interpreted engine, § Design choices) names the four rewrites and [ADR-0011](../../docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md) point 6 already allows choice dispatch (skip an alternative whose FIRST set excludes the next item and which is not nullable). Calls for the plan: how to benchmark before INT-08's public `Parse` exists (Creole is not yet in `Creole.Benchmarks`); how far "an `Or` over primitives" reaches, since Worked Example A's `Value` alternatives are `Select`, `As` and `Between` over `Kind`, not bare primitives; value-type results already flow through generic `Plan<TIn, TOut>` and `SkipRepeatPlan` returns `Unit`, so that bullet is an audit and an allocation measurement, not a rewrite; ERR-02 owns what a skipped alternative records (G7), so the switch plan must leave room for it. Next: `/build` plan.
- 2026-10-22 (Build): Plan agreed with the maintainer. Increments: (1) the run scan with the harness; (2) `Select` collapse; (3) dispatch; (4) value-type audit, allocation tests, the keep-or-drop table and a `docs/engines/` page on what the interpreter speeds up. Dispatch reaches each alternative's leading item (through `Map`, `Named`, `Context`, `Slice`, `Text`, `Atomic`, a `Seq`'s first item and `String`'s first character), with a per-key list of candidate alternatives, so FIRST sets need not be disjoint. That is wider than § Interpreted engine's "an `Or` over primitives", so increment 3 waits for a decision record (`/decide`). Benchmarks are micro-benchmarks only (`LoweringBenchmarks`); BENCH-01 judges realistic grammars. Keep rule: a rewrite stays if the median of three same-invocation runs is at least 5% faster on its target and no other category is more than 5% slower. `Switch` stays out of scope; INT-03's Outcome no longer says INT-05 makes it faster.
- 2026-10-22 (Build): Increment 1 of 4 done. `PlanBuilder` takes internal `LoweringOptions` (all on by default; `PlanCache` uses the default). `SkipRepeat` over a `CharClassNode` lowers to `ScanPlan`: `IndexOfAnyExcept` over the class, or `IndexOfAny` over its complement when that is smaller, segment by segment, keeping min and max. `Satisfy` is not scanned. Tests: `ScanLoweringTests` (14) compare outcome, value, position and cut flag with the rewrite on and off (`Rewrites.Compare`), including a CsCheck property over random classes, bounds, inputs, segment sizes and starting cut flags. Mutants that stop a run at a segment end, ignore `max`, or swap the two searches failed 7, 3 and 11 tests. `Creole.Benchmarks` gets `InternalsVisibleTo` and `LoweringBenchmarks`; `LoweringInputs` hashes are pinned and each benchmark must read its whole input. Results on this machine, default job, median of three runs: `ScanLongRuns` 730.8 µs → 3.786 µs (193×), `ScanShortRuns` 1,039.2 µs → 161.6 µs (6.4×), no allocation; kept. Verified: `dotnet build` 0 warnings, `dotnet test` 1,086 passed, AOT smoke publish clean. Seen in passing: the item-by-item baseline costs about 7 to 10 ns a character, much of it `CharSet.Contains`'s binary search behind a delegate, so every single-character class test pays it too; not in this card's scope. Next: increment 2, `Select` collapse; draft the dispatch decision record before increment 3.
- 2026-10-22 (Build): Increment 2 of 4 done. `Plan<TIn, TOut>` gains a virtual `Select`, which `MapPlan` overrides (Pidgin's pattern, as the INT-02 research note planned). With `LoweringOptions.CollapseSelect`, `Map` lowers as `Lower(source).Select(selector)`: a `Select` over a `Select` becomes one `MapPlan<TIn, TSource, TMiddle, TResult>` that calls both functions in turn, and a third link composes onto its second function. A `ForwardPlan` or staged plan is never collapsed through. Two designs were measured (`CollapseSelect` category, `Digit.Select(f).Select(g).SkipMany()` over 100 KB of digits, median of three runs): composing into one closure, as Pidgin does, was 1.043× faster (696.8 → 668.3 µs), below the keep rule, because the closure adds back the delegate call that the removed plan level saved; a plan holding both functions was 1.070× faster (695–722 → 651–654 µs) and is kept. The gain is about 0.45 ns per item; most of the per-item cost is `CharSet.Contains`. Scan categories unchanged (190.6×, 6.3×). Tests: `SelectCollapseTests` (6), including a CsCheck property comparing chains of 1 to 4 over committed, uncommitted, cutting and nullable sources with the rewrite on and off, a chain over a rule still being lowered, and a shared inner `Select`. Deleting the override fails only the plan-shape test, as expected, since a missing collapse cannot be observed in a parse. Verified: `dotnet build` 0 warnings, `dotnet test` 1,092 passed, AOT smoke publish clean. Next: draft the dispatch decision record (`/decide`), then increment 3.
