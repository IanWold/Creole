# GEN-05: Generated DFAs, scans and jump tables

| | |
| --- | --- |
| Epic | [Source-generated engine](../../README.md#gen--source-generated-engine) |
| Phase | Phase 3 — Staging |
| Size | L |
| Status | Not started |

## Summary

Bring the interpreter's lowering rewrites into generated code.

## Scope

- Emit DFA tables or code for regular subtrees and `Longest`, with Reps memoization where needed.
- Emit `SearchValues<char>` scans for character-class runs.
- Emit jump tables for deterministic grammars.

## Acceptance criteria

- [ ] Generated lexers match interpreted DFAs on property-generated inputs.

## Blocked by

- [INT-06 DFA compiler for regular subtrees](../03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md)
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
