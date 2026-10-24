# STRM-04: Huge single documents

| | |
| --- | --- |
| Epic | [Streaming and async input](../../README.md#strm--streaming-and-async-input) |
| Phase | Phase 1 — Layers |
| Status | Not started |

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

## Progress notes

- 2026-10-24 (Not started): FND-10 research found Parlot 2.0.0 runs large `TextReader` parses synchronously on a pool thread, as Creole plans (note, finding 23). Next: unchanged.
