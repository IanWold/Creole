# RSLN-02: Share the analysis engine with the Roslyn component

| | |
| --- | --- |
| Epic | [Build-time analyzers and code fixes](../../README.md#rsln--build-time-analyzers-and-code-fixes) |
| Phase | Phase 2 — Analysis |
| Size | M |
| Status | Not started |

## Summary

The same analysis must run at run time (net11.0) and inside the compiler (netstandard2.0).

## Scope

- Factor analysis over an abstract grammar model, shared by source linking or a common netstandard2.0 library.
- Adapters from runtime nodes and from the build-time model.

## Acceptance criteria

- [ ] Runtime and build-time analysis produce identical findings on a shared test corpus.

## Blocked by

- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [RSLN-01 Roslyn grammar reader](../10-roslyn-analyzers/RSLN-01-roslyn-grammar-reader.md)

## Unblocks

- [RSLN-03 Build-time diagnostic reporting](../10-roslyn-analyzers/RSLN-03-build-time-diagnostic-reporting.md)

## Whitepaper references

- [§ Where analysis runs](../../../whitepaper.md#where-analysis-runs)
