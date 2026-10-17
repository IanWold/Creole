# RSLN-05: Grammar class hygiene analyzers

| | |
| --- | --- |
| Epic | [Build-time analyzers and code fixes](../../README.md#rsln--build-time-analyzers-and-code-fixes) |
| Phase | Phase 2 — Analysis |
| Size | S |
| Status | Not started |

## Summary

Nudge authors toward grammars the generator can stage.

## Scope

- CRL0401: grammar class is not `partial`; fix adds `partial`.
- Ask for the `static` modifier on non-capturing lambdas passed to `Select`.

## Acceptance criteria

- [ ] Both diagnostics fire and their fixes apply cleanly.

## Blocked by

- [RSLN-01 Roslyn grammar reader](../10-roslyn-analyzers/RSLN-01-roslyn-grammar-reader.md)

## Unblocks

- [RSLN-07 Grammar bug corpus](../10-roslyn-analyzers/RSLN-07-grammar-bug-corpus.md)

## Whitepaper references

- [§ Source-generated engine](../../../whitepaper.md#source-generated-engine)
- [§ Diagnostics](../../../whitepaper.md#diagnostics)
