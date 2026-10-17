# ERR-07: Lexer recovery and cascade suppression

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 2 — Analysis |
| Size | M |
| Status | Not started |

## Summary

Lexer layers recover like parsers, and downstream layers do not re-report lexer errors.

## Scope

- `Layer.Tokens(..., onError: Recover.SkipChar(...))` emits an error token and continues.
- `Kind(k)` never matches an error token.
- Drop downstream diagnostics whose unexpected item is an error token.

## Acceptance criteria

- [ ] An input with one bad character produces exactly one diagnostic.

## Blocked by

- [ERR-05 Recover combinator and strategies](../04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)

## Unblocks

- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)
- [BENCH-02 Full benchmark suite](../13-benchmarks/BENCH-02-full-benchmark-suite.md)
- [REL-04 Public API review and freeze](../14-release/REL-04-public-api-review-and-freeze.md)

## Whitepaper references

- [§ Recovery](../../../whitepaper.md#recovery)
