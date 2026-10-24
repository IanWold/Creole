# ANA-07: Linear maximal munch

| | |
| --- | --- |
| Epic | [Static analysis](../../README.md#ana--static-analysis) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

Maximal munch can go quadratic when a DFA backs up over unbounded input.

## Scope

- CRL0106: detect token sets whose backup is unbounded, with INT-06's backup analysis over the commit-aware automaton ([ADR-0017](../../docs/decisions/0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md); [prototype note](../../docs/research/2026-10-23-commit-aware-automaton-prototypes.md), finding 7).
- Implement Reps' memoized tokenization (1998) so remaining cases stay linear. Under commit semantics the memo stores the scan's stop state, not a failure flag (finding 8).
- Build the backup analysis and stop-state memoization over INT-06's automata here, with their first user (moved from INT-06).

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

## Artifacts

- [Research: DFA compilation under commit semantics](../../docs/research/2026-10-23-dfa-compilation-under-commit-semantics.md) How regular subtrees can compile to DFAs that agree with commit semantics; construction, errors, alphabet, backup and prior art
- [Research: commit-aware automaton prototypes](../../docs/research/2026-10-23-commit-aware-automaton-prototypes.md) Differential prototypes: the commit rule, lexer sizes, speed, proof sketches, Option B's condition, backup, blow-ups and a wider eager fragment
