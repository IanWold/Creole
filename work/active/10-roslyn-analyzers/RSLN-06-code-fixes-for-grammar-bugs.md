# RSLN-06: Code fixes for grammar bugs

| | |
| --- | --- |
| Epic | [Build-time analyzers and code fixes](../../README.md#rsln--build-time-analyzers-and-code-fixes) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

Offer a fix wherever the fix is mechanical.

## Scope

- Rewrite left-recursive operator rules with `Expression.Build`.
- Wrap an overlapping alternative in `Atomic`.
- Rewrite an independent LINQ query as `Seq`.

## Acceptance criteria

- [ ] Each fix is covered by a before-and-after test.

## Blocked by

- [RSLN-03 Build-time diagnostic reporting](../10-roslyn-analyzers/RSLN-03-build-time-diagnostic-reporting.md)
- [RSLN-04 CRL0004 forward field reference analyzer](../10-roslyn-analyzers/RSLN-04-crl0004-forward-field-reference-analyzer.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Developer experience for grammar authors](../../../whitepaper.md#developer-experience-for-grammar-authors)
