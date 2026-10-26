# GEN-05: Generated DFAs, scans and jump tables

| | |
| --- | --- |
| Epic | [Source-generated engine](../../README.md#gen--source-generated-engine) |
| Phase | Phase 3 — Staging |
| Status | Not started |

## Summary

Bring the interpreter's lowering rewrites into generated code.

## Scope

- Emit DFA tables or code for regular subtrees and `Longest`, with Reps memoization where needed. They are the same commit-aware automata the interpreter builds, with the same fallback when construction passes the state cap or the work budget ([ADR-0017](../../docs/decisions/0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md), [ADR-0025](../../docs/decisions/0025-automaton-construction-stops-at-a-state-cap-or-a-work-budget.md)). Built at compile time, it may pass its own limits. A generated `Longest` records no failure when it matches and re-runs on a non-match, as the interpreter's does ([ADR-0022](../../docs/decisions/0022-a-successful-longest-restores-the-furthest-failure.md)).
- `src/Creole/Analysis/` cannot run inside the netstandard2.0 generator as written: it uses .NET 9 APIs (`IAlternateEqualityComparer`, `GetAlternateLookup`), casts to runtime node classes, and computes named character sets from the running runtime's Unicode data. Either port it with RSLN-02, or have generated code call into the runtime for a compiled `Longest`, so the engines agree by construction. Decide by decision record ([Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) finding 11).
- Emit `SearchValues<char>` scans for character-class runs.
- Emit jump tables for deterministic grammars.

## Acceptance criteria

- [ ] Generated lexers match interpreted DFAs on property-generated inputs.

## Blocked by

- [INT-06 DFA compiler for regular subtrees](../03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md)
- [INT-10 Dispatched Longest, measured against the automaton](../03-interpreter/INT-10-dispatched-longest-measured-against-the-automaton.md)
- [ANA-06 Deterministic mode](../09-static-analysis/ANA-06-deterministic-mode.md)
- [ANA-07 Linear maximal munch](../09-static-analysis/ANA-07-linear-maximal-munch.md)
- [GEN-02 Emit the core combinators](../11-source-generator/GEN-02-emit-the-core-combinators.md)

## Unblocks

- [GEN-08 Differential engine-agreement tests](../11-source-generator/GEN-08-differential-engine-agreement-tests.md)
- [BENCH-02 Full benchmark suite](../13-benchmarks/BENCH-02-full-benchmark-suite.md)
- [FUS-01 Fusion feasibility measurement](../16-layer-fusion/FUS-01-fusion-feasibility-measurement.md)

## Whitepaper references

- [§ Interpreted engine](../../../whitepaper.md#interpreted-engine)
- [§ Deterministic mode](../../../whitepaper.md#deterministic-mode)

## Artifacts

- [Research: DFA compilation under commit semantics](../../docs/research/2026-10-23-dfa-compilation-under-commit-semantics.md) How regular subtrees can compile to DFAs that agree with commit semantics; construction, errors, alphabet, backup and prior art
- [Research: commit-aware automaton prototypes](../../docs/research/2026-10-23-commit-aware-automaton-prototypes.md) Differential prototypes: the commit rule, lexer sizes, speed, proof sketches, Option B's condition, backup, blow-ups and a wider eager fragment
- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
