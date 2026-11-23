# ERR-04: Diagnostic withdrawal

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 1 — Layers |
| Status | Building |

## Summary

Diagnostics raised on paths that are later abandoned must disappear, so every error is reported once.

## Scope

- Every diagnostic records the layer and the step that raised it.
- Marks record their own layer's diagnostic count. Rewinding a mark withdraws the diagnostics its own layer raised after it, never another layer's ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 4).
- Marks save the diagnostic count only in a layer whose grammar contains `Recover`, `OrInsert`, `Embed` or `Bind`. A `Longest` whose winner can raise diagnostics runs the winner again when another alternative ran after it ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) points 6 and 9; [ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 6).
- A hook for mode invalidation and restarts to withdraw the diagnostics raised by steps that are run again or whose outputs are discarded (used by MODE-02 and STRM-02).
- A withdrawn diagnostic that was built from the furthest failure gives that failure back, before the rewind undoes mode changes ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 8).

## Acceptance criteria

- [ ] Property test: no diagnostic survives from an abandoned alternative.

## Blocked by

- [ERR-01 Error and result types](../04-errors-and-recovery/ERR-01-error-and-result-types.md)
- [LAY-02 Marks and retention](../06-layers-and-pipelines/LAY-02-marks-and-retention.md)

## Unblocks

- [ERR-05 Recover combinator and strategies](../04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md)
- [MODE-02 Invalidation rule](../07-mode-channels/MODE-02-invalidation-rule.md)

## Whitepaper references

- [§ Recovery](../../../whitepaper.md#recovery)

## Progress notes

- 2026-11-23 (Build): Started. Build what ADR-0030 points 4, 6, 8 and 9 and ADR-0031 point 6 fix: per-layer diagnostics tagged with their step, count-based withdrawal with failure give-back, and a step-withdrawal hook. Nothing raises a diagnostic yet (`Recover`, `OrInsert` and lexer recovery are ERR-05 to ERR-07), so the plan must say how the property test raises one.
- 2026-11-23 (Build): Plan agreed in three increments: (1) a refactor that gives every plan taking a mark a mark type parameter; (2) diagnostics in the failure log, with `KeptFailure`, give-back and the step hook; (3) `FullMark`, `LoweringOptions.LightMarks` with the per-root check, the withdrawals in `Atomic`, `Lookahead`, `Not` and `Longest`, the re-run of a `Longest` winner, and the model-based property test. Increment 1 done: `IMark<TSelf>` (static `Take`, `FailedEmpty`, `Rewind`); `LightMark` implements it with a `Rewind` that does nothing. Optional, Or, Dispatch, Repeat, SkipRepeat, Until, Separated, Expression and its operator plans, Atomic, Lookahead, Not and Longest take `TMark`. `ExpressionOperator.Lower<TMark>` keeps the expression's call into itself direct. The builder passes `LightMark` everywhere. Evidence: `dotnet build` 0 warnings; `dotnet test` 1,637 of 1,637 pass; AOT smoke publish clean and prints OK. Speed: three gate runs give 1.74× Parlot and fail. The base commit 6cde18d, run the same way, gives 1.72× and also fails (Parlot 2.3.1 since FND-09; BENCH-01 passed at 1.37× on Parlot 2.0.0). Creole's JSON times and every allocation are unchanged. Arithmetic read about 4% slower over six runs, but its JIT output for the expression path matches the base except where tiered PGO inlines differently, and PGO's choices vary between runs of one binary. With `DOTNET_TieredPGO=0`, alternating runs give 482.8 ns against the base's 484.4 ns (medians of three). Next: increment 2, after the maintainer commits this one.
- 2026-11-23 (Build): Increment 2 done: diagnostics in the failure log. `FailureLog` is partial; [FailureLog.Diagnostics.cs](../../../src/Creole/Errors/FailureLog.Diagnostics.cs) holds the diagnostics, each with the failure it reports (`FailureCopy`), the start of the step that raised it, and whether withdrawing it gives that failure back. `Raise(position, message)` raises one in the context in force. `RaiseFromFurthestFailure(position)` reports the furthest failure and takes it out of the log, leaving saves alone; with none recorded, it reports the item at `position` and gives nothing back. `Withdraw(count)` gives failures back in raise order: a failure past the furthest one replaces it and brings back its own context stack; one at the same position adds its expectations after those recorded since, and its message if there is none. `WithdrawFromStep(stepStart)` gives nothing back. `Preserve` also copies what diagnostics' failures read, and `Clear` forgets them. `ToError` now builds from a `FailureCopy`, the same code as diagnostics. A take bumps a generation that `FailureMark` carries and `Restore` puts back, so `Named` does not bring back expectations that a take removed. `Tokens` and `Transduce` set `StepStart` at each step; no test reaches it yet, because nothing exposes a layer's log until LAY-05. Evidence: `dotnet build` 0 warnings; `dotnet test` 1,652 of 1,652 pass; [DiagnosticLogTests.cs](../../../tests/Creole.Tests/DiagnosticLogTests.cs) has 15 tests, and each of ten mutations (no give-back, no restack, no expectations or message given back, no generation bump, a take that does not clear, a restore that keeps the generation, no preserve, the step boundary, a clear that keeps diagnostics) fails at least one. Next: increment 3, after the maintainer commits this one.
- 2026-11-23 (Build): Increment 3 done: marks withdraw. [FullMark](../../../src/Creole/Plans/FullMark.cs) saves the position, the cut flag and the diagnostic count; `FailedEmpty` and `Rewind` withdraw to it, which gives back the failures `RaiseFromFurthestFailure` took. `IMark.Rewinds` tells a `Longest` whether to run its winner again when another alternative ran after it (ADR-0030 point 6); `LightMark` says no, so the re-run compiles away. `LoweringOptions.LightMarks` (in `All`) gives a grammar that cannot raise diagnostics light marks. [PlanBuilder.Marks.cs](../../../src/Creole/Plans/PlanBuilder.Marks.cs) decides once per builder, from the first parser it lowers, by walking the grammar through `Rec`: a `Bind` means full marks, and ERR-05, ERR-06 and `Embed` add their nodes. The decision is per grammar root, not per layer: finer than ADR-0030 point 9 and as safe, because a plan runs only its own root's grammar and a `Bind` continuation is lowered as its own root. Tests that lower with one rewrite now expect `FullMark` (11 assertions), because `LightMarks` is a rewrite. Tests: `ControlModel` gains a `Raise` leaf and tracks diagnostics by ADR-0030 point 2's table; [DiagnosticWithdrawalTests.cs](../../../tests/Creole.Tests/DiagnosticWithdrawalTests.cs) (10) compares the interpreter with it over every shape to depth 3 on leaves `a`, `ab`, `Cut` and `Raise` with every input up to length 3, and over 20,000 random deeper shapes. It also covers the attempts the model leaves out (expression operators and atom, `SkipMany`, bounded `Repeat`, `SepBy1`, `SepEndBy`), ADR-0030's `let` case through a test node that reports the failure it catches, and the lowering decision. Mutation sweep: each of 17 mutations fails at least one test (no withdrawal in `FailedEmpty` or `Rewind`; none in `Atomic`, `Lookahead` or `Not`; no winner re-run; `Bind` not seen; the walk stopping at the root; each lowering site taking `LightMark`; no give-back). Evidence: `dotnet build` 0 warnings; `dotnet test` 1,662 of 1,662 pass; AOT smoke clean and OK. Speed: gate grammars use no `Longest` or `Bind`, so their code is unchanged. The interpreted lexers (`*LexerWithoutDfa`, PGO off, alternating with HEAD) first showed the C# lexer 2.7% slower, from indexing the alternatives field; with the array in a local they are level (34.4 ms against 34.2 ms, ranges overlapping). Three gate runs give 1.73× Parlot and 0.25× Pidgin, against 1.72× and 0.24× at 6cde18d, with the same allocations: the gate still fails, for the Parlot 2.3.1 reason in increment 1's note. Every acceptance criterion now appears met. Not done here, for ERR-05: a winner that is run again also sees the failures its losers recorded, so a `Recover` diagnostic inside a `Longest` winner could depend on the order of the alternatives. ADR-0030 point 6 does not cover it. Next: the maintainer commits, opens the PR and runs `/complete`.
