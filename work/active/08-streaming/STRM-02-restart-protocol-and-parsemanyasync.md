# STRM-02: Restart protocol and ParseManyAsync

| | |
| --- | --- |
| Epic | [Streaming and async input](../../README.md#strm--streaming-and-async-input) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

Async record streams parse one top-level item at a time and restart that item when data runs dry.

## Scope

- Bookmark across every layer at each item start, including layer states, mode logs and the diagnostic bag.
- On Starved, await at least double the buffered data, rewind and retry.
- `ParseManyAsync` over `PipeReader` and `Stream`, returning `IAsyncEnumerable<ParseResult<T>>`.
- Large JSON-lines peak-memory test.
- A streamed parse records failures as it goes. Measure whether an item that fails can run again from its bookmark with a log instead, as in-memory parses do, and adopt it only for a measured gain ([ADR-0027](../../docs/decisions/0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md)).

## Acceptance criteria

- [ ] Peak memory on a multi-gigabyte JSON-lines input tracks the largest record.

## Blocked by

- [SPAN-06 Byte sources and the decoding layer](../05-source-spans/SPAN-06-byte-sources-and-the-decoding-layer.md)
- [LAY-05 Pipeline API and sources](../06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md)
- [LAY-08 Per-layer parse state](../06-layers-and-pipelines/LAY-08-per-layer-parse-state.md)
- [STRM-01 Starved outcome](../08-streaming/STRM-01-starved-outcome.md)

## Unblocks

- [STRM-03 Streaming diagnostics](../08-streaming/STRM-03-streaming-diagnostics.md)
- [BENCH-02 Full benchmark suite](../13-benchmarks/BENCH-02-full-benchmark-suite.md)
- [RES-01 Resumable async measurement](../17-resumable-async/RES-01-resumable-async-measurement.md)

## Whitepaper references

- [§ The restart protocol](../../../whitepaper.md#the-restart-protocol)
- [§ Sources](../../../whitepaper.md#sources)

## Artifacts

- [Research: Parlot 2.0 baselines and generator](../../docs/research/2026-10-24-parlot-2-0-baselines-and-generator.md) Prior art: Parlot 2.0.0's `ParseManyAsync` retries a value on a sticky `HitEnd` and doubles its window (finding 23)

## Progress notes

- 2026-10-24 (Not started): FND-10 research found Parlot 2.0.0's `ParseManyAsync` uses a restart protocol close to Creole's Starved retry (note, finding 23). Read it before designing. Next: unchanged.
