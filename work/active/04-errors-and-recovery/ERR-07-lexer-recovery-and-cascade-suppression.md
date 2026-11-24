# ERR-07: Lexer recovery and cascade suppression

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

Lexer layers recover like parsers, and downstream layers do not re-report lexer errors.

## Scope

- `Layer.Tokens(..., onError: Recover.SkipChar(...))` emits an error token and continues.
- `Kind(k)` never matches an error token.
- Drop downstream diagnostics whose unexpected item is an error token.
- Lexer diagnostics belong to the lexer layer. A parser's mark never withdraws them; they are withdrawn only when the step that raised them is run again or its tokens are discarded ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 4).
- `Layer.Utf8()` ends the parse at the first invalid sequence. Decide whether it recovers as a lexer does: replace each invalid sequence with U+FFFD, raise a diagnostic over its bytes, and carry on ([SPAN-06](../05-source-spans/SPAN-06-byte-sources-and-the-decoding-layer.md)).
- Test `ParseMany`'s rule for lower layers' diagnostics ([ADR-0038](../../docs/decisions/0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md) point 4): one raised in lookahead past an item's end goes to the next item; a zero-width one at an item's end, from the step that produced the item's last token, stays with that item; one left at the end of input comes out in a last `Failed` result. If ERR-05 or SPAN-07 lets a layer below the parser raise a diagnostic first, that card runs these tests.

## Acceptance criteria

- [ ] An input with one bad character produces exactly one diagnostic.
- [ ] Tests from [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 11: counterexample 4, and a re-run step whose earlier outputs were consumed reports its diagnostic once.

## Blocked by

- [ERR-05 Recover combinator and strategies](../04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)

## Unblocks

- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)
- [BENCH-02 Full benchmark suite](../13-benchmarks/BENCH-02-full-benchmark-suite.md)
- [REL-04 Public API review and freeze](../14-release/REL-04-public-api-review-and-freeze.md)

## Whitepaper references

- [§ Recovery](../../../whitepaper.md#recovery)
