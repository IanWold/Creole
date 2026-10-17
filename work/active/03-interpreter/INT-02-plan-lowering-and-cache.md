# INT-02: Plan lowering and cache

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Size | M |
| Status | Not started |

## Summary

On first use a grammar is lowered to an immutable execution plan and cached.

## Scope

- Plan node base: `abstract bool TryParse(ref Cursor<TIn> c, ref TOut result)`.
- Lowering from grammar tree to plan, including `Rec` cycles.
- `ConditionalWeakTable` cache keyed by node; plans immutable and thread-safe; parse state lives in the call.
- A lookup hook so staged code registered by the generator (GEN-01) replaces plan nodes.

## Acceptance criteria

- [ ] Concurrent first use from many threads lowers each grammar once and produces correct results.

## Blocked by

- [CORE-01 Grammar node model](../02-core-combinators/CORE-01-grammar-node-model.md)
- [INT-01 Cursor and in-memory input](../03-interpreter/INT-01-cursor-and-in-memory-input.md)

## Unblocks

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [GEN-01 Generator skeleton and registration](../11-source-generator/GEN-01-generator-skeleton-and-registration.md)

## Whitepaper references

- [§ Interpreted engine](../../../whitepaper.md#interpreted-engine)
