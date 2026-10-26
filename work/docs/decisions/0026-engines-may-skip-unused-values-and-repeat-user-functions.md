# ADR-0026: Engines may skip a value function whose value is unused and may run any user function again; a decision function is skipped only when its outcome is known

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-26 |
| Cards | [BENCH-01](../../active/13-benchmarks/BENCH-01-phase-0-speed-gate.md), [INT-06](../../active/03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md), [GEN-02](../../active/11-source-generator/GEN-02-emit-the-core-combinators.md), [GEN-03](../../active/11-source-generator/GEN-03-lambda-and-helper-inlining.md), [GEN-08](../../active/11-source-generator/GEN-08-differential-engine-agreement-tests.md), [TOOL-06](../../active/12-grammar-author-tooling/TOOL-06-engineagreementcheck.md), [LAY-04](../../active/06-layers-and-pipelines/LAY-04-layer-local-state.md), [INC-04](../../active/15-incremental-reparsing/INC-04-reusable-parser-rules.md) |
| Whitepaper | [§ Equivalence](../../../whitepaper.md#equivalence), [§ Interpreted engine](../../../whitepaper.md#interpreted-engine), [§ Longest match](../../../whitepaper.md#longest-match), [§ Combinator semantics](../../../whitepaper.md#combinator-semantics) |
| Research | [Phase 0 review](../research/2026-10-25-phase-0-review.md), findings 3, 7 and 27; prior art read for this record by a research subagent on 2026-10-26, cited inline |
| Supersedes | — |

## Context

[§ Equivalence](../../../whitepaper.md#equivalence) holds every engine to the interpreter's value, position, diagnostics and expected sets. [§ Interpreted engine](../../../whitepaper.md#interpreted-engine) says a size limit "decides only which engine runs, never the result", and [ADR-0025](0025-automaton-construction-stops-at-a-state-cap-or-a-work-budget.md) says "a limit never changes a result". None of them says how often user code runs. The XML docs of `Select`, `Where`, `Bind` and `SelectMany` say each function "runs once per match".

The compiled `Longest` breaks all three for some grammars (review finding 7, reproduced through the public API):

- The automaton computes only the winner's value, from the matched span or by running the winner again. The interpreter's `LongestPlan` runs every alternative, and each alternative's `Select` runs when that alternative matches.
- So a value function on a losing alternative runs under the interpreter but not under the automaton. If it throws, the engine decides whether `Parse` throws. Below, the `Where` makes `Dec` non-Regular, so the second `Longest` runs on the interpreter:

  ```csharp
  var d = Range('0', '9').SkipAtLeastOnce();
  var Int = d.Text().Select(s => (double)long.Parse(s));
  var Dec = Seq(d, Char('.'), d).Text().Select(s => double.Parse(s, CultureInfo.InvariantCulture));
  Longest(Int, Dec).Parse("99999999999999999999.5");                   // Parsed 1E+20
  Longest(Int, Dec.Where(_ => true)).Parse("99999999999999999999.5");  // throws OverflowException
  ```

- A counting `Select` on `String("<")` inside `Longest(…, String("<="))` runs 0 times compiled and once interpreted.

Only value functions are affected today. `Regular` admits `Map`, `Named`, `Context`, `Slice` and `Text` as wrappers, and rejects `Filter`, `Bind`, `Satisfy`, `Custom`, `Switch`, `Match`, `Token` and `Kind` ([`Regular.cs`](../../../src/Creole/Analysis/Regular.cs)). So no function that decides the parse can sit inside a compiled `Longest`.

"Once per match" is not what the rest of the design does either:

- The interpreter runs value functions in attempts it later discards: inside `Lookahead` and `Not`, in an `Atomic` that rewinds, in every alternative of a `Longest`.
- A mode change re-runs the lexer steps it invalidates ([§ Mode channels](../../../whitepaper.md#3-mode-channels)), so lexer functions run again.
- A reused result skips its rule's functions entirely ([§ Reusing parser results](../../../whitepaper.md#reusing-parser-results)).
- Dispatch reads a token's `Kind` once per choice, not once per alternative ([ADR-0016](0016-or-dispatches-on-each-alternatives-leading-item.md)). A user-defined `Kind` getter therefore already runs a different number of times with the rewrite on and off.
- The docs of `Satisfy` already say "an engine may call it more than once for the same item".

Later work needs a rule:

- BENCH-01's second record, on when failures are recorded, weighs running a parse without a failure log and running it again with one when it fails (review finding 3). That runs every user function again on a failed parse, and reproduces the error only if the functions that decide the parse give the same answers the second time.
- [GEN-02](../../active/11-source-generator/GEN-02-emit-the-core-combinators.md) and [GEN-03](../../active/11-source-generator/GEN-03-lambda-and-helper-inlining.md) emit code for `Lookahead`, `Not`, skip forms and `Select`. Whether that code may skip computing a value nobody reads decides what it emits.
- [INC-04](../../active/15-incremental-reparsing/INC-04-reusable-parser-rules.md) reuses results without running their functions.
- [GEN-08](../../active/11-source-generator/GEN-08-differential-engine-agreement-tests.md) and [TOOL-06](../../active/12-grammar-author-tooling/TOOL-06-engineagreementcheck.md) compare engines, and need to know which differences count.

User code in a grammar falls into three roles:

| Role | Members | What its result does |
| --- | --- | --- |
| Value function | `Select`, on one value and on a `Seq` tuple; a query's result selector; a `Match` case's `combine`; `Expression.Build`'s operator functions | Becomes a value |
| Decision function | `Where`; `Bind`, and a query's later `from` that uses an earlier value; `Satisfy`; a custom scanner's `Scan`; members of item and key types that Creole calls, such as `Equals` for `Token` and `Switch` keys and a token's `Kind` | Decides whether, or how, the parse goes on |
| Construction code | `Rec`'s reference; `Expression.Build`'s table; a scanner's `Info` | Builds the grammar. Each runs when its member's docs say, not during a parse |

Constraints:

- **G7, two engines, one semantics** ([§ Design goals and non-goals](../../../whitepaper.md#design-goals-and-non-goals)). Whatever the rule, both engines must follow it, and GEN-08 must be able to test it.
- **Speed.** [INT-06](../../active/03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md) kept the compiled `Longest` because it tokenized 3.31× faster with the 12-rule JSON lexer and 13.58× faster with the 138-rule C#-like lexer (medians of three runs). Both lexers have a `Select` inside an alternative, whose value `Ignore` or `As` then discards ([`LongestLexers.cs`](../../../benchmarks/Creole.Benchmarks/LongestLexers.cs): block comments and escapes in the C#-like lexer, numbers and escapes in the JSON lexer).
- **G12 and G4.** Reuse and re-lexing already change how often functions run, by design.
- **Simplicity.** One rule that users can state in two sentences.

Prior art. Few libraries state a rule. These were read from source or docs by a research subagent. The author did not re-read them.

| Library | Value functions | Decision functions |
| --- | --- | --- |
| chumsky 0.12 | "may freely optimise out `map_with` closures or even invoke them an arbitrary number of times" ([`key_concepts.md`](https://raw.githubusercontent.com/zesterer/chumsky/main/guide/key_concepts.md)). In check mode, `Map` and `MapWith` do not call their closures ([`combinator.rs`](https://codeberg.org/zesterer/chumsky/raw/tag/0.12/src/combinator.rs)) | `Filter`, `TryMap` and `Validate` run their parser with output and always call the closure (same file). The prose asks every closure to be "semantically pure" ([`technical_notes.md`](https://codeberg.org/zesterer/chumsky/raw/branch/main/guide/technical_notes.md)) |
| nom 8.0.0 | `Map` drops its function in check mode, so a `map` inside `recognize` never runs ([`internal.rs`](https://raw.githubusercontent.com/rust-bakery/nom/8.0.0/src/internal.rs)) | `MapRes`, `MapOpt`, `FlatMap` and `Verify` always call theirs ([`combinator/mod.rs`](https://raw.githubusercontent.com/rust-bakery/nom/8.0.0/src/combinator/mod.rs)) |
| LPeg 1.1 | "LPeg does not specify when, if, or how many times it evaluates its captures. Therefore, captures should avoid side effects" ([manual](https://www.inf.puc-rio.br/~roberto/lpeg/lpeg.html)) | A match-time capture "is evaluated immediately when a match occurs (even if it is part of a larger pattern that fails later)" (same page) |
| ANTLR 4.13.2 | Actions run once, on the chosen path, after prediction ([`predicates.md`](https://raw.githubusercontent.com/antlr/antlr4/4.13.2/doc/predicates.md)) | "A predicate in a lexer rule might be executed multiple times even during a single token match" (same page) |
| Bison 3.8.1, GLR | While several parsers run, "semantic actions are recorded, but not performed" ([manual](https://www.gnu.org/software/bison/manual/html_node/GLR-Parsers.html)) | A `%?{}` predicate "is always evaluated immediately (even when in nondeterministic mode)" ([manual](https://www.gnu.org/software/bison/manual/html_node/Semantic-Predicates.html)) |
| FastParse 3.1.1 | `.map` runs on every success, inside lookahead too. There is no mode that skips it ([`package.scala`](https://raw.githubusercontent.com/com-lihaoyi/fastparse/3.1.1/fastparse/src-3/fastparse/package.scala)) | `.flatMap` "is evaluated every time this parser is tried". `.trace()` re-runs the whole parse ([docs](https://com-lihaoyi.github.io/fastparse/)) |
| Parlot 2.0.0 | Generated code never skips a callback: "Skipping a callback's result does not skip its execution, required input values, or exceptions" ([`source-generation.md`](https://raw.githubusercontent.com/sebastienros/parlot/v2.0.0/docs/source-generation.md)). Its skipping covers only built-in values: delimiters, lookaheads and `Text` | No stated rule |

The combinator libraries that skip work (chumsky, nom, LPeg) skip value functions and always run decision functions, which is two roles. The parser generators (ANTLR, Bison) defer actions until the path is chosen. Parlot, Creole's closest competitor, chose the opposite: callbacks run alike in every engine. Whether Parlot runs callbacks on a losing alternative of its choice combinators was not checked.

## Options

### A. Exact: every engine makes the interpreter's calls

Engines and lowering rewrites call each user function with the same arguments, the same number of times and in the same order as the interpreter with no rewrites. Reuse and re-lexing work alike in every engine, so they stay. To restore the rule now, a `Longest` compiles only when no alternative contains a user function.

- **For:**
  - What user code sees never depends on the engine, so side effects in a value function at least repeat the same way.
  - § Equivalence and ADR-0025 stand as written.
  - Parlot 2.0 makes this choice for its generated code, so users moving from Parlot keep what they rely on.
- **Against:**
  - Both lexers behind INT-06's measurements would run on the interpreter, giving up 3.31× and 13.58×, unless rewritten without functions.
  - The generated engine must compute every value the interpreter does, including values that a `Lookahead`, `Not` or skip form throws away.
  - It must still exempt members of item types, because dispatch already reads a token's `Kind` fewer times than the alternatives would.
  - It does not avoid a purity rule. A log-free run with a re-run stays possible, since every engine would do the same, but only if decision functions are deterministic.

### B. Two roles: value functions may be skipped when unused, decision functions only when the outcome is known

Value functions are pure. An engine may run one any number of times, including zero for a value the parse does not use. Decision functions are deterministic. An engine may run one more than once, and skips it only where the outcome is already known. This is the rule critic A proposed in the review.

- **For:**
  - It describes what the compiled `Longest` already does. No code changes.
  - It allows a log-free run with a re-run, reuse without exceptions, and generated code that does not compute unused values.
  - It agrees with what `Satisfy`'s docs, reuse, re-lexing and dispatch already do.
  - chumsky, nom and LPeg draw the same line in code, and chumsky and LPeg state it in their docs.
  - Users who need state during a parse have a mechanism built for it: layer-local state ([§ Layer-local state](../../../whitepaper.md#2-layer-local-state), [LAY-04](../../active/06-layers-and-pipelines/LAY-04-layer-local-state.md)), which bookmarks restore when the parse backtracks.
- **Against:**
  - A value function with side effects, or one that throws on a value its parser can produce, behaves differently by engine. Nothing checks this at run time. The grammar above is one.
  - Users who count or log in a `Select` lose that technique. Until LAY-04, Phase 0 offers no supported way to keep state during a parse.
  - The docs must explain two roles.

### C. Deferred values: compute values only for matches the parse keeps

Engines run discarded attempts and losing alternatives without values, or record which matches survive and run value functions afterwards. Each value function then runs once for each value the parse keeps.

- **For:**
  - Exactly-once calls, so side effects in value functions become safe.
  - ANTLR and Bison's GLR parsers defer actions this way.
- **Against:**
  - ANTLR and Bison can defer because prediction there reads no values. In a combinator grammar, decisions read values.
  - A value that a decision function reads must exist when it runs, which may be inside an attempt the parse later discards. Any grammar with a `Where`, `Bind`, `Switch` or `Match` on a computed value loses exactly-once again.
  - Recording kept matches costs memory in proportion to the input. That goes against "a parse whose results are value types … allocates only its result" ([docs/engines/interpreter.md](../../../docs/engines/interpreter.md)). The alternative is a value-free form of every plan.
  - It is a large change to a built engine, for a property few grammars need.

### D. Defer: fix the compiled `Longest` alone

Make the interpreter's `Longest` over Regular alternatives compute only the winner's value: one pass without values, then the winner again. The two lowerings then agree on today's case. Leave a general rule to GEN-02.

- **For:** the smallest change in words.
- **Against:**
  - BENCH-01's record on recording needs a rule for re-runs now.
  - The interpreter's `Longest` gets slower: every Regular alternative runs, then the winner runs again.
  - The XML docs stay wrong.
  - GEN-02, INC-04 and a re-run each meet the same question later.

## Decision

**B.** Creole may run any function in a grammar more than once, and may skip a call whose result it does not need. Users write functions whose result depends only on their arguments.

1. **Value functions:** `Select`, on one value and on a `Seq` tuple; a query's result selector; a `Match` case's `combine`; `Expression.Build`'s operator functions.
   - An engine runs a value function at least once for each value the parse uses. A value is used if it reaches the result, or if a decision function, `Switch` or `Match` reads it.
   - It may run one more than once. For a value the parse does not use, it may run the function or skip it. Unused values include a losing `Longest` alternative's value, a value inside `Lookahead` or `Not`, a value that `As`, `Ignore` or a skip form discards, and a value from an attempt that is rolled back.
   - It passes a value function only values that its parser produced on the input.
   - A value function must be pure: its result depends only on its arguments, and the grammar must not rely on its side effects or on how often it runs.
   - It should not throw on a value its parser can produce. If it does, `Parse` throws whenever an engine runs it. Whether an engine runs it for an unused value is not specified.
2. **Decision functions:** `Where`; `Bind`, and a query's later `from` that uses an earlier value; `Satisfy`; a custom scanner's `Scan`; and the members of item and key types that Creole calls, such as `Equals` for `Token` and `Switch` keys and a token's `Kind`.
   - An engine runs a decision function wherever its result could change the outcome, with the arguments the interpreter would pass.
   - It may run one more than once, as when a parse runs again or a mode change re-lexes.
   - It may skip a call only where the outcome is already known: dispatch has ruled out the alternative, or a reused result already holds it.
   - A decision function must be deterministic: the same arguments give the same result within a parse and when the parse runs again. The grammar must not rely on its side effects or on how often it runs.
   - LAY-04's `Scan` function returns layer state as well as a value. It is a decision function, because later parsing reads that state.
3. **Display members.** `Describe` and an item's `ToString` run when the reported error is built. They must be deterministic, so that engines agree on the error.
4. **Construction code** runs when its member's docs say, and is outside this rule.
5. **Equivalence.** Engines and lowering rewrites agree on the value, the position, every diagnostic and every expected set for grammars whose functions follow points 1–3. How often a function runs is not part of the agreement. This qualifies [§ Equivalence](../../../whitepaper.md#equivalence), [ADR-0017](0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md) point 4 and ADR-0025's "a limit never changes a result". None is superseded.

Deciding reason: B is the only option that keeps the compiled `Longest`'s measured gain on the lexers that justified it. It also states what the design already does: discarded attempts, re-lexing, reuse and dispatch all run user code a varying number of times. The combinator libraries that skip work draw the same line.

What would change this:

- Evidence that users rely on side effects in value functions often enough that docs and a check cannot cover it. Then C would be worth its cost for value functions that no decision reads.
- Migration from Parlot showing that its users depend on callbacks always running. Then A, at least for the generated engine, would deserve another look.
- A measurement showing the compiled `Longest` gains little once values are in a realistic lexer. Then A's restriction would cost little.

## Consequences

- **Easier:**
  - The compiled `Longest` is correct as built.
  - BENCH-01's record on recording may choose a log-free run with a re-run.
  - GEN-02 and GEN-03 may emit code that does not compute values for `Lookahead`, `Not` and skip forms.
  - INC-04's reuse needs no exception.
  - GEN-08 and TOOL-06 generate only pure functions. On a user's grammar, a mismatch points to an impure function or an engine bug.
- **Harder:**
  - The docs of every member that takes user code state its role, and "runs once per match" goes from the XML docs.
  - Users lose side effects in `Select` as a technique, and use layer-local state instead once LAY-04 is built.
- **Ruled out:**
  - Any promise of how many times a function runs.
  - Catching exceptions from value functions to hide the difference between engines. It would cost a `try` block per call and hide real bugs.
- **Risks and how they are watched:**
  - **A value function that throws, or has side effects, behaves differently by engine.** The docs say so on each member and in the concepts pages. `EngineAgreement.Check` (TOOL-06) reports such a grammar as a mismatch. GEN-03's analyzer already asks for `static` lambdas, which cannot capture locals, the commonest place for a side effect. This record plans no further check.
  - **A later engine feature skips a decision function whose outcome it does not know.** GEN-08's differential tests compare outcomes, so a wrong skip shows up as a mismatch.

## Changes made

- **Whitepaper.**
  - § Combinator semantics: a new § Functions in grammars gives the two roles, what counts as an unused value, and what each kind of function must be. It links this record.
  - [§ Equivalence](../../../whitepaper.md#equivalence): engines agree for grammars whose functions follow § Functions in grammars, and how often a function runs is not part of the agreement.
  - [§ Interpreted engine](../../../whitepaper.md#interpreted-engine): the size-limits sentence holds under the same condition. The values bullet says only the winner's value functions run.
  - [§ Longest match](../../../whitepaper.md#longest-match): the DFA computes only the winner's value.
  - Checked and unchanged: § LINQ query syntax and § Mode channels, whose functions run at construction time; § Source-generated engine; § Reusing parser results; § Still open, which has nothing open.
- **Decision index.** Status, and a note that this record qualifies ADR-0017 point 4 and ADR-0025.
- **Docs.** [docs/concepts/sequencing.md](../../../docs/concepts/sequencing.md) gains "How often your functions run", with the throwing `Longest` as its example. A throwaway test confirmed that the example compiles, parses when compiled, throws on the interpreter's `Longest`, and parses in both with the `Where` fix. [docs/concepts/parsing.md](../../../docs/concepts/parsing.md), [docs/concepts/custom-scanners.md](../../../docs/concepts/custom-scanners.md) and [docs/engines/interpreter.md](../../../docs/engines/interpreter.md) link to it.
- **XML docs.** "Runs once per match" is gone. Value functions say they must be pure, and decision functions say they must be deterministic: `Select`, the seven tuple `Select` overloads, `SelectMany`'s result selector and remarks, `Bind`, `Where`, `Case`'s combine function, the five operator factory methods and `IScanner`. No behaviour changes.
- **Cards.** [GEN-02](../../active/11-source-generator/GEN-02-emit-the-core-combinators.md), [GEN-03](../../active/11-source-generator/GEN-03-lambda-and-helper-inlining.md), [GEN-08](../../active/11-source-generator/GEN-08-differential-engine-agreement-tests.md), [TOOL-06](../../active/12-grammar-author-tooling/TOOL-06-engineagreementcheck.md), [LAY-04](../../active/06-layers-and-pipelines/LAY-04-layer-local-state.md) and [INC-04](../../active/15-incremental-reparsing/INC-04-reusable-parser-rules.md) each gain a scope line citing this record. [BENCH-01](../../active/13-benchmarks/BENCH-01-phase-0-speed-gate.md) returns to Building.
