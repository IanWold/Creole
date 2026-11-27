# STRM-02: Restart protocol and ParseManyAsync

| | |
| --- | --- |
| Epic | [Streaming and async input](../../README.md#strm--streaming-and-async-input) |
| Phase | Phase 1 — Layers |
| Status | Designing |

## Summary

Async record streams parse one top-level item at a time and restart that item when data runs dry.

## Scope

- Bookmark across every layer at each item start, including layer states, mode logs and each layer's diagnostics. Each item starts with no furthest failure and no cut in force.
- On Starved, await at least double the buffered data, rewind and retry.
- The restart bookmark is part of each layer's horizon, so `ParseOptions.MaxBufferedItems` also caps one item. Decide how data the driver has read ahead counts toward it, and stop doubling at the limit, so an item that reaches it ends with the limit's error rather than retrying ([ADR-0039](../../docs/decisions/0039-each-layers-buffer-is-bounded-from-its-horizon-and-exceeding-it-ends-the-parse.md) point 6).
- `ParseManyAsync` over `PipeReader` and `Stream`, returning `IAsyncEnumerable<ParseResult<T>>`.
- Large JSON-lines peak-memory test.
- A streamed parse records failures as it goes. Measure whether an item that fails can run again from its bookmark with a log instead, as in-memory parses do, and adopt it only for a measured gain ([ADR-0027](../../docs/decisions/0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md)). If adopted, it applies to `ParseMany` over every source too, which records as it goes until then ([ADR-0038](../../docs/decisions/0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md) point 3).
- On Starved, undo mode changes, clear every layer's failure record and the Starved flag, and reset scoped state to its base. Keep the pins the layers and the driver hold ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 7).
- `ParseManyAsync` follows `ParseMany`'s rules ([ADR-0038](../../docs/decisions/0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md) points 2 to 4). The pipeline's parser is the item parser, and a failed item or a layer failure is the last result. An item holds the parser's diagnostics raised while parsing it and a lower layer's from the steps it reads, so a diagnostic raised in lookahead past an item's end waits for the next item. It was raised before that item's bookmark, so a restart of that item keeps it ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 4).
- A `Stream` or `PipeReader` is a byte root: byte *i* has \[*i*, *i* + 1), and a pipeline over it starts with `Layer.Utf8()`. The decoder treats input that ends inside a sequence as invalid, so when buffered data runs out the root must end the attempt Starved, not report the end of its input ([SPAN-06](../05-source-spans/SPAN-06-byte-sources-and-the-decoding-layer.md)).

## Acceptance criteria

- [ ] Peak memory on a multi-gigabyte JSON-lines input tracks the largest record.
- [ ] Test from [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 11: an attempt that starves inside a `Lookahead` and a `Context`, after recording a failure, leaves no failure, save or frame after the retry.

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
- [ADR-0044](../../docs/decisions/0044-huge-documents-parse-on-a-caller-started-thread-and-byte-pipelines-read-a-blocking-stream.md) Accepted: byte pipelines read a blocking `Stream` through `StreamUpstream`, which never starves
- [ADR-0045](../../docs/decisions/0045-parsemanyasync-reads-a-stream-and-restarts-a-starved-item-from-a-bookmark-in-every-layer.md) Proposed: `ParseManyAsync` over a `Stream`, a bookmark in every layer, a retry once the data has doubled or the source is idle, up to the buffer limit, lazy yielding, and a trial of failure-log re-runs

## Progress notes

- 2026-10-24 (Not started): FND-10 research found Parlot 2.0.0's `ParseManyAsync` uses a restart protocol close to Creole's Starved retry (note, finding 23). Read it before designing. Next: unchanged.
- 2026-11-25 (Not started): STRM-01 is done. The restart clears `StreamBuffer.Starved`, which has only a private setter. Once a starving source exists, docs/engines/cursor.md and docs/concepts/custom-scanners.md should say that where streamed input runs out, `TryPeek`, `IsAtEnd` and an empty `Available` look like the end, and the item is restarted. Dispatch fails on a starved look at the next item, even where trying alternatives in order would have succeeded first with one that reads nothing; at an item's end that means waiting for data past it, so check it against request-and-response protocols. Next: unchanged.
- 2026-11-26 (Not started): STRM-04 added `src/Creole/StreamUpstream.cs`, a blocking byte root over a `Stream` that never starves, behind `Parse(Stream)` and `ParseMany(Stream)` (ADR-0044). The starving root this card needs can start from its slot layout. ADR-0044 expects the overloads to stay if one root can serve both cases. `PipeTests` and the `json-document` memory input show how a `Pipe` is driven in tests and in the harness. Next: unchanged.
- 2026-11-27 (Designing): Started. The card leaves its own choices open and no record covers them: the starving byte root and whether `StreamUpstream` becomes it, how `ParseManyAsync` reads a `PipeReader` and a `Stream` and doubles its window, how read-ahead data counts toward `MaxBufferedItems`, when an item is yielded relative to the look for the next one, and the API shape. The failure-log re-run stays a build measurement. Next: draft the record with `/decide`, reading Parlot 2.0.0's `ParseManyAsync` source for its Context.
- 2026-11-27 (Designing): Drafted ADR-0045 (Proposed). The maintainer chose `Stream` only (fork 1). Recommended by argument: every layer back to a bookmark (2A), received bytes held in the root and counted toward the limit (3A), doubling up to the cap (4A), yielding before the look for the next item (5A). Fork 6, failure-log re-runs, is a trial (criterion 8). Checked on .NET 11.0.0: `Pipe` thresholds, a non-consuming reader does not stall the writer, `AsStream().ReadAsync` behaviour. Next: the maintainer approves the options, criteria and trial plan, then `/build`.
- 2026-11-27 (Designing): Parlot 2.0.0's driver also reads until its window doubles, and its guide says that stalls request-and-response sources. The maintainer chose fork 4C: retry once the data has doubled, or when a short read is followed by one that does not complete at once. ADR-0045 now cites Parlot's source and records a correction to finding 23 (its `ParseManyAsync` positions saturate, not cap). Next: the maintainer approves the options, criteria and trial plan, then `/build`.
