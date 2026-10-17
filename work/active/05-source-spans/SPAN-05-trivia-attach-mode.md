# SPAN-05: Trivia attach mode

| | |
| --- | --- |
| Epic | [Source spans and provenance](../../README.md#span--source-spans-and-provenance) |
| Phase | Phase 1 — Layers |
| Size | M |
| Status | Not started |

## Summary

Keep skipped input as leading and trailing trivia, for formatters and refactoring tools.

## Scope

- `Layer.Tokens(item, skip, trivia: TriviaMode.Attach)`.
- Trivia stored as span ranges, not strings; zero cost when off.

## Acceptance criteria

- [ ] A round-trip test reproduces the source exactly from tokens and trivia.

## Blocked by

- [SPAN-02 Out-of-band span storage](../05-source-spans/SPAN-02-out-of-band-span-storage.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Trivia (optional)](../../../whitepaper.md#trivia-optional)
