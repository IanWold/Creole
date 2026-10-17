# ANA-07: Linear maximal munch

| | |
| --- | --- |
| Epic | [Static analysis](../../README.md#ana--static-analysis) |
| Phase | Phase 2 — Analysis |
| Size | M |
| Status | Not started |

## Summary

Maximal munch can go quadratic when a DFA backs up over unbounded input.

## Scope

- CRL0106: detect token sets whose backup is unbounded.
- Implement Reps' memoized tokenization (1998) so remaining cases stay linear.

## Acceptance criteria

- [ ] Tokens `a` and `a*b` on `aaaa…` are flagged, and lexing time is linear with memoization on.

## Blocked by

- [INT-06 DFA compiler for regular subtrees](../03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md)
- [ANA-02 Analysis report and Debug enforcement](../09-static-analysis/ANA-02-analysis-report-and-debug-enforcement.md)

## Unblocks

- [RSLN-07 Grammar bug corpus](../10-roslyn-analyzers/RSLN-07-grammar-bug-corpus.md)
- [GEN-05 Generated DFAs, scans and jump tables](../11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md)

## Whitepaper references

- [§ Deterministic mode](../../../whitepaper.md#deterministic-mode)
