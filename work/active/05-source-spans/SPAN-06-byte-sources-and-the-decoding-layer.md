# SPAN-06: Byte sources and the decoding layer

| | |
| --- | --- |
| Epic | [Source spans and provenance](../../README.md#span--source-spans-and-provenance) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

Decoding is just another layer, and spans carry both byte and character offsets.

## Scope

- A `Layer<byte, char>` decoder, UTF-8 at minimum, with diagnostics for invalid sequences.
- Spans carry byte offsets and character offsets when a decoding layer exists; text-rooted pipelines skip the character fields.
- Store the four offsets densely in the stream buffer. Until then, a span whose character offsets differ from its root offsets is kept whole beside its segment ([SPAN-02](SPAN-02-out-of-band-span-storage.md)).

## Acceptance criteria

- [ ] Diagnostics on a UTF-8 byte source report correct byte offsets and correct line and column.

## Blocked by

- [SPAN-02 Out-of-band span storage](../05-source-spans/SPAN-02-out-of-band-span-storage.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)

## Unblocks

- [STRM-02 Restart protocol and ParseManyAsync](../08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md)

## Whitepaper references

- [§ Source spans and provenance](../../../whitepaper.md#source-spans-and-provenance)
- [§ Sources](../../../whitepaper.md#sources)
