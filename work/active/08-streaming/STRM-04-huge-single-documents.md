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
