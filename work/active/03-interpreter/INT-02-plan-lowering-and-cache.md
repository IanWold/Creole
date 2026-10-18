# INT-02: Plan lowering and cache

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

On first use a grammar is lowered to an immutable execution plan and cached.

## Scope

- Plan node base: `abstract bool TryParse(ref Cursor<TIn> c, ref TOut result)`.
- Lowering from grammar tree to plan through an `internal abstract` lowering member that every node overrides ([ADR-0001](../../docs/decisions/0001-grammar-node-encoding-and-engine-dispatch.md)).
- `ConditionalWeakTable` cache keyed by node; plans immutable and thread-safe; parse state lives in the call.
- A lookup hook so staged code registered by the generator (GEN-01) replaces plan nodes.
- Out: lowering `Rec` cycles. CORE-09 adds it with the `Rec` node.

## Acceptance criteria

- [ ] Concurrent first use from many threads lowers each grammar once and produces correct results.

## Blocked by

- [CORE-01 Grammar node model](../02-core-combinators/CORE-01-grammar-node-model.md)
- [INT-01 Cursor and in-memory input](../03-interpreter/INT-01-cursor-and-in-memory-input.md)

## Unblocks

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [GEN-01 Generator skeleton and registration](../11-source-generator/GEN-01-generator-skeleton-and-registration.md)
- [CORE-09 Recursion with Rec](../02-core-combinators/CORE-09-recursion-with-rec.md)

## Whitepaper references

- [§ Interpreted engine](../../../whitepaper.md#interpreted-engine)

## Artifacts

- [Research: grammar node encoding and dispatch](../../docs/research/2026-10-18-grammar-node-encoding-and-dispatch.md)
- [ADR-0001: Grammar node encoding and engine dispatch](../../docs/decisions/0001-grammar-node-encoding-and-engine-dispatch.md)

## Progress notes

- 2026-10-18 (Build): Started. Add an `internal abstract` lowering member on `Parser<TIn, TOut>` (overridden by `ReturnNode` and `FailNode`), sealed plan nodes with `TryParse(ref Cursor<TIn>, ref TOut)`, a lowering context that memoizes by node so cycles resolve, a `ConditionalWeakTable` plan cache, and an internal staging lookup for GEN-01. The `Rec` node is CORE-09, so cycle handling is tested with a test-only recursive node.
- 2026-10-18 (Build): Moved lowering of `Rec` cycles to CORE-09 with the maintainer, since the `Rec` node does not exist yet; CORE-09 is now blocked by INT-02. Next: `/build` plan.
- 2026-10-18 (Build): Increment 1 of 3 done. Added `src/Creole/Plans/`: `Plan<TIn, TOut>` with `TryParse(ref Cursor<TIn>, ref TOut)`, sealed `ReturnPlan` and `FailPlan`, and `PlanBuilder`, which memoizes by node reference so a node reached twice in one grammar is lowered once. `Parser<TIn, TOut>` gained `internal abstract Lower(PlanBuilder)`; `ReturnNode` and `FailNode` override it. Leaf builder methods are static (CA1822); builder methods for nodes with children will be instance methods. Tests: `PlanLoweringTests` (4) with test-only `CountingNode` and `PairNode` in `TestNodes.cs`. Verified: build clean, `dotnet test` 118 passed, AOT smoke publish clean. No docs page: nothing public changed. Next: increment 2, `PlanCache` and the concurrency test for the criterion.
- 2026-10-18 (Build): Increment 2 of 3 done. Added `PlanCache.Get`: a `ConditionalWeakTable<INode, Lazy<object>>` keyed by the root node, with `ExecutionAndPublication` so lowering runs once even when `GetValue` runs its callback twice in a race. Tests: `PlanCacheTests` (3): repeated use returns the same plan; 64 threads behind a `Barrier`, over 20 rounds of 100 fresh grammars, see one lowering per grammar, one shared plan and correct results (the evidence for the criterion); a cached grammar can still be collected. A mutant that lowers inside the `GetValue` callback failed the concurrency test in 3 of 3 runs (lower counts of 2 and 4). Verified: build clean, `dotnet test` 121 passed, `PlanCacheTests` pass in Release, AOT smoke publish clean. Next: increment 3, the staging hook.
- 2026-10-18 (Build): Merged origin/main (CORE-07's repetition nodes). Their `Lower` overrides call `PlanBuilder.NotYetExecutable`, which throws `NotSupportedException`; executing repetition is INT-03's scope, recorded there. Verified: build clean, `dotnet test` 190 passed, AOT smoke publish clean. Next: increment 3, the staging hook.
- 2026-10-18 (Build): Increment 3 of 3 done. Added internal `Staging` (`Register`, `TryGetPlan`, a `ConditionalWeakTable` keyed by node), the `StagedParse<TIn, TOut>` delegate (`ref Cursor<TIn>`, `out TOut`, the generator's `Parse_<Rule>` shape) and `StagedPlan`, which leaves the result unchanged on failure. `PlanBuilder.Lower` uses registered code before lowering a node, for roots and children. Registering a rule twice throws `InvalidOperationException`; registering after first use does not change the cached plan. GEN-01 makes `Staging` public. Tests: `StagingTests` (5). Verified: build clean, `dotnet test` 195 passed, AOT smoke publish clean. All increments done and the criterion appears met (increment 2's concurrency test). Next: `/complete`.
