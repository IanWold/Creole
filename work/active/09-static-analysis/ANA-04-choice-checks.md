# ANA-04: Choice checks

| | |
| --- | --- |
| Epic | [Static analysis](../../README.md#ana--static-analysis) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

Warn about alternatives that can never succeed or never be tried.

## Scope

- CRL0101: shadowed alternative, exact via a product of INT-06's commit-aware automata when both are regular, otherwise from FIRST sets ([ADR-0017](../../docs/decisions/0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md)). Regex automata would be wrong here: the regex reading of a regular rule can disagree with the interpreter ([prototype note](../../docs/research/2026-10-23-commit-aware-automaton-prototypes.md), finding 4).
- CRL0102: overlapping FIRST in `Or` without `Atomic`.
- CRL0103: two `Longest` alternatives match the same string, decided on the same automata.
- Build the product construction over INT-06's automata here, with its first user (moved from INT-06).
- CRL0105: `Match` leaves a union case without a branch.

## Acceptance criteria

- [ ] Each check fires on its whitepaper example with no false positives on Examples A to C.

## Blocked by

- [INT-06 DFA compiler for regular subtrees](../03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md)
- [ANA-02 Analysis report and Debug enforcement](../09-static-analysis/ANA-02-analysis-report-and-debug-enforcement.md)

## Unblocks

- [RSLN-03 Build-time diagnostic reporting](../10-roslyn-analyzers/RSLN-03-build-time-diagnostic-reporting.md)

## Whitepaper references

- [§ Diagnostics](../../../whitepaper.md#diagnostics)

## Artifacts

- [Research: DFA compilation under commit semantics](../../docs/research/2026-10-23-dfa-compilation-under-commit-semantics.md) How regular subtrees can compile to DFAs that agree with commit semantics; construction, errors, alphabet, backup and prior art
- [Research: commit-aware automaton prototypes](../../docs/research/2026-10-23-commit-aware-automaton-prototypes.md) Differential prototypes: the commit rule, lexer sizes, speed, proof sketches, Option B's condition, backup, blow-ups and a wider eager fragment
