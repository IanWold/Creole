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
- Capture line, column and a snippet: the window rendering would show ([ADR-0029](../../docs/decisions/0029-rendered-errors-bound-text-from-input-with-caller-set-limits.md) point 4), at a width from the parse's options. `Render` narrows it further when its `MaxWidth` is smaller (point 8).
- Whole-input parses still resolve lazily.
- `Diagnostics.Render()` uses the captured lines, so streamed and whole-input results render with the same call ([ADR-0024](../../docs/decisions/0024-a-parses-diagnostics-hold-their-source-text.md) point 5).
- Over a byte root, count lines and columns in the decoded text, not in bytes, so a captured position matches what `LineMap` gives for that text: columns in scalar values, from the first character after any leading byte-order mark ([SPAN-06](../05-source-spans/SPAN-06-byte-sources-and-the-decoding-layer.md)).
- `Parse` and `ParseMany` over a `TextReader` hold captured lines too. Until this card, their errors render one line each, and they take no `source:`, since nothing shows a name. Add `source:` after the reader, where the overloads over text have it ([ADR-0038](../../docs/decisions/0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md) point 2).

## Acceptance criteria

- [ ] A diagnostic from record 1,000,000 renders correctly after its text is gone.

## Blocked by

- [ERR-03 Diagnostic rendering](../04-errors-and-recovery/ERR-03-diagnostic-rendering.md)
- [STRM-02 Restart protocol and ParseManyAsync](../08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md)

## Unblocks

- [REL-04 Public API review and freeze](../14-release/REL-04-public-api-review-and-freeze.md)

## Whitepaper references

- [§ Streaming and async input](../../../whitepaper.md#streaming-and-async-input)
