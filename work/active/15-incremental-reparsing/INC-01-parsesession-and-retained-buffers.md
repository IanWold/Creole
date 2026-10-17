# INC-01: ParseSession and retained buffers

| | |
| --- | --- |
| Epic | [Incremental re-parsing (1.x)](../../README.md#inc--incremental-re-parsing-1x) |
| Phase | Phase 4 — 1.x releases |
| Status | Not started |

## Summary

Editors keep a parse session open and feed it edits.

## Scope

- `pipeline.ParseIncremental(text)` returning a `ParseSession` that keeps every layer's buffer.
- `session.Edit(span, newText)` returning the next `ParseResult`.

## Acceptance criteria

- [ ] A session re-parses correctly after arbitrary edits (initially by full re-parse).

## Blocked by

- [LAY-05 Pipeline API and sources](../06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md)
- [GEN-08 Differential engine-agreement tests](../11-source-generator/GEN-08-differential-engine-agreement-tests.md)

## Unblocks

- [INC-02 Examined-extent tracking](../15-incremental-reparsing/INC-02-examined-extent-tracking.md)

## Whitepaper references

- [§ Retained parses](../../../whitepaper.md#retained-parses)
