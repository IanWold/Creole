# INT-12: Close the interpreter test gaps BENCH-01 left

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 1 — Layers |
| Status | Building |

## Summary

Two behaviours that [BENCH-01](../13-benchmarks/BENCH-01-phase-0-speed-gate.md) relies on were checked once or by inspection, and no kept test guards them. Add tests that can fail for each.

## Scope

- **The compiled `Longest`'s fallback.** When the winner, run again on the interpreter, does not match the input the automaton gave it, `DfaLongestPlan.Rerun` falls back to the interpreter's `LongestPlan`. BENCH-01 checked this once in Release with a throwaway test. In Debug builds, which the tests run in, `Debug.Assert` fires first, so no test was kept. Make the fallback testable in the configuration the tests run in. One way is to report a disagreement through something a test can observe instead of an assertion. Then keep a test that a wrong automaton still gives the interpreter's result.
- **Where a run without a failure log stops.** `Parse`'s first run has no failure log ([ADR-0027](../../docs/decisions/0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md)). `Execution.Disagreement` compares its result and error with one run that has a log, but not where each run stopped, because `Parse` does not report it. So the claim that both runs consume the same input rests on inspection. Compare end positions at plan level, with and without a log, over the same grammars and inputs.

## Acceptance criteria

- [ ] A kept test builds a compiled `Longest` whose automaton disagrees with its alternatives and shows the parse returns the interpreter's result, in the build configuration the test suite runs.
- [ ] Runs with and without a failure log are compared on where they stop as well as on value and error, across the error tests' grammars and every control shape to depth 3.
- [ ] A throwaway mutation, recorded in the progress notes, shows each new test can fail.

## Blocked by

- [INT-06 DFA compiler for regular subtrees](INT-06-dfa-compiler-for-regular-subtrees.md)
- [BENCH-01 Phase 0 speed gate](../13-benchmarks/BENCH-01-phase-0-speed-gate.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Interpreted engine](../../../whitepaper.md#interpreted-engine)
- [§ Failure positions](../../../whitepaper.md#failure-positions)

## Progress notes

- 2026-11-27 (Build): Started. Replace the `Debug.Assert` in `DfaLongestPlan.Rerun` with a disagreement a test can observe, then test a deliberately wrong automaton; compare plan-level end positions with and without a failure log over the error tests' grammars and the control shapes to depth 3.
- 2026-11-27 (Build): Increment 1, the compiled `Longest`'s fallback. `DfaLongestPlan` takes an optional `disagreed` callback that `Rerun` calls when the winner, run again, does not match what the automaton gave it; with none, Debug builds still assert, so lowering keeps the assertion and only a test passes a callback. `DfaLoweringTests.ACompiledLongestWhoseAutomatonIsWrongGivesTheInterpretersResult` builds two wrong automata (one for `ab | a` over `a | ab`, one for `ab | abc` over `xy | xyz`) and checks each parse of `ab` gives what the same `Longest` lowered with `LoweringOptions.None` gives, (true, "ab", 2) and a failure, with one disagreement reported. Throwaway mutation: `Rerun` returning success instead of falling back fails it with (True, "a", 1). Without a callback, the test fails in Debug with xUnit's `Trace/Debug.Assert() Failure`, so the assertion still fires there. 2,262 of 2,263 tests pass; `NodeModelTests.NodeTypesAreInternalAndSealed` fails at fad0ba5 without this change, since that commit unsealed the node types. Next: increment 2, plan-level end positions with and without a failure log in `Execution.Disagreement`.
- 2026-11-27 (Build): Increment 2, where a run without a failure log stops. `Execution.Disagreement` now also runs the cached plan twice, set up as `Parse` sets up its runs (`Cursor.FromMemory`, `MaxDepth`), once without a log and once with one, and reports a difference in how each ended (succeeded, failed, or exceeded `MaxDepth`) or at what position. It reaches the error tests through `Execution.Error` (90 call sites), every control shape to depth 3 on every input to length 3 through `ShapeDisagreement`, and 9 other direct calls, with no new call sites. The control-shape test went from 17.7–18.1 s to 18.9 s. Throwaway mutations, each run against the old and the new helper over all of Creole.Tests: (A) `StringPlan` rewinding after a failure only with a log fails two more tests with the new helper, `FailureRecordingTests.AStringExpectsItselfWhereItStarted` and the JSON case `tru`; the 11 it fails under both run without a log and check positions directly. (B) `AtomicPlan` rewinding only with a log fails 43 more, among them `EveryControlShapeToDepthThreeFailsAsOneRunWithTheLogDoes` (`Atomic(ab) on "a": the run without a log failed at` 1, the run with one at 0) and error tests in eight files. Under the old helper no test that goes through `Disagreement` failed either mutation. 2,262 of 2,263 tests pass, the exception being the `NodeTypesAreInternalAndSealed` failure from fad0ba5. `StreamBufferTests.SpannedAtEveryItemAllocatesNothingPerItem` and `HoldingASpanAtEachItemAsItGoesAllocatesNothingPerItem` failed in 1 of 3 full runs with this change, and the first in 1 of 3 at the previous helper: each measures one 1M-item run with `attempts: 1`, so a collection that parallel tests cause adds to its count. Every criterion appears met. Next: `/complete`.
