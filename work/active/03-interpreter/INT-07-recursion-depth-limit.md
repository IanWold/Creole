# INT-07: Recursion depth limit

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

Untrusted input must never cause a stack overflow.

## Scope

- Count one level while a `Rec`, the parser a `Bind` continuation returned, a prefix operator's operand or an `InfixR` right operand runs ([ADR-0020](../../docs/decisions/0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md)). The counter lives on the cursor, one per layer. `Rec` lowers to a counting step.
- Past depth 20, each counted entry calls `RuntimeHelpers.TryEnsureSufficientExecutionStack()`.
- Exceeding the limit or the stack ends the parse with an internal abort carrying the position, the depth and the cause. No combinator catches it. Tests pass the limit to the internal runner directly.
- Measure what counting costs on an existing Creole benchmark, and record it in the Outcome.
- Out: `ParseOptions.MaxDepth`, its default and the `Failed` result with its diagnostic ([INT-08](INT-08-single-layer-parse-api.md)).

## Acceptance criteria

- [x] A deeply nested input stops the interpreter with a depth abort at the offending position instead of crashing the process. For each counted point, an input at the limit parses and one more level aborts. An input 100,000 levels deep on a 256 KB thread aborts through the stack probe.
- [x] `Not`, `Atomic` and `Or` around the deep part do not change the outcome.

## Blocked by

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ERR-01 Error and result types](../04-errors-and-recovery/ERR-01-error-and-result-types.md)

## Unblocks

- [INT-08 Single-layer Parse API](../03-interpreter/INT-08-single-layer-parse-api.md)
- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)

## Whitepaper references

- [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input)
- [§ Expressions](../../../whitepaper.md#expressions)

## Artifacts

- [ADR-0020](../../docs/decisions/0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md) Depth counts each `Rec`, `Bind` continuation and recursive operand, and exceeding it or the stack ends the parse (Accepted)

## Progress notes

- 2026-10-24 (Designing): Started. The whitepaper fixes the feature but not what counts as a level, how exceeding the limit stops the parse, the default, whether a stack probe backs the limit, or how the card divides work with INT-08, which owns `ParseOptions` and the `Parse` API that returns `Failed`. Parlot (the stated model) counts `Deferred` and `Unary` entries, is off by default and throws `ParseException`. Next: a decision record via `/decide`.
- 2026-10-24 (Designing): Drafted ADR-0020 (Proposed). It is based on measurements of stack use per level, probe cost and unwinding cost on the current interpreter, plus prior art (Parlot, Roslyn `StackGuard`, System.Text.Json, Pidgin, Superpower). The maintainer chose a default of 1,000, and that INT-07 builds the mechanism while INT-08 exposes `ParseOptions.MaxDepth` and returns `Failed`. Next: the maintainer accepts, rejects or revises ADR-0020; on acceptance, update the whitepaper and the INT-07, INT-08, BENCH-01, ERR-05 and GEN-06 cards, then move to Building.
- 2026-10-24 (Building): The maintainer accepted ADR-0020. Updated the whitepaper (§ Limits for untrusted input, § Expressions, § Recovery, § Equivalence) and the INT-07, INT-08, BENCH-01, ERR-05 and GEN-06 cards. INT-08 is now blocked by INT-07. Next: `/build` to plan the counting step, cursor counter, probe and abort against the criteria.
- 2026-10-24 (Building): Built the mechanism. `Cursor<T>` gained internal `Depth`, `MaxDepth` (default `int.MaxValue` until INT-08 sets it from `ParseOptions`), `EnterLevel` and `LeaveLevel`; past depth 20, entering a level calls `TryEnsureSufficientExecutionStack`. The limit is checked before the probe, so the limit wins when both apply. `Rec` lowers to a new `RecPlan`; `BindPlan` counts around the chosen parser; `PrefixOperatorPlan` counts around the operand; `InfixOperatorPlan` counts around the right operand of `InfixR`. Exceeding either throws the internal `DepthExceededException`, which carries `Position`, `Depth` and a `DepthCause` (`MaxDepth` or `Stack`). `DepthLimitTests` (40 tests) covers, for each counted point: limits 5 and 100 at the limit and one level past it; `Not`, `Atomic`, `Or` and `Atomic` inside `Or`; a balanced count; 100,000 levels on a 256 KB thread. Four tests in `RecLoweringTests` and `WorkedExampleATests` now unwrap the `RecPlan`. docs/engines/interpreter.md has a "Deep input" section. All 1,228 tests pass in Debug and `Creole.Tests` in Release; the AOT smoke publishes with no warnings and runs. Cost, measured on `LoweringBenchmarks` `DispatchKinds` (`CreoleTokenJson`, medium document). Old and new builds alternated, three runs each on the default job; allocation unchanged at 206.77 KB:

  | Benchmark | Old (µs) | New (µs) | Median ratio |
  | --- | --- | --- | --- |
  | `KindsWithDispatch` | 82.77, 80.59, 79.30 | 84.37, 85.54, 83.90 | 1.047 |
  | `KindsWithoutDispatch` | 103.46, 100.25, 100.62 | 103.85, 102.40, 104.17 | 1.032 |

  Every criterion appears met. Next: the maintainer reviews and commits, then `/complete` ticks the criteria and moves the cost into the Outcome.
- 2026-10-24 (Done): Both criteria verified on 462dfc4. The full suite passes (1,228), and so do all 40 `DepthLimitTests` cases. An independent critic review agreed, found nothing blocking, and re-measured the cost (in the Outcome).

## Outcome

The interpreter counts recursion depth at the four points [ADR-0020](../../docs/decisions/0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md) names. Past depth 20 it also checks the stack. Going past the limit or the stack ends the parse with an internal abort that no combinator catches. The user-facing half (`ParseOptions.MaxDepth`, its default of 1,000, and the `Failed` result) is [INT-08](INT-08-single-layer-parse-api.md)'s.

- **Counter:**
  - `Cursor<T>` holds `Depth`, `MaxDepth`, `EnterLevel` and `LeaveLevel` in `src/Creole/Cursor.cs`.
  - The fast path is one compare below depth 20. The limit is checked before the probe, so `MaxDepth` wins when both apply.
  - `MaxDepth` defaults to `int.MaxValue`, leaving only the probe, until INT-08 sets it.
- **Abort:** `src/Creole/DepthExceededException.cs` carries `Position` (where the level would have started), `Depth` (the levels already entered) and `Cause`. `Cause` is a `DepthCause` (`src/Creole/DepthCause.cs`): `MaxDepth` or `Stack`.
- **Counted points:**
  - `Rec` lowers to `src/Creole/Plans/RecPlan.cs`.
  - `BindPlan` counts around the chosen parser.
  - `PrefixOperatorPlan` counts around the operand.
  - `InfixOperatorPlan` counts around an `InfixR` right operand only.
- **Tests:**
  - `tests/Creole.Tests/DepthLimitTests.cs` has 40 cases over the four points:
    - at limits 5 and 100, the limit and one level past it;
    - `Not`, `Atomic`, `Or`, and `Atomic` inside `Or`;
    - a balanced count;
    - 100,000 levels on a 256 KB thread.
  - The critic measured where the stack check fired at 100,000 levels on a 256 KB thread: 206–340 levels. With the probe removed, the test host crashed with a stack overflow, so the test does detect a missing probe.
  - `RecLoweringTests` and `WorkedExampleATests` unwrap the new `RecPlan`.
- **Docs:** the "Deep input" section of [docs/engines/interpreter.md](../../../docs/engines/interpreter.md#deep-input).
- **Cost:** measured on `LoweringBenchmarks` `DispatchKinds`, which runs `CreoleTokenJson` on the medium document. Each measurement alternated the old and new builds, three runs each on the default job. Allocation was unchanged at 206.77 KB.

  | Benchmark | Build session, median µs | Critic's rerun, median µs |
  | --- | --- | --- |
  | `KindsWithDispatch` | 80.59 → 84.37 (1.047×) | 85.10 → 90.37 (1.062×) |
  | `KindsWithoutDispatch` | 100.62 → 103.85 (1.032×) | 107.39 → 111.38 (1.037×) |

  So counting costs about 1.03–1.06× on token-level JSON. The benchmark's JSON nests only a few levels, so this is the counter and the extra `RecPlan` step alone; the probe never runs. No Creole benchmark covers prefix or `InfixR` counting yet. ADR-0020 names the remedy if BENCH-01 finds the cost too high: let the `ForwardPlan` that closes a cycle do the count.
- **Deviations:** none.
- **Not verified:** the probe at run time under Native AOT. The AOT smoke publishes with no warnings but parses nothing.
- **Follow-ups:**
  - INT-08 makes the limit settable. The `Rec`, `Bind` and `Expression.Build` pages in `docs/concepts/` should then say that these count.
  - GEN-06 counts and probes at the same points. A `Rec` replaced by `Staging` with compiled code bypasses `RecPlan`.
