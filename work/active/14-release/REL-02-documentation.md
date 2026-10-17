# REL-02: Documentation

| | |
| --- | --- |
| Epic | [Samples, docs and 1.0 release](../../README.md#rel--samples-docs-and-10-release) |
| Phase | Phase 3 — Staging |
| Status | Not started |

## Summary

User documentation for 1.0.

## Scope

- Getting started and concepts: commit semantics, layers, the three feedback tools, spans, recovery.
- One reference page per CRL diagnostic.
- Engines, staging rules and AOT guidance.

## Acceptance criteria

- [ ] Every public API and diagnostic ID is documented.

## Blocked by

- [RSLN-03 Build-time diagnostic reporting](../10-roslyn-analyzers/RSLN-03-build-time-diagnostic-reporting.md)
- [GEN-04 Interpreter fallback and CRL0301](../11-source-generator/GEN-04-interpreter-fallback-and-crl0301.md)
- [TOOL-01 Creole.Testing assertions](../12-grammar-author-tooling/TOOL-01-creoletesting-assertions.md)

## Unblocks

- [REL-05 Ship 1.0](../14-release/REL-05-ship-10.md)

## Whitepaper references

- [§ Developer experience for grammar authors](../../../whitepaper.md#developer-experience-for-grammar-authors)
