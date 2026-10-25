# ADR-0025: A compiled `Longest`'s construction stops at a state cap or a work budget, and the path limit goes

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-25 |
| Cards | [INT-09](../../complete/03-interpreter/INT-09-cheaper-automaton-construction.md), [INT-06](../../complete/03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md), [GEN-05](../../active/11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md), [ANA-04](../../active/09-static-analysis/ANA-04-choice-checks.md), [ANA-07](../../active/09-static-analysis/ANA-07-linear-maximal-munch.md) |
| Whitepaper | [§ Interpreted engine](../../../whitepaper.md#interpreted-engine) |
| Research | [DFA compilation under commit semantics](../research/2026-10-23-dfa-compilation-under-commit-semantics.md), [Commit-aware automaton prototypes](../research/2026-10-23-commit-aware-automaton-prototypes.md) |
| Supersedes | — |

## Context

[ADR-0017](0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md) point 3 builds a compiled `Longest`'s automaton eagerly, when the grammar is first lowered. Above a state cap, the `Longest` runs on the interpreter. INT-06 set the cap at 2,000 states. [§ Interpreted engine](../../../whitepaper.md#interpreted-engine) states the same rule.

The state cap limits the size of the result, not the cost of finding out whether it fits. Measured at commit 0ea4256 with a scratch harness ([INT-09](../../complete/03-interpreter/INT-09-cheaper-automaton-construction.md) progress notes):

| `Longest` | States | Lowering time | Allocated |
| --- | --- | --- | --- |
| C#-like lexer, 138 rules | 513 | 123–129 ms | 516 MB |
| 300 keywords and an identifier | 1,622 | 376 ms | 1.6 GB |
| 400 keywords and an identifier | 2,171 without the cap; falls back | 582 ms | 2.5 GB |

The last row spends the most and gains nothing. About 75% of the allocation is work repeated for every alternative on every transition, even for alternatives with no paths left. INT-09 removes most of that. After it, a transition is expected to cost roughly in proportion to the number of paths in the state being left; that is inference, to be measured. That number has no bound short of the construction's path limit, and transitions number up to the state cap times the number of character classes. So cheaper construction lowers the cost of a build that will not fit but does not bound it.

The construction already has a second limit that no document records: a state may hold at most 1,000 paths (`MaxCandidates` in `CommitAutomaton`), or the build stops. In effect it also stops every `Longest` of more than 1,000 alternatives, since the first state holds at least one path for each.

Three facts constrain the choice:

- **A limit never changes a result.** A compiled `Longest` agrees with the interpreter on value, position and failures ([ADR-0017](0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md), [ADR-0022](0022-a-successful-longest-restores-the-furthest-failure.md)). A limit decides only which engine runs, so it affects speed alone.
- **The same grammar should get the same plan.** Plans are cached per node. If whether a `Longest` compiles varies between runs or machines, so does its speed, and a benchmark or a bug report cannot be reproduced.
- **Prior art counts.** .NET's `NonBacktracking` engine switches to NFA mode at a node-count threshold, `NfaNodeCountThreshold = 125_000`. Lazy DFAs in RE2 and regex-automata fall back to an NFA when their cache churns (earlier note, finding 13).

Lazy construction is out of scope here. ADR-0017 point 3 makes it a separate decision because it ends plan immutability. INT-09 proposes it only if eager construction cannot meet its criteria.

## Options

### A. The state cap only

Leave point 3 as it is and keep the undocumented path limit.

- **For:** nothing to add or explain.
- **Against:** the cost of a build that does not fit stays unbounded except through the path limit. A grammar with many alternatives alive across many states pays for the whole exploration at run time and gains nothing.

### B. The state cap and a work budget counted by the construction; the path limit goes

The construction counts the paths it steps across all transitions, and stops when the count passes a budget. The path limit is removed, because the budget bounds the cost it bounded.

- **For:**
  - The count is exact, so the same grammar always gets the same plan.
  - It measures what the cost is proportional to.
  - Two limits remain, one for the result's size and one for the cost of building it, and both are documented.
- **Against:**
  - A grammar under the state cap can fall back if its construction is costly. Only speed is affected.
  - The count depends on the construction's implementation, so a later change to construction can move a grammar near the budget across it. The budget is re-measured whenever construction changes, as the state cap is.
  - A `Longest` of more than 1,000 alternatives is no longer stopped at its first state. It runs until a limit stops it, at a cost the budget bounds.

### C. As B, but keep the path limit

- **For:** a very wide state stops the build at once rather than after it uses up the budget.
- **Against:** three limits to explain where two do the job.

### D. A limit measured by the machine: elapsed time, or the runtime's allocation counter

- **For:** each measures directly what the user pays.
- **Against:** neither is exact.
  - With time, whether a `Longest` compiles depends on the machine and its load.
  - The per-thread allocation counter also sees allocation the runtime makes on the thread now and then: a few kilobytes, per `Allocation.Fewest` in the test project. A grammar near the limit could compile on one run and not the next.

### E. Estimate the size before exploring

- **For:** cost is bounded before any work is done.
- **Against:** no useful estimate is known.
  - The state count does not follow from the number of rules. 12 rules gave 39 states, 138 gave 513, and 301 keywords and an identifier gave 1,622.
  - In general a DFA can be exponentially larger than its term.
  - A loose estimate either rejects lexers that fit or admits builds that do not.

## Decision

**B.** Building a compiled `Longest`'s automaton stops when it would exceed either of two limits, and the `Longest` then runs on the interpreter:

1. **The state cap**, as in ADR-0017 point 3.
2. **A work budget.** The construction counts each path it steps when it explores a transition, over the whole build. Once the count passes the budget, the build stops.

The limit of 1,000 paths per state is removed.

**Values.** INT-09 sets the budget from measurements after its cheaper construction, as INT-06 set the state cap. It records the number and the measurements in the `PlanBuilder` remarks and in [docs/engines/interpreter.md](../../../docs/engines/interpreter.md). The C#-like lexer and a `Longest` of 300 keywords and an identifier must still compile.

**Not configurable.** Neither limit is exposed to users. `LoweringOptions` already lets a caller turn compilation off.

The deciding reason: a count the construction keeps is exact and costs nothing to read. It bounds the cost the state cap leaves open, and it makes the path limit redundant, so the design gains one documented limit and loses one undocumented one.

What would change this decision:

- a realistic grammar that the budget sends to the interpreter while it fits under the state cap, and that needs the speed;
- a construction whose cost does not grow with the paths it steps, which would make the count a poor measure;
- lazy construction, which would need its own limit on cache churn instead.

## Consequences

- **Easier.**
  - A build that will not fit costs at most the budget's worth of work, whatever the grammar.
  - The docs can state every reason a `Longest` runs on the interpreter.
- **Harder.**
  - Changing construction means measuring the budget again, since the count depends on the implementation.
  - A test that a `Longest` compiles now depends on two limits, not one.
- **Ruled out.**
  - Limits measured by elapsed time or by the runtime's allocation counter.
  - A per-state path limit.
- **Build-time users.** GEN-05, ANA-04 and ANA-07 build the same automata at build time, where cost matters less. `CommitAutomaton.Build` takes its limits as arguments, so they may pass their own. This record sets only the limits lowering uses.
- **Risks and how they are watched.**
  - **The budget is set too low and a realistic lexer falls back.** INT-09's tests check that the C#-like lexer and 300 keywords and an identifier compile.
  - **The budget is set too high to matter.** INT-09's test bounds what the 400-keyword build allocates before it falls back.

## Changes made

- **Whitepaper.** [§ Interpreted engine](../../../whitepaper.md#interpreted-engine): the "Size cap" bullet becomes "Size limits". It names the state cap and the work budget, says either decides only which engine runs, and links this record. Checked and unchanged: § Longest match and § Properties, which say nothing about limits.
- **Decision index.** A note says this record refines ADR-0017 point 3. ADR-0017 is not superseded.
- **[INT-09](../../complete/03-interpreter/INT-09-cheaper-automaton-construction.md).** Scope now builds the work budget and removes the path limit. It returns to Building.
- **[GEN-05](../../active/11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md), [ANA-04](../../active/09-static-analysis/ANA-04-choice-checks.md), [ANA-07](../../active/09-static-analysis/ANA-07-linear-maximal-munch.md).** Each scope notes that construction stops at either limit and that it may pass its own.
- **Not yet.** [docs/engines/interpreter.md](../../../docs/engines/interpreter.md) changes with the code, in INT-09's build.
