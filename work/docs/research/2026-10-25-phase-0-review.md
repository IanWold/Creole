# Before Phase 0 closes: what is unnecessary, what could be simpler, where have decisions drifted, and what was missed?

| | |
| --- | --- |
| Date | 2026-10-25 |
| Cards | [BENCH-01](../../active/13-benchmarks/BENCH-01-phase-0-speed-gate.md), [GEN-01](../../active/11-source-generator/GEN-01-generator-skeleton-and-registration.md), [GEN-05](../../active/11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md), [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md), [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md), [INC-04](../../active/15-incremental-reparsing/INC-04-reusable-parser-rules.md), [RSLN-02](../../active/10-roslyn-analyzers/RSLN-02-share-the-analysis-engine-with-the-roslyn-component.md) |
| Whitepaper | [§ Design goals and non-goals](../../../whitepaper.md#design-goals-and-non-goals), [§ Core model: parsers as data](../../../whitepaper.md#core-model-parsers-as-data), [§ Interpreted engine](../../../whitepaper.md#interpreted-engine), [§ Equivalence](../../../whitepaper.md#equivalence), [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention), [§ Roadmap](../../../whitepaper.md#roadmap) |
| Confidence | High for facts about code, cards and records: each finding below was read in the source, and the two defects marked *reproduced* were run. Medium for speed numbers: short BenchmarkDotNet jobs on one machine, not the gate protocol. Medium for remedies: each is a proposal for the maintainer. |

## Short answer

Phase 0's core holds. Commit by default, `Atomic`, furthest failure and the `Named` rule compose into a model that fits in four sentences. Line coverage is 97.2%, no subsystem is dead, and most proposals to delete things did not survive an adversarial pass.

Two cheap changes bring the speed gate within reach, measured on this machine:

- A one-line fix to `SepBy`'s list capacity takes JSON allocation from 1.07× Pidgin to 0.996×.
- Skipping failure recording on successful in-memory parses takes the Parlot ratio from 1.88× to 1.52×, against a threshold of 1.5×.

There is one correctness defect, *reproduced*. A compiled `Longest` skips value functions that the interpreter runs, so the engine choice can change a parse's result. A second defect sits beside it: a disagreement between the automaton and the interpreter goes unnoticed in Release builds.

The largest gaps are not in Phase 0's code but in what no card plans:

- the public contract that generated code needs, since every hook it would call is internal;
- the per-layer parse state that rollback, recovery, restart and reuse must preserve;
- retention for plans that read behind the cursor;
- reuse of `Analysis/` at build time under netstandard2.0;
- any speed gate after Phase 0.

The largest simplification is the fixed-arity overload families, about 4,000 lines, but they need a superseding decision.

## Context

The maintainer asked for a review before BENCH-01, because choices that look minor on one card can add up across many. The review asked four questions:

1. What is unnecessary?
2. What could be simpler?
3. Where have decisions departed from the plan?
4. What has been missed?

Method:

1. **Shared evidence.** Every reviewer started from the same evidence:
   - test counts: 1,496 tests, all passing;
   - line coverage, measured with `dotnet-coverage`;
   - a reflection inventory of the public API: 47 written types;
   - code sizes;
   - one short benchmark run with Creole entries added to both gate benchmarks (the first slice of BENCH-01);
   - an ablation of each lowering rewrite on the gate grammars.
2. **Eight reviewers ran in parallel**, all read-only:
   - five by area: authoring surface; nodes and lowering; compiled `Longest`; errors and spans; tests, benchmarks and tooling;
   - three across the project: the decision trail, fit with later phases, and a fresh user who wrote CSV, arithmetic and JSON grammars from `docs/` alone.

   The author's own early observations were kept out of the briefs; all eight were covered independently.
3. **Two critics** were briefed to argue for keeping each proposed removal or change in semantics.
4. **The author checked the key code claims** mechanically, reproduced the value-function defect, and re-measured the gate with the two cheap fixes applied in a scratch copy.

Reviewer IDs (R1–R5, X1–X3) and critic IDs (A, B) are cited below so each claim can be traced. The working files were in the session scratchpad and are not kept, so every number this note relies on is reproduced here.

## Findings

### The Phase 0 speed gate

1. **Where the gate stands.** Creole JSON and arithmetic grammars were added to the gate benchmarks: `CreoleJson` is character level and written like the Pidgin baseline, and `CreoleArithmetic` uses `Expression.Build`. Each was run under three variants in a scratch copy, using `--job short` with 10 iterations and Pidgin and Parlot in the same run. Ratios are Creole's mean divided by the baseline's, in the order arithmetic, JSON small, medium, large:

   | Variant | ÷ Pidgin (gate: geomean ≤ 1.0, none > 1.25) | ÷ Parlot (gate: geomean ≤ 1.5) | JSON allocation ÷ Pidgin (gate: ≤ 1.0 each) |
   | --- | --- | --- | --- |
   | V0, as built | 0.37, 0.30, 0.33, 0.40 (0.35) | 1.61, 2.25, 1.98, 2.22 (**2.00**) | 1.06, 1.07, 1.07 |
   | V1, `SepBy` capacity fix | 0.38, 0.29, 0.31, 0.38 (0.34) | 1.47, 2.10, 1.90, 2.11 (**1.88**) | **0.996**, 0.996, 0.996 |
   | V2, V1 plus no log on success | 0.29, 0.22, 0.28, 0.32 (0.27) | 1.11, 1.68, 1.56, 1.81 (**1.52**) | 0.996, 0.996, 0.996 |

   As measured: the Pidgin half passes with a wide margin in every variant. Run-to-run noise is visible in the baselines; Parlot's arithmetic mean moved between 299 and 353 ns across runs. A gate run is three default-job runs on the gate machine ([benchmarks/README.md](../../../benchmarks/README.md)), so these numbers are direction, not verdict.

2. **`SepBy` lists start at capacity 1.** [`SeparatedPlan.cs:40`](../../../src/Creole/Plans/SeparatedPlan.cs) writes `List<T> items = [value];`, which sizes the list to one element. `Many` starts at the default capacity. R2 modelled the extra growth on the three gate inputs at 0.8, 66.4 and 658.7 KB, against a measured gap to Pidgin of 0.75, 63 and 623 KB, and predicted 0.996× after the fix. V1 measured exactly 0.996×. This is a bug fix, not a design change.

3. **Every `Parse` records failures, even when the parse succeeds.** `Parse` attaches a failure log ([`Parsing.cs:118`](../../../src/Creole/Parsing.cs)). Scans, dispatch gaps, item tests and expression operators record into it on the success path.
   - **Measured cost:** from V1 to V2, recording costs 1.39× on arithmetic and 1.44×, 1.21× and 1.16× on small, medium and large JSON. ERR-02 measured 1.42–1.46× on token JSON.
   - **Why a log-free first run is safe:** every log access is `Failures?.…`, plans never branch on recorded state, and the log runs no user code while recording. Critic A checked each of these. So a run without a log returns the same value and position, and re-running with the log after a failure gives the same error, provided user code is deterministic (finding 8).
   - **Limits:**
     - streamed and pipelined input cannot be re-run (G2, G3);
     - `Recover` needs the log during the parse;
     - a failed parse costs up to twice as much;
     - `Where`, `Bind` and `Custom` code runs again on failure.
   - **The alternative:** whitepaper [§ Design choices](../../../whitepaper.md#design-choices) promises "an interned expectation-set handle", which ERR-02 did not build. Critic A prefers making recording cheaper first, because that keeps one policy for every kind of input.
   - Inference: V2's 1.52× is within noise of the threshold, so a re-run alone may not settle the gate.

4. **What remains of the JSON gap.** At V2 the arithmetic ratio to Parlot is 1.11×, but JSON is 1.56–1.81×.
   - **Allocation:** Creole JSON allocates about twice what Parlot does, 9.35 MB against 4.75 MB on the large input. The grammar's string handling (chunks joined with `string.Concat`) has not been separated from the engine's share. Parlot uses its built-in string literal.
   - **Item tests:** every `Char`, class, `Token` and `Kind` test goes through a delegate into a binary search over ranges. That is [`PlanBuilder.Primitives.cs:12`](../../../src/Creole/Plans/PlanBuilder.Primitives.cs) and `ItemPlan`. INT-05 measured it at 7–10 ns per test and left it unaddressed (R2).
   - **`Named` defeats the scan rewrite.** The rewrite applies only to a bare `CharClassNode` ([`PlanBuilder.Repetition.cs`](../../../src/Creole/Plans/PlanBuilder.Repetition.cs), `SkipRepeat`), so labelling a class inside `SkipMany` turns it off. X3 measured 1.7–1.9× on CSV and noted that no doc warns about it.

5. **Lowering rewrites on the gate grammars.** The rewrite ablation (shared evidence, short job, with a log):

   | Rewrites on | Arithmetic | JSON medium |
   | --- | ---: | ---: |
   | All | 548 ns | 1,116 µs |
   | All but `Scan` | 768 ns | 1,734 µs |
   | All but `Dispatch` | 525 ns | 1,231 µs |
   | All but `CollapseSelect` | 534 ns | 1,125 µs |
   | None | 727 ns | 1,747 µs |

   - **`Scan`** carries nearly all of the gain.
   - **`Dispatch`** gains about 10% on JSON.
   - **`CollapseSelect`** shows nothing, but neither gate grammar contains a `Select` directly over a `Select`. It stays on its targeted evidence of 1.07–1.08× with non-overlapping ranges, and on the whitepaper's list (critic B; R2 had proposed deleting it).
   - **The compiled `Longest`** takes no part in the gate: neither grammar uses `Longest`. INT-06 nonetheless blocks BENCH-01 (X1).

6. **The gate procedure has no tooling** (R5).
   - The geometric mean over the median of three runs is worked out by hand.
   - Run folders record neither the commit nor the Parlot version, which ADR-0003 requires.
   - A full run includes 43 benchmarks that no gate uses; Sprache alone takes 265 ms per operation on large JSON.
   - `bench-compare.py` serves no gate.
   - The hosted benchmark workflow was deleted, but FND-05's Outcome, `benchmarks/README.md` and [ADR-0003](../decisions/0003-numeric-gate-thresholds.md) still describe it.

### The compiled `Longest`

7. **Value functions run or not depending on the engine (*reproduced*).** `DfaLongestPlan` computes only the winner's value, from the matched span where it can. `LongestPlan` runs every alternative, and `MapPlan` applies each selector eagerly (R3). The author ran this through the public API:

   ```csharp
   var d = Range('0', '9').SkipAtLeastOnce();
   var Int = d.Text().Select(s => (double)long.Parse(s));
   var Dec = Seq(d, Char('.'), d).Text().Select(s => double.Parse(s, CultureInfo.InvariantCulture));
   Longest(Int, Dec).Parse("99999999999999999999.5");                   // Parsed 1E+20
   Longest(Int, Dec.Where(_ => true)).Parse("99999999999999999999.5");  // throws OverflowException
   ```

   - **Side effects differ too.** A counting `Select` on `String("<")` inside `Longest(…, String("<="))` runs 0 times compiled and once interpreted.
   - **What it contradicts:** § Interpreted engine says lowering "decides only which engine runs, never the result"; [ADR-0025](../decisions/0025-automaton-construction-stops-at-a-state-cap-or-a-work-budget.md) says "a limit never changes a result"; and § Equivalence states the same rule.
   - **What the docs say now:** XML docs say `Select` "runs once per match", which is already false. No rule covers functions that throw.
   - **Critic A's proposed rule, in two tiers:**
     - Value functions (`Select`, tuple selectors, `As`, operator functions) are pure and total, and may run any number of times, including zero.
     - Functions that decide the parse (`Where`, `Bind` continuations, `Switch` and `Match` selectors, `Custom`) may run more than once if deterministic. They may be skipped only where the outcome is already known.
   - **Why it matters elsewhere:** the rule is also a prerequisite for finding 3's re-run.

8. **A disagreement goes unnoticed in Release builds.** `Rerun` ([`DfaLongestPlan.cs:32–45`](../../../src/Creole/Plans/DfaLongestPlan.cs)) re-runs the winning alternative on the interpreter, checks agreement with `Debug.Assert`, and returns `true` whatever the result. The span-value path has no check in any build. Fragment E′ is backed by differential testing, not a proof: INT-06 records about 145 million prototype cases and about 200 million from its critic's harness (critic A). Falling back to the interpreter on disagreement costs nothing on the hot path.

9. **The evidence behind the automaton is thinner than it looks.**
   - **The benchmark check cannot fail.** The lexer benchmarks' agreement test cannot fail: `Tokenize` advances by one on a failure ([`LoweringBenchmarks.cs`](../../../benchmarks/Creole.Benchmarks/LoweringBenchmarks.cs)), and `BaselineAgreementTests` asserts the input length (R5). The benchmarked `LongestLexers` duplicates the test `LexerSamples`. `DfaLoweringTests` checks the same rules token by token, but on sample text, not the benchmark corpus.
   - **Only one baseline was measured.** The 3.31× and 13.58× gains compare against a `Longest` that tries every alternative. No dispatched `Longest` exists to compare against. On the same JSON input, a dispatched `OneOf` with scans ran 383–392 µs (INT-06) and the compiled 12-rule `Longest` lexer ran 458 µs (INT-09). These come from different runs and categories, so the comparison is indicative only. It reverses the prototype's "2.6× faster than a dispatching `OneOf`".
   - **Removal would save less than it seems.** ANA-04 and ANA-07 need `CommitAutomaton` for exact CRL0101 and CRL0103 whether or not it executes ([ADR-0017](../decisions/0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md) option C). Removing execution would save about 250 lines, not 1,500 (critic A correcting R3).
   - **What the automaton gives that dispatch cannot:** it reads each character once whatever the rule count. A dispatched `Longest` still runs about ten candidates at `s` in the C#-like lexer.

10. **Smaller automaton costs** (R3):
    - **Retries multiply build cost.** A nested `Longest` that fails a limit is rebuilt from each suffix ([`PlanBuilder.Choice.cs:7–10`](../../../src/Creole/Plans/PlanBuilder.Choice.cs)). INT-09 measured 3 builds, 142.6 ms and 128.9 MB for 400 nested keywords. ADR-0025's bound holds per build, not per lowering.
    - **There is no minimum size.** Every build allocates a 128 KB class table, even `Longest(String("<"), String("<="))`.
    - **Dead code.** About 50 lines of duplicate-path removal in `CommitAutomaton` never run in the test suite; INT-06 recorded that a mutant removing them survived.
    - **`Custom` blocks compilation.** Any `Longest` with a `Custom` alternative goes to the interpreter ([`Regular.cs:53`](../../../src/Creole/Analysis/Regular.cs)). The whitepaper says engines "run the scanner alongside the DFA", which ADR-0017 assigns to fusion (X1).

### Build-time reuse and the generated engine

11. **`Analysis/` cannot run inside the netstandard2.0 Roslyn component as written.** Three reviewers found this independently (X1, X2, R3).
    - **What expects it to:** ADR-0017 point 7, ADR-0025, ANA-01 ("not a second implementation"), ANA-04, ANA-07 and GEN-05 all expect the runtime's `Regular` and `CommitAutomaton` to run at build time.
    - **What the code depends on:**
      - `IAlternateEqualityComparer` and `GetAlternateLookup` (.NET 9 and later; [`CommitAutomaton.cs:182, 224`](../../../src/Creole/Analysis/CommitAutomaton.cs)), added in INT-09 against that card's own timing note;
      - casts to runtime node classes in `Regular`;
      - `System.Type` in `FirstSet`;
      - named character sets computed from the running runtime's Unicode data.
    - **What no one owns:** no card owns the port.
    - **Remedies:** record the constraint and compile `Analysis/` for netstandard2.0 in CI. Or, R3's alternative: generated code calls into the runtime for a compiled `Longest`, so nothing is ported and the engines agree by construction.

12. **Nothing plans the runtime contract for generated code** (X2; R2 and critic B agree). Generated code lives in user assemblies.
    - **Every hook it needs is internal:** the cut flag, depth counting, failure recording, `Reset`, `Since` and the staging types. The public cursor offers only `Advance`, `TryPeek`, `TryRead`, `Available`, `IsAtEnd` and `Position`.
    - **What GEN-01 plans to expose:** only `Staging` and the name table.
    - **Other gaps:**
      - The whitepaper's generated sample calls `c.PeekKind()`, which does not exist.
      - A fallback for a subtree inside a rule field can only be reached through internal `Children`.
      - `Staging.Register` throws on a second registration, so a grammar class with a field that aliases another rule fails under staging but works when interpreted.
    - **Critic B:** keep the hook, but fix its contract in GEN-01's scope.

13. **Agreement testing has gaps** (X2).
    - **GEN-06 can pass without exercising generated code.** Its criterion, "all interpreter test suites pass against the generated engine", passes through fallback: the suites build grammars at run time, and only one test grammar class is `partial`.
    - **GEN-08 and MODE-06** do not cover generated layers or modes.
    - **The reference is unnamed.** Neither GEN-08 nor TOOL-06 names the reference configuration. If GEN-05 shares `CommitAutomaton`, a bug in it shows up in both engines, unless the comparison also runs with `LoweringOptions.None`.

14. **Error text can depend on initialisation order** (R1, R4). `TokenGrammar`'s static constructor sets `Display<TToken>.Tokens ??= …` ([`TokenGrammar.cs`](../../../src/Creole/TokenGrammar.cs)). The same grammar on the same input can therefore render differently depending on whether any token grammar over that type has been initialised in the process. A grammar over `IKinded` tokens that derives from `Grammar<TTok>` never gets the display at all.

### Later phases: parse state and retention

15. **Per-layer parse state is listed nowhere** (X1 and X2, each with counterexamples). The decisions added state one record at a time:
    - the cut flag ([ADR-0011](../decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md));
    - the depth count ([ADR-0020](../decisions/0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md));
    - the furthest-failure record and context frames ([ADR-0021](../decisions/0021-how-failures-are-recorded-and-merged.md));
    - the mode log and layer state.

    Each consumer saves a different subset: `LightMark`, LAY-02's marks, `Recover` (ERR-05), the restart bookmark (STRM-02), the reuse memo (INC-04) and mode invalidation (MODE-02). None restores the failure record, and INC-04 omits depth. Counterexamples worked by hand:
    - **Reuse and depth:** reusing a rule lets a parse through that a fresh parse ends at `MaxDepth`. That contradicts "the limit never changes what a grammar accepts".
    - **Reuse and errors:** a reused rule records nothing, so the error drops an expectation that a fresh parse reports.
    - **Mode change:** after a mode change and rewind, a stale furthest failure from the old token stream hides the real error.

16. **Copies taken at a construct's entry go stale under a mode change** (X1; R4 from the other side).
    - **Where the copies are taken:** filters and chained operators copy the first item before the construct runs ([`FilterPlan.cs:25`](../../../src/Creole/Plans/FilterPlan.cs); [ADR-0014](../decisions/0014-a-rejected-filter-reports-at-the-start-of-the-value.md) point 6). LAY-01 plans the same for `Context` spans.
    - **Why they go stale:** if the construct begins with `SetMode`, the copied item is the one invalidation discards. Example C's own `Operand` rule does this.
    - **The deferral is inconsistent:** span capture was deferred to LAY-01, but item capture stayed in Phase 0.
    - **What to do:** design the two together when LAY-01 starts.

17. **Retention misses plans that read behind the cursor** (X2; X1 on the LAY-02 conflict).
    - **What pins and what does not:** only `Atomic`, `Lookahead`, `Not` and `Longest` pin the buffer. But `Slice`, `Text`, `String` (which resets after a partial read), `CIString` and the compiled `Longest` all read from the construct's start.
    - **The failure:** streamed, the gate's own `NoneOf(...).SkipAtLeastOnce().Text()` across a segment boundary makes `Since` throw.
    - **A card contradicts the design:** LAY-02's scope says only a `Longest` "over non-regular alternatives" pins. ADR-0017 and § Bookmarks and retention say a `Longest` pins whether or not it compiles.

18. **Speed is gated once** (X2).
    - **Phase 1 changes the hot path** BENCH-01 measures: pulls, five-field light marks, pins and Starved checks.
    - **Nothing re-measures it.** No later gate re-measures the interpreter, and no gate measures any engine on a two-layer pipeline. Example A's two-layer JSON is the headline case.
    - **BENCH-02's only criterion** is "results published".

19. **Smaller design gaps** (X2):
    - **"Debug builds" has no mechanism.** ANA-02 and ANA-08 (CRL0206) key behaviour to Debug builds, but a single Release package cannot tell what configuration its consumer was built in.
    - **The public scanner contract** cannot carry what later cards need: STRM-01's Starved flag, INC-02's examined extent, and RES-02's suspension. A copy-and-discard cursor loses by-value state, and reads through `Available` are invisible.

### `Cut`

20. **Dropping `Cut` was proposed and rejected.**
    - **The proposal (X1):** under commit by default, a narrow `Atomic` gives `Cut`'s two stated purposes. The rule was rewritten twice in one day (ADR-0008, 0010, 0011), and option E (defer `Cut`) was rejected for procedural reasons only.
    - **Why it was rejected (critic A):**
      - The equivalence fails for nested `Atomic`s. With a cut, the inner one's commitment reaches the outer one; with narrowing, the outer one rewinds.
      - It also fails for commit points that depend on the path taken, such as a cut in one branch of an `Or` inside an `Atomic`, or inside a repetition. Narrowing those duplicates the prefix or restructures the rule.
      - The cross-rule case X1 concedes is the documented primary use.
      - The run-time cost is negligible (R2), and only about 93 of the 841 lines of control model mention a cut.
    - **What does hold:**
      - LAY-02 should release at the `Atomic`'s exit unless a measurement asks for precise release.
      - `docs/concepts/control.md` should present narrow `Atomic` first.

### Public surface

21. **The fixed-arity overload families are the largest simplification available** (R1; critic B).
    - **Size:**
      - `OneOf` and `Longest` have 15 overloads each, about 2,700 lines, 1,730 of them XML docs.
      - `Match` and `Switch` have 8 fixed overloads each beside their collection forms, about 1,280 lines.
    - **The cap bites:**
      - The 16-alternative cap forced a 15-way `Flat` switch and nested `Level` helpers in both the test and benchmark lexers.
      - It shaped a whitepaper sentence in § Properties.
    - **Correction to the reviewer:** [ADR-0006](../decisions/0006-switch-and-match-take-a-collection-beyond-eight-cases.md)'s Arity table does decide `OneOf`'s 2–16. `Longest`'s arity comes from CORE-06's scope.
    - **Cost of `params`:** a `params` span must come last, so no caller-info parameter can follow it. A null alternative would be named by index only until CRL0004 (Phase 2). RSLN-04 calls this forward-reference bug the most common one in Sprache and Pidgin grammars.
    - **`Match`:** its per-case null guard is nearly unreachable, so making it collection-only loses almost nothing.
    - **Drift:** collection-form messages name the index but not the key that ADR-0006 promises.
    - **Options for a superseding ADR:**
      - `params` only, deleting about 2,600 lines;
      - fixed overloads plus one `params` overload to lift the cap.

22. **Phase 0 cannot run the token grammars it ships** (X3, X2).
    - **`Parse` exists only for `Parser<char, T>`**, so `TokenGrammar`, `Kind`, `Case<T>()` and `IKinded` compile but cannot run. The error docs show token output no user can produce.
    - **This contradicts the plan.** § Roadmap says "Phase 0 alone is a modern single-layer library", and G1 covers tokens.
    - **The fix is small either way:** a generic `Parse(ReadOnlyMemory<TIn>)`, since the log, cursor and display are already generic; or an editorial change.

23. **What a fresh user missed** (X3; three grammars, 51 inputs, all correct):
    - **Spans for values.** No `Position`, `Spanned` or `WithSpan`, though [§ Using spans in grammars](../../../whitepaper.md#using-spans-in-grammars) lists them. An evaluator needs spans to report "unknown variable".
    - **Hiding token internals in errors.** No way to keep whitespace and a token's internals out of expectations. `Named` cannot hide them once the token consumed input. Wrapping a token in `Longest(p, p)` works, because a successful `Longest` restores the failure record, which shows the engine can already do it.
    - **No public `CharSet` combinator.** `CharSet` is public by ADR-0012, but no public combinator builds a class from one. The repository's own lexers reach the internal `CharClassNode`.
    - **A rule named `Expression`** hides the nested `Grammar<TIn>.Expression` (CS0108, then CS1061).
    - **Result access.** `result.Value` is the boxed case, not `T`. There is no `TryGetValue` or value-or-throw helper.
    - **Left recursion** fails every parse with the `MaxDepth` error, even on input `1`.
    - **No getting-started page.** The only complete single-layer grammar quoted in the docs lives in `tests/`.

24. **Rendered errors have no size bound** (X3, measured). One error on a 617,799-character minified line renders to 1,235,763 characters. A depth-limit error on 1,500 nested `[` prints 1,000 context notes, about 40 KB. Both are the untrusted-input case that [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input) exists for.

25. **Small surface items**, after critic B:
    - **`Grammar.Deterministic`** is a public no-op ahead of ANA-06. Deleting it does not hold: 1.0 ships after Phase 2, so no external grammar can break. But its XML summary claims an assertion that does not run.
    - **`LineMap.Builder`** can be internal: its only external use is a 2 GB test, and that works through `InternalsVisibleTo`.
    - **`SourceRegistry`** has no producer, and the source's name is held in two places. Decide at LAY-05.
    - **`Case<T>()`:** whitepaper lines 136 and 234 leave open whether union tokens need `IKinded`, and the code requires it. Decide before REL-04; removal is not cheaper than deciding.
    - **`Token(c)` over `char`** matches like `Char(c)` but is not Regular, so a `Longest` containing it silently stays on the interpreter.
    - **[ADR-0007](../decisions/0007-maybe-as-a-non-boxing-union.md)'s revisit trigger has fired** ("nothing in Creole calls `Value`"): `MatchPlan.cs:25` reads `union.Value`, at 2,936 B per 100 parses. That also contradicts § Design choices' "without boxing".

### Tests and tooling

26. **From R5:**
    - **Tests that pin implementation, not behaviour:** five tests pin the plan graph (`RecLoweringTests`, `PlanLoweringTests.EachBuilderLowersAfresh`).
    - **A coverage gap:** the exhaustive control-model generators omit `Named` and `Context` since ERR-02, so ADR-0011's "every grammar to depth 3" no longer covers every control node.
    - **Tests that cannot fail:** 57 tests cannot fail because of a Creole defect: two BCL samples, 55 subsumed table cases, and two uniqueness checks the dictionary already enforces.
    - **Dependabot** has no `ignore` rules. It will propose bumps to deliberately pinned Parlot, Pidgin and `Microsoft.CodeAnalysis.*`, and its first PR is red.
    - **`work.py` slug bug:** `work.py` blanks inline code before slugging headings, so links to headings that contain code are reported broken. Two cards' notes already work around it.
    - **Stale reason:** three places still give INT-08 as the reason for using internals.

### Conflicts between sources

27. **Each is a defect under the precedence rule:**

    | Lower source | Says | Higher source or code | Says |
    | --- | --- | --- | --- |
    | [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md) scope | A `Longest` over non-regular alternatives pins | ADR-0017; § Bookmarks and retention | Every `Longest` pins, compiled or not |
    | Whitepaper § Custom primitives | Engines run the scanner alongside the DFA | ADR-0017; `Regular.cs:53` | Fusion only; the interpreter takes the whole `Longest` |
    | Whitepaper § Design choices; ERR-02 scope | Failures record an interned expectation-set handle | `FailureLog` | An array log with linear de-duplication |
    | Whitepaper § Design choices | Value-type results flow without boxing | `MatchPlan.cs:25` | `Match` boxes through `union.Value` |
    | Whitepaper § Feedback between layers and § Interpreted engine | Plans are cached by node | `PlanCache.cs` | Cached by root; children are lowered per root |
    | Whitepaper § Source-generated engine sample | `c.PeekKind()` | `Cursor<T>` | No such member |
    | XML docs of `Select`, `Where`, `Bind` | Runs once per match | `DfaLongestPlan` | Losing alternatives' functions never run |
    | ADR-0006 | Collection-form errors name a case by index and key | `Choice.Switch.cs` | Index only |
    | § Roadmap; G1 | Phase 0 alone is a usable single-layer library over any `T` | `Parsing.cs` | Characters only |
    | FND-05 Outcome; `benchmarks/README.md`; ADR-0003 | Hosted runners catch regressions | `.github/workflows/` | The benchmark workflow was deleted |
    | `Grammar.Deterministic` summary | Asserts LL(1) | `Grammar.cs` | Returns its argument |

## Disconfirming evidence

Each reviewer listed what it examined and kept. Two critics argued for the status quo on 15 proposals, checking each against code, cards, records and the whitepaper. What argues against a finding, or held up under review:

- **The core composes** (X1). Commit by default, `Atomic`, `Lookahead` and `Not`, furthest failure, and `Named` on empty failures can be stated in four sentences. ADR-0013, 0015 and 0020 each add one rule with no exceptions.
- **G7 is not inflated** (X2). What the generator must reproduce for G7 is dominated by ADR-0021's error rules. ADR-0022 and ADR-0025 shrank it, and the lowering rewrites are tested against `LoweringOptions.None`. The risk lies in reaching the hooks and in agreement testing (findings 12 and 13), not in how much there is to reproduce.
- **Tests and tooling that earn their place:**
  - Coverage is 97.2% of coverable lines.
  - The four test layers catch different faults; ERR-02's recorded mutation checks show it (R5).
  - `ControlModel` is what ADR-0011 names as its risk control.
- **Error and rendering choices:**
  - The East Asian width table is justified: ERR-03's criterion names wide characters, and .NET has no width API (R4).
  - ADR-0021's merge rules were traced adversarially, and the code matched (R4).
- **Benchmarks and projects:**
  - The non-gate baselines are named in § Benchmark plan; only their place in gate runs is a problem.
  - The placeholder projects hold NuGet IDs, and later cards name them.
- **Proposals rejected by the critics:**
  - Drop `Cut` (finding 20).
  - Delete `Grammar.Deterministic`, `CollapseSelect`, `TextNode` and the cross-source context path.
  - Delete the staging hook (fix its contract in GEN-01 instead).
  - Replace the compiled `Longest` (freeze it and measure a dispatched `Longest` instead).
- **Reviewer facts the critics corrected:**
  - ADR-0006 does decide `OneOf`'s arity.
  - The cut-specific test model is about 93 lines, not 840.
  - Removing the automaton's execution saves about 250 lines, not 1,500.
  - ADR-0007's quoted sentence is its revisit trigger, not its reason.
- **Prior art cited by reviewers** was not re-read by the author and is *unverified* here: FastParse's re-run in `Failure.trace`; FastParse's and nom's cuts in backtrack-by-default libraries; Superpower's and Parlot's ordered token choice.

## Implications for Creole

Each row is a proposal; the maintainer decides. Code changes are separate discrete changes, not part of this note.

| Proposed outcome | Items | Findings |
| --- | --- | --- |
| **Fix before the gate run** | `SepBy` list capacity; `Rerun` falls back to the interpreter on disagreement; lexer benchmarks return a token hash and compare with the rewrite on and off; benchmark run folders record the commit and the Parlot version; filter gate runs to the gate classes | 2, 6, 8, 9 |
| **Measure in BENCH-01** | Log-free configuration and a cheaper-recording prototype; the share of JSON's gap due to the grammar's string handling; a sealed class-test plan in place of the delegate | 3, 4 |
| **Propose an ADR** | Value-function purity in two tiers (§ Equivalence, ADR-0025); when failures are recorded (log-free in-memory parse or cheaper recording); fixed-arity overloads (supersede ADR-0006 and the CORE-02 convention); the list of per-layer parse state and the rule that rollback, recovery, restart and reuse preserve all of it; retention for plans that read behind the cursor; speed re-gated after Phase 0 and for two layers (amend ADR-0003); build-time reuse of `Analysis/` or calling into the runtime; how "Debug builds" is detected | 3, 7, 11, 15, 17, 18, 19, 21 |
| **New card** | Runtime contract for generated code, blocking GEN-01 and GEN-02; a dispatched `LongestPlan`, measured against the automaton before GEN-05; a lowering-wide automaton budget and a minimum size; a value-span combinator (`Position`, `Spanned`); a combinator that hides a token's internal expectations; a bounded rendering window and collapsed context notes; a generic `Parse` for token input, or an editorial change | 9, 10, 12, 22, 23, 24 |
| **Card scope edits** | LAY-02: every `Longest` pins, and release at `Atomic` exit by default; GEN-01: aliased registration, and hooks for failures, cuts, depth and reset; GEN-06, GEN-08, TOOL-06: emit test grammars as source, cover layers and modes, compare against `None`; RSLN-02 and GEN-05: the netstandard2.0 constraint; LAY-01: design item and span capture together; LAY-05: `SourceRegistry`; ANA-01 or REL-04: `Case<T>()`; GEN-02 or REL-04: ADR-0007's trigger | 12, 13, 16, 17, 25 |
| **Whitepaper editorial** | The rows of finding 27's table where the whitepaper is the lower source, once the matching decisions are made | 27 |
| **Docs and small code** | `Deterministic` summary; `LineMap.Builder` internal; `Named` defeats scans; left recursion; the `Expression` name collision; `ParseResult` access; a getting-started page; collection-form messages name the key; control docs present narrow `Atomic` first; stale "until INT-08" text | 4, 20, 21, 23, 25, 26 |
| **Tooling** | `work.py` heading slugs; Dependabot `ignore` rules; add `Named` and `Context` to the control-model generators; rewrite the five plan-graph tests as behaviour; delete the 57 tests that cannot fail | 26 |
| **Keep, recorded** | `Cut`; `CollapseSelect`; `TextNode`; cross-source rendering; East Asian width; `ControlModel`; non-gate baselines; placeholder projects | 5, 20, Disconfirming evidence |

## Open questions

- How much of the remaining JSON gap to Parlot comes from the grammar's string handling, and how much from the engine?
- Does cheaper recording reach the gate without a re-run policy?
- How does a dispatched `Longest` compare with the automaton on the C#-like lexer?
- Is any package planned before 1.0? A preview would change the weight of the `Deterministic` stub, the unrunnable token grammars and the public surface.
- Does Parlot record anything on a successful parse? The answer affects how fair the gate comparison is.

## Sources

1. Creole repository at `b61c0fd`, with Creole entries added to `JsonBenchmarks` and `ArithmeticBenchmarks`: code, tests, cards, decision records and whitepaper as cited inline — read by reviewers and spot-checked by the author.
2. Local measurements, 2026-10-25, on an i9-13900H with .NET 11.0.0-rc.2 and BenchmarkDotNet 0.16.0-preview.2. They comprise `dotnet test` with `dotnet-coverage` 18.12.0, two short gate runs, and a rewrite ablation. Runs were not kept; the numbers are reproduced above.
3. X3's trial grammars: CSV, arithmetic and JSON, written against the Release `Creole.dll` from `docs/` alone. Not kept; the excerpts and measurements are reproduced above.
4. FastParse, nom, Superpower, Parlot and Megaparsec sources, as summarised by reviewer and critic agents in this session. Not re-read by the author — *unverified*.
