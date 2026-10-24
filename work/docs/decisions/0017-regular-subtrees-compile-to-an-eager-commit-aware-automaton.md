# ADR-0017: Regular subtrees compile to an eager commit-aware automaton, and everything else runs on the interpreter

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-23 |
| Cards | [INT-06](../../active/03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md), [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md), [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md), [ANA-04](../../active/09-static-analysis/ANA-04-choice-checks.md), [ANA-07](../../active/09-static-analysis/ANA-07-linear-maximal-munch.md), [GEN-05](../../active/11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md) |
| Whitepaper | [§ Properties](../../../whitepaper.md#properties), [§ Longest match](../../../whitepaper.md#longest-match), [§ Interpreted engine](../../../whitepaper.md#interpreted-engine), [§ Deterministic mode](../../../whitepaper.md#deterministic-mode) |
| Research | [DFA compilation under commit semantics](../research/2026-10-23-dfa-compilation-under-commit-semantics.md), [Commit-aware automaton prototypes](../research/2026-10-23-commit-aware-automaton-prototypes.md) |
| Supersedes | — |

## Context

[§ Interpreted engine](../../../whitepaper.md#interpreted-engine) says regular character subtrees compile to DFA tables, and [§ Longest match](../../../whitepaper.md#longest-match) says a `Longest` over regular alternatives compiles to one DFA. [§ Properties](../../../whitepaper.md#properties) defines Regular as "contains no `Rec`, `Bind` or state". That definition is too wide:

- **Commit semantics are not regex semantics.** `Seq(Digits, Optional(Seq('.', Digits)))` fails on `1.x`, but a maximal-munch DFA over its regex reading returns `1`. Maximal munch disagreed with the interpreter on 3 of 13 realistic tokens and on 10–14% of random cases (prototype note, finding 4). Running a `Longest` alternative atomically does not fix this, so INT-06's Scope claim that `Longest` "is unaffected" is wrong (earlier note, finding 2).
- **No published construction fits.** None models Parsec's committed and empty failures (earlier note, finding 6). The commit-aware priority automaton is our own.
- **The eager rule.** A choice resolves as soon as a thread commits inside its high branch or finishes it, and a match prunes everything below it. On fragment E, where no explicit `Atomic` contains a choice, this is exact by proof sketch and by test (prototype note, findings 1 and 13–16).
- **E′, a wider fragment.** E′ admits a choice inside `Atomic` when one of two clauses holds. Either its high branch, followed by the rest of the outermost `Atomic`, never fails after consuming. Or its low branch is not nullable and the two branches start with different characters. On about 128,000 random terms in E′ (145 million cases) the eager rule had no mismatch. E′ includes both measured lexers and every idiomatic `Atomic` token tested. It is not proved (finding 19).
- **Outside E′.** A deferred rule with a "sure `Atomic`" rule is exact by test. It needs a register per match, like a tagged DFA, and one class of terms still grows without bound (findings 3 and 9–11).
- **Size.** The automata are small: 41 states for a 12-rule JSON lexer and 517 for a 138-rule C#-like lexer, with one register per rule (finding 5).
- **Speed.** The eager DFA tables tokenized 6.0× faster than the interpreter's `Longest` on JSON and 21.9× faster on the C#-like lexer. On JSON they were 2.6× faster than a dispatching `OneOf`. These are medians of three runs (finding 12). The prototype's loop produces no values and reports no errors, so these are upper bounds.
- **Equivalence.** [§ Equivalence](../../../whitepaper.md#equivalence) holds every engine to the interpreter's value, position, errors and expected sets. ERR-02, which defines expected sets, is not built.
- **Ownership.** INT-06 is in Phase 0 and needs the property now. ANA-01, which computes properties for analysis, is in Phase 2.

## Options

### A. The eager automaton on E′, with the interpreter for everything else (chosen)

Compile a subtree only when it is Regular as redefined below, which includes membership of E′. Everything else, including any subtree over a state cap, runs on the interpreter as now.

- **Pros.** It has the simplest execution: one register per `Longest` alternative and the plain maximal-munch loop that was measured. It covers every realistic token tested. Its failure mode is falling back, not a wrong result.
- **Cons.** The construction is new. E′ is backed by tests, not a proof, and the first version of E′ was unsound until depth-5 tests caught it (finding 19). Terms outside E′ get no speed-up.
- **Evidence.** Findings 1, 5, 12, 16 and 19.

### B. A, plus the deferred automaton outside E′

- **Pros.** It compiles nearly every regular term.
- **Cons.** It needs per-match registers with copy operations, like a tagged DFA, and a state cap for the class that grows without bound. No realistic token tested needs it. That makes it complexity without a demonstrated need.

### C. A textbook DFA, behind Option B's sufficient condition

Use Thompson's construction, subset construction and minimization, applied only where a structural condition proves that commit semantics equal the regex reading.

- **Pros.** It is standard and well understood.
- **Cons.** The condition is conservative. It certifies 82–83% of standalone terms that agree, and 50–58% of `Longest` sets. It rejects the `Atomic` block comment, which does agree (finding 18). The commit-aware automaton is needed anyway for exact CRL0101 and CRL0103 (ANA-04), so this option adds a second construction rather than removing one.

### D. No compiled execution in Phase 0

Build the automata for analysis with the ANA cards in Phase 2, and compile only if a benchmark later shows the need.

- **Pros.** Phase 0 is smaller, and BENCH-01 loses a blocker.
- **Cons.** It gives up the measured lexer gain, and it changes the whitepaper's stated design for Phase 0. The maintainer preferred A.

## Decision

1. **Regular.** A node is Regular when it meets all of the following.
   - **Input.** It reads characters. Token-level input is left for later.
   - **Node kinds.** It is built only from the node kinds the prototypes tested: character classes (including `Any`), `String`, `Sequence`, `Or`, `Optional`, `Repeat` and `SkipRepeat` with a minimum of 0 or 1 and no maximum, `Atomic` and `Longest`.
   - **Value-only wrappers.** These change no match and are also allowed: `Map`, `Slice`, `Text`, `Named` and `Context`.
   - **E′.** Every choice inside an explicit `Atomic` meets finding 19's condition. A choice is an `Or`, an `Optional` or a repetition.

   Every other node kind is not Regular: `Rec`, `Bind`, `Expression`, `Filter`, `Switch`, `Match`, `Satisfy`, `Custom`, `Lookahead`, `Not`, `Cut`, the token nodes, and anything with layer state. Another kind joins only with differential tests that cover it. This replaces "contains no `Rec`, `Bind` or state" in § Properties.
2. **What compiles.** A `Longest` whose alternatives are all Regular compiles to one automaton, with one register per alternative, and the earliest wins a tie. Other Regular subtrees compile when the build's measurements show a gain. They are kept or dropped by benchmark, as INT-05's rewrites were, so a lone class or literal stays as it is. Construction is the eager commit-aware automaton of the prototype note (findings 13–16) over character classes from a sweep of `CharSet` boundaries.
3. **State cap and fallback.** Lowering builds the automaton eagerly, so plans stay immutable. Above a state cap, the subtree runs on the interpreter. The build sets the cap and measures lowering time on the JSON and C#-like lexers. The untuned prototype took 1.7–2.6 s to explore the C# automaton. If tuned construction is still slow, lazy construction becomes a separate decision, because it would change plan immutability.
4. **Values.**
   - **From the span.** When the value of the winning alternative is a function of its span, the plan computes it directly from the span. That covers a constant (`As`), `Unit` from a skip form, `Slice`, `Text`, and a `Map` over one of those.
   - **Otherwise.** The plan re-runs the winning alternative's interpreter plan from the start of its span.
5. **Failures.** When the automaton finds no match, the plan re-runs the subtree on the interpreter, and the interpreter's result is returned: committed or empty, its position, and what it records. The automaton never reports an error of its own.
6. **Expected sets on success.** A compiled subtree that succeeds must contribute the same expectations as the interpreter, which records them from the alternatives and loop exits it tried (G7). ERR-02 decides how, and INT-06 exposes how far each scan read. The candidates:
   - a lazy re-run of the subtree when its stop position becomes the reported failure;
   - expectation labels on states (earlier note, finding 31).
7. **Ownership.** INT-06 implements the Regular predicate in `src/Creole/Analysis/`. ANA-01 reuses it rather than computing its own.
8. **Not built now.** Neither the deferred automaton (option B) nor Option B's condition (option C) is built for execution. The condition's other use, a diagnostic, is the idea [Commit versus regex reading diagnostic](../../ideas/commit-versus-regex-reading-diagnostic.md).

The deciding reason: option A gives the measured gain with the simplest execution. Its risk is an unproven fragment, and the risk is bounded. A term the condition wrongly admits would give a wrong parse, but the build's differential tests are the same kind of test that found every earlier error in this construction.

What would change this decision:

- a realistic grammar that falls outside E′ and needs the speed (option B);
- a product implementation whose gain, once values and errors are added, falls below INT-05's keep rule (option D);
- a counterexample in E′ that a corrected condition cannot exclude.

## Consequences

- **Easier.**
  - The whitepaper's `Longest`-to-DFA design holds, with a precise meaning: the DFA agrees with the interpreter, not with a regex.
  - ANA-04 can decide CRL0101 and CRL0103 exactly as products of these automata.
  - ANA-07 gets the backup analysis and stop-state memoization of findings 7 and 8.
  - GEN-05 can emit the same tables.
- **Harder.**
  - The value paths and the failure re-run add code to every compiled plan.
  - ERR-02 must cover compiled subtrees.
  - Lowering does real work for large lexers.
- **Ruled out for now.**
  - Compiling `Lookahead`, `Not` or `Cut`.
  - Compiling choices inside `Atomic` outside E′.
  - Compiling token-level `Longest`.
  - Using the regex reading for execution.
- **Risks and how they are watched.**
  - **E′ is unproven.** INT-06's acceptance criteria require property-based differential tests against the interpreter, with the rewrite on and off (`Rewrites.Compare`). The generated terms must include nested `Atomic`, `String` literals and depth 5, and the tests must cover outcome, length, commit flag, winning alternative, value and cut flag.
  - **The gain could shrink.** The build measures it with values and failures in place, median of three runs, and keeps the rewrite only if it pays.
  - **Lowering cost.** It is measured and recorded (point 3).

## Changes made

- **Whitepaper.**
  - [§ Properties](../../../whitepaper.md#properties): Regular is redefined as in point 1, with the condition for choices inside `Atomic`, and this record is linked.
  - [§ Longest match](../../../whitepaper.md#longest-match): it now says the DFA agrees with the interpreter rather than with a regex. It gives the `1.x` example, and the `Atomic` fraction that restores the regex behaviour.
  - [§ Interpreted engine](../../../whitepaper.md#interpreted-engine): the DFA bullet now covers how choices resolve, `Longest` with a register per alternative, the state cap and fallback, values and failures. It links this record.
  - [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention): a pinning bookmark is now taken by "a `Longest` that does not run as a DFA", because a regular `Longest` over the cap runs on the interpreter.
  - **Checked, unchanged.**
    - § Deterministic mode: rule 5 and the CRL0106 paragraph read correctly under the new definition.
    - § Diagnostics (CRL0101).
    - § Custom primitives: running a scanner alongside a `Longest` DFA is a fusion feature for FUS-03, not part of this decision.
    - § Layer fusion.
- **[INT-06](../../active/03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md).** Summary, Scope and acceptance criteria are rewritten from points 1–8. The claim that `Longest` "is unaffected" is gone. INT-06 now unblocks ANA-01 and is Building.
- **[ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md).** Regular comes from INT-06's predicate (point 7). ANA-01 is now blocked by INT-06; no wave changed.
- **[ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md).** A scope item covers expectations from compiled subtrees (point 6).
- **[ANA-04](../../active/09-static-analysis/ANA-04-choice-checks.md), [ANA-07](../../active/09-static-analysis/ANA-07-linear-maximal-munch.md), [GEN-05](../../active/11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md).** Their scopes point at this record and at the commit-aware automata.
