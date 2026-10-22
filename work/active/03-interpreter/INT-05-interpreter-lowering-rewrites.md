# INT-05: Interpreter lowering rewrites

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

Rewrites that make the interpreter competitive with Pidgin and Parlot.

## Scope

- Collapse chains of `Select` into one plan step that applies each function in turn.
- Lower `Or` to dispatch on the next character or token kind: each alternative takes the keys of its leading primitive, and the `Or` runs only the candidates for that key, in order ([ADR-0016](../../docs/decisions/0016-or-dispatches-on-each-alternatives-leading-item.md)).
- Lower runs of a character class to a `SearchValues<char>` scan with `IndexOfAnyExcept`.
- Value-type results flow through generic plan nodes without boxing; skipped results use `Unit`.

## Acceptance criteria

- [x] Benchmarks show each rewrite helps or the rewrite is dropped.

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
- 2026-10-22 (Build): Increment 4 of 4 done. Value-type audit: plans are generic over `ref TOut`, skipped results use `Unit`, and no plan allocates per parse except by design (lists in repetitions, `Text`, `CIString`'s text, a slice of streamed input, `Bind` when its continuation builds a new parser). `AllocationTests` (14 cases) shows 0 bytes over 100 parses after warm-up for primitives in a sequence, a `Select` chain, `Optional`, scanned and item-by-item runs, dispatched and in-order choices, `Atomic`, `Lookahead`, `Not` and `Cut`, `Longest`, `Named` and `Context`, a slice of in-memory input, `Where`, `Switch`, `Rec` with `Expression.Build`, and dispatch over struct `IKinded` tokens. A mutant that boxes in `MapPlan` fails 3 cases. Exceptions found: `Case<T>()` boxes a struct union token to read its case, which § Where unions are not used already accepts. `Match` reads its scrutinee through `IUnion.Value`, so `Match` over a non-boxing union boxes the case on every parse: measured 2,936 bytes per 100 parses for `Match` over `Maybe<int>`. That contradicts ADR-0007's consequence that "nothing in Creole calls" `Maybe<T>.Value`. It is reported to the maintainer, not changed here: an interpreter cannot call C# 15's non-boxing `TryGetValue` pattern without reflection. Keep or drop, median of three runs: run scan kept (189.6× long runs, 6.45× short runs); `Select` collapse kept as a two-function plan (1.07–1.08×; one composed closure measured 1.043× and was not kept); dispatch kept (1.48× over characters, 1.24× over token kinds); value types are not a rewrite, and `AllocationTests` is their evidence. Docs: new docs/engines/interpreter.md (lowering and caching, the three rewrites and how to write grammars that benefit, what allocates), indexed in docs/README.md; choice.md points to it. Verified: `dotnet build` 0 warnings, `dotnet test` 1,117 passed; this increment changes no runtime source, so the AOT smoke publish was not rerun (clean at increment 3). Every criterion appears met. Next: `/complete`.
- 2026-10-22 (Build): `/complete` found that ADR-0016's dispatch threshold had not been set by benchmarks. Measured with the keyed alternatives first and never matching (median of three runs): dispatch was 1.21× slower with one keyed alternative (1.33× slower when it matched half the items), 1.10× slower with two, 1.03× faster with three and 1.17× faster with four. With four keyed alternatives placed after the ones that match, it was 1.30× slower. A dispatch costs about 3 ns per call, about as much as two or three alternatives that fail at once. The maintainer chose a threshold of four: `PlanBuilder.DispatchThreshold`, overridable through an internal `dispatchThreshold` parameter. `Rewrites.Compare` and `ControlModelExecutionTests` lower with a threshold of one, so every keyed choice still dispatches under the exactness tests. The tuning categories for one, two and three keys were removed; `FourKeysSkipped` and `FourKeysLast` stay as the threshold's best and worst case. Docs state the threshold and advise putting alternatives that start with a literal first. Editorial: § Interpreted engine and this card's scope now say chains of `Select` collapse into "one plan step that applies each function in turn", which is what was kept ("one composed function" was measured and not kept). `Match` over non-boxing unions captured as the idea `match-over-non-boxing-unions`. Verified: `dotnet build` 0 warnings, `dotnet test` 1,116 passed, AOT smoke publish clean, final benchmark runs below. Done.

## Outcome

Delivered the interpreter's lowering rewrites: skipped character-class runs scan with `SearchValues`, chains of `Select` collapse into one plan step, and `Or` dispatches on each alternative's leading item ([ADR-0016](../../docs/decisions/0016-or-dispatches-on-each-alternatives-leading-item.md)). Value-type results flow through plans without boxing, with two exceptions noted below. Each rewrite can be switched off for tests and benchmarks.

| Path | Contents |
| --- | --- |
| `src/Creole/Plans/LoweringOptions.cs` | Internal flags `Scan`, `CollapseSelect` and `Dispatch`; `PlanCache` applies all of them. |
| `src/Creole/Plans/ScanPlan.cs` | `SkipRepeat` over a character class: `IndexOfAnyExcept` over the class, or `IndexOfAny` over its complement when that is smaller, segment by segment, keeping min and max. |
| `src/Creole/Plans/Plan.cs`, `MapPlan.cs` | A virtual `Select` on plans; `MapPlan` overrides it to return `MapPlan<TIn, TSource, TMiddle, TResult>`, which calls both functions in turn. |
| `src/Creole/Plans/DispatchPlan.cs`, `PlanBuilder.Dispatch.cs`, `PlanBuilder.Choice.cs` | The leading-item walk (an exhaustive `NodeKind` switch), key readers for characters, enum items and `IKinded` tokens, and the dispatch plan with ordered candidate lists per key range. `PlanBuilder.DispatchThreshold` is 4. |
| `src/Creole/IKindNode.cs`, `Nodes/KindNode.cs`, `Analysis/FirstSet.cs` | How dispatch reads a token's kind; `FirstSet.EnumKey` shared. |
| `tests/Creole.Tests/` | `Rewrites` (with-and-without comparison), `ScanLoweringTests`, `SelectCollapseTests`, `DispatchLoweringTests`, `AllocationTests`; `ControlModelExecutionTests` lowers with every keyed choice dispatching; `WorkedExampleATests` checks that Example A's `Value` dispatches. |
| `benchmarks/Creole.Benchmarks/` | `LoweringBenchmarks` and `LoweringInputs`; `Json/` gains `JsonKind`, `JsonToken`, `JsonTokenLexer` and `CreoleTokenJson`. `InternalsVisibleTo` from `Creole`. |
| `docs/engines/interpreter.md` | New page: lowering and caching, the rewrites and how to write grammars that benefit, what allocates. Indexed in docs/README.md; choice.md points to it. benchmarks/README.md describes `LoweringBenchmarks`. |

Evidence (2026-10-22, this machine, default job, median of three runs in one invocation each, with every other rewrite on):

| Rewrite | Category | Without | With | Ratio | Kept |
| --- | --- | --- | --- | --- | --- |
| Scan | `ScanLongRuns` (100 KB of quoted strings) | 729.5 µs | 3.83 µs | 191.6× | Yes |
| Scan | `ScanShortRuns` (100 KB of words and spaces) | 1,033.7 µs | 162.6 µs | 6.38× | Yes |
| `Select` collapse | `CollapseSelect` (two-link chain over 100 KB of digits) | 698.5 µs | 650.6 µs | 1.07× | Yes |
| Dispatch | `DispatchChars` (character-level JSON token choice, 104 KB) | 563.9 µs | 386.5 µs | 1.46× | Yes |
| Dispatch | `DispatchKinds` (Example A's `Value` over the lexed tokens) | 91.4 µs | 72.6 µs | 1.26× | Yes |
| Dispatch | `FourKeysSkipped` (best case at the threshold) | 404.7 µs | 338.0 µs | 1.19× | — |
| Dispatch | `FourKeysLast` (worst case at the threshold) | 240.6 µs | 337.5 µs | 0.71× | — |

Allocations are equal with and without each rewrite. `dotnet build` 0 warnings; `dotnet test` 1,116 passed; AOT smoke publish clean. Mutants that break the scan (3), drop or reorder dispatch candidates or walk through `Optional` (5), or box in `MapPlan` failed tests; deleting the `Select` override fails only the plan-shape test, as a missing collapse cannot be observed.

Deviations and decisions:

- [ADR-0016](../../docs/decisions/0016-or-dispatches-on-each-alternatives-leading-item.md) (Accepted): dispatch reaches each alternative's leading item through wrappers, with candidates per key, replacing § Interpreted engine's "an `Or` over primitives with disjoint FIRST sets".
- `Select` collapse calls the functions in turn from one plan, not one composed closure: the closure measured 1.043×, below the keep rule. § Interpreted engine and the scope were reworded editorially.
- The dispatch threshold is four keyed alternatives, set by the measurements in the last progress note; dispatch still costs about 3 ns per choice when its keyed alternatives come after the ones that match, which docs/engines/interpreter.md tells authors to avoid.
- `Switch` keeps its linear key scan; INT-03's Outcome no longer says INT-05 speeds it up.

Follow-ups:

- [`Match` over non-boxing unions](../../ideas/match-over-non-boxing-unions.md) (idea): `Match` reads `IUnion.Value`, so `Match` over `Maybe<int>` boxes on every parse (2,936 bytes per 100 parses), contradicting ADR-0007's consequence and § Design choices. `Case<T>()` over struct union tokens boxes too, as § Where unions are not used accepts.
- `CharSet.Contains` is a binary search behind a delegate, so single-character class tests cost about 7–10 ns; not addressed here.
- [ERR-02](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md) records what a skipped alternative expects, using `DispatchPlan.Alternatives` (ADR-0016 point 4). [ANA-01](../09-static-analysis/ANA-01-grammar-property-analysis.md) may supply key sets instead of the walk.
- [BENCH-01](../13-benchmarks/BENCH-01-phase-0-speed-gate.md) measures realistic grammars; `LoweringBenchmarks` decides no gate.
