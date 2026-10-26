# RES-02: Resumable generated parsers

| | |
| --- | --- |
| Epic | [Resumable async (1.x candidate)](../../README.md#res--resumable-async-1x-candidate) |
| Phase | Phase 4 — 1.x releases |
| Status | Not started |
| Gate | Resumable async gate |

## Summary

Emit each rule as a state machine that suspends at the exact read that ran dry (generated engine only).

## Scope

- State-machine emission with parse state on the heap.
- Suspension checks on the read path; measure their cost.
- Opt-in per grammar.
- A `Custom` scanner, a `Bind` continuation and an interpreter fallback cannot suspend: `Scan` is synchronous over a ref struct, and `Bind` runs its continuation on the interpreter. "Never restarts an item" holds only for grammars without them ([Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) finding 19).

## Acceptance criteria

- [ ] `ParseManyAsync` never restarts an item in a grammar with no `Custom` scanner, `Bind` or interpreter fallback; throughput on JSON lines is at least 1.2× the restart driver's when the median record fills a pipe segment, and at least 0.95× on smaller records ([ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md)).

## Blocked by

- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)
- [RES-01 Resumable async measurement](../17-resumable-async/RES-01-resumable-async-measurement.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Why not async combinators](../../../whitepaper.md#why-not-async-combinators)

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
