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
- Lower `Or` to dispatch on the next character or token kind: each alternative takes the keys of its leading primitive, and the `Or` runs only the candidates for that key, in order ([ADR-0016](../../docs/decisions/0016-or-dispatches-on-each-alternatives-leading-item.md)).
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

## Artifacts

- [ADR-0016](../../docs/decisions/0016-or-dispatches-on-each-alternatives-leading-item.md) The interpreter's `Or` dispatches on each alternative's leading item (Accepted)

## Progress notes

- 2026-10-22 (Build): Started. Build, because the whitepaper (§ Interpreted engine, § Design choices) names the four rewrites and [ADR-0011](../../docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md) point 6 already allows choice dispatch (skip an alternative whose FIRST set excludes the next item and which is not nullable). Calls for the plan: how to benchmark before INT-08's public `Parse` exists (Creole is not yet in `Creole.Benchmarks`); how far "an `Or` over primitives" reaches, since Worked Example A's `Value` alternatives are `Select`, `As` and `Between` over `Kind`, not bare primitives; value-type results already flow through generic `Plan<TIn, TOut>` and `SkipRepeatPlan` returns `Unit`, so that bullet is an audit and an allocation measurement, not a rewrite; ERR-02 owns what a skipped alternative records (G7), so the switch plan must leave room for it. Next: `/build` plan.
- 2026-10-22 (Build): Plan agreed with the maintainer. Increments: (1) the run scan with the harness; (2) `Select` collapse; (3) dispatch; (4) value-type audit, allocation tests, the keep-or-drop table and a `docs/engines/` page on what the interpreter speeds up. Dispatch reaches each alternative's leading item (through `Map`, `Named`, `Context`, `Slice`, `Text`, `Atomic`, a `Seq`'s first item and `String`'s first character), with a per-key list of candidate alternatives, so FIRST sets need not be disjoint. That is wider than § Interpreted engine's "an `Or` over primitives", so increment 3 waits for a decision record (`/decide`). Benchmarks are micro-benchmarks only (`LoweringBenchmarks`); BENCH-01 judges realistic grammars. Keep rule: a rewrite stays if the median of three same-invocation runs is at least 5% faster on its target and no other category is more than 5% slower. `Switch` stays out of scope; INT-03's Outcome no longer says INT-05 makes it faster.
- 2026-10-22 (Build): Increment 1 of 4 done. `PlanBuilder` takes internal `LoweringOptions` (all on by default; `PlanCache` uses the default). `SkipRepeat` over a `CharClassNode` lowers to `ScanPlan`: `IndexOfAnyExcept` over the class, or `IndexOfAny` over its complement when that is smaller, segment by segment, keeping min and max. `Satisfy` is not scanned. Tests: `ScanLoweringTests` (14) compare outcome, value, position and cut flag with the rewrite on and off (`Rewrites.Compare`), including a CsCheck property over random classes, bounds, inputs, segment sizes and starting cut flags. Mutants that stop a run at a segment end, ignore `max`, or swap the two searches failed 7, 3 and 11 tests. `Creole.Benchmarks` gets `InternalsVisibleTo` and `LoweringBenchmarks`; `LoweringInputs` hashes are pinned and each benchmark must read its whole input. Results on this machine, default job, median of three runs: `ScanLongRuns` 730.8 µs → 3.786 µs (193×), `ScanShortRuns` 1,039.2 µs → 161.6 µs (6.4×), no allocation; kept. Verified: `dotnet build` 0 warnings, `dotnet test` 1,086 passed, AOT smoke publish clean. Seen in passing: the item-by-item baseline costs about 7 to 10 ns a character, much of it `CharSet.Contains`'s binary search behind a delegate, so every single-character class test pays it too; not in this card's scope. Next: increment 2, `Select` collapse; draft the dispatch decision record before increment 3.
- 2026-10-22 (Build): Increment 2 of 4 done. `Plan<TIn, TOut>` gains a virtual `Select`, which `MapPlan` overrides (Pidgin's pattern, as the INT-02 research note planned). With `LoweringOptions.CollapseSelect`, `Map` lowers as `Lower(source).Select(selector)`: a `Select` over a `Select` becomes one `MapPlan<TIn, TSource, TMiddle, TResult>` that calls both functions in turn, and a third link composes onto its second function. A `ForwardPlan` or staged plan is never collapsed through. Two designs were measured (`CollapseSelect` category, `Digit.Select(f).Select(g).SkipMany()` over 100 KB of digits, median of three runs): composing into one closure, as Pidgin does, was 1.043× faster (696.8 → 668.3 µs), below the keep rule, because the closure adds back the delegate call that the removed plan level saved; a plan holding both functions was 1.070× faster (695–722 → 651–654 µs) and is kept. The gain is about 0.45 ns per item; most of the per-item cost is `CharSet.Contains`. Scan categories unchanged (190.6×, 6.3×). Tests: `SelectCollapseTests` (6), including a CsCheck property comparing chains of 1 to 4 over committed, uncommitted, cutting and nullable sources with the rewrite on and off, a chain over a rule still being lowered, and a shared inner `Select`. Deleting the override fails only the plan-shape test, as expected, since a missing collapse cannot be observed in a parse. Verified: `dotnet build` 0 warnings, `dotnet test` 1,092 passed, AOT smoke publish clean. Next: draft the dispatch decision record (`/decide`), then increment 3.
- 2026-10-22 (Design): Back to Design for increment 3. Drafted [ADR-0016](../../docs/decisions/0016-or-dispatches-on-each-alternatives-leading-item.md) (Proposed): an alternative takes the keys of the primitive at its leading position, found through `Map`, `Named`, `Context`, `Slice`, `Text`, `Atomic`, a filter, a `Bind`'s source and a sequence's first item. For each key, the dispatch plan holds the alternatives that can start with it and those with no keys, in order, so FIRST sets need not be disjoint. For each key it keeps the alternatives it skips, for ERR-02; ANA-01 may later supply the key sets. Increments 1 and 2 stand. Next: the maintainer accepts, rejects or revises ADR-0016; on acceptance, fold it into § Interpreted engine, INT-05's scope and ANA-01, return to Build and do increment 3.
- 2026-10-22 (Build): The maintainer accepted ADR-0016. Folded into § Interpreted engine, this card's scope and ANA-01's scope. Back to Build. Next: increment 3, dispatch.
- 2026-10-22 (Build): Increment 3 of 4 done. `Or` lowers to `DispatchPlan` under `LoweringOptions.Dispatch`, per ADR-0016. `PlanBuilder.Dispatch.cs` walks each alternative's leading position with an exhaustive `NodeKind` switch (ADR-0001), through `Atomic`, `Bind`, `Context`, a filter, `Map`, `Named`, `Sequence`, `Slice` and `Text`, to a primitive that is not nullable or opaque and whose FIRST set holds only keys. Keys are read as the character, as the item itself for an enum input, or through the new `IKindNode<TToken>` (which `KindNode` implements) when every `Kind` node shares one kind type. `FirstSet.EnumKey` is now shared for that. `DispatchPlan` builds key ranges with ordered candidate lists (keyed alternatives that contain the key, plus every alternative with no keys), a direct table for keys below 128 and binary search above, and the alternatives with no keys at the end of input. It keeps `Alternatives`, so ERR-02 can tell which alternatives a key skips. Threshold, not tuned: an `Or` dispatches when at least two alternatives have keys. Tests: `DispatchLoweringTests` (7), with CsCheck properties comparing dispatch with running in order over random `Or`s of 2 to 6 character alternatives (keyed, unkeyed, overlapping, committing, cutting, nullable, opaque, a nested `Or`, `CIString` with the Kelvin sign), over enum inputs and over `IKinded` tokens, with segment sizes and starting cut flags. There is also a cut inside `Atomic`, and shape tests for the threshold and for mixed kind types. `WorkedExampleATests` checks that Example A's `Value` lowers to `DispatchPlan` and agrees on four documents. The existing suites now run with dispatch on through `PlanCache`, including `ControlModelExecutionTests` (over 30,000 grammars against the model) and the CORE-06, CORE-07 and CORE-08 tables. Mutants that drop unkeyed alternatives from candidates, walk through `Optional`, try nothing at the end of input, move unkeyed alternatives last, or break the range search failed 3 to 6 tests each. Benchmarks (median of three runs): `DispatchChars` (a character-level JSON token choice over the 104 KB JSON input) 560.8 → 377.1 µs (1.48×); `DispatchKinds` (Example A's `Value` over that input lexed to kinds) 90.5 → 72.7 µs (1.24×); kept. Other categories unchanged: `CollapseSelect` 1.08×, scans 189.6× and 6.45×. Allocations equal with and without dispatch. Verified: `dotnet build` 0 warnings, `dotnet test` 1,103 passed, AOT smoke publish clean. Next: increment 4, value-type audit, allocation tests, keep-or-drop table and the `docs/engines/` page.
