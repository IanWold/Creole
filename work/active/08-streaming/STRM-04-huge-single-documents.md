# STRM-04: Huge single documents

| | |
| --- | --- |
| Epic | [Streaming and async input](../../README.md#strm--streaming-and-async-input) |
| Phase | Phase 1 — Layers |
| Status | Building |

## Summary

Cover huge single documents in 1.0 without async combinators.

## Scope

- Blocking `TextReader` adapter over a `PipeReader`, run on a dedicated thread.
- Document the approach and its one-thread cost.

## Acceptance criteria

- [ ] A large single document parses from a pipe with bounded memory.

## Blocked by

- [LAY-05 Pipeline API and sources](../06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Why not async combinators](../../../whitepaper.md#why-not-async-combinators)

## Artifacts

- [Research: Parlot 2.0 baselines and generator](../../docs/research/2026-10-24-parlot-2-0-baselines-and-generator.md) Prior art: Parlot 2.0.0 parses large `TextReader` input synchronously on a pool thread over a compacting buffer (finding 23)
- [ADR-0044](../../docs/decisions/0044-huge-documents-parse-on-a-caller-started-thread-and-byte-pipelines-read-a-blocking-stream.md) Proposed: no `ParseAsync`, a documented recipe on a dedicated thread; `Parse(Stream)` and `ParseMany(Stream)` on byte pipelines; cancel at the source

## Progress notes

- 2026-10-24 (Not started): FND-10 research found Parlot 2.0.0 runs large `TextReader` parses synchronously on a pool thread, as Creole plans (note, finding 23). Next: unchanged.
- 2026-11-26 (Designing): Started. `Parse(TextReader)` already reads with bounded memory, and `System.IO.Pipelines` ships in the .NET 11 shared framework (`Microsoft.NETCore.App` 11.0.0), so the open questions are the public surface (a documented recipe, a `ParseAsync` convenience or an adapter type) and whether decoding happens in a `StreamReader` (character offsets only) or in the pipeline's `Layer.Utf8()` over a blocking byte source (byte offsets kept, as § Sources promises). Next: draft the decision record with `/decide`.
- 2026-11-26 (Designing): Drafted ADR-0044 (Proposed). The maintainer chose a documented recipe over `ParseAsync`, and a blocking byte source over `StreamReader` decoding. A check on the 11.0.0 runtime showed `StreamReader` over `PipeReader.AsStream()` blocks, and `CancelPendingRead` makes the read throw `OperationCanceledException`. The record's criteria and plan await approval; it is the working spec in place of the scope's `TextReader` adapter line. Next: on approval, move to Building and run `/build`.
- 2026-11-26 (Building): Increment 1 of 3 (ADR-0044 point 2). Added `Parse(Stream)` and `ParseMany(Stream)` on byte pipelines over a blocking root, `StreamUpstream`, with tests that each matches the in-memory byte overload on eight sources, through a `MemoryStream` and a one-byte-per-read `TrickleStream`. Shifting the root's offsets by one fails 10 of them. Docs: pipelines.md § Streams and table rows, layers.md, docs/README.md. All 2,025 tests pass; the AOT smoke publishes with no warnings. Next: increment 2, the huge-document recipe on a dedicated thread with cancellation tests over a real `Pipe`.
