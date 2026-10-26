# LAY-05: Pipeline API and sources

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

Chain layers into a parser and run it over in-memory or reader input.

## Scope

- `Pipeline.From(layer).Then(...)`.
- `Parse(text, source:)` and lazy `ParseMany` returning `IEnumerable<ParseResult<T>>`.
- `TextReader` sources with blocking pulls.
- Slices follow [ADR-0013](../../docs/decisions/0013-slices-view-in-memory-input-and-copy-streamed-input.md): `Parse(text)` builds its root cursor over the caller's memory, so slices are views; over a `TextReader` they are copies.
- Immutable, thread-shareable pipelines; each call owns its buffers and state.
- `SourceRegistry` has no producer: `Parse` always uses `SourceId.Unnamed`, and a source's name is held both in the registry and in `SourceText.Name`. Decide whether pipelines take registry ids or the registry goes ([Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) finding 25).
- A pipeline's result holds its root source text, as [INT-08](../03-interpreter/INT-08-single-layer-parse-api.md)'s does, so Example A's `result.Diagnostics.Render()` shows the full format ([ADR-0024](../../docs/decisions/0024-a-parses-diagnostics-hold-their-source-text.md) points 1 and 5).
- A pipeline over in-memory input runs without failure logs and, when it fails, runs again whole with them ([ADR-0027](../../docs/decisions/0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md)). Decide whether the second run reuses items already buffered or produces them again.

## Acceptance criteria

- [ ] Example A's pipeline runs end to end and reports diagnostics against the file.

## Blocked by

- [ERR-01 Error and result types](../04-errors-and-recovery/ERR-01-error-and-result-types.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)

## Unblocks

- [SPAN-07 Embedded languages](../05-source-spans/SPAN-07-embedded-languages.md)
- [LAY-07 Three-layer demo and bounded-memory check](../06-layers-and-pipelines/LAY-07-three-layer-demo-and-bounded-memory-check.md)
- [STRM-02 Restart protocol and ParseManyAsync](../08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md)
- [STRM-04 Huge single documents](../08-streaming/STRM-04-huge-single-documents.md)
- [TOOL-01 Creole.Testing assertions](../12-grammar-author-tooling/TOOL-01-creoletesting-assertions.md)
- [TOOL-02 Tracing](../12-grammar-author-tooling/TOOL-02-tracing.md)
- [INC-01 ParseSession and retained buffers](../15-incremental-reparsing/INC-01-parsesession-and-retained-buffers.md)

## Whitepaper references

- [§ Running pipelines](../../../whitepaper.md#running-pipelines)
- [§ Sources](../../../whitepaper.md#sources)

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
