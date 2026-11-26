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
- `SourceMap` mapping sub-pipeline spans through escapes to raw characters. A character that came from raw text maps to a span wider than zero, and a synthetic item stays zero-width ([ADR-0034](../../docs/decisions/0034-a-synthetic-item-is-one-whose-span-is-zero-width.md) point 6).
- The sub-pipeline's root reports `InputStart` and `InputEnd` as the mapped start and end of the token's text, so its trivia stays inside that text ([ADR-0036](../../docs/decisions/0036-trivia-is-the-source-between-items-read-with-a-zero-width-parser.md) point 5).
- Sub-pipeline diagnostics land in the parent's bag.
- Add `Embed` to the nodes that give a grammar full marks in [PlanBuilder.Marks.cs](../../../src/Creole/Plans/PlanBuilder.Marks.cs) ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 9, [ERR-04](../04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md) Outcome).
- If this card is the first to let a layer below the parser raise a diagnostic, it puts layer diagnostics in a pipeline's results and builds and tests what is listed for them in [ERR-07](../04-errors-and-recovery/ERR-07-lexer-recovery-and-cascade-suppression.md) ([ADR-0038](../../docs/decisions/0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md) point 4).

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

## Artifacts

- [ADR-0034: A synthetic item is one whose span is zero-width, and no flag is stored](../../docs/decisions/0034-a-synthetic-item-is-one-whose-span-is-zero-width.md)
