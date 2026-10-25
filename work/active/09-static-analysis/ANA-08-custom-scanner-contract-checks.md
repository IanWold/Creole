# ANA-08: Custom scanner contract checks

| | |
| --- | --- |
| Epic | [Static analysis](../../README.md#ana--static-analysis) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

Analysis trusts `ScannerInfo`, so Debug builds verify it.

## Scope

- Debug builds and the tracing interpreter check declared nullability and FIRST on every call. A scanner with no `First` declares any item, so only its nullability and no-consumption-on-failure are checked ([ADR-0012](../../docs/decisions/0012-custom-scanner-metadata.md)).
- Report CRL0206 when a scanner breaks its declaration or consumes on failure.

## Acceptance criteria

- [ ] A deliberately lying scanner triggers CRL0206 in a Debug test run.

## Blocked by

- [INT-04 Execute Bind, Expression.Build and Custom](../../complete/03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md)
- [ANA-02 Analysis report and Debug enforcement](../09-static-analysis/ANA-02-analysis-report-and-debug-enforcement.md)

## Unblocks

- [RSLN-07 Grammar bug corpus](../10-roslyn-analyzers/RSLN-07-grammar-bug-corpus.md)

## Whitepaper references

- [§ Custom primitives](../../../whitepaper.md#custom-primitives)
- [§ Diagnostics](../../../whitepaper.md#diagnostics)
