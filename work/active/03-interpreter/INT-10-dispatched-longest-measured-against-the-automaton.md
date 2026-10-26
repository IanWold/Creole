# INT-10: Dispatched Longest, measured against the automaton

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

Give the interpreter's `Longest` dispatch on the next item, measure it against the compiled automaton, and bound what building automata costs, so GEN-05 knows what to generate.

## Scope

- **Dispatch.** `LongestPlan` tries every alternative. Make it dispatch on each alternative's leading item, as `Or` does ([ADR-0016](../../docs/decisions/0016-or-dispatches-on-each-alternatives-leading-item.md)).
  - A skipped alternative fails without consuming, so it cannot win.
  - A failing `Longest` records the expectations it skipped, as `DispatchPlan` does.
  - Dispatch also covers token-level `Longest` and a `Longest` with non-Regular alternatives, which the automaton never compiles.
- **Measurement.** Measure on the JSON lexer (12 rules) and the C#-like lexer (138 rules) in `LoweringBenchmarks`, median of three runs, with three variants each: dispatched, compiled and undispatched.
  - INT-06 and INT-09 measured the automaton only against the undispatched plan.
  - On JSON, a dispatched `OneOf` with scans ran 383–392 µs (INT-06) and the compiled 12-rule `Longest` 458 µs (INT-09). These come from different runs, so they are indicative only.
- **Construction cost.**
  - Use one work budget for a whole lowering, so a nested `Longest` that fails a limit is not rebuilt from each suffix. INT-09 measured 3 builds, 142.6 ms and 128.9 MB for 400 nested keywords, and [ADR-0025](../../docs/decisions/0025-automaton-construction-stops-at-a-state-cap-or-a-work-budget.md)'s bound holds per build, not per lowering.
  - Set a minimum size below which a `Longest` is not compiled. Every build allocates a 128 KB class table, even for `Longest(String("<"), String("<="))`.
- **Dead code.** Delete the duplicate-live-path code in `CommitAutomaton` that no test reaches, or add a test term that needs it.
- **Decision.** Decide by decision record where the automaton stays: in execution, in analysis only (ANA-04 and ANA-07 need it for exact CRL0101 and CRL0103 either way), or both. Amend [ADR-0017](../../docs/decisions/0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md) if the answer changes it.
- From the [Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md), findings 9 and 10.

## Acceptance criteria

- [ ] The dispatched `LongestPlan` passes the existing `Longest` and lowering tests. `Rewrites.Compare` shows it agrees with the undispatched plan on outcome, value, position, errors and the cut flag.
- [ ] Both lexer categories have dispatched, compiled and undispatched rows, median of three runs, recorded in this card.
- [ ] A test lowers a right-folded `Longest` of 1,000 keywords and an identifier within one work budget.
- [ ] An accepted decision record, or a recorded "no change" to ADR-0017, says where the automaton stays.

## Blocked by

- [INT-06 DFA compiler for regular subtrees](INT-06-dfa-compiler-for-regular-subtrees.md)
- [INT-09 Cheaper automaton construction](INT-09-cheaper-automaton-construction.md)

## Unblocks

- [GEN-05 Generated DFAs, scans and jump tables](../11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md)

## Whitepaper references

- [§ Longest match](../../../whitepaper.md#longest-match)
- [§ Interpreted engine](../../../whitepaper.md#interpreted-engine)
- [§ Properties](../../../whitepaper.md#properties)

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
