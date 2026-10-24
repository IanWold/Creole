# INT-06: DFA compiler for regular subtrees

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

Regular character subtrees and `Longest` over regular alternatives compile to DFA tables that agree with the interpreter's commit semantics ([ADR-0017](../../docs/decisions/0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md)). The same automata library serves analysis (CRL0101, CRL0103, CRL0106) and fusion.

## Scope

- **The Regular predicate.** It goes in `src/Creole/Analysis/` (ADR-0017 point 1), and ANA-01 reuses it.
  - Character input only.
  - The node kinds the prototypes tested: character classes, `String`, `Sequence`, `Or`, `Optional`, `Repeat` and `SkipRepeat` with a minimum of 0 or 1 and no maximum, `Atomic` and `Longest`.
  - The value-only wrappers `Map`, `Slice`, `Text`, `Named` and `Context`.
  - Every choice inside an explicit `Atomic` meets fragment E′'s condition ([prototype note](../../docs/research/2026-10-23-commit-aware-automaton-prototypes.md), finding 19).
  - Another node kind joins only with differential tests that cover it.
- **The eager commit-aware automaton** (prototype note, findings 13–16).
  - Construction: thread lists determinized into states, over character classes from a sweep of `CharSet` boundaries.
  - Minimization only if it pays.
  - `Longest` becomes one automaton with a register per alternative, and ties go to the earliest.
- **Lowering.**
  - Tables are built in full at lowering, so plans stay immutable.
  - Above a state cap, the subtree runs on the interpreter.
  - Lowering time is measured on the JSON and C#-like lexers. If tuned construction is still slow, lazy construction needs its own decision (ADR-0017 point 3).
- **Execution.**
  - The plan runs maximal munch with backup, as a lowering rewrite under `LoweringOptions`.
  - It is kept or dropped by benchmark, as INT-05's rewrites were. Which non-`Longest` subtrees are worth compiling is set by measurement.
- **Values and failures** (ADR-0017 points 4–6).
  - A value that is a function of the span comes from the span. Otherwise the winning alternative is re-run.
  - When the automaton finds no match, the interpreter re-runs the subtree.
  - The plan exposes how far each scan read, for ERR-02.
- **Not compiled.**
  - `Lookahead`, `Not`, `Cut`.
  - Token-level input.
  - Choices inside `Atomic` outside E′.
  - The deferred automaton and Option B's condition are not built (ADR-0017 point 8).
- **Docs.** `docs/engines/interpreter.md` gains a section on which subtrees compile and how to write them so they do.

## Acceptance criteria

- [ ] Compiled plans agree with the interpreter with the rewrite on and off (`Rewrites.Compare`) on property-generated terms and inputs. The comparison covers outcome, consumed length, commit flag, winning alternative, value and cut flag. The generated terms include nested `Atomic`, `String` literals, `Longest` and depth 5.
- [x] The Regular predicate gives the expected answer on a table of known terms. The table includes both prototype lexers and the counterexamples of the prototype note's findings 2 and 19.
- [ ] Subtrees over the state cap or outside E′ run on the interpreter, and the differential tests cover them.
- [ ] Lowering time and tokenizing speed for the JSON and C#-like lexers are measured, median of three runs, and recorded on this card. The rewrite is kept only if it beats the interpreter.

## Blocked by

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)

## Unblocks

- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [ANA-04 Choice checks](../09-static-analysis/ANA-04-choice-checks.md)
- [ANA-07 Linear maximal munch](../09-static-analysis/ANA-07-linear-maximal-munch.md)
- [GEN-05 Generated DFAs, scans and jump tables](../11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md)
- [BENCH-01 Phase 0 speed gate](../13-benchmarks/BENCH-01-phase-0-speed-gate.md)

## Whitepaper references

- [§ Longest match](../../../whitepaper.md#longest-match)
- [§ Interpreted engine](../../../whitepaper.md#interpreted-engine)
- [§ Properties](../../../whitepaper.md#properties)

## Artifacts

- [Research: DFA compilation under commit semantics](../../docs/research/2026-10-23-dfa-compilation-under-commit-semantics.md) How regular subtrees can compile to DFAs that agree with commit semantics; construction, errors, alphabet, backup and prior art
- [Research: commit-aware automaton prototypes](../../docs/research/2026-10-23-commit-aware-automaton-prototypes.md) Differential prototypes: the commit rule, lexer sizes, speed, proof sketches, Option B's condition, backup, blow-ups and a wider eager fragment
- [Decision: ADR-0017 Regular subtrees compile to an eager commit-aware automaton](../../docs/decisions/0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md) Accepted: the eager automaton on fragment E′, the interpreter for everything else

## Progress notes

- 2026-10-23 (Research): Started. Build is premature: what the DFA must compute is not settled. Commit semantics are not regex semantics, even under `Longest`. `OptionalPlan` fails committed when its item fails after consuming, and repetition is greedy, so on `1.x` the alternative `Seq(Digits, Optional(Seq('.', Digits)))` fails, while a maximal-munch DFA over its regex reading returns `1`. The Scope's claim that `Longest` "is unaffected" therefore does not hold in general. § Properties defines Regular as "no `Rec`, `Bind` or state", which is narrower than this card's Scope; that conflict goes to the Design stage. Next: a research note on constructions that model commit semantics, then a decision record (`/decide`).
- 2026-10-23 (Research): Research note written (five questions, five researcher passes; key citations spot-checked first-hand). Findings: no published construction models commit semantics; non-recursive PEGs are regular but only as languages; leftmost-first priority DFAs (RE2, regex-automata, .NET NonBacktracking) plus a commit-pruning rule look exact but are unproven; PetitParser already detects regular sub-parsers automatically; C, R and SQL token sets have unbounded backup, so CRL0106 as a Warning would be noisy; Parlot 2.0.0 has no DFA or longest match but is now stable with its generator (whitepaper drift, outside this card). Next: `/decide` on the definition of Regular and what the automaton computes (options A commit-aware automaton, B textbook DFA on a provably safe fragment, C analysis only in Phase 0), optionally after a prototype that tests the pruning rule against the interpreter.
- 2026-10-23 (Research): Scratchpad prototype of the commit-aware priority automaton (C#, scratch project named `Creole.Tests` to reach plans; compared against the real interpreter with `LoweringOptions.All`; alphabet a, b, c; every input to length 6 or 7 plus random ones to 20; `Lookahead`, `Not`, `Cut`, values and errors not covered). Eager rule (a commit or a finished high branch settles the choice at once; a match prunes everything below): 0 mismatches on 20,000 random terms without explicit `Atomic` (about 22.6 million cases), on 10,000 where no `Atomic` contains a choice, and on 13 realistic token shapes, with one register per `Longest` alternative and at most 79 states; with a choice inside `Atomic` it is wrong (thousands of mismatches). Deferred rule (a thread still waiting on a higher-priority branch only marks the event; per-match registers, like a tagged DFA): 0 mismatches on 40,000 random terms including arbitrary `Atomic`, but up to 132 registers, and 4 of 10,000 `Atomic` terms of the form `Many(Or(Atomic(<unbounded>), <consuming branch>))` grow without bound (no finite DFA under this construction). The regex reading disagrees with the interpreter on 11–14% of random cases and on 3 of 13 realistic tokens (Parsec-style decimal and exponent, block comment without `Atomic`, which fails on `/**/`). Code: scratchpad only, not kept. Next: maintainer to choose further prototypes (lexer-sized state counts, speed against the interpreter, a proof for the eager rule, Option B's condition), then a research note on the prototype results and `/decide`.
- 2026-10-23 (Research): Ran all six prototypes; results in the [prototype note](../../docs/research/2026-10-23-commit-aware-automaton-prototypes.md). The eager rule is exact on the fragment where no `Atomic` contains a choice (proof sketch in the note), has one register per alternative, and gives 41 states for a JSON lexer and 517 for a 138-rule C# lexer. The deferred rule plus a corrected "sure `Atomic`" rule is exact on arbitrary `Atomic`, but one class still grows without bound (`Atomic` body that can fail after consuming, in a repetition), so it needs a cap and fallback. Backup analysis and stop-state memoization work. Option B's condition held on 32,000 terms and covers about 82% of standalone terms that agree. Speed was stopped by a power outage. One JSON run had the DFA 5.8× faster than `Longest` and 2.6× faster than `OneOf`. The C# setup check found the DFA and the interpreter disagreeing, which is not yet investigated, and the benchmark config also produced a job that could not build. Prototype code is in this session's scratchpad only (`/tmp/claude-1000/-home-ianwold-Source-Creole/718b2ea4-9e02-4ad2-821b-dcc499529812/scratchpad/commit-dfa/`) and may be lost. Next: if the code survives, fix the benchmark (one in-process job) and investigate the C# disagreement; then three repeated runs; then `/decide`.
- 2026-10-23 (Research): Moved the prototype from the scratchpad (tmpfs) into `work/docs/research/prototypes/2026-10-23-commit-aware-automaton`; it builds there, outside the solution. The C# "disagreement" was the harness: both lexers stopped at a character no rule matches. Both now skip it as an error token, and the three tokenizers agree on JSON and C#. The benchmark is fixed to a single in-process job. Next: three repeated `bench` runs on mains power, update finding 12, then `/decide`.
- 2026-10-23 (Design): Ran the tokenizing benchmark three times on mains power (medians: JSON lexer, DFA 6.0× faster than `Longest` and 2.6× faster than dispatching `OneOf`; C#-like lexer, 21.9× faster than `Longest`); finding 12 rewritten. Both lexers put a choice inside `Atomic`, outside the proven fragment E, so a seventh prototype tested a wider fragment E′ (finding 19): 0 mismatches on about 128,000 terms (145 million cases); the first version was unsound and depth 5 caught it. The maintainer chose the eager automaton on E′ over analysis-only and over adding the deferred automaton. Drafted [ADR-0017](../../docs/decisions/0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md) (Proposed). Captured the idea [commit-versus-regex-reading-diagnostic](../../ideas/commit-versus-regex-reading-diagnostic.md). Next: maintainer accepts, rejects or revises ADR-0017; on acceptance, `/decide accept 0017` edits the whitepaper, INT-06, ANA-01 and ERR-02, and INT-06 moves to Building.
- 2026-10-23 (Design): The maintainer accepted [ADR-0017](../../docs/decisions/0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md). The whitepaper now carries the new Regular definition (§ Properties), what the DFA agrees with (§ Longest match), how compiled subtrees run (§ Interpreted engine) and the bookmark rule for a `Longest` that does not run as a DFA (§ Bookmarks and retention). Scope and criteria above rewritten from the decision; the claim that `Longest` "is unaffected" is gone. ANA-01, ERR-02, ANA-04, ANA-07 and GEN-05 updated. Moved to Building. Next: `/build INT-06`, starting with a plan against the four criteria; the prototype in `work/docs/research/prototypes/2026-10-23-commit-aware-automaton/` is the reference.
- 2026-10-23 (Build): At the maintainer's request, the prototype code and its raw output are out of the repository. The research note quotes every number it relies on, and the link in the earlier note above is now plain text. A local copy outside git, at `/home/ianwold/Source/creole-prototypes/2026-10-23-commit-aware-automaton`, builds against this checkout and stays the reference for the build; see its README. Next: `/build INT-06`.
- 2026-10-23 (Build): Increment 1 of 4 done: the Regular predicate. `Regular.Of` in `src/Creole/Analysis/Regular.cs` turns a character parser into a `RegularTerm` (`Analysis/RegularTerm.cs`), or returns null. A `RegularTerm` is a choice, sequence, repetition, class or `Atomic`, and caches nullable, consumes, FIRST, never-fails and never-fails-after-consuming. The predicate applies ADR-0017 point 1's whitelist and fragment E′, with the continuation running to the outermost `Atomic`. Small calls from the plan: nested `Longest` is not Regular; case-insensitive `String` becomes exact case-variant classes (`StringNode.CharactersAt`, reusing `CaseVariants`); repetitions need a minimum of 0 or 1, no maximum and an item that cannot match empty (new internal `IRepetitionNode`). The analysis APIs for ANA-04 and ANA-07 moved to those cards' scopes, with the maintainer's approval. Tests: `RegularTests` (50), a table of 46 terms covering every whitelisted and excluded node kind, realistic tokens, both counterexamples (finding 2, found by rerunning the local prototype's eager rule; finding 19) and constructed E′ cases, plus every rule of both prototype lexers (`LexerSamples`, 12 and 138 rules). Five mutants (rest reset at a nested `Atomic`, either E′ clause dropped, `Atomic` dropped from terms, nullable repetition items allowed) each fail 1 to 4 tests. Verified: `dotnet build` 0 warnings, `dotnet test` 1,166 passed, AOT smoke publish clean. Criterion 2 met. Next: increment 2, the eager automaton and its differential tests.
- 2026-10-24 (Build): Increment 2 of 4 done: the eager commit-aware automaton. `CommitAutomaton.Build(terms, maxStates)` in `src/Creole/Analysis/CommitAutomaton.cs` ports only the prototype's eager rule, with no marks, sure-`Atomic` rule or per-match registers. It determinizes priority-ordered candidate lists into dense tables: a `ushort` class per code unit from a boundary sweep of every `CharSet`, transitions, the alternatives whose match ends on each transition, the matches each state holds, and a committed flag for a single term's failure. It returns null over the state cap, or when one configuration holds more than 1,000 candidates. `Match(span)` is the reference scan. Tests: `CommitAutomatonTests` (9). CsCheck properties against the interpreter, lowered with no rewrites: random regular terms to depth 5 over a, b and c (400 terms, every input to length 6 plus 10 random ones to length 14); terms with a choice inside `Atomic`, from a generator weighted towards `Atomic` (1,000); and `Longest` of 2 or 3 alternatives (400, every input to length 5). Both lexers tokenize sample JSON and C# text as the interpreter's nested `Longest` does. Also worked examples and the cap. Mutants: turning off the E′ check fails the `Atomic` property, which it did not before the weighted generator. Commits passing through `Atomic`, a consumed `Atomic` not committing when left, a finished first branch not resolving its choice, orphaned second-branch entries kept, and no committed flag each fail 2 to 6 tests. Two mutants survive by design: a match dropping lower paths, which resolving each choice at the end of its first branch already implies, and dropping duplicate live paths, which only bounds size. Sizes match the prototype: JSON 41 states and 30 classes, C# 517 and 65. Building the C# automaton took 1.5 s in one cold Release run, as slow as the untuned prototype, so increment 4 must tune it or set the cap from it. Small call for increment 3: lowering will flatten a `Longest` nested directly inside a `Longest`, the documented way past 16 alternatives (`docs/concepts/choice.md`), into one automaton. The lexer test already checks that the flat automaton agrees with the interpreter's nested form. Verified: `dotnet build` 0 warnings, `dotnet test` 1,175 passed, AOT smoke publish exit 0 with no IL warnings. Next: increment 3, `DfaLongestPlan` under `LoweringOptions.Dfa`.
