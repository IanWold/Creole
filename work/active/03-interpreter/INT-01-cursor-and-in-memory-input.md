# INT-01: Cursor and in-memory input

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

The allocation-free cursor every engine reads through.

## Scope

- `Cursor<TIn>` ref struct with a fast path for reads inside the current segment.
- In-memory sources over `string`, `ReadOnlyMemory<char>` and arrays of `T`.
- Consumption is read from the cursor (moved or not), per the outcome model.
- Shape the cursor so LAY-01's `StreamBuffer<T>` can back it later without API change.

## Acceptance criteria

- [ ] Micro-benchmarks show no allocation per read.

## Blocked by

- [FND-02 Repository and solution scaffolding](../01-foundations/FND-02-repository-and-solution-scaffolding.md)

## Unblocks

- [INT-02 Plan lowering and cache](../03-interpreter/INT-02-plan-lowering-and-cache.md)
- [LAY-01 StreamBuffer and pulling cursor](../06-layers-and-pipelines/LAY-01-streambuffer-and-pulling-cursor.md)

## Whitepaper references

- [§ Interpreted engine](../../../whitepaper.md#interpreted-engine)
- [§ Results](../../../whitepaper.md#results)
