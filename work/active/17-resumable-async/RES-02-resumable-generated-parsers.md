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

## Acceptance criteria

- [ ] `ParseManyAsync` never restarts an item; throughput on JSON lines is at least 1.2× the restart driver's when the median record fills a pipe segment, and at least 0.95× on smaller records ([ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md)).

## Blocked by

- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)
- [RES-01 Resumable async measurement](../17-resumable-async/RES-01-resumable-async-measurement.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Why not async combinators](../../../whitepaper.md#why-not-async-combinators)
