# STRM-02: Restart protocol and ParseManyAsync

| | |
| --- | --- |
| Epic | [Streaming and async input](../../README.md#strm--streaming-and-async-input) |
| Phase | Phase 1 — Layers |
| Size | L |
| Status | Not started |

## Summary

Async record streams parse one top-level item at a time and restart that item when data runs dry.

## Scope

- Bookmark across every layer at each item start, including layer states, mode logs and the diagnostic bag.
- On Starved, await at least double the buffered data, rewind and retry.
- `ParseManyAsync` over `PipeReader` and `Stream`, returning `IAsyncEnumerable<ParseResult<T>>`.
- Large JSON-lines peak-memory test.

## Acceptance criteria

- [ ] Peak memory on a multi-gigabyte JSON-lines input tracks the largest record.

## Blocked by

- [SPAN-06 Byte sources and the decoding layer](../05-source-spans/SPAN-06-byte-sources-and-the-decoding-layer.md)
- [LAY-05 Pipeline API and sources](../06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md)
- [STRM-01 Starved outcome](../08-streaming/STRM-01-starved-outcome.md)

## Unblocks

- [STRM-03 Streaming diagnostics](../08-streaming/STRM-03-streaming-diagnostics.md)
- [BENCH-02 Full benchmark suite](../13-benchmarks/BENCH-02-full-benchmark-suite.md)
- [RES-01 Resumable async measurement](../17-resumable-async/RES-01-resumable-async-measurement.md)

## Whitepaper references

- [§ The restart protocol](../../../whitepaper.md#the-restart-protocol)
- [§ Sources](../../../whitepaper.md#sources)
