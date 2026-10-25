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
