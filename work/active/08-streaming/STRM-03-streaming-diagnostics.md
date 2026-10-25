# STRM-03: Streaming diagnostics

| | |
| --- | --- |
| Epic | [Streaming and async input](../../README.md#strm--streaming-and-async-input) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

Streaming parses discard text, so diagnostics resolve their position and snippet when raised.

## Scope

- Line counter updated by the root layer as segments are released.
- Capture line, column and a snippet capped at a configurable length.
- Whole-input parses still resolve lazily.
- `Diagnostics.Render()` uses the captured lines, so streamed and whole-input results render with the same call ([ADR-0024](../../docs/decisions/0024-a-parses-diagnostics-hold-their-source-text.md) point 5).

## Acceptance criteria

- [ ] A diagnostic from record 1,000,000 renders correctly after its text is gone.

## Blocked by

- [ERR-03 Diagnostic rendering](../04-errors-and-recovery/ERR-03-diagnostic-rendering.md)
- [STRM-02 Restart protocol and ParseManyAsync](../08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md)

## Unblocks

- [REL-04 Public API review and freeze](../14-release/REL-04-public-api-review-and-freeze.md)

## Whitepaper references

- [§ Streaming and async input](../../../whitepaper.md#streaming-and-async-input)
