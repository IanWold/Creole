# How can regular subtrees compile to DFAs that agree with commit semantics?

| | |
| --- | --- |
| Date | 2026-10-23 |
| Cards | [INT-06](../../complete/03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md), [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md), [ANA-04](../../active/09-static-analysis/ANA-04-choice-checks.md), [ANA-07](../../active/09-static-analysis/ANA-07-linear-maximal-munch.md), [GEN-05](../../active/11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md) |
| Whitepaper | [§ Longest match](../../../whitepaper.md#longest-match), [§ Interpreted engine](../../../whitepaper.md#interpreted-engine), [§ Properties](../../../whitepaper.md#properties), [§ Deterministic mode](../../../whitepaper.md#deterministic-mode), [§ Equivalence](../../../whitepaper.md#equivalence), [§ The error model](../../../whitepaper.md#the-error-model) |
| Confidence | Medium. Facts about papers and libraries were read in source, and the passages this note leans on were spot-checked first-hand. The proposed construction is our inference: no source does it, and nothing proves it. A prototype is the missing evidence. |

## Short answer

No published construction compiles terms with Parsec-style commit semantics to a finite automaton, and none tells failure after consuming from failure without consuming. The target is finite-state: non-recursive PEGs, regexes with lookahead and regexes with atomic groups all denote regular languages [1][5][7]. Those results are about languages, though, not about the consumed length and the four outcomes Creole needs. The closest executable model is the leftmost-first (priority) DFA used by RE2, Rust's regex-automata and .NET's NonBacktracking engine [3][13][15]. By our inference, one added rule makes it exact for Creole: a thread that commits drops the lower-priority branches of the choices it is in. That is unproven. PetitParser's compiler already finds regular sub-parsers automatically and runs them on a finite automaton [48], so the idea is not new; exact agreement with commit semantics, `Longest`, and error reporting are what remain open. The backup literature is settled (Reps [20]; Li, Yang and Mamouras [21]), but it shows that the C, R and SQL token sets have unbounded backup, so CRL0106 as a Warning would fire on most C-family lexers.

## Context

INT-06 compiles regular character subtrees and `Longest` over regular alternatives to DFA tables, and provides the product construction and backup analysis that ANA-04, ANA-07 and GEN-05 build on. At `/start` it went to Research because what the DFA must compute was not settled:

- [§ Properties](../../../whitepaper.md#properties) defines Regular as "contains no `Rec`, `Bind` or state". The card's Scope adds that an `Or` whose alternative can fail after consuming, `Atomic`, `Lookahead`, `Not` and `Cut` must be excluded or modelled, but says `Longest` "is unaffected, because each alternative runs atomically". The whitepaper outranks the card, so the conflict needs a decision.
- [§ Equivalence](../../../whitepaper.md#equivalence) requires both engines to agree on the value, the consumed position, every error's span and every expected set. A DFA that replaces an interpreted subtree is held to the same rule, so the DFA must report errors as the interpreter does. [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md), which defines furthest-failure tracking, is not built yet.

This note feeds the decision record that INT-06's Design stage needs: the definition of Regular, what the automaton computes, and how it reports errors and values.

## Findings

### What the automaton must compute

Read from the interpreter (`src/Creole/Plans/`, commit 751719e), not from a source:

1. `OptionalPlan` fails when its item fails after consuming ([OptionalPlan.cs](../../../src/Creole/Plans/OptionalPlan.cs)). `RepeatPlan` fails the whole repetition when an item fails after consuming, and never gives back an item it took ([RepeatPlan.cs](../../../src/Creole/Plans/RepeatPlan.cs)). `LongestPlan` runs each alternative from the same start, keeps the one that consumed most, and ties go to the earliest ([LongestPlan.cs](../../../src/Creole/Plans/LongestPlan.cs)).
2. The interpreter therefore disagrees with both standard regex semantics on ordinary terms:

   | Term | Input | Interpreter | Leftmost-longest DFA (maximal munch) | Leftmost-first (RE2, Perl) |
   | --- | --- | --- | --- | --- |
   | `Seq(Digits, Optional(Seq(Char('.'), Digits)))` | `1.x` | Fails committed; inside `Longest`, the alternative does not match | `1` | `1` |
   | `Seq(Or(Char('a'), String("ab")), Char('c'))` | `abc` | Fails committed | `abc` | `abc` |
   | `Or(Seq(Char('a'), Char('b')), Seq(Char('a'), Char('c')))` | `ac` | Fails committed | `ac` | `ac` |
   | `Or(String("<="), Char('<'))` | `<x` | `<` (the literal fails without consuming) | `<` | `<` |

   Running an alternative atomically inside `Longest` turns its committed failure into an empty one. It does not make the alternative match what its regex reading matches, so the card's "`Longest` … is unaffected" does not hold in general.

### Constructions (question 1)

3. **Non-recursive PEGs are regular.** Chida and Kuramitsu define linear PEGs, which keep "the full set of prioritized choice, unlimited lookahead, and greedy repetition" and exclude only some recursion. They prove that LPEGs are equivalent to DFAs (Theorem 24). The route is to rewrite `e1 / e2` as `e1 | !e1 e2` (Theorem 14), build a boolean finite automaton, and then determinize [1]. Theorem 20 states `L(G) = L(D)`: the result concerns the language, not the consumed length. They note that "the conversion judgement of parsing expressions into DFAs is undecidable in general" [1]. Inference: a non-recursive term is trivially an LPEG. Commit semantics can be written as PEG semantics plus predicates (`Or(p, q)` as `p / (&E(p) q)`, where `E(p)` means "p fails without consuming"), so each of Creole's outcome sets is regular. That does not give an automaton that computes the consumed length.
4. **Lookahead and atomic groups are regular, but at a cost.** Berglund, van der Merwe and van Litsenborgh build boolean automata for regexes with lookahead. They report a minimal DFA of at most D(|r|+2) states, where D is the Dedekind number, which is doubly exponential, and they cite a worst-case lower bound of 2^(2^Ω(√n)) [5]. Their semantics is set-based, and greedy disambiguation is left as future work [5]. Sugiyama and Minamide translate atomic groups into automata with lookahead, which shows they are regular [7] (abstract only). A single "cut" operator can force exponentially large NFAs [6].
5. **Priority is solved for leftmost-first semantics.** Moseley et al. keep alternatives ordered inside derivatives and prune them once an earlier one is nullable. The authors say the rewrite system keeps the derivatives finite, and they prove matching linear in the input [3]. This is the semantics of .NET's NonBacktracking engine. RE2 and regex-automata implement the same `LeftmostFirst` semantics with subset construction [13][15]. Inference: this is backtracking-regex semantics, so it returns `1` on `1.x` (finding 2).
6. **No published construction models commit.** The constructions in [1][2][3][4][5][6] all have two outcomes, match or fail. The researcher found no paper that compiles Parsec's consumed and empty failures to automata. Moss's PEG derivatives are a recognizer with an O(n⁴) worst case and make no claim of finiteness [2].
7. **Lookarounds and atomic groups are excluded in practice.** In .NET 7 to 11, NonBacktracking rejects atomic groups and lookarounds [8][9]. Moseley et al. say unrestricted lookarounds would lose their linear-time result [3]. RE2 marks `(?>re)`, possessive quantifiers and lookarounds "NOT SUPPORTED" [14].

### Outcomes, errors and values (question 2)

8. **DFA lexers do not compute expected sets.** ANTLR 4's lexer reports "token recognition error at" the token's start, with no expected set [19]. flex aborts or echoes [23]. re2c and Ragel hand failures to user actions [24][25]. Lezer and tree-sitter produce error nodes [26][27]. Ragel's local error actions, which "are stored in the states", are the closest precedent for annotating states with error information [25].
9. **Expected sets read from a merged automaton go wrong.** Pottier shows that listing valid futures "based on the current state alone" is impossible in some LR(1) cases [29]. Bison says that under default reductions and state merging, "the expected token list … can both contain invalid tokens and omit valid tokens", and adds an exploratory parse (LAC) to fix this [30]. CPython's old pgen reports an expected token only when the state has exactly one outgoing label [31].
10. **Values are usually found in a second pass.** RE2 finds the match bounds with the DFA and then runs an NFA engine for submatches [13]. regex-automata runs a slower engine "only when a match is found" [15]. .NET NonBacktracking finds the end, runs in reverse for the start, and runs a third phase over the match only when captures are needed [9]. Tagged DFAs record positions during the scan; re2c's authors report that TDFA(1) and an ordinary DFA "are close in size and speed" [36].
11. **Re-running is how combinator libraries get precise errors.** PariPari runs a fast acceptor and, only if it fails, a reporter over the same input. FastParse's `.trace()` "performs a second parse". Both work on the whole parse, not on a sub-parser [34][35]. Scala parsley compiles a choice into a jump table keyed on each alternative's leading token and precomputes every alternative's expected items [32]. Haskell parsley, the ICFP 2020 library, says "Currently there are no error messages" [33].

### Alphabet and tables (question 3)

12. **Every engine compresses the alphabet first.** .NET NonBacktracking partitions character sets into minterms with BDDs and maps a char to a minterm through a `byte[]` of length `maxChar + 1`. An `int[]` is used only above 255 minterms, which the source calls "incredibly rare" [9]. RE2 and regex-automata use byte classes; JFlex uses a two-level table with 256-entry blocks shared when equal; ICU uses a `UCPTrie` with two loads for the BMP [14][15][17][18]. The table's width depends on how many distinct classes the grammar uses, not on how many ranges a class has.
13. **The shared layout is a dense `int[]` with a power-of-two stride.** .NET indexes `(stateId << mintermsLog) | mintermId`, starts with room for 1,024 states, doubles it as needed, and fills transitions lazily. It switches to NFA mode at `NfaNodeCountThreshold = 125_000` [9]. regex-automata premultiplies state ids. Its docs say sparse tables are three to five times slower, and that minimization costs "around an order of magnitude more time" than determinization [15]. Lazy DFAs in RE2 and regex-automata fall back to an NFA when their cache churns [13][15].
14. **Author measurements.** The .NET PR that replaced a BDD lookup with a 64 KB array reports about 3× on Unicode input [10]. Rust's dense DFA for Unicode `\w{50}` takes about 1 s and 15 MB to build, against under 1 ms for ASCII. That blowup comes from UTF-8 byte automata, which a UTF-16 class map avoids [15][16]. ANTLR keeps every char above 127 out of its DFA cache [19].
15. **Microsoft ships no C# DFA emitter.** `RegexOptions.Compiled` and `[GeneratedRegex]` do not support NonBacktracking [9][12]. GEN-05 has no in-ecosystem precedent. Logos (Rust) compiles token sets to DFA code at build time [49].
16. **Local evidence.** INT-05's progress notes measured the item-by-item interpreter at about 7 to 10 ns a character, much of it `CharSet.Contains`'s binary search behind a delegate. A class map would turn that search into one or two array loads.

### Backup (question 4)

17. **Reps's algorithm and its decision procedure.** Reps memoizes `failed_previously[q, i]` for failed (state, position) pairs, which gives linear time with a table of |Q| × (n+1) bits [20]. His footnote uses the whitepaper's example, "token classes a and a\*b". § 3 computes `Tab`, the states that need memoizing, which "allows a non-tabulating scanner to be substituted when Tab = ∅" [20]. Reps also writes that quadratic behaviour "is almost certainly not a problem in practice" [20].
18. **Backup can be bounded exactly.** Li, Yang and Mamouras define the maximum token neighbor distance (max-TND). It is either ∞ or at most m+1 for a minimal DFA of m states. A finite value k bounds backup to k and makes the standard algorithm O(k·n). It is computable in O(M²) over a DFA of M states. In the size of the grammar, deciding max-TND ≤ 1 is PSPACE-hard, and max-TND is computable in polynomial space [21].
19. **Real token sets often have unbounded backup.** Measured by those authors, max-TND is 3 for JSON, 1 for CSV, 2 for TSV and 6 for XML, but ∞ for C, R and SQL. In their sample of 2,669 GitHub grammars, 32% are unbounded [21]. flex's `-b` lists every state that backs up, bounded or not, and treats it as a constant-factor cost [23].
20. **Other lexers neither warn nor memoize.** re2c rolls back with `YYMARKER`. Ragel's `|* *|` scanners backtrack. Lezer and tree-sitter keep the last accept [24][25][26][27]. ZipLex, a verified lexer, uses Reps-style memoization [28].

### Prior art in libraries (question 5)

21. **Parlot 2.0.0 has no DFA and no longest match.** Its docs say, of keyword lookups, "Choices are ordered, not longest-match" [37]. `OneOf` builds a first-character map, and the generator emits a `switch`. Literal-only choices get a generated decision tree that the docs say "is not a DFA". `AnyOf` and `NoneOf` scan with `SearchValues`. `Sequence` resets the position on failure, so Parlot's semantics are PEG-like, with no commit [37].
22. **Parlot 2.0.0 is stable, and its generator is too.** The v2.0.0 GitHub release (2026-10-20, not a prerelease) removes `Compile()`: "Compiled parsers are removed". Grammars for generation now live in `.parlot.cs` files, and the release adds parsing from `TextReader` with `Commit()` cuts [37]. NuGet lists 2.0.0 as the latest version of both `Parlot` and `Parlot.SourceGenerator` [38].
23. **Yoakke is a conventional regex-to-DFA lexer generator.** It uses Thompson's construction, determinizes and minimizes. When several tokens accept, the first declared wins, and it backs up to the last accept. Its regex dialect is standard [39].
24. **The other libraries compile nothing automaton-like.** Superpower tries recognizers in order, and `Span.Regex` wraps .NET `Regex` [40]. chumsky's `regex` feature runs a user-written pattern through regex-automata [41]. Pidgin, FParsec and winnow have nothing of this kind [42][43][45]. Longest match, where offered, covers literal sets only: FastParse's `StringIn` trie, lexy's `literal_set` and parsley's `strings` [32][35][44]. flap's lexer is a separate list of regexes with longest-match semantics, built with derivatives [46]. Hammer's regular backend changes semantics from its packrat backend [47].
25. **PetitParser already finds regular sub-parsers automatically.** Kurš et al.: "During a dedicated optimization phase all expressions are analyzed, and those recognized as regular expressions are marked :regular. All regular expressions are wrapped with a <Scanner> combinator", and the scanner "uses a FSA". They concede that, because PEGs "have different semantics than regular expressions this is not always possible". Their counterexample is `('PetitParser' / 'Petit') 'P'`. Their condition for regular parsing expressions is informal: no self-reference, and "If a parsing expression e accepts x, e also accepts any string xy". The pages read give no construction and no proof that the automaton agrees with the PEG [48].

### A commit-aware priority automaton (inference)

Everything in this subsection is our inference. No source does it.

26. **One rule separates commit semantics from leftmost-first.** In a leftmost-first automaton, a lower-priority thread survives until a higher-priority thread reaches a match [3][15]. Under commit semantics it survives only until a higher-priority thread of the same choice commits, that is, consumes input it will not give back. Lower-priority threads are the later alternatives of an `Or` and the stop branch of `Optional` and of a repetition. A character class commits at once. Inside a `String` the consumption is tentative until the literal completes. A commit propagates outward through enclosing choices up to the nearest `Atomic`.
27. **The rule gets `1.x` right.** On `.`, the thread inside `Optional` commits and drops the stop branch that had accepted after `1`. On `x` every thread dies, so nothing is left to back up to.
28. **Execution keeps the maximal-munch shape.** One register holds the last accepting position, and a commit by a higher-priority thread clears it. When every thread dies, the result is the register's position if one is held. Otherwise the result is a failure, committed if the state's commit bit is set and empty if not. `Longest` labels accepting states with their alternative, and the earliest wins a tie. Each alternative is atomic, so commits do not prune across alternatives.
29. **Extensions get harder in this order.** `Atomic` adds scoped pruning and makes a failure inside it empty for the outside. `Cut` inside `Atomic` depends on a flag that differs between paths. `Lookahead` and `Not` need a conjunction of automata, with the doubly exponential bounds of finding 4. A first version that excludes `Lookahead`, `Not` and `Cut` matches what .NET and RE2 exclude [8][14].
30. **Two things are unproven.** The first is that the states stay finite under the pruning rule. The second is that dropping a duplicate thread at the same position stays sound when the two threads come from different choice instances. A derivative representation, as in [3], keeps choice scopes explicit and may avoid the second problem.
31. **Errors need care whatever the construction.** The automaton runs lower-priority threads that the interpreter would run only if every higher-priority thread failed empty. A thread's failure is real only under that condition, so the expected set cannot simply be the union over threads that die. There are two ways to stay exact.
    - **Annotate states.** Each state carries its expected labels. Minimization must then treat those labels as output, like a Moore machine, or it merges states that report differently; that is the lesson of findings 9 and 30.
    - **Re-run lazily.** Record that a subtree failed at its start and how far it read, and re-run the interpreter on that subtree only if that failure is the one reported. This applies PariPari's and FastParse's whole-parse approach to a single subtree [34][35].

## Disconfirming evidence

- **PetitParser already does this** [48]. Automatic detection of regular sub-parsers is not new. Creole's remaining contribution is exactness under commit semantics, `Longest`, and error reporting.
- **Analysis has a cost.** Deciding whether an arbitrary PEG is regular is undecidable [1]. Lookahead bounds are doubly exponential [5], and one cut can force exponentially large NFAs [6].
- **CRL0106 would be noisy.** Most C-family token sets have unbounded backup [21], and Reps himself calls the quadratic case "almost certainly not a problem in practice" [20]. Inference: CRL0106 as a Warning would fire on most C-family lexers. Unbounded backup is also necessary for quadratic time, not sufficient.
- **A DFA may not beat simpler code.** Toub reports the backtracking engine "~128x faster" than NonBacktracking on `a.*b`, because it can vectorize `IndexOf` [11]. In Moseley et al.'s CredScan evaluation, the plain interpreter beat NonBacktracking on small repositories, and they blame the alphabet compression step for the start-up cost [3]. Creole's INT-05 scans already give 6.4× to 193× on runs, by INT-05's own measurements. Whether a DFA beats INT-05's dispatch and scans on Creole's JSON benchmark is unmeasured.
- **Parsing is moving to build time.** Parlot 2.0.0 removed its runtime compilation in favour of generation [37], which weakens any case for a runtime DFA built for speed alone. This is weak evidence, since Creole's interpreter is a reference semantics and serves grammars built at run time.
- **Searches that found nothing:**
  - Parsec-style consumed and empty failures compiled to automata;
  - PEG-to-DFA constructions that compute consumed length (2018–2025);
  - forward citations of [1];
  - "compile regular sub-parser to DFA, preserve labels";
  - a lexer DFA that derives expected sets from a dead state;
  - quadratic-lexing bug reports for flex or re2c;
  - JavaScript backup data;
  - a Parlot DFA.

## Implications for Creole

- **Whitepaper.** Through a decision:
  - [§ Properties](../../../whitepaper.md#properties) needs a definition of Regular that is tied to commit semantics.
  - [§ Longest match](../../../whitepaper.md#longest-match) and [§ Interpreted engine](../../../whitepaper.md#interpreted-engine) should say what a compiled DFA agrees with.
  - [§ Deterministic mode](../../../whitepaper.md#deterministic-mode) states CRL0106's purpose, and its severity is in § Diagnostics; both should be revisited in light of finding 19.
  - PetitParser [48] and Reps [20] belong in the References. Reps is cited in § Deterministic mode but is missing from the list, which is an editorial gap.
- **The decision for INT-06's Design stage.** The options are:
  - **A.** A commit-aware priority automaton, findings 26–30. It is exact for the regular fragment excluding `Lookahead`, `Not` and `Cut`, and it is new.
  - **B.** Textbook Thompson, subset construction and minimization, applied only where commit semantics provably equals the regex reading. Literal sets qualify, which is all that other libraries compile [32][35][37][44]. A wider condition would need a proof, and Kurš et al.'s is informal [48].
  - **C.** No compiled execution in Phase 0. INT-05's dispatch and scans carry speed, and automata serve analysis (CRL0101, CRL0103, CRL0106) only. Exact analysis still needs commit-aware automata.

  Whichever option is chosen, the decision should also settle three things. Values: re-run the winning alternative over its span, or produce `Slice`, `Text` and `Unit` values directly (finding 10). Errors: annotated states or lazy re-run (finding 31). Alphabet: classes from a sweep over `CharSet` boundaries, an ASCII fast path plus a two-level class map, a dense `int[]` with a power-of-two stride, a state cap with fallback to the plan, and `SearchValues` for self-loops (findings 12–14).
- **Ordering.** An exact DFA must reproduce ERR-02's expected sets and positions, and ERR-02 has not defined them yet: for example, where a partly matched `String` reports. INT-06 built before ERR-02 risks rework, unless errors come from re-running the interpreter.
- **Card scope.**
  - INT-06's Scope claim that `Longest` "is unaffected" should be corrected through the decision.
  - ANA-04's exact CRL0101 and CRL0103 need products of commit-aware automata, not regex automata.
  - ANA-07 can take Reps's `Tab = ∅` test or max-TND [20][21] as CRL0106's decision procedure. Both apply only once the automaton's accepting states encode commit (finding 28).
  - ANA-01 and INT-06 both list "compute the regular property"; the decision should say which card owns it.
- **Outside INT-06.** Parlot 2.0.0 is now stable, and so is `Parlot.SourceGenerator` [37][38].
  - The whitepaper's statements that Parlot's generator is "so far only in preview packages" are now stale (§ Background, § Comparison).
  - So are § Source-generated engine's description of how Parlot's generator works, and the `Compile()` figure in § Risks.
  - § Roadmap says gates compare with Parlot's interpreted parsers "until `Parlot.SourceGenerator` ships a stable release"; that condition has now been met.
  - `Directory.Packages.props` pins Parlot 1.5.12.
  - The [numeric gate thresholds note](2026-10-18-numeric-gate-thresholds.md) called the generator preview-only; that was true on its date.

  These need a `/sync` pass or a decision on the gate baseline. They are outside this card.

## Open questions

1. Does the pruning rule of finding 26 keep the states finite, and does it agree with the interpreter? A prototype would settle this better than reading. It would build the automaton for random regular terms and compare its outcome, length and commit flag with `LongestPlan` and the other plans on random inputs, as INT-06's acceptance criterion already requires. It has not been run.
2. Should `Atomic` be inside the first version's regular fragment? Users write `Optional(Atomic(Seq('.', Digits)))` to get regex-like fractions, so excluding it shrinks what compiles.
3. Does a DFA beat INT-05's dispatch and scans on Creole's JSON tokens? Nothing in BENCH-01's sense has been measured, since INT-08's `Parse` is not built. A micro-benchmark in `LoweringBenchmarks` could answer it first.
4. Which is faster in .NET: a flat class map, or a two-level one? No source measured this.
5. Do expected-label annotations make the automata much larger? No source measured this.
6. Unread: the full text of Li and Mamouras [22] (ACM returned 403); Moseley et al. [3] beyond the relevant sections; Sugiyama and Minamide [7] beyond the abstract.

## Sources

Researcher subagents read these, mostly through a summarizing fetch tool. "Spot-checked" marks passages that this note's author also read first-hand in the full text.

1. N. Chida, K. Kuramitsu. Linear Parsing Expression Grammars. LATA 2017, LNCS 10168. https://arxiv.org/abs/1707.01814 — full; abstract and Theorems 14, 20 and 24 spot-checked
2. A. Moss. Derivatives of Parsing Expression Grammars. AFL 2017, EPTCS 252. https://arxiv.org/abs/1405.4841 — relevant sections (§ 1–5)
3. D. Moseley, M. Nishio, J. Perez Rodriguez, O. Saarikivi, S. Toub, M. Veanes, T. Wan, E. Xu. Derivative Based Nonbacktracking Real-World Regex Matching with Backtracking Semantics. PLDI 2023. https://doi.org/10.1145/3591262 ([PDF](https://www.microsoft.com/en-us/research/uploads/prod/2023/04/pldi23main-p249-final.pdf)) — relevant sections (§ 1–5.5, § 8–9, pp. 12–21)
4. S. Medeiros, F. Mascarenhas, R. Ierusalimschy. From Regexes to Parsing Expression Grammars. Science of Computer Programming 93, 2014. https://arxiv.org/abs/1210.4992 — relevant sections (pp. 1–11)
5. M. Berglund, B. van der Merwe, S. van Litsenborgh. Regular Expressions with Lookahead. JUCS 27(4), 2021. https://doi.org/10.3897/jucs.66330 — full
6. M. Berglund, H. Björklund, F. Drewes, B. van der Merwe, B. Watson. Cuts in Regular Expressions. DLT 2013, LNCS 7907. https://im.saske.sk/hospodar/literature/2013_berglund_bjorklund_drewes_merwe_watson_dlt_cuts.pdf — relevant sections
7. S. Sugiyama, Y. Minamide. Translating Regular Expressions Extended with Atomic Grouping to Automata. IPSJ Transactions on Programming 6(1), 2013. https://ipsj.ixsq.nii.ac.jp/records/89448 — abstract only
8. Microsoft Learn. Regular expression options, NonBacktracking mode (updated 2026-07-08). https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-options — relevant section
9. dotnet/runtime, `main` (same `Symbolic/` head as `release/11.0`, e267ddd): `Symbolic/MintermClassifier.cs`, `MintermGenerator.cs`, `CharSetSolver.cs`, `UInt64Solver.cs`, `SymbolicRegexMatcher.cs`, `SymbolicRegexMatcher.Automata.cs`, `SymbolicRegexThresholds.cs`, `SymbolicRegexRunnerFactory.cs`, `RegexNodeConverter.cs`; `RegexNode.cs` (`SupportsCompilation`). https://github.com/dotnet/runtime/tree/main/src/libraries/System.Text.RegularExpressions/src/System/Text/RegularExpressions/Symbolic — full or relevant sections
10. dotnet/runtime PR #102655, NonBacktracking Regex optimizations, merged 2024-07-11. https://github.com/dotnet/runtime/pull/102655 — discussion
11. S. Toub. Regular Expression Improvements in .NET 7. .NET Blog, 2022. https://devblogs.microsoft.com/dotnet/regular-expression-improvements-in-dotnet-7 — first two-thirds, including the NonBacktracking section
12. Microsoft Learn. .NET regular expression source generators (updated 2026-02-23). https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-source-generators — full
13. R. Cox. Regular Expression Matching in the Wild. 2010. https://swtch.com/~rsc/regexp/regexp3.html — full
14. google/re2, `main`: `re2/prog.h`; Syntax wiki. https://github.com/google/re2 — relevant sections
15. regex-automata 0.4.18: docs for `dfa`, `dense::Config`, `hybrid`, `ByteClasses`, `MatchKind`, `meta::Regex` (https://docs.rs/regex-automata/0.4.18/regex_automata/); rust-lang/regex `master`: `regex-automata/src/dfa/dense.rs`, `hybrid/dfa.rs` — relevant sections
16. A. Gallant. Regex engine internals as a library. 2023-07-05. https://burntsushi.net/regex-internals/ — full
17. jflex-de/jflex, `master`: `CMapBlock.java`, `CharClasses.java`; JFlex 1.9.1 manual. https://github.com/jflex-de/jflex — source full, manual partial
18. unicode-org/icu, `main`: `rbbidata.h`, `ucptrie.h`. https://github.com/unicode-org/icu — relevant sections
19. antlr/antlr4, `dev`: `Lexer.java`, `LexerATNSimulator.java`, `DFAState.java`, `ATN.java`. https://github.com/antlr/antlr4 — relevant sections
20. T. Reps. "Maximal-Munch" Tokenization in Linear Time. ACM TOPLAS 20(2):259–273, 1998. https://doi.org/10.1145/276393.276394 ([preprint](https://research.cs.wisc.edu/wpis/papers/toplas98b.pdf)) — full; the footnote example, § 3's `Tab` and the closing remark on practice spot-checked
21. A. W. Li, Y. Yang, K. Mamouras. Static Analysis for Efficient Streaming Tokenization. ASPLOS 2026. https://doi.org/10.1145/3779212.3790227 ([PDF](https://kmamouras.github.io/papers/streaming-tokenization-ASPLOS-2026.pdf)) — full; Table 1 and the RQ1 results spot-checked
22. A. W. Li, K. Mamouras. Efficient Algorithms for the Uniform Tokenization Problem. PACMPL 9 (OOPSLA1), 2025. https://2025.splashcon.org/details/OOPSLA/87/Efficient-Algorithms-for-the-Uniform-Tokenization-Problem — abstract only
23. flex manual (`doc/flex.texi`, `master`). https://github.com/westes/flex — relevant sections (`-b`, performance, FAQ)
24. re2c manual (C). https://re2c.org/manual/manual_c.html — relevant sections
25. A. Thurston. Ragel State Machine Compiler User Guide 6.10. https://www.colm.net/files/ragel/ragel-guide-6.10.pdf — § 3.2.3, § 4.2.4, § 6.3
26. lezer-parser/lr, `main`: `src/token.ts`; Lezer System Guide. https://lezer.codemirror.net/docs/guide/ — relevant sections
27. tree-sitter, `master`: `lib/src/parser.h`, `parser.c`, `lexer.c`, `api.h`; grammar docs. https://github.com/tree-sitter/tree-sitter — relevant sections
28. S. Chassot, V. Kunčak. Formally Verified Linear-Time Invertible Lexing. arXiv:2510.18479v2, 2026. https://arxiv.org/abs/2510.18479 — relevant sections
29. F. Pottier. Reachability and Error Diagnosis in LR(1) Parsers. CC 2016. https://cambium.inria.fr/~fpottier/publis/fpottier-reachability-cc2016.pdf — pp. 1–4, 7–9
30. GNU Bison manual, LAC. https://www.gnu.org/software/bison/manual/html_node/LAC.html — full section
31. CPython `Parser/parser.c` at 83ac3305 (pgen era). https://chromium.googlesource.com/external/github.com/python/cpython — relevant section
32. j-mie6/parsley (Scala), `master` de25f0e: `OptInstrs.scala`, `AlternativeEmbedding.scala`, `character.scala`. https://github.com/j-mie6/parsley — relevant sections
33. parsley 2.0.0.1 (Haskell), Hackage description. https://hackage.haskell.org/package/parsley — description
34. minad/paripari, `master`: `README.md`, `src/Text/PariPari/Internal/Run.hs`; Hackage 0.7.0.0. https://github.com/minad/paripari — relevant sections
35. FastParse 3.1.1 docs (https://com-lihaoyi.github.io/fastparse/); `MacroImpls.scala` at 475d042 — relevant sections
36. A. Borsotti, U. Trofimovich. A closer look at TDFA. arXiv:2206.01398. https://arxiv.org/abs/2206.01398 — § 2–5 skimmed
37. sebastienros/parlot, v2.0.0 (2a34f3d): `src/Parlot/Fluent/OneOf.cs`, `Sequence.cs`, `ZeroOrMany.cs`, `SourceGeneration/KeywordChoiceSource.cs`, `TextChoiceSource.cs`, `KnownStringLookup.cs`, `docs/keyword-lookups.md`, `docs/source-generation.md`, `docs/parsers.md`; v2.0.0 release notes. https://github.com/sebastienros/parlot/releases/tag/v2.0.0 — full; `keyword-lookups.md` lines 26–27 and the release notes spot-checked
38. NuGet flat-container version indexes for `Parlot` and `Parlot.SourceGenerator`. https://api.nuget.org/v3-flatcontainer/parlot/index.json, https://api.nuget.org/v3-flatcontainer/parlot.sourcegenerator/index.json — read first-hand on 2026-10-23
39. LanguageDev/Yoakke, `master` de15ee8: `Lexer.Generator/LexerSourceGenerator.cs`, `RegExParser.cs`, `Templates/lexer.sbncs`, `Automata/RegExAst/RegExOrNode.cs`. https://github.com/LanguageDev/Yoakke — full
40. datalust/superpower, `dev` bac1b4b: `Tokenizers/TokenizerBuilder.cs`, `Parsers/Span.cs`. https://github.com/datalust/superpower — full
41. zesterer/chumsky, `main` 4879268: `src/regex.rs`, `src/primitive.rs`; chumsky 0.13.0 docs. https://github.com/zesterer/chumsky — full or relevant sections
42. benjamin-hodgson/Pidgin, `main` 103dfc6: `Pidgin/Parser.OneOf.cs`. https://github.com/benjamin-hodgson/Pidgin — full
43. FParsec CharParsers reference. https://www.quanttec.com/fparsec/reference/charparsers.html — relevant sections
44. lexy reference: `literal_set`, `dsl::token`. https://lexy.foonathan.net/reference/ — relevant sections
45. winnow 1.0.4, `dispatch!`. https://docs.rs/winnow/1.0.4/winnow/macro.dispatch.html — full
46. J. Yallop, N. Xie, N. Krishnaswami. flap: A Deterministic Parser with Fused Lexing. PLDI 2023. https://arxiv.org/abs/2304.05276 (whitepaper reference 11) — pp. 1–7, 13–16, 20–23
47. UpstandingHackers/hammer, `master` cc733ff: `src/backends/regex.c`, `src/hammer.h`, `src/parsers/choice.c`. https://github.com/UpstandingHackers/hammer — relevant sections
48. J. Kurš, J. Vraný, M. Ghafari, M. Lungu, O. Nierstrasz. Efficient Parsing with Parser Combinators. Science of Computer Programming, 2018. https://scg.unibe.ch/assets/archive/papers/Kurs17a.pdf — pp. 1–4, 10–18, 36–47; § 4.1 and Appendix A.1 spot-checked
49. Logos 0.16.1 docs. https://docs.rs/logos/0.16.1/logos/ — front page

*Unverified*, not opened in this session: S. Owens, J. Reppy, A. Turon, "Regular-expression derivatives re-examined", JFP 19(2), 2009, seen only as cited by [3]; A. Morihata on regex with lookahead to automata (2012, in Japanese), cited by [1] and [5]; V. Laurikari, "NFAs with Tagged Transitions", SPIRE 2000; M. Berglund, B. van der Merwe, B. Watson, N. Weideman, "On the Semantics of Atomic Subgroups in Practical Regular Expressions", CIAA 2017; A. Frisch, L. Cardelli, "Greedy Regular Expression Matching", ICALP 2004, search snippet only; N. B. B. Grathwohl, F. Henglein, L. Nielsen, U. T. Rasmussen, "Two-Pass Greedy Regular Expression Parsing", CIAA 2013, search snippet only; J. Willis, N. Wu, M. Pickering, "Staged Selective Parser Combinators", ICFP 2020 (whitepaper reference 8), which the publisher refused to serve.
