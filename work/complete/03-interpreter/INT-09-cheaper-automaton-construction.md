# INT-09: Cheaper automaton construction

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

Lowering builds a compiled `Longest`'s automaton in full, and for a large lexer that is costly. INT-06 measured the 138-rule C#-like lexer at about 107 ms to lower, allocating about 516 MB of short-lived memory. This card cuts that cost within eager construction first, and turns to lazy construction only if that falls short.

## Scope

- **Find where the cost goes.** Profile `CommitAutomaton.Build` on the C#-like lexer. The suspects:
  - the string key built for every configuration the construction reaches;
  - the signature strings built to drop duplicate paths;
  - copying every alternative's candidate list on every step.
- **Cut it within eager construction.** Possible means: hash states structurally, without building strings; compute each path's signature once; copy fewer lists.
- **Bound the cost of a build that will not fit.** The state cap (2,000) limits states, not cost. Measured during INT-06's completion, a `Longest` of 400 keyword alternatives plus an identifier reached 2,130 states. It spent about 500 ms and allocated about 2.5 GB before falling back to the interpreter, gaining nothing; 300 keywords fit at 1,642 states, 339 ms and 1.5 GB. Allocation per state grows with the number of alternatives. Add the work budget of [ADR-0025](../../docs/decisions/0025-automaton-construction-stops-at-a-state-cap-or-a-work-budget.md): the construction counts each path it steps, and stops once the count passes the budget. Remove the limit of 1,000 paths per state (`MaxCandidates`). Set the budget from measurements after the cheaper construction, and check with the allocation tests' measurement that it holds allocation down.
- **Only if that cannot meet the criteria:** propose lazy construction with `/decide`. It would build states on demand while scanning, which ends plan immutability (ADR-0017 point 3), and it needs thread-safe table growth, a cache cap, and a fallback when the cache churns.
- **Who pays the cost.** Grammars the source generator stages get their tables at build time (GEN-05). So this cost falls on grammars built at run time or left unstaged.
- **Timing.** No gate needs this card; BENCH-01 measures parsing, not lowering. Pick it up when a grammar built at run time shows the cost matters, or once GEN-05 shows how much of it staging removes.

## Acceptance criteria

- [x] Lowering the C#-like lexer (`DfaLowerCSharp` in `LoweringBenchmarks`) allocates at least 10× less than before this card, and takes no longer, median of three runs.
- [x] A `Longest` of 400 keywords plus an identifier falls back to the interpreter having allocated at most N MB, with N set during the build, and the C#-like lexer and a `Longest` of 300 keywords plus an identifier still compile.
- [x] Tokenizing (`DfaJsonLexer`, `DfaCSharpLexer`) stays within 5% of its speed before this card, median of three runs.
- [x] The automaton, lowering and allocation tests pass unchanged.
- [x] If construction becomes lazy, a decision record covering what replaces plan immutability is accepted before the build starts.

## Blocked by

- [INT-06 DFA compiler for regular subtrees](../03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Interpreted engine](../../../whitepaper.md#interpreted-engine)

## Artifacts

- [ADR-0025](../../docs/decisions/0025-automaton-construction-stops-at-a-state-cap-or-a-work-budget.md): construction stops at a state cap or a work budget, and the path limit goes (Accepted).

## Progress notes

- 2026-10-25 (Building): Started. Profile `CommitAutomaton.Build` on the C#-like lexer, cut allocation within eager construction, then add a deterministic bound on the cost of builds that will not fit.
- 2026-10-25 (Building): Scope changed with the maintainer: the cost bound counts allocation or work, not time, so whether a `Longest` compiles does not depend on the machine.
- 2026-10-25 (Building): With the maintainer: the bound is a count the construction keeps, not the allocation counter, and a criterion now covers it.
- 2026-10-25 (Building): Profiled at 0ea4256 with a scratch harness (`GC.GetAllocatedBytesForCurrentThread` and allocation-tick sampling). C#-like lexer: 123–129 ms, 515.8 MB. About 75% is per-alternative overhead on every transition, paid even for alternatives with no paths left: two `List<Candidate>` per alternative (211 MB), a closure per alternative from `Cleanup` (106 MB), two alternative-length arrays (55 MB). String keys and signatures take about 35 MB and LINQ iterators about 30 MB. Keywords plus an identifier: 300 took 376 ms, 1.6 GB and 1,622 states; 400 took 582 ms and 2.5 GB, then fell back (2,171 states uncapped). Next: plan for approval.
- 2026-10-25 (Designing): Back to Design before any code, at the maintainer's request, to record the work budget. Drafted ADR-0024 (Proposed): a work budget counted by the construction joins the state cap, and the 1,000-path limit per state goes. Next: the maintainer accepts, rejects or revises it; on acceptance, update § Interpreted engine and return to Building at increment 1 (baseline benchmarks).
- 2026-10-25 (Designing): Renumbered the draft to ADR-0025, because another branch drafted ADR-0024.
- 2026-10-25 (Building): The maintainer accepted ADR-0025. § Interpreted engine, the decision index, and the GEN-05, ANA-04 and ANA-07 scopes are updated. Next: increment 1, three default-job baseline runs of `DfaLowerCSharp`, `DfaJsonLexer` and `DfaCSharpLexer` at the current commit.
- 2026-10-25 (Building): Increment 1, baseline. Three default-job runs at dc25f4c (construction code as at 0ea4256), runs `20261025-193546`, `-193848` and `-194147`; i9-13900H on AC, .NET 11 RC 2, BenchmarkDotNet 0.16.0-preview.2, which reported 0.91 GB of memory available. Medians of the three means: `LowerCSharpWithDfa` 127.9 ms (126.4–128.6), 515,826,078–515,826,224 B allocated; `JsonLexerWithDfa` 505.8 µs and `CSharpLexerWithDfa` 1,410.3 µs, both allocating nothing. Without the DFA: JSON 1,726.6 µs, C# 21,513.7 µs, lowering the C# lexer 25.5 µs. So criterion 1 needs at most 51.6 MB and 127.9 ms, and criterion 3 at most 531.1 µs (JSON) and 1,480.8 µs (C#). Next: increment 2, cheaper construction.
- 2026-10-25 (Building): Increment 2, cheaper construction, in `CommitAutomaton.cs` only. A configuration holds only the alternatives that still have paths; an alternative whose paths all die on a symbol is skipped before anything is copied, and a path that dies becomes a dead path built from its source. States are keyed by integers written into a reused buffer after renaming, looked up through a `Dictionary` alternate lookup on `ReadOnlySpan<int>`; a configuration is copied for storage only when it is a new state. Duplicate paths are dropped with a reused set and a structural comparer instead of signature strings. Loops replace LINQ and closures on the per-transition path. `Candidate.Scope` is now `required`, so a new path no longer allocates a list it throws away. Evidence: (1) a scratch comparison against the old construction found identical tables (`Alternatives`, `Classes`, `ClassOf`, `Committed`, `Ends`, `Initial`, `Live`, `Matched`, `Next`) for the JSON and C#-like lexers, the C#-like lexer at a cap of 50 (both give up), 100–300 keywords plus an identifier, 400 without the cap (2,171 states), and 20,000 random `Longest`s of one to four terms of depth up to five with `Atomic`: no mismatches. (2) `dotnet build` 0 warnings; `dotnet test` 1,456 passed, no test changed; AOT smoke publish with no IL warnings. (3) Three default-job runs (`20261025-195431`, `-195730`, `-200039`; 1.8–4.2 GB of memory available, against 0.91 GB for the baseline): `LowerCSharpWithDfa` 10.4 ms (10.4–10.6), 16,490,352–16,490,622 B, so 31.3× less allocation and 12.2× faster than the baseline; `LowerJsonWithDfa` 238.4 µs, 470.5 KB; `JsonLexerWithDfa` 492.3 µs (2.7% faster) and `CSharpLexerWithDfa` 1,371.5 µs (2.8% faster). The tables are identical and the rows without the DFA were also 3–8% faster, so the tokenizing difference is noise. These runs preceded a style-only edit (primary constructor, a `Bit` helper, line breaks); after it the comparison still found no mismatch and allocation was unchanged. (4) Cold, in a new process, three each (scratch): building the C#-like automaton took 682–689 ms and 517 MB before and 69–88 ms and 16.1 MB after; a first lowering now takes 85–93 ms for the C#-like lexer and 28–30 ms for JSON, mostly JIT. (5) 400 keywords plus an identifier now fall back after about 52 ms and 41.5 MB (582 ms and 2.5 GB before); 300 compile in about 107 ms and 35 MB, single warm-ish runs. Docs: the Cost paragraph of `docs/engines/interpreter.md` and the `DfaStateCap` remarks give the new numbers, cold and warm. Criteria 1 and 3 appear met. Next: increment 3, the work budget, the removal of `MaxCandidates`, their tests and the Size bullet in the docs.
- 2026-10-25 (Building): Increment 3, the work budget of ADR-0025. `Determinizer.Run` adds, for each transition it explores, the number of paths in the state it leaves, and returns null once the count passes `maxWork`. `CommitAutomaton.Build` takes `maxWork` (default `long.MaxValue`, so direct callers keep no budget); `PlanBuilder` passes `DfaWorkBudget`, 500,000, with a `dfaWorkBudget` parameter for tests. `MaxCandidates` is gone, and `Make` can no longer fail. `Resolve`'s `Debug.Assert` uses a loop instead of LINQ and a closure (Debug builds only). Work counted (scratch): JSON lexer 1,767, C#-like lexer 90,816, 300 keywords and an identifier 135,120 (the tests' `LexerSamples.Keywords` set; 1,564 states), 350 157,680, 1,001 single characters 1,004,003. Finding: `LexerSamples.Longest` nests 16 to a level, and when the whole group does not fit, lowering the interpreter's `Longest` lowers the nested `Longest`, which tries to build again on its own. For 400 keywords and an identifier: 401 alternatives over the cap, the last 386 over the cap, the last 371 compile at 1,934 states. Whole lowering 142.6 ms and 128.9 MB in Release, 128.8 MB in Debug. This predates the card. The maintainer chose to keep the retries, since a suffix still compiles; N for criterion 2 is 140 MB for the whole lowering, and the docs say a nested `Longest` is tried on its own. Tests: `DfaLoweringTests` gains `ALongestOverTheWorkBudgetRunsOnTheInterpreter`, `ALongestThatOutgrowsTheStateCapFallsBackCheaply` (`Allocation.Fewest`, at most 140 MB) and `TheLexersAndThreeHundredKeywordsCompile` (JSON, C#, 300 keywords); `CommitAutomatonTests` gains `ALongestOfMoreThanAThousandAlternativesIsBuilt` and `AnAutomatonOverTheWorkBudgetIsNotBuilt`; `LexerSamples.Keywords(count)` builds the words from a fixed linear congruential sequence. The only change to an existing test is a new optional parameter on `DfaLoweringTests.Lower`. Docs: the Size bullet of `docs/engines/interpreter.md` gives both limits, links ADR-0025 and describes the retry; the `DfaWorkBudget` remarks give the counts. Evidence: `dotnet build` 0 warnings; `dotnet test` 1,463 passed; the scratch comparison still finds no mismatch over 20,000 random terms and the lexers; lowering the C#-like lexer still allocates 16.5 MB; AOT smoke publish with no IL warnings. Every criterion appears met: 1 and 3 by increment 2's runs, since this increment adds one addition and one comparison per transition; 2 by the two new lowering tests; 4 by the unchanged suite; 5 does not apply, as construction stays eager. Next: `/complete INT-09`, with a confirming benchmark run if wanted.
- 2026-10-25 (Building): Verified for completion at 1d4fc0f. `dotnet build` 0 warnings; `dotnet test` 1,463 passed; the automaton, lowering and allocation test classes 73 passed. Three default-job runs (`20261025-205740`, `-210037`, `-210306`; 3.8–5.0 GB of memory available): `LowerCSharpWithDfa` 11.1 ms (10.9–11.4), 16,506,885–16,507,104 B; `JsonLexerWithDfa` 496.6 µs; `CSharpLexerWithDfa` 1,389.2 µs. Every criterion met; card done.

## Outcome

Building a compiled `Longest`'s automaton now allocates 31× less and runs 12× faster once warm, and about 9× faster cold, with exactly the same tables. A build that will not fit is bounded by the state cap and a new work budget ([ADR-0025](../../docs/decisions/0025-automaton-construction-stops-at-a-state-cap-or-a-work-budget.md)).

- **Measurements.** Medians of three default-job runs, at 3ebda99 before and 1d4fc0f after:

  | Benchmark | Before | After |
  | --- | --- | --- |
  | `LowerCSharpWithDfa` | 127.9 ms, 515.8 MB | 11.1 ms, 16.5 MB (31.2× less allocation, 0.086× the time) |
  | `JsonLexerWithDfa` | 505.8 µs | 496.6 µs (1.8% faster) |
  | `CSharpLexerWithDfa` | 1,410.3 µs | 1,389.2 µs (1.5% faster) |

  The tables are identical, so the tokenizing difference is noise. Cold, in a new process, building the C#-like automaton fell from 682–689 ms to 69–88 ms (scratch harness, three runs each).
- **Where the cost went.** A profile at 0ea4256 put about 75% of the allocation in work repeated for every alternative on every transition, including alternatives with no paths left. String keys and signatures took about 35 MB and LINQ about 30 MB. The card's three suspects were real but minor next to that.
- **Construction:** `src/Creole/Analysis/CommitAutomaton.cs`.
  - A configuration holds only the alternatives that still have paths. An alternative whose paths all die on a symbol is skipped before anything is copied.
  - States are keyed by integers written into a reused buffer after renaming. They are looked up through a `Dictionary` alternate lookup on `ReadOnlySpan<int>`, and a configuration is copied for storage only when it is a new state.
  - Duplicate paths are dropped with a reused set and a structural comparer.
  - Loops replace LINQ and closures on the path taken per transition.
  - A scratch comparison against the old construction found identical tables for both lexers, keyword sets of 100–400, and 20,000 random terms.
- **Limits (ADR-0025).** `CommitAutomaton.Build` takes `maxWork`, and each transition explored counts the paths of the state it leaves. `PlanBuilder.DfaWorkBudget` is 500,000: the JSON lexer takes 1,767, the C#-like lexer 90,816, and 300 keywords and an identifier 135,120. The 1,000-path limit per state (`MaxCandidates`) is gone.
- **Tests:**
  - `DfaLoweringTests`:
    - `ALongestOverTheWorkBudgetRunsOnTheInterpreter`.
    - `ALongestThatOutgrowsTheStateCapFallsBackCheaply`: 400 keywords and an identifier, at most 140 MB; measured 128.8 MB.
    - `TheLexersAndThreeHundredKeywordsCompile`.
  - `CommitAutomatonTests`: `ALongestOfMoreThanAThousandAlternativesIsBuilt` and `AnAutomatonOverTheWorkBudgetIsNotBuilt`.
  - `LexerSamples.Keywords(count)`: a fixed keyword set.
  - The only change to existing test code is a new optional parameter on `DfaLoweringTests.Lower`.
- **Docs:**
  - [docs/engines/interpreter.md](../../../docs/engines/interpreter.md#when-a-longest-compiles): the Size bullet names both limits and the retry of a nested `Longest`. The Cost paragraph gives cold and warm building times and the new break-even: about 21 KB of JSON or 57 KB of C#-like text, against 85 KB and 0.7 MB before.
  - The `DfaStateCap` and `DfaWorkBudget` remarks.
- **Decision:** [ADR-0025](../../docs/decisions/0025-automaton-construction-stops-at-a-state-cap-or-a-work-budget.md). The whitepaper's [§ Interpreted engine](../../../whitepaper.md#interpreted-engine) "Size limits" bullet reflects it, and the decision index notes that it refines ADR-0017 point 3.
- **Found along the way:**
  - **The old docs understated a first lowering.** They quoted 107 ms, a warm benchmark, for the C#-like lexer; the old construction took about 687 ms cold.
  - **A nested `Longest` retries on its own.** When a group nested 16 to a level does not fit, the interpreter's `Longest` lowers the nested `Longest`, which tries to build again. For 400 keywords that is three builds, the last of which (371 alternatives, 1,934 states) compiles. This predates the card. The maintainer kept it, since a suffix still compiles, and the docs now describe it.
  - **Debug builds allocated more.** `Resolve`'s `Debug.Assert` allocated on every call in Debug builds; it now uses a loop.
- **Not done, with reasons:**
  - **Lazy construction.** Eager construction met every criterion, so no decision was needed.
  - **Caching `Run` frames.** Expansion still allocates a frame per step; the remaining 16.5 MB did not justify it.
- **Follow-ups:** none.
