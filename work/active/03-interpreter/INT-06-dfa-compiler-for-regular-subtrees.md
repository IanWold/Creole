# INT-06: DFA compiler for regular subtrees

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

Regular character subtrees and `Longest` over regular alternatives compile to DFA tables. The same automata library serves analysis (CRL0101, CRL0106) and fusion.

## Scope

- Compute the regular property (no `Rec`, `Bind` or state). A DFA must give the same outcome as running the subtree with commit semantics, so the property also excludes, or the construction models, what a plain DFA ignores: an `Or` whose alternative can fail after consuming (`Or(ab, ac)` on `ac` fails committed), `Atomic`, `Lookahead`, `Not` and `Cut`. `Longest` over regular alternatives is unaffected, because each alternative runs atomically. Raised by the CORE-08 review.
- NFA construction, subset construction and minimization.
- `Longest` as one DFA with accepting states labelled by alternative, priority by order.
- Interpreter executes DFA tables with maximal-munch backup.
- Product construction and backup analysis APIs for later analysis items.

## Acceptance criteria

- [ ] DFA execution agrees with the naive `Longest` execution on property-generated inputs.

## Blocked by

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)

## Unblocks

- [ANA-04 Choice checks](../09-static-analysis/ANA-04-choice-checks.md)
- [ANA-07 Linear maximal munch](../09-static-analysis/ANA-07-linear-maximal-munch.md)
- [GEN-05 Generated DFAs, scans and jump tables](../11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md)
- [BENCH-01 Phase 0 speed gate](../13-benchmarks/BENCH-01-phase-0-speed-gate.md)

## Whitepaper references

- [§ Longest match](../../../whitepaper.md#longest-match)
- [§ Interpreted engine](../../../whitepaper.md#interpreted-engine)
- [§ Properties](../../../whitepaper.md#properties)
