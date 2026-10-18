# RSLN-07: Grammar bug corpus

| | |
| --- | --- |
| Epic | [Build-time analyzers and code fixes](../../README.md#rsln--build-time-analyzers-and-code-fixes) |
| Phase | Phase 2 — Analysis |
| Status | Not started |
| Gate | Phase 2 gate |

## Summary

The Phase 2 gate: classic grammar bugs are caught at build time.

## Scope

- A corpus of deliberately buggy grammars, one per CRL error and warning, each paired with a near miss that must not trigger it.
- A test project that compiles the corpus and asserts the expected build-time diagnostics.

## Acceptance criteria

- [ ] Every buggy grammar produces its expected diagnostic at build time, and no near miss produces one ([ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md)).

## Blocked by

- [ANA-07 Linear maximal munch](../09-static-analysis/ANA-07-linear-maximal-munch.md)
- [ANA-08 Custom scanner contract checks](../09-static-analysis/ANA-08-custom-scanner-contract-checks.md)
- [RSLN-03 Build-time diagnostic reporting](../10-roslyn-analyzers/RSLN-03-build-time-diagnostic-reporting.md)
- [RSLN-04 CRL0004 forward field reference analyzer](../10-roslyn-analyzers/RSLN-04-crl0004-forward-field-reference-analyzer.md)
- [RSLN-05 Grammar class hygiene analyzers](../10-roslyn-analyzers/RSLN-05-grammar-class-hygiene-analyzers.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Diagnostics](../../../whitepaper.md#diagnostics)
- [§ Roadmap](../../../whitepaper.md#roadmap)
