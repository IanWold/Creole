# RSLN-03: Build-time diagnostic reporting

| | |
| --- | --- |
| Epic | [Build-time analyzers and code fixes](../../README.md#rsln--build-time-analyzers-and-code-fixes) |
| Phase | Phase 2 — Analysis |
| Size | M |
| Status | Not started |

## Summary

Report CRL findings in the IDE as you type, at the right source locations.

## Scope

- Map findings to Roslyn diagnostics with locations on the offending combinator call.
- Severity configuration through `.editorconfig`.
- Keep analyzer cost acceptable in large solutions.

## Acceptance criteria

- [ ] Every runtime CRL check that applies to readable grammars appears in the IDE.

## Blocked by

- [ANA-03 Loop and recursion checks](../09-static-analysis/ANA-03-loop-and-recursion-checks.md)
- [ANA-04 Choice checks](../09-static-analysis/ANA-04-choice-checks.md)
- [ANA-05 Informational and hygiene checks](../09-static-analysis/ANA-05-informational-and-hygiene-checks.md)
- [ANA-06 Deterministic mode](../09-static-analysis/ANA-06-deterministic-mode.md)
- [RSLN-02 Share the analysis engine with the Roslyn component](../10-roslyn-analyzers/RSLN-02-share-the-analysis-engine-with-the-roslyn-component.md)

## Unblocks

- [RSLN-06 Code fixes for grammar bugs](../10-roslyn-analyzers/RSLN-06-code-fixes-for-grammar-bugs.md)
- [RSLN-07 Grammar bug corpus](../10-roslyn-analyzers/RSLN-07-grammar-bug-corpus.md)
- [REL-02 Documentation](../14-release/REL-02-documentation.md)
- [INC-06 CRL0205 stale span analyzer](../15-incremental-reparsing/INC-06-crl0205-stale-span-analyzer.md)

## Whitepaper references

- [§ Where analysis runs](../../../whitepaper.md#where-analysis-runs)
- [§ Diagnostics](../../../whitepaper.md#diagnostics)
