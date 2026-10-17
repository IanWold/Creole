# SPAN-07: Embedded languages

| | |
| --- | --- |
| Epic | [Source spans and provenance](../../README.md#span--source-spans-and-provenance) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

Run a whole sub-pipeline over a token's text, such as SQL in a string literal, with spans mapped back to the parent source.

## Scope

- `p.Embed(pipeline)`.
- `SourceMap` mapping sub-pipeline spans through escapes to raw characters.
- Sub-pipeline diagnostics land in the parent's bag.

## Acceptance criteria

- [ ] An error inside an escaped embedded string points at the right raw characters.

## Blocked by

- [SPAN-02 Out-of-band span storage](../05-source-spans/SPAN-02-out-of-band-span-storage.md)
- [LAY-05 Pipeline API and sources](../06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md)

## Unblocks

- [REL-04 Public API review and freeze](../14-release/REL-04-public-api-review-and-freeze.md)

## Whitepaper references

- [§ Embedded languages](../../../whitepaper.md#embedded-languages)
- [§ Span rules](../../../whitepaper.md#span-rules)
