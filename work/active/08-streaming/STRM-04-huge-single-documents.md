# STRM-04: Huge single documents

| | |
| --- | --- |
| Epic | [Streaming and async input](../../README.md#strm--streaming-and-async-input) |
| Phase | Phase 1 — Layers |
| Status | Done |

## Summary

Cover huge single documents in 1.0 without async combinators.

## Scope

- `Parse(Stream)` and `ParseMany(Stream)` on byte pipelines over a blocking byte root, so a `PipeReader` parses through `AsStream()` with `Layer.Utf8()` decoding and byte offsets kept. The BCL's `AsStream()` is already the blocking adapter ([ADR-0044](../../docs/decisions/0044-huge-documents-parse-on-a-caller-started-thread-and-byte-pipelines-read-a-blocking-stream.md)).
- Document the approach and its one-thread cost: a thread the caller starts with `LongRunning`, not `Task.Run`, and cancellation through `CancelPendingRead`. No `ParseAsync`.

## Acceptance criteria

- [x] A large single document parses from a pipe with bounded memory.

## Blocked by

- [LAY-05 Pipeline API and sources](../06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Why not async combinators](../../../whitepaper.md#why-not-async-combinators)

## Artifacts

- [Research: Parlot 2.0 baselines and generator](../../docs/research/2026-10-24-parlot-2-0-baselines-and-generator.md) Prior art: Parlot 2.0.0 parses large `TextReader` input synchronously on a pool thread over a compacting buffer (finding 23)
- [ADR-0044](../../docs/decisions/0044-huge-documents-parse-on-a-caller-started-thread-and-byte-pipelines-read-a-blocking-stream.md) Accepted: no `ParseAsync`, a documented recipe on a dedicated thread; `Parse(Stream)` and `ParseMany(Stream)` on byte pipelines; cancel at the source

## Progress notes

- 2026-10-24 (Not started): FND-10 research found Parlot 2.0.0 runs large `TextReader` parses synchronously on a pool thread, as Creole plans (note, finding 23). Next: unchanged.
- 2026-11-26 (Designing): Started. `Parse(TextReader)` already reads with bounded memory, and `System.IO.Pipelines` ships in the .NET 11 shared framework (`Microsoft.NETCore.App` 11.0.0), so the open questions are the public surface (a documented recipe, a `ParseAsync` convenience or an adapter type) and whether decoding happens in a `StreamReader` (character offsets only) or in the pipeline's `Layer.Utf8()` over a blocking byte source (byte offsets kept, as § Sources promises). Next: draft the decision record with `/decide`.
- 2026-11-26 (Designing): Drafted ADR-0044 (Proposed). The maintainer chose a documented recipe over `ParseAsync`, and a blocking byte source over `StreamReader` decoding. A check on the 11.0.0 runtime showed `StreamReader` over `PipeReader.AsStream()` blocks, and `CancelPendingRead` makes the read throw `OperationCanceledException`. The record's criteria and plan await approval; it is the working spec in place of the scope's `TextReader` adapter line. Next: on approval, move to Building and run `/build`.
- 2026-11-26 (Building): Increment 1 of 3 (ADR-0044 point 2). Added `Parse(Stream)` and `ParseMany(Stream)` on byte pipelines over a blocking root, `StreamUpstream`, with tests that each matches the in-memory byte overload on eight sources, through a `MemoryStream` and a one-byte-per-read `TrickleStream`. Shifting the root's offsets by one fails 10 of them. Docs: pipelines.md § Streams and table rows, layers.md, docs/README.md. All 2,025 tests pass; the AOT smoke publishes with no warnings. Next: increment 2, the huge-document recipe on a dedicated thread with cancellation tests over a real `Pipe`.
- 2026-11-26 (Building): Increment 2 of 3 (ADR-0044 points 1 and 3). docs/concepts/pipelines.md § Huge documents from a pipe gives the `LongRunning` recipe, its one-thread cost, why not `Task.Run`, cancelling with `CancelPendingRead`, a larger `maxStackSize` for deep nesting, and that the value the grammar builds is not bounded. PipeTests backs each claim over a real `Pipe`: a document written in parts that split characters parses off the pool; `CancelPendingRead` ends it with `OperationCanceledException`; a writer's `Complete(IOException)` ends it with that exception; 10,000 nested levels fail on a 512 KiB stack and parse on 256 MiB. Six repeated runs passed; all 2,029 tests pass. Next: increment 3, the memory measurement for criterion 1.
- 2026-11-26 (Building): Increment 3 of 3 (criterion 1). Added a `json-document` input to `benchmarks/Creole.MemoryGate`: the JSON-lines records joined into one array, written as UTF-8 into a `Pipe` and parsed with `Parse(Stream)` on a `LongRunning` thread, skipping each element. `scripts/memory-gate.py --inputs json-document` runs it outside the Phase 1 gate. On cce1483 with the harness uncommitted: 145,320, 148,952 and 153,744 bytes retained at 10 MB, 100 MB and 1 GB, 1.06× (threshold 1.5×). A byte buffer that releases nothing fails it at 15.08× from 2 MB to 20 MB. The small rise across sizes is not explained; the likely cause, unverified, is a peak taken over more samples that each catch up to 64 KiB of unread pipe bytes. ADR-0044 revised in place with each option's result and a criteria table, and a corrected risk (the gate's Example B is already one long `Parse`). All 2,031 tests pass. Every criterion appears met. Next: maintainer review and commit, optionally rerun the measurement from the clean commit, then `/complete STRM-04`, which accepts ADR-0044 and edits the whitepaper's § Why not async combinators and § Sources.
- 2026-11-26 (Done): Verified on a9aafc2: all 2,031 tests pass, and `python3 scripts/memory-gate.py --inputs json-document` measured 145,200, 148,920 and 149,536 bytes at 10 MB, 100 MB and 1 GB (1.03×). The maintainer accepted ADR-0044; the whitepaper's § Sources and § Why not async combinators now describe it, STRM-03's scope covers the new source, and STRM-02 has a pointer to `StreamUpstream`.

## Outcome

A huge single document from a pipe parses with bounded memory on a thread the caller starts, with no async in the engines ([ADR-0044](../../docs/decisions/0044-huge-documents-parse-on-a-caller-started-thread-and-byte-pipelines-read-a-blocking-stream.md)).

- **Code.** [src/Creole/StreamUpstream.cs](../../../src/Creole/StreamUpstream.cs) is a blocking byte root over a `Stream`: byte *i* has \[*i*, *i* + 1), it never starves, and it leaves the stream open. [src/Creole/Pipeline.cs](../../../src/Creole/Pipeline.cs) adds `Parse(Stream)` and `ParseMany(Stream)` to byte pipelines. A `PipeReader` reaches them through `AsStream()`, so no adapter type and no `ParseAsync` were added.
- **Tests.** [ByteSourceDiagnosticsTests.cs](../../../tests/Creole.Tests/ByteSourceDiagnosticsTests.cs) shows both overloads return the in-memory byte overloads' values and errors on eight sources, through a `MemoryStream` and a one-byte-per-read [TrickleStream](../../../tests/Creole.Tests/Utilities/TrickleStream.cs). [PipeTests.cs](../../../tests/Creole.Tests/PipeTests.cs) runs the recipe over a real `Pipe`: a document in parts, `CancelPendingRead`, a writer's exception, and a larger stack for deep nesting. [PipelineTests.cs](../../../tests/Creole.Tests/PipelineTests.cs), [ParseManyTests.cs](../../../tests/Creole.Tests/ParseManyTests.cs) and [ParseResultTests.cs](../../../tests/Creole.Tests/ParseResultTests.cs) cover the overloads like the `TextReader` ones.
- **Criterion (bounded memory).** The `json-document` input of [benchmarks/Creole.MemoryGate](../../../benchmarks/Creole.MemoryGate/JsonDocumentInput.cs) writes 3.6 million JSON-lines records as one array into a `Pipe` and parses it with `Parse(Stream)` on a `LongRunning` thread. On a9aafc2 it retained 145,200, 148,920 and 149,536 bytes above its baseline at 10 MB, 100 MB and 1 GB: 1.03×, against the Phase 1 threshold of 1.5× that ADR-0044 borrowed. A byte buffer that releases nothing fails it at 15.08×. `scripts/memory-gate.py --inputs json-document` runs it; the Phase 1 gate's own inputs are unchanged.
- **Docs.** [docs/concepts/pipelines.md](../../../docs/concepts/pipelines.md) § Streams and § Huge documents from a pipe (the recipe, its one-thread cost, why not `Task.Run`, cancelling, stack size, and what the grammar keeps); [docs/concepts/layers.md](../../../docs/concepts/layers.md); [benchmarks/README.md § Memory gate](../../../benchmarks/README.md#memory-gate).
- **Deviation from the scope.** The card asked for a blocking `TextReader` adapter. ADR-0044 replaced it with a blocking byte source, so spans keep byte offsets and invalid UTF-8 ends the parse as it does in memory; the whitepaper's § Why not async combinators and § Sources were updated on acceptance.
- **Follow-ups.** None new. STRM-03 now covers captured lines and `source:` for the `Stream` overloads. RES-01 may revisit `ParseAsync` (ADR-0044, "What would change this").
