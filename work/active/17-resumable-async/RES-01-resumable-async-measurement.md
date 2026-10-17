# RES-01: Resumable async measurement

| | |
| --- | --- |
| Epic | [Resumable async (1.x candidate)](../../README.md#res--resumable-async-1x-candidate) |
| Phase | Phase 4 — 1.x releases |
| Status | Not started |

## Summary

Decide whether restart overhead and the dedicated-thread option justify resumable parsers.

## Scope

- Measure restart cost on realistic record streams and thread cost for huge documents.
- Go or no-go against the FND-06 threshold.

## Acceptance criteria

- [ ] Decision recorded with numbers.

## Blocked by

- [FND-06 Fix numeric gate thresholds](../01-foundations/FND-06-fix-numeric-gate-thresholds.md)
- [STRM-02 Restart protocol and ParseManyAsync](../08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md)
- [BENCH-02 Full benchmark suite](../13-benchmarks/BENCH-02-full-benchmark-suite.md)

## Unblocks

- [RES-02 Resumable generated parsers](../17-resumable-async/RES-02-resumable-generated-parsers.md)

## Whitepaper references

- [§ Why not async combinators](../../../whitepaper.md#why-not-async-combinators)
