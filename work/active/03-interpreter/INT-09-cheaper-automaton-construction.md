# INT-09: Cheaper automaton construction

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Building |

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

- [ ] Lowering the C#-like lexer (`DfaLowerCSharp` in `LoweringBenchmarks`) allocates at least 10× less than before this card, and takes no longer, median of three runs.
- [ ] A `Longest` of 400 keywords plus an identifier falls back to the interpreter having allocated at most N MB, with N set during the build, and the C#-like lexer and a `Longest` of 300 keywords plus an identifier still compile.
- [ ] Tokenizing (`DfaJsonLexer`, `DfaCSharpLexer`) stays within 5% of its speed before this card, median of three runs.
- [ ] The automaton, lowering and allocation tests pass unchanged.
- [ ] If construction becomes lazy, a decision record covering what replaces plan immutability is accepted before the build starts.

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
