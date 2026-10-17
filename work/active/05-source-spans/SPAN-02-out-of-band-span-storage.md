# SPAN-02: Out-of-band span storage

| | |
| --- | --- |
| Epic | [Source spans and provenance](../../README.md#span--source-spans-and-provenance) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

Spans travel beside items in the stream buffer, never inside user token types.

## Scope

- Store spans as 32-bit offsets relative to each segment's base; expose `long` publicly.
- Root-layer rule and derived-item rule; input consumed by `skip` is excluded.
- No wrapper objects per item.

## Acceptance criteria

- [ ] A token's span never includes leading whitespace.
- [ ] Memory per buffered item matches the design budget.

## Blocked by

- [SPAN-01 SourceSpan, SourceId and LineMap](../05-source-spans/SPAN-01-sourcespan-sourceid-and-linemap.md)
- [LAY-01 StreamBuffer and pulling cursor](../06-layers-and-pipelines/LAY-01-streambuffer-and-pulling-cursor.md)

## Unblocks

- [SPAN-03 Synthetic items](../05-source-spans/SPAN-03-synthetic-items.md)
- [SPAN-04 Span combinators](../05-source-spans/SPAN-04-span-combinators.md)
- [SPAN-05 Trivia attach mode](../05-source-spans/SPAN-05-trivia-attach-mode.md)
- [SPAN-06 Byte sources and the decoding layer](../05-source-spans/SPAN-06-byte-sources-and-the-decoding-layer.md)
- [SPAN-07 Embedded languages](../05-source-spans/SPAN-07-embedded-languages.md)

## Whitepaper references

- [§ Span rules](../../../whitepaper.md#span-rules)
- [§ Source spans and provenance](../../../whitepaper.md#source-spans-and-provenance)
