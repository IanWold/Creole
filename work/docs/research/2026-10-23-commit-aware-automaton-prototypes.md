# Does a commit-aware automaton agree with Creole's interpreter, and what does it cost?

| | |
| --- | --- |
| Date | 2026-10-23 |
| Cards | [INT-06](../../complete/03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md), [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md), [ANA-04](../../active/09-static-analysis/ANA-04-choice-checks.md), [ANA-07](../../active/09-static-analysis/ANA-07-linear-maximal-munch.md), [GEN-05](../../active/11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md) |
| Whitepaper | [§ Longest match](../../../whitepaper.md#longest-match), [§ Interpreted engine](../../../whitepaper.md#interpreted-engine), [§ Properties](../../../whitepaper.md#properties), [§ Deterministic mode](../../../whitepaper.md#deterministic-mode) |
| Confidence | Medium to high for what was measured: every claim below is a differential test against the real interpreter or a measurement on this machine. Medium for generality: the random terms use a three-letter alphabet and depth up to 5, and `Lookahead`, `Not`, `Cut`, values and errors are not covered. The two proofs are sketches, and the wider fragment of finding 19 is tested but not proved. |

## Short answer

The commit rule of the [earlier note](2026-10-23-dfa-compilation-under-commit-semantics.md) (findings 26–28) works, with one refinement. In its eager form, a choice resolves as soon as a thread commits inside its high branch or finishes it, and a match prunes everything below it. That form agreed with the interpreter on every case tested whenever no explicit `Atomic` contains a choice, and on all 13 realistic token shapes. It needs one register per `Longest` alternative and stayed small: 41 states for a JSON lexer and 517 for a 138-rule C# lexer. When a choice sits inside an explicit `Atomic`, the eager form can be wrong. A structural condition, E′, admits the choices inside `Atomic` that cannot cause this, including every idiomatic token tested and both lexers. On about 128,000 random terms in E′ the eager form had no mismatch (finding 19). For the rest, a deferred form works: a provisional thread only marks its events. With a "sure `Atomic`" rule, the deferred form also stays finite, except when an `Atomic` body that can fail after consuming sits in a repetition. That case needs a state cap with fallback to the interpreter. Reps-style memoization carries over, with one change: it stores the scan's stop state, not a yes-or-no flag. The textbook regex reading (Option B) disagrees with the interpreter on 3 of 13 realistic tokens. A sufficient condition for when it agrees never failed on 32,000 terms and covers about 82% of the standalone terms where it does agree. On speed (finding 12, medians of three runs), the DFA tables tokenized 6.0× faster than the interpreter's `Longest` on a JSON lexer, and 2.6× faster than a dispatching `OneOf`. On the C#-like lexer they were 21.9× faster than `Longest`.

## Context

The [earlier note](2026-10-23-dfa-compilation-under-commit-semantics.md) found no published construction that models Parsec-style commit semantics. It proposed one (findings 26–31) but left it unproven. The maintainer asked for prototypes of that construction, covering six questions:

1. state counts at lexer size;
2. speed against the interpreter;
3. a proof sketch for the construction;
4. Option B's condition;
5. backup analysis and memoization;
6. the blow-ups found in the first runs.

A seventh prototype was added before the decision, because both lexers fell outside the fragment that the proof sketch covers. It asks whether that fragment can be widened (finding 19).

This note records the results. It feeds the decision record for INT-06's Design stage.

## Method

The prototype is a C# program of about 2,000 lines. It is not in the repository: it was a research tool, and this note quotes every number it relies on.

- **Name.** It builds as an assembly named `Creole.Tests`, which Creole's `InternalsVisibleTo` admits, so it can lower parsers with `PlanBuilder` and run the real plans.
- **Terms.** Terms use character classes (as `CharSet`), `String` literals, `Seq`, `Or`, `Optional`, `Many`, `AtLeastOnce` (`Many1` below), `Atomic` and a top-level `Longest`. Each term is built both as a Creole parser and as the prototype's own term.
- **Oracle.** The interpreter, lowered with `LoweringOptions.All` and a dispatch threshold of 1. Each case compares success and length, or failure and whether it consumed. For `Longest` it also compares which alternative won.
- **Automaton.** A priority-ordered list of threads per alternative. Each thread holds a continuation and a scope, which records the unresolved choices it is inside (High or Low branch) and the open `Atomic`s. The list is determinized lazily into states, after canonical renaming. Classes are split by a boundary sweep over the `CharSet` ranges, and one representative character stands for each class.
- **Random experiments.**
  - The three-letter alphabet a, b, c, with every input up to length 6 or 7 plus 40 random inputs up to length 14.
  - Standalone terms of depth 4 (some runs depth 5), and `Longest` over 2 or 3 alternatives of depth 3.
  - With and without explicit `Atomic`, over seeds 1 to 6.
  - For finding 19, only terms with a choice inside an explicit `Atomic`: depth 4 (`Longest` depth 3) over seeds 2 to 4, and depth 5 (`Longest` depth 4) over seeds 5 to 8.
- **Speed.** BenchmarkDotNet 0.16.0-preview.2, in process, one job, three separate runs on mains power. The machine is an i9-13900H on .NET 11 RC 2, with `intel_pstate` active and both its energy-performance preference and the platform profile set to `performance`. Each run first checks that the three tokenizers produce the same tokens.
- **Realistic inputs.**
  - 13 token shapes, each encoded over a, b, c.
  - A JSON lexer of 12 rules over the 100 KB benchmark document (`JsonInputs.Medium`).
  - A C#-like lexer of 138 rules (all 77 C# keywords, 54 operators, identifiers, numbers, strings, characters and comments) over the first 120,000 characters of `src/Creole`.

## Findings

### The commit rule (prototype of findings 26–28)

1. **The eager rule is exact on the fragment where no explicit `Atomic` contains a choice.**
   - It had 0 mismatches against the interpreter on all of these:
     - 20,000 random terms without explicit `Atomic`, about 22.6 million cases;
     - 3,000 depth-5 terms;
     - 10,000 terms whose `Atomic`s contain no choice;
     - all 13 realistic token shapes;
     - both lexers.
   - It needs one register per `Longest` alternative.
   - The largest random automaton had 79 states, with medians of 6 for standalone terms and 12 for `Longest`.
2. **The eager rule can be wrong when a choice sits inside an explicit `Atomic`.** Over two seeds of 5,000 such terms there were 5,288 and 2,778 mismatching cases. Finding 19 says which of these choices are safe. A thread on a low branch can settle an outer choice while a higher thread inside the same `Atomic` is still running. If that higher thread later fails after consuming, the `Atomic` absorbs the failure, and the settlement was wrong. `String` literals are atomic too, but they cannot cause this, because nothing inside a literal can fail after consuming.
3. **Deferral is exact, but on its own it can grow without bound.** In the deferred form, a provisional thread only marks its events. They take effect once the higher branches it waits on have failed without consuming. A match also prunes nothing below it until it is certain. This form had 0 mismatches on 40,000 random terms, arbitrary `Atomic` included. It holds several provisional matches at once, so the automaton needs a register per match, like a tagged DFA: up to 132 were live at once. And 4 of 10,000 standalone `Atomic` terms grew without bound, so no finite DFA exists for them under this construction. All four had the shape `Many(Or(Atomic(<unbounded scan>), <branch that consumes>))`.
4. **The regex reading disagrees with the interpreter often.** Maximal munch over the term's language disagreed on 10–14% of cases, and matched on every case for only 35–54% of terms. Among realistic tokens it disagreed on 3 of 13, all Parsec idioms:
   - A decimal written `Seq(Digits, Optional(Seq('.', Digits)))` fails on `1.` followed by anything but a digit; maximal munch returns `1`.
   - A decimal with an exponent fails the same way.
   - A block comment written without `Atomic` fails on `/**/`, because `*` followed by `/` fails the loop's item after consuming.

   The interpreter is the semantics, so these are grammar bugs that a regex reading would hide.

### Lexer-sized automata (prototype 1)

5. **Lexer automata stay small.**

   | Lexer | Rules | Classes | States (raw) | States (minimized) | Registers live at most | Tokens checked | Agreement |
   | --- | --- | --- | --- | --- | --- | --- | --- |
   | JSON | 12 | 30 | 41 | 40 | 1 | 25,484 | every token |
   | C#-like | 138 | 65 | 517 | 517 | 4 | 14,595 | every token |

   The deferred form gives the same state counts on both lexers, with up to 2 and 4 registers. Exploring the C# automaton took 1.7–2.6 s in the untuned prototype, which builds string keys and uses LINQ throughout. Inference: a dense table of 517 states and 66 symbols is about 136 KB with `int` entries, or 68 KB with `ushort`. That is in the range of flex-generated C lexers (*unverified*: from memory).
6. **The harness had one bug, and the comparison caught it.** The first C# run found 19 mismatches. The cause was the harness: the class containing `⊤` was represented by U+0000, the prototype's end-of-input sentinel. After the fix, every token agreed.

### Backup and memoization (prototype 5)

7. **The backup analysis flags what the literature predicts.** The analysis looks for a reachable cycle of live states that hold a match and set no register. Otherwise the longest such path, plus one character, bounds how far a scan reads past its result.

   | Token set | Result |
   | --- | --- |
   | `a` against `a*b` | unbounded |
   | keyword against identifier | bounded, 1 character |
   | `<` `<=` `<<=` | bounded, 1 character |
   | `/` against block comment | unbounded |
   | JSON lexer | bounded, 3 characters |
   | C#-like lexer | unbounded |

   Li, Yang and Mamouras report max-TND 3 for their JSON grammar and ∞ for C (earlier note, finding 19). Inference: CRL0106 as a Warning would fire on any C-family lexer.
8. **Reps's memoization carries over with one change.**
   - **The change.** Under commit semantics a held match can still be pruned later. So the memo maps (state, position) to the state where the scan stopped, not to a yes-or-no failure flag. The result is then the stop state's held matches with the current registers.
   - **Where it bites.** On `a^n` with tokens `a` and `a*b`, the transitions taken fell from 501,500 to 2,000 at n = 1,000, and from 128,024,000 to 32,000 at n = 16,000. On 50 unterminated `/* ` comments followed by 4,000 characters, they fell from 50.14 to 2.05 per character.
   - **Where it does not.** On real JSON and C# it changed nothing: 1.10 and 1.09 transitions per character, with or without it.
   - **Correctness.** Every run produced the same tokens with and without memoization.

### Blow-ups and the sure-`Atomic` rule (prototype 6)

9. **A "sure `Atomic`" rule removes the observed blow-ups.**
   - **The rule.** An `Atomic` whose body never fails after consuming must succeed once its body has consumed. So when the consuming path is certain inside the `Atomic`, the commit outward fires at once, not at the `Atomic`'s end. The fire is blocked only by entries outside the `Atomic`.
   - **The four observed blow-ups** became 6–18 states, and agreed on every input up to length 9.
   - **Random terms.** 20,000 depth-4 `Atomic` terms (seeds 2 and 3) and 6,000 depth-5 terms (seeds 5 and 6) had 0 mismatches. At depth 4 there were no blow-ups and at most 7 registers live at once.
10. **The first version of that rule was unsound, and the stress test found it.**
    - **What the first version did.** It fired as soon as any thread inside the `Atomic` consumed.
    - **The counterexample.** At depth 5, `Many1(Seq(Atomic(Or(Many("c"), a)), Atomic(Seq(Seq(a, [ac]), Atomic([ab])))))` on `aaaa`: the interpreter returns `Ok(3)`; the automaton returned a committed failure.
    - **Why.** Inside the first `Atomic`, the high branch `Many("c")` succeeds without consuming, but only provisionally, while the low branch `a` consumes.
    - **The fix.** Require the consuming path to be certain inside the `Atomic`.
11. **One class still grows: an `Atomic` body that can fail after consuming, inside a repetition with an overlapping alternative.** Two terms show it:
    - `Many1(Or(Atomic(Seq(Many1(b), c)), "b"))`, constructed by hand;
    - `Many1(Or(Atomic(Seq(Many1(c), Or([bc], [ac]))), [bc]))`, which turned up in about 1 in 3,000 random depth-5 terms.

    Inference: the interpreter's behaviour on these is still finite-state (earlier note, finding 3). The construction keeps redundant provisional threads that a cleverer merge might drop. A state cap with fallback to the interpreter, as RE2 and .NET do (earlier note, finding 13), covers them meanwhile.

### Speed (prototype 2)

12. **The DFA tables tokenize 6.0× faster than the interpreter's `Longest` on JSON, and 21.9× faster on the C#-like lexer.** The table gives the median mean time of three runs, tokenizing the whole text (see Method). None of the three tokenizers allocates.

    | Lexer | Text | Interpreter, `Longest` | Interpreter, `OneOf` with dispatch | DFA tables |
    | --- | --- | --- | --- | --- |
    | JSON, 12 rules | 100 KB document | 2,184.9 µs | 936.1 µs | 359.9 µs |
    | C#-like, 138 rules | 120,000 characters | 7,622.2 µs | not run | 341.9 µs |

    - **Ratios.** On JSON the DFA is 6.0× faster than `Longest` (5.9–6.1× across runs) and 2.6× faster than `OneOf` (2.5–2.6×). On C# it is 21.9× faster than `Longest` (20.7–22.8×).
    - **Spread.** The DFA on C# varied by 9% between runs; every other row varied by 4% or less.
    - **Why `OneOf` is JSON only.** Ordered choice cannot reproduce maximal munch for keywords against identifiers. Put the identifier first and keywords never match; put keywords first and `iffy` lexes as `if` then `fy`. The JSON rules do not overlap, so there `OneOf` gives the same tokens.
    - **Why C#'s ratio is larger.** The interpreter's `Longest` tries every one of the 138 rules at every token, nested 16 to a level as the docs advise. The DFA's cost does not grow with the rule count.
    - **What is not measured.** The prototype's loop returns only the winning rule's index. It produces no values, reports no errors and makes no fallback check. Inference: a product DFA will do more work per token, so these ratios are an upper bound.
    - **Earlier figures.** These runs replace a preliminary single run, which measured 2,150.5, 906.7 and 372.6 µs on JSON. An earlier draft of this finding misquoted that run.

### Why the eager rule is exact (prototype 3, proof sketch)

13. **Setting.**
    - Terms are built from character classes, `Seq`, `Or`, `Optional`, `Many` and `Many1` (items not nullable), and `Atomic`. A `String` literal is treated as `Atomic` over a sequence of characters.
    - Fragment E: no `Atomic` body contains a choice (`Or`, `Optional` or a repetition). Every atomic body is therefore a fixed sequence of character classes, of length at most L.
    - The interpreter is Parsec's depth-first search over paths in priority order. It moves past a choice's high branch only if that branch fails without consuming.
14. **Lemma 1 (commits are safe).** Suppose a live thread T commits choice c: it consumes outside every atomic body inside c's high branch, or it finishes that branch. Then, for every continuation of the input, the search never takes a low path of c.
    - **Case 1:** the search's path through c is T's own. Then c's high branch has consumed for good or has finished, so c takes it.
    - **Case 2:** the search takes a higher path H. H and T diverge at a choice d inside c's high branch, and d is unresolved because T is live.
      - Everything H has consumed since d's fork lies inside an atomic body, or H would have committed d and pruned T.
      - In E that body contains no choice.
      - If the body completes, H commits d and then c. No `Atomic` encloses d, since an atomic body cannot contain a choice.
      - If the body fails, it fails without consuming relative to d. The search then moves to a lower branch of d, which is still inside c's high branch.
    - Either way, c's low paths are never tried.
15. **Lemma 2 (a match is safe).** If T reaches the end of the term, no path lower than T can be the result.
    - **Setup.** A lower path is tried only if T's path is abandoned, which needs a higher path H to be chosen at their divergence d.
    - **If H fails after consuming,** the failure propagates to the top of the term, or to its `Longest` alternative, and nothing lower is tried. No `Atomic` encloses d, so nothing absorbs it.
    - **If H fails without consuming,** T's path is next, and it succeeds.
16. **The theorem.**
    - **Exactness.** By induction on the number of characters read, using the two lemmas, the live threads are exactly the paths the search may still take, in its order. So the lowest remaining entry when the scan stops is the interpreter's result.
    - **Finiteness.** A choice stays unresolved only while every live thread on its high branch is inside an atomic body begun after the fork. So every unresolved choice instance was created within the last L characters, and each step creates boundedly many. Continuations are bounded because the term is not recursive. Hence scopes, threads and states are finite up to renaming. Each alternative holds at most one match, so one register per alternative suffices.
    - **Limits.** This is a sketch, not a mechanized proof. Findings 1 and 2 are its empirical support. It also covers less than finding 1 shows: the eager rule also held on idiomatic tokens whose `Atomic` contains a choice, such as `Optional(Atomic(Seq('.', Digits)))`. Inference: the harm in Lemma 1 needs the rest of the `Atomic`, after the choice's high branch consumes, to be able to fail, and that is a sharper condition than "no choice inside `Atomic`".

### Option B's condition (prototype 4)

17. **The condition.**
    - **Definitions.** `J(t, K)` means `Seq(t, K)` agrees with maximal munch over its regex language, given that the continuation K does. K is summarized by FIRST(K) and whether it is nullable. A term is safe standalone when `J(t, end)` holds. The other terms are:
      - FIRST(p): the characters that can start a match of p;
      - EXT(p): the characters that can extend a match of p into a longer one;
      - N(p): p is nullable;
      - "p never fails after consuming": computed by structural rules over commit semantics.
    - **The rules:**

    | Term | Sufficient condition |
    | --- | --- |
    | class, literal | always |
    | `Atomic(p)` | `J(p, K)` |
    | `Seq(p, q)` | `J(q, K)` and `J(p, q·K)` |
    | `Or(p, q)`, `Optional(p)` as `Or(p, ε)` | p safe standalone; `J(q, K)`; EXT(p) ∩ FIRST(K) = ∅; FIRST(p) ∩ FIRST(q) = ∅; if N(p), FIRST(q) = ∅; if N(q), FIRST(p) ∩ FIRST(K) = ∅ and (p never fails after consuming, or K is not nullable) |
    | `Many(p)`, `Many1(p)` | p safe standalone and not nullable; EXT(p) is disjoint from FIRST(p) and from FIRST(K); FIRST(p) ∩ FIRST(K) = ∅; p never fails after consuming, or K is not nullable |

    - **Proof sketch.** It is by induction on these rules. The key step is for `Seq`. A shorter match u of p, a proper prefix of the greedy match u*, would be followed by a character in EXT(p). That character cannot start q or K, so no other split of the input beats the greedy one. The context clause ("or K is not nullable") is what accepts a string with escapes: there, a committed failure inside the loop cannot be rescued by the closing quote.
18. **It never failed, and covers most standalone terms that agree.**
    - **Soundness.** Over 32,000 random terms (seeds 2–4), no certified term disagreed with the interpreter on any input up to length 7.
    - **Coverage.**

      | Terms | Certified, of those that agree on every input |
      | --- | --- |
      | Standalone | 82–83% |
      | `Longest` sets | 50–58% |

      Without the context clause, coverage drops slightly.
    - **Realistic tokens.** It certifies integers, the `Atomic` decimal, identifiers, operators, and strings with escapes (the last only with the context clause). It rejects the Parsec-style decimal, correctly. It also rejects the `Atomic` block comment, which does agree; that is conservative.

### A wider fragment for the eager rule (prototype 7)

19. **The eager rule had no mismatch on a wider fragment, E′, which includes both lexers.**
    - **Why it was needed.** Both lexers put a choice inside `Atomic`, so they lie outside fragment E, and finding 16 does not cover them. The JSON and C# numbers do it with `Optional(Atomic(Seq('.', Many1(Digit))))` and the exponent. The C# block comment does it with `Atomic(Seq(Many1('*'), Not('*', '/')))`.
    - **The condition.** Harm needs two things (finding 2). First, a lower thread leaves an `Atomic` and settles a choice outside it while a higher thread is still live. Second, the higher thread later fails after consuming. E′ admits a choice inside an explicit `Atomic` when one of the two is ruled out. Let h be its high branch, l its low branch, and K the rest of the term up to the end of the outermost explicit `Atomic` around it. Then either:
      - h·K never fails after consuming (finding 17's structural rules); or
      - l·K is not nullable, and FIRST(h·K) ∩ FIRST(l·K) = ∅, so at most one of the two threads survives the first character.

      FIRST here is the language FIRST, so the check is conservative. E is contained in E′.
    - **Results.**

      | Terms with a choice inside `Atomic` | Terms | In E′ | Cases in E′ | Mismatches in E′ | Outside E′, sampled terms that mismatch |
      | --- | --- | --- | --- | --- | --- |
      | Standalone, depth 4 | 60,000 | 77.4% | 52.6 million | 0 | 286 of 6,000 |
      | `Longest` of 2 or 3, depth 3 | 60,000 | 87.4% | 59.4 million | 0 | 0 of 6,000 |
      | Standalone, depth 5 | 20,000 | 68.3% | 15.5 million | 0 | 138 of 2,000 |
      | `Longest` of 2 or 3, depth 4 | 20,000 | 75.3% | 17.1 million | 0 | 49 of 2,000 |

    - **Realistic tokens.** Both lexers are in E′, as are the `Atomic` fraction, the `Atomic` exponent and the `Atomic` block comment.
    - **The first version was unsound, and depth 5 found it.**
      - **What it did.** It restarted K at each nested `Atomic`.
      - **The counterexample.** It admitted `Many(Atomic(Seq(Atomic(Many("ba")), Or(a, b))))`. On `ba` the interpreter returns `Ok(0)`, and the eager automaton returned an empty failure.
      - **Why.** The inner `Atomic`'s stop branch leaves the outer `Atomic` while the `"ba"` thread is still tentative. That thread then fails in `Or(a, b)`, and the outer `Atomic` absorbs the failure.
      - **The fix.** K runs to the end of the outermost `Atomic`. The corrected check excludes the counterexample, and keeps variants of it that agree, such as `Many(Atomic(Seq(Atomic(Many("ba")), a)))`.
    - **Limits.** There is no proof, only the tests above. E′ is conservative: about 95% of the sampled terms it rejects did agree.

## Disconfirming evidence

- **My own rule was wrong three times before testing caught it.** The eager rule fails with choices inside `Atomic` (finding 2), the first sure-`Atomic` rule was unsound (finding 10), and so was the first version of E′ (finding 19). Both proofs here are sketches, and the random tests explore depth 5 at most. That is a reason to keep differential tests against the interpreter as INT-06's acceptance criterion, not to trust the sketches alone.
- **One blow-up class remains** (finding 11). An exact automaton for every regular term does not exist under this construction; some terms need the interpreter fallback.
- **The regex reading is often wrong** (finding 4). Option B can only be used behind a condition, and that condition is conservative (finding 18).
- **CRL0106 would fire on C-family lexers** (finding 7), as the earlier note predicted from Li, Yang and Mamouras.
- **The speed gain is measured on lexers, not on the Phase 0 gate.** BENCH-01 measures character-level JSON and arithmetic grammars on the interpreter. Inference: such grammars use `Or` and repetition more than `Longest`, so their gain would come from compiling regular subtrees such as numbers and strings. That gain is unmeasured, and INT-05's scans already cover runs (earlier note, disconfirming evidence).
- **Not tested:** `Lookahead`, `Not`, `Cut`, nested `Longest`, values, error positions and expected sets, token-level input, and inputs beyond the three-letter alphabet in the random runs.

## Implications for Creole

- **For the INT-06 decision.** Option A splits into two tiers.
  - **A1, the eager automaton,** exact on fragment E by proof sketch (finding 16) and on E′ by test (finding 19). It has one register per alternative and plain maximal-munch execution, and E′ covers every realistic token tested and both lexers.
  - **A2, the deferred automaton with the sure-`Atomic` rule,** for terms outside E′. It needs per-match registers and a state cap with fallback to the interpreter.

  Option B is no longer needed for execution. Its condition is useful as a diagnostic, below. Option C (analysis only) gives up the gain of finding 12: 6.0× on a JSON lexer and 21.9× on a C#-like one.
- **A possible diagnostic.** "This rule's commit semantics differ from its regex reading" would flag the Parsec-style decimal and the comment that fails on `/**/` (finding 4). Two ways to decide it:
  - exactly, as a product of the commit-aware automaton and the regex DFA;
  - conservatively, with finding 17's condition.

  It is captured as the idea [Commit versus regex reading diagnostic](../../ideas/commit-versus-regex-reading-diagnostic.md).
- **The definition of Regular** in [§ Properties](../../../whitepaper.md#properties) should say what the construction needs: no `Rec`, `Bind`, state, `Lookahead`, `Not` or `Cut`, plus the size cap. It no longer needs to exclude `Or` alternatives that fail after consuming, or `Atomic`.
- **ANA-07** can use the analysis of finding 7 for CRL0106, and the stop-state memoization of finding 8 for linear tokenization. The severity question from the earlier note stands, because unbounded backup is common and usually harmless.
- **ANA-04's** exact CRL0101 and CRL0103 can be products of these automata.
- **The tables** are small at lexer size (finding 5), which supports GEN-05 emitting them.
- **Values and errors** are still open. The prototype only finds match ends and winners.

## Open questions

1. Is there a merge rule that keeps the remaining blow-up class finite (finding 11)? Or is a state cap the right answer?
2. Can E′ (finding 19) be proved, as E is sketched in finding 16? Until it is, the eager automaton covers the idiomatic tokens by test only.
3. How should errors be reported? This depends on [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md). The earlier note's finding 31 lists the options.
4. Does `Lookahead`/`Not` support pay for its cost? The earlier note's finding 4 suggests not for a first version.
5. What do minimization and register allocation achieve on registers? The prototype numbers matches by position only and does not minimize registers.
6. How much of finding 12's gain survives once the DFA produces values, reports errors and checks for fallback? And how much does a character-level grammar gain from compiled subtrees, as opposed to a lexer?

## Sources

Measurements are first-hand, from this session. The prototype code and its raw output are not in the repository; the numbers above are copied from that output. Earlier sources are cited through the [earlier note](2026-10-23-dfa-compilation-under-commit-semantics.md) by its finding numbers.

1. Creole interpreter at commit 751719e: `src/Creole/Plans/` (`PlanBuilder`, `OptionalPlan`, `RepeatPlan`, `LongestPlan`, `AtomicPlan`, `StringPlan`), `Analysis/CharSet.cs` — read and executed as the oracle
2. `benchmarks/Creole.Benchmarks/Inputs/JsonInputs.cs` — the 100 KB JSON document, compiled into the prototype
3. Earlier note: [How can regular subtrees compile to DFAs that agree with commit semantics?](2026-10-23-dfa-compilation-under-commit-semantics.md) — findings 3, 13, 19 and 26–31
