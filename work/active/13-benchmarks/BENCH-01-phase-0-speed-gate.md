# BENCH-01: Phase 0 speed gate

| | |
| --- | --- |
| Epic | [Benchmarks](../../README.md#bench--benchmarks) |
| Phase | Phase 0 — Core |
| Status | Building |
| Gate | Phase 0 gate |

## Summary

The Phase 0 gate: core tests green, and the interpreter at least as fast as Pidgin and within 1.5× of Parlot.

## Scope

- Character-level JSON and arithmetic grammars on the interpreter.
- Compare against the FND-05 Pidgin (`PidginFolds` for arithmetic) and stable Parlot baselines in the same run; median of three runs on the gate machine ([ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md)).
- Creole runs with its depth limit on (the default, 1,000), and Parlot with its limit off (its default). benchmarks/README.md's known differences say so ([ADR-0020](../../docs/decisions/0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md)).
- Before the gate runs, draft two decision records with `/decide`, from the [Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md):
  - **Value-function purity** (findings 3 and 7). A compiled `Longest` skips value functions the interpreter runs, so the engine choice can change a result. The record sets which user functions must be pure and total and may run any number of times, including zero, and which must be deterministic and may only be skipped where the outcome is already known. It amends § Equivalence and [ADR-0025](../../docs/decisions/0025-automaton-construction-stops-at-a-state-cap-or-a-work-budget.md).
  - **When failures are recorded** (finding 3). Either a successful in-memory parse runs without a failure log and re-runs with one on failure, or recording itself gets cheaper. The record is decided on measurements of three configurations: always recording, log-free with a re-run, and a cheaper-recording prototype.
- Gate runs record the commit and the Parlot version, and run only the gate classes (review finding 6).

## Acceptance criteria

- [ ] The decision records on value-function purity and on when failures are recorded are drafted, and the gate is measured in the configuration the accepted record on recording chooses.
- [ ] The Phase 0 speed thresholds in [ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md) are met (time: geometric mean ≤ 1.0× Pidgin, no benchmark above 1.25× Pidgin, geometric mean ≤ 1.5× Parlot; allocations ≤ Pidgin's on every benchmark), or the gap is understood and recorded.

## Blocked by

- [FND-06 Fix numeric gate thresholds](../01-foundations/FND-06-fix-numeric-gate-thresholds.md)
- [FND-10 Move the Parlot baselines to 2.0.0](../01-foundations/FND-10-move-the-parlot-baselines-to-200.md)
- [INT-05 Interpreter lowering rewrites](../03-interpreter/INT-05-interpreter-lowering-rewrites.md)
- [INT-06 DFA compiler for regular subtrees](../03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md)
- [INT-08 Single-layer Parse API](../03-interpreter/INT-08-single-layer-parse-api.md)
- [ERR-02 Furthest-failure tracking and labels](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Performance design and benchmark plan](../../../whitepaper.md#performance-design-and-benchmark-plan)
- [§ Roadmap](../../../whitepaper.md#roadmap)

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
- [ADR-0026: Engines may skip a value function whose value is unused and run any user function again](../../docs/decisions/0026-engines-may-skip-unused-values-and-repeat-user-functions.md) (Accepted) Value-function purity: value functions pure, decision functions deterministic

## Progress notes

- 2026-10-25 (Building): Started, ahead of the Phase 0 review, to give the review gate numbers. Added the interpreter to `JsonBenchmarks` (`CreoleJson`, character level, built like the Pidgin grammar) and `ArithmeticBenchmarks` (`CreoleArithmetic`, on `Expression.Build`), with agreement cases in `BaselineAgreementTests` (27 of 27 pass). One `--job short` run is indicative only. Next: after the review's simplifications land, run the gate three times on the gate machine and record the ratios.
- 2026-10-25 (Building): The Phase 0 review measured the gate in a scratch copy (short jobs, indicative). As built: 0.35× Pidgin, 2.00× Parlot, JSON allocation 1.07× Pidgin. With `SepBy` lists sized by default (one line in `SeparatedPlan`): allocation 0.996× Pidgin. With that and no failure log on successful parses: 1.52× Parlot. Next: land the `SepBy` fix and the review's other pre-gate fixes as separate changes, then measure a log-free configuration and a cheaper-recording prototype before the three gate runs ([review](../../docs/research/2026-10-25-phase-0-review.md), findings 1–6).
- 2026-10-25 (Building): Fixed `SepBy` lists starting at capacity 1 (`SeparatedPlan` now starts them empty, as `RepeatPlan` does). `AllocationTests.ASeparatedListAllocatesWhatManyDoesForTheSameItems` failed before (128 against 64 bytes per parse) and passes now; 1,497 of 1,497 tests pass. The review measured the same change at 0.996× Pidgin's allocation on all three JSON inputs. Next: the `Rerun` fallback and the lexer benchmark check.
- 2026-10-25 (Building): A compiled `Longest` whose winner, run again, disagrees with the automaton now falls back to the interpreter's `LongestPlan` instead of returning success; Debug builds still assert. Checked once in Release with a throwaway test (an automaton for `"ab"` over an alternative `"xy"`): it failed before the change and passes after. No test is kept, because in Debug the assertion fires first. 1,497 of 1,497 tests pass. Next: the lexer benchmark check.
- 2026-10-25 (Building): The lexer benchmarks' agreement check could not fail: `Tokenize` skipped a character on each failure and the test asserted only the input length. The lexers now return a fingerprint of each token's end and value, and `LoweringBenchmarksTokenizeAlikeWithAndWithoutTheAutomaton` compares the compiled and interpreted lexers. A throwaway mutation (ties going to the later alternative) failed the new test, and the old check would have passed it; 28 of 28 benchmark tests pass. Next: scope the ADRs this card drafts.
- 2026-10-25 (Building): Scope widened with the maintainer: before the gate runs, this card drafts the decision records on value-function purity and on when failures are recorded, and makes gate runs record their commit and Parlot version. Next: `/decide` on value-function purity, then measure the three recording configurations for the second record.
- 2026-10-26 (Designing): Drafted [ADR-0026](../../docs/decisions/0026-engines-may-skip-unused-values-and-repeat-user-functions.md), Proposed, on value-function purity. Options: exact calls in every engine, two roles (recommended), deferred values, a local fix to the compiled `Longest`. Checked that `Regular` admits no decision function, and that both lexers behind INT-06's speedups contain a `Select` inside an alternative, so the exact option would send them to the interpreter. Next: the maintainer accepts, rejects or revises ADR-0026; on acceptance, update the whitepaper, docs and XML docs, then back to Building for the gate tooling.
- 2026-10-26 (Building): ADR-0026 accepted as written. Whitepaper (§ Functions in grammars, new; § Equivalence, § Interpreted engine, § Longest match), docs, XML docs and six card scopes updated; no behaviour change. Next: increment 2, the gate tooling (`run.json` with commit and Parlot version, a `Gate` category, `scripts/bench-gate.py`).
- 2026-10-26 (Building): Increment 2, gate tooling. Each run folder now has a `run.json` with the commit, whether the tree had uncommitted changes, and the Parlot and Pidgin versions (`RunManifest`). The six gate benchmarks carry a `Gate` category, so `--anyCategories Gate` runs 12 benchmarks. `scripts/bench-gate.py` takes the median ratios over the runs and their geometric means, and checks ADR-0003's Phase 0 thresholds. It refuses runs from different commits or Parlot versions, and warns about uncommitted changes, a job other than the default, a different host, or a run count other than three. Checked on three `--job dry` gate runs: 12 benchmarks each, `run.json` survives the run, and the summary and verdict print. Also checked the mixed-commit refusal and the warning for a run without `run.json`. 28 of 28 benchmark tests pass. benchmarks/README.md's gate section is rewritten, and its stale sentence about hosted runners is gone. Next: increment 3, measuring the three recording configurations with this tooling.
- 2026-10-26 (Building): Increment 3, the three recording configurations, measured at b4dcc38 on the gate machine. Each configuration got three default-job gate runs, summarized by `bench-gate.py`. (a) As built: 0.33× Pidgin, **1.93× Parlot**. (b) The first run has no failure log, and a failed parse runs again with one (only `Parsing.TryRun` changes): 0.26× Pidgin, **1.53× Parlot**. (c) Cheaper recording: an advance resets the message, item and span only if one was set, and the store is inlined. It measured 0.32× Pidgin, **1.86× Parlot**. Allocation was 0.996× Pidgin on JSON and 0.067× on arithmetic in all three. Against (a), Creole's time is 0.73× on arithmetic and 0.77×, 0.81× and 0.86× on small, medium and large JSON with (b), and 0.96–0.97× with (c). Parlot's own times stayed within 2% across the batches. With (b), a failed parse of the medium JSON costs 1.82× (broken in the middle) and 1.87× (broken at the end) as long as (a), and allocates 2.00×, since the first run's values are built again. Both prototypes pass all 1,513 tests. A Stopwatch ablation explains (c): storing each expectation reference into the log's array is most of the cost. Even with expectations not stored at all, recording costs 1.03–1.10× over no log, so cheaper recording alone cannot reach 1.5×. The JSON inputs make about one record per two characters, half advancing the furthest failure and half merging, none behind it. Inspection confirms the review's safety claim for (b): plans reach the log only through `Failures?.` and never branch on recorded state. Prototypes and results were kept in the session scratchpad only. Next: increment 4, draft the record on when failures are recorded. No configuration clears 1.5× Parlot, so criterion 2 will need the remaining gap explained.
