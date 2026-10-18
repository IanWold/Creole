# What numbers should the roadmap gates use?

| | |
| --- | --- |
| Date | 2026-10-18 |
| Cards | [FND-06](../../active/01-foundations/FND-06-fix-numeric-gate-thresholds.md) |
| Whitepaper | [§ Roadmap](../../../whitepaper.md#roadmap), [§ Performance design and benchmark plan](../../../whitepaper.md#performance-design-and-benchmark-plan), [§ Risks and open questions](../../../whitepaper.md#risks-and-open-questions) |
| Confidence | Medium. The noise and allocation measurements are first-hand and repeatable, and the sources were read or spot-checked. The proposed thresholds are inference: no parser library publishes a numeric competitor gate to copy. |

## Short answer

Time measured on the maintainer's laptop varies by 5–10% between identical runs, and by up to 30% on single benchmarks, including the Pidgin baseline. A single run cannot support a time gate tighter than about 25% per benchmark; a gate needs repeated runs and a summary across benchmarks. Allocations are deterministic and can be gated exactly. "Near Pidgin" is a low bar: Parlot, by its own measurements and ours, is 5–7× faster than Pidgin on JSON and 4–11× on arithmetic, depending on the Pidgin configuration. A Phase 0 gate set only against Pidgin would not test the "match Parlot" aim in § Risks. Parlot's source generator is still preview-only and should not be a gate baseline yet, but Parlot 2.0 removes `Compile()`, so the "Parlot compiled" baseline has no future. Every gate can be made numeric. The proposals below are a starting point for `/decide`.

## Context

FND-06 must give every gate in § Roadmap a measurable threshold: Phase 0 speed, Phase 1 bounded memory, Phase 2 analysis, Phase 3 engine agreement, and the 1.x gates for incremental re-parsing, fusion and resumable async. The cards that apply the gates are BENCH-01, LAY-07, RSLN-07, GEN-08, INC-07, FUS-01 and RES-01. A threshold is only useful if it exceeds measurement noise, so the first job was to measure that noise.

## Findings

### Measured baselines and noise

1. **Setup.** The FND-05 harness ran three times back to back with the default BenchmarkDotNet job: `dotnet run -c Release --project benchmarks/Creole.Benchmarks`, .NET 11 RC 2 (11.0.0-rc.2.26504.105), BenchmarkDotNet 0.16.0-preview.2, i9-13900H laptop (hybrid P and E cores), Ubuntu 25.10, `powersave` governor, other work running (load average 3 to 7). Runs took 19.5, 13.2 and 13.9 minutes. Run folders: `20261018-190717`, `20261018-192649`, `20261018-194002` (git-ignored, not kept).

2. **Gate baselines.** Mean per operation, the range across the three runs:

   | Benchmark | System.Text.Json | Parlot | Parlot compiled | Pidgin | Superpower | Sprache |
   | --- | --- | --- | --- | --- | --- | --- |
   | JSON small (1.3 KB) | 3.71–3.98 µs | 6.17–6.43 µs | 5.79–6.14 µs | 38.5–46.0 µs | 154–173 µs | 315–420 µs |
   | JSON medium (104 KB) | 413–440 µs | 551–622 µs | 539–600 µs | 3.05–3.38 ms | 14.6–21.2 ms | 30.7–36.9 ms |
   | JSON large (1.03 MB) | 5.17–5.37 ms | 6.51–6.86 ms | 6.39–6.90 ms | 32.5–39.9 ms | 133–147 ms | 306–323 ms |
   | JSON large, allocated | 4.79 MB | 4.64 MB | 4.64 MB | 9.17 MB | 111.8 MB | 1,102–1,123 MB |

   | Benchmark | Hand-written | Parlot | Parlot compiled | Pidgin `ExpressionParser` | Pidgin folds |
   | --- | --- | --- | --- | --- | --- |
   | Arithmetic, per expression | 82.5–86.4 ns | 330–375 ns | 327–398 ns | 3.76–3.97 µs | 1.30–1.37 µs |
   | Allocated | 0 B | 328 B | 256 B | 343 B | 356 B |

   Measured ratios: Parlot is 5–7× faster than Pidgin on JSON, about 4× faster than Pidgin folds on arithmetic and about 11× faster than `ExpressionParser`. Parlot is within 1.2–1.4× of System.Text.Json on large JSON.

3. **Time noise.** Between identical runs most means moved by 3–8%. The worst moves between consecutive runs were Superpower medium (0.69×), Sprache small (0.75×), Parlot compiled arithmetic (1.22×, then 0.84×) and Pidgin JSON large (1.22×). Pidgin JSON large, the Phase 0 baseline itself, ranged from 32.5 to 39.9 ms. The ratio between two libraries in one run also moves: Parlot over Pidgin on JSON large was 0.19, 0.21 and 0.16.
   Inference: on this machine a single run cannot tell a 20% difference on one benchmark from noise. Repeating runs and summarizing across benchmarks (a geometric mean) narrows that, but the gate machine should also be quieter.

4. **Allocations are deterministic.** Allocated bytes per operation were identical in every run for every benchmark except Sprache large (1,101.87, 1,123.15 and 1,103.55 MB). Inference: allocation gates can be exact, and they are the most reliable signal on noisy hardware.

5. **`Compile()` buys Parlot little here.** Parlot compiled and interpreted overlap within noise on JSON (large: 6.39 vs 6.51 ms in run 1, 6.90 vs 6.86 ms in run 2) and allocate the same. This agrees with Parlot's own 1.x tables (finding 7).

### Parlot

6. **Versions.** Stable Parlot is 1.5.12 (2026-10-13). `Parlot.SourceGenerator` has shipped only previews: 1.6.0-preview.1 to .8 (Dec 2025 to Jan 2026, from a branch) and 2.0.0-preview.1 to .3 (Jul to Sep 2026, from `main`). The README calls `release/1.x` stable and `main` Parlot 2.0. No stable date is published [1][2][3]. Versions read: `main` 143fc0a (2026-10-16), `release/1.x` da34158 (2026-10-14).

7. **The "about 20%" in § Risks is about `Compile()`, not the generator.** Parlot's 1.x README says of `Compile()`: "This can improve the performance by 20% (see benchmarks results)" (checked first-hand). Its own tables show 1–13% (expressions 1.01×; JSON 1.01–1.13×) on an i7-1260P with .NET 9 [3]. Parlot 2.0.0-preview.1's release notes say "Remove compiled parser support" [2].

8. **Generator speed, Parlot's own claim.** On `main` (M4 Pro, .NET 10, BenchmarkDotNet ShortRun, "indicative"), generated against Fluent: expressions 178 vs 223 ns (small) and 1,004 vs 1,303 ns (big), about 20–23% faster; JSON 8–19% faster [4]. Parlot reports Fluent "about 12-15 times faster than Pidgin" on expressions, and on JSON big, generated 32.1 µs against Pidgin 77.5 µs [4]. On an email regex, Pidgin (99.69 ns) beats Parlot Fluent (149.88 ns) [4].

9. **How the 2.0 generator works.** The user writes a factory marked `[GenerateParser(nameof(TryParse))]` in a build-only `.parlot.cs` file plus a `partial` `TryParse` declaration. The generator runs the factory at build time and emits direct code with no runtime Parlot dependency. Its docs say "No interceptor namespace or other call-site rewriting configuration is required." Limits: input must be `string` or `TextReader`; callbacks cannot capture arbitrary locals; unsupported references fail generation rather than falling back [5].

10. **Recent correctness fixes.** Parlot PR #420 (merged 2026-10-14, in 1.5.12) fixed compiled parsers that did not match `Parse()`: compiled `Deferred` and `Unary` skipped recursion limits, and `Separated` and `TextBefore` loops differed [6]. The FND-05 agreement tests already check both Parlot variants against the reference results.

### How other projects set thresholds

11. **Relative threshold plus a nonparametric test.** dotnet/performance's ResultsComparer reports a difference only when a Mann–Whitney equivalence test (TOST) at a user-set threshold and a 0.3 ns noise floor both agree [7][8]. Its runtime workflow uses `--threshold 2%` [9]. The auto-filing bot marks a regression at 5% [10]. BenchmarkDotNet's `--statisticalTest` takes the same kind of threshold, for example `5%` [11].

12. **Aggregate plus per-test bound.** Chromium gates competitive benchmarks at 0.3% overall and 1% per subtest (Speedometer, JetStream) and reverts a regressing patch after one business day [12]. rustc-perf uses a significance threshold from the spread of historical results, not a fixed percentage [13].

13. **Shared CI runners are noisy.** CodSpeed measured a coefficient of variation of 2.66% on GitHub-hosted runners against 0.56% on bare metal (vendor measurement) [14]. Laaber et al. found CV from 0.03% to over 100% across cloud instance types, and could detect slowdowns of 10% or less only with same-instance, randomized runs [15] (abstract only).

14. **No numeric competitor gate found.** No parser or serializer library examined (Parlot, Pidgin, simdjson, System.Text.Json) publishes a "within X% of competitor" gate. System.Text.Json stated a goal of "up to 2x over Json.NET", an outcome rather than a gate [16]. No project examined sets a numeric minimum gain for an optimization: LLVM asks for "evidence of performance improvement on real-world workloads" [17], and dotnet/runtime asks for "a compelling reason" [18].

### Bounded memory

15. **No .NET API reports peak live heap.** `GC.GetTotalMemory(true)` returns heap size excluding fragmentation, after waiting for collections [19]. `GCMemoryInfo` and `GCGenerationInfo` give heap and per-generation sizes and fragmentation at the last GC [20]. `GetTotalAllocatedBytes` and BenchmarkDotNet's `MemoryDiagnoser` report bytes allocated, which grow with input even when retained memory is constant [21][22]. `PeakWorkingSet64` is process-wide and includes code and shared pages [23]. Applies to .NET 11 per the docs.

16. **Pooled memory counts and lingers.** `ArrayPool.Shared` rents ordinary managed arrays and trims only on Gen2 collections, needing at least two to drop an array [24]. Inference: a memory test must warm up and subtract a baseline. `DOTNET_GCHeapHardLimit` caps the GC heap, and DATAS (on by default since .NET 9) changes heap sizing [25].

17. **Prior art compares residency across sizes.** GHC's maximum residency is sampled only at major collections, and its own test suite found residency tests "very sensitive" to small changes [26][27]. Well-Typed diagnosed a conduit space leak by comparing max residency with and without the leak [28] (snippet only). No test found asserts a fixed byte cap.

### Incremental re-parsing

18. **Dubroy and Warth** replayed 891 recorded keystrokes on a 279 KB JavaScript file: incremental re-parse mean 6.2 ms and median 4.7 ms, against about 1.5 s for a full parse with the same parser, and 23.7 ms for Acorn. The memo table cost 11–12% more memory. Their target was the 100 ms HCI limit for "instantaneous" [29].

19. **Dubroy and Warth's cost grows with file size.** Yedidia and Chong (GPeg) show its re-parse time is linear in file size, because applying an edit shifts the memo table. GPeg itself stays near 1 ms per edit on 1–100 MB files (checked first-hand: "logarithmic in the size of the input for typical edits, compared with linear-time reparsing for Incremental Packrat") [30].

20. **Others give no numbers or only node counts.** Tree-sitter aims to be "fast enough to parse on every keystroke" with no figure [31]. Lezer is qualitative [32]. Roslyn rebuilds "typically about O(log n)" nodes per edit [33]. Nielsen gives 0.1 s as the limit for feeling instantaneous [34].

### Fusion, staging and async

21. **flap's fusion gain is measured against a weak unfused baseline.** flap reports "two to six times faster than ocamlyacc" (checked first-hand) [35]. Its Figure 11 puts flap against menhir's code back end at 1.2× (arith) to 4.2× (json), and against its own unfused variant, which connects lexer and parser through OCaml's `Stream`, at 1.7–7.4×. That unfused variant is slower than ocamlyacc on two of six benchmarks [35].
    Inference: the fusion-only gain on .NET, against a span-based token buffer, will be smaller than flap's figures.

22. **Parsley reports no staged-versus-unstaged figure.** Its evaluation compares only against other libraries [36].

23. **System.Text.Json resumes rather than restarts** async deserialization, through a `ReadStack` of suspended frames [37]. No published cost figure was found. Synchronous completion of a `ValueTask` method allocates nothing [38]; no per-await cost figure for .NET was found.

### Analysis and engine agreement

24. **Property tests default to 100 cases** in QuickCheck, Hypothesis, CsCheck and FsCheck [39][40][41][42]. No project examined uses "N random cases, zero mismatches" as a release gate. GCC's fuzzing CI records mismatches without blocking [43] (snippet only).

25. **Clippy gates every lint on a committed UI test** with at least one expected diagnostic [44]. RSLN-07 already plans the same: one corpus entry per CRL error and warning.

## Disconfirming evidence

- **Against "near Pidgin" as the Phase 0 bar.** Parlot is faster than Pidgin by large factors in our runs and its own, so passing "near Pidgin" says little about the "match Parlot" aim. Pidgin does beat Parlot on one workload (regex), so per-benchmark results depend on the grammar [4].
- **Against the whitepaper's Parlot claims.** The "20%" is a claim about `Compile()`, which Parlot's own tables show gaining 1–13% and which 2.0 removes [2][3]. The 2.0 generator does not intercept calls [5], which makes whitepaper lines 24, 681 and 1019 stale (the 1.6 previews were not checked).
- **Against the incremental design.** § Incremental re-parsing builds on Dubroy and Warth, whose cost GPeg shows is linear in file size [30]. Creole's offset table for spans may avoid this, but the gate must test growth with file size, not one file.
- **Against a zero-mismatch agreement gate on fresh seeds.** Differential fuzzing produces duplicate and spurious mismatches (GoldenFuzz, snippet only, *unverified*), which favours a fixed seed corpus that blocks plus fresh seeds that file issues.
- Searched without finding a numeric competitor gate: Parlot, Pidgin, simdjson, System.Text.Json. Searched without finding a numeric minimum-win rule: LLVM, dotnet/runtime. The "5% compile time for 5% runtime" LLVM heuristic appeared only in a snippet (*unverified*).

## Implications for Creole

Proposed thresholds, all inference, to settle through `/decide`. Each speed gate runs on one fixed machine with the default job, measures Creole and its baseline in the same run, repeats the run three times and uses the median ratio. Where a per-benchmark bound applies, a Mann–Whitney test at that threshold must agree (BenchmarkDotNet `--statisticalTest`).

| Gate | Proposal | Reason |
| --- | --- | --- |
| Phase 0 speed | Interpreter on character-level JSON (three sizes) and arithmetic: geometric mean of time ratio against Pidgin (`PidginFolds` for arithmetic) ≤ 1.0, no benchmark above 1.25; allocations ≤ Pidgin's on every benchmark. Report the ratio to stable Parlot, without gating on it | Pidgin is the stated bar; measured noise (up to 1.3× on one benchmark) rules out a tighter per-benchmark bound; allocations are exact |
| Phase 3 speed (new) | Generated engine: geometric mean ≤ 1.2× stable Parlot interpreted | Makes "match Parlot" in § Risks testable. Revisit when `Parlot.SourceGenerator` is stable |
| Phase 1 memory | Peak retained heap (forced full GC every K items, maximum, minus warm-up baseline) on Example B and JSON lines at 10 MB, 100 MB and 1 GB: peak at 1 GB ≤ 1.5× peak at 10 MB | A size ratio survives runtime changes; a fixed cap does not |
| Phase 2 analysis | Every CRL error and warning in § Static analysis has at least one corpus grammar that triggers it and one near miss that does not; 100% caught, 0 false positives | Countable; matches RSLN-07 and Clippy practice |
| Phase 3 agreement | Zero mismatches on a fixed-seed corpus of N random grammar and input pairs on every CI run; fresh seeds run nightly and file issues | Fixed seeds block reliably; fresh seeds find new bugs without flaky builds. N is a CI-time choice |
| Incremental (1.x) | Replay a keystroke log on a file of about 300 KB and on a 10× copy: median re-parse ≤ 2% of full parse, p99 ≤ 100 ms, and median on the 10× file ≤ 2× the base file | Tests both "tracks the edit" and the GPeg pitfall; the 100 ms is Nielsen's limit |
| Fusion (1.x) | Fused over best unfused generated code: geometric mean ≥ 1.2× faster on JSON, arithmetic and Example B, no benchmark slower | Exceeds noise; flap's figures are an upper bound |
| Resumable async (1.x) | Over the restart driver on JSON lines: ≥ 1.2× throughput when the median record is at least one pipe segment, and no worse than 0.95× when records are small | Resumable code adds checks to the common path, so it must not slow it |

Other implications:

- **Whitepaper editorial fixes** (separate change): § Risks line 1051 should attribute "about 20%" to `Compile()` and give Parlot's own 1–13%, or cite the 2.0 generator's 8–23%. Lines 24, 681 and 1019 should not say Parlot intercepts calls.
- **Harness:** replace "Parlot compiled" with the generated variant when `Parlot.SourceGenerator` reaches a stable release; note this on BENCH-02.
- **Machine:** pin the gate machine and its settings (performance governor, quiet load, P-cores only if possible) in benchmarks/README.md, or noise will exceed the thresholds.
- **FND-06's acceptance criterion** names every gate, including Phase 2 and 3; the Scope list omits them. The table above covers all of them.

## Open questions

- Whether the Phase 0 bar should be Pidgin, Parlot or both. That is a positioning choice for the maintainer, not a research question.
- N for the agreement corpus, and K and the size ratio for the memory test, need a spike on real code.
- How noisy this harness is on the GitHub Actions runner. The on-demand workflow has not run yet.
- Whether the 1.6 generator previews used interceptors.
- Fleming and Wallace (CACM 1986) on geometric means could not be opened; the recommendation to use the geometric mean of ratios is *unverified* from that source.

## Sources

1. Parlot on NuGet. https://www.nuget.org/packages/Parlot — versions tab
2. Parlot.SourceGenerator on NuGet and Parlot GitHub releases. https://www.nuget.org/packages/Parlot.SourceGenerator, https://api.github.com/repos/sebastienros/parlot/releases — versions tab; first 15 releases
3. Parlot README, `release/1.x` (da34158). https://github.com/sebastienros/parlot/blob/release/1.x/README.md — relevant sections; the "20%" sentence read first-hand
4. Parlot README, `main` (143fc0a). https://github.com/sebastienros/parlot — relevant sections, spot-checked first-hand
5. Parlot source generation guide. https://github.com/sebastienros/parlot/blob/main/docs/source-generation.md — full, via a fetch summary
6. Parlot PR #420. https://github.com/sebastienros/parlot/pull/420 — description
7. dotnet/performance ResultsComparer README and Program.cs. https://github.com/dotnet/performance/tree/main/src/tools/ResultsComparer — relevant sections
8. ResultsComparer TwoInputsComparer.cs. https://github.com/dotnet/performance/blob/main/src/tools/ResultsComparer/TwoInputsComparer.cs — full
9. Benchmarking workflow for dotnet/runtime. https://github.com/dotnet/performance/blob/main/docs/benchmarking-workflow-dotnet-runtime.md — relevant sections
10. dotnet/perf-autofiling-issues #18434 (2023). https://github.com/dotnet/perf-autofiling-issues/issues/18434 — relevant sections
11. BenchmarkDotNet console arguments. https://github.com/dotnet/BenchmarkDotNet/blob/master/docs/articles/guides/console-args.md — relevant sections; and IntroStatisticalTesting, https://benchmarkdotnet.org/articles/samples/IntroStatisticalTesting.html — full
12. Chromium. Regressions to Competitive Benchmarks. https://chromium.googlesource.com/chromium/src/+/main/docs/benchmark_performance_regressions.md — full
13. rustc-perf comparison analysis. https://github.com/rust-lang/rustc-perf/blob/master/docs/comparison-analysis.md — full
14. CodSpeed. Benchmarks in CI without noise. https://codspeed.io/blog/benchmarks-in-ci-without-noise — full (vendor)
15. Laaber, Scheuner, Leitner. Software Microbenchmarking in the Cloud. How Bad is it Really? EMSE 2019. https://research.chalmers.se/en/publication/511491 — abstract only
16. Landwerth. Try the new System.Text.Json APIs. .NET Blog 2019. https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-apis/ — full
17. LLVM Developer Policy. https://llvm.org/docs/DeveloperPolicy.html — relevant sections
18. dotnet/runtime performance guidelines. https://github.com/dotnet/runtime/blob/main/docs/project/performance-guidelines.md — relevant sections
19. GC.GetTotalMemory. https://learn.microsoft.com/en-us/dotnet/api/system.gc.gettotalmemory — full
20. GCMemoryInfo and GCGenerationInfo. https://learn.microsoft.com/en-us/dotnet/api/system.gcmemoryinfo, https://learn.microsoft.com/en-us/dotnet/api/system.gcgenerationinfo — full
21. GC.GetTotalAllocatedBytes. https://learn.microsoft.com/en-us/dotnet/api/system.gc.gettotalallocatedbytes — full
22. BenchmarkDotNet diagnosers. https://benchmarkdotnet.org/articles/configs/diagnosers.html — relevant sections
23. Process.PeakWorkingSet64. https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.peakworkingset64 — full
24. SharedArrayPool.cs. https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Buffers/SharedArrayPool.cs — relevant sections
25. Garbage collector runtime configuration. https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector — relevant sections
26. GHC User's Guide 9.14.1, runtime control. https://downloads.haskell.org/ghc/latest/docs/users_guide/runtime_control.html — relevant sections
27. GHC ticket #10557 (2015). https://mail.haskell.org/pipermail/ghc-tickets/2015-June/025381.html — full
28. Well-Typed. Sharing, Space Leaks, and Conduit. 2016. https://well-typed.com/blog/2016/09/sharing-conduit — search snippet only
29. Dubroy, Warth. Incremental Packrat Parsing. SLE 2017. https://ohmjs.org/pubs/sle2017/incremental-packrat-parsing.pdf — evaluation sections
30. Yedidia, Chong. Fast Incremental PEG Parsing. SLE 2021. https://people.seas.harvard.edu/~chong/pubs/gpeg_sle21.pdf — §6–8; key sentence checked first-hand
31. Tree-sitter documentation. https://tree-sitter.github.io/tree-sitter/ — relevant sections
32. Haverbeke. Lezer. https://marijnhaverbeke.nl/blog/lezer.html — full
33. Lippert. Persistence, façades and Roslyn's red-green trees. 2012. https://ericlippert.com/2012/06/08/red-green-trees/ — full
34. Nielsen. Response Times: The 3 Important Limits. https://www.nngroup.com/articles/response-times-3-important-limits/ — relevant sections
35. Yallop, Xie, Krishnaswami. flap: A Deterministic Parser with Fused Lexing. PLDI 2023. https://arxiv.org/abs/2304.05276 — §2.2 and §6; abstract checked first-hand
36. Willis, Wu, Pickering. Staged Selective Parser Combinators. ICFP 2020. https://spiral.imperial.ac.uk/bitstreams/2f688fa7-fcac-4d2d-a962-594258e8fb7f/download — pp. 20–26
37. System.Text.Json ReadStack.cs. https://source.dot.net/System.Text.Json/System/Text/Json/Serialization/ReadStack.cs.html — relevant sections; and dotnet/runtime#1562, https://github.com/dotnet/runtime/issues/1562 — full
38. Toub. Understanding the Whys, Whats, and Whens of ValueTask. 2018. https://devblogs.microsoft.com/dotnet/understanding-the-whys-whats-and-whens-of-valuetask/ — full
39. QuickCheck 2.19 documentation. https://hackage-content.haskell.org/package/QuickCheck-2.19.0.0/docs/Test-QuickCheck.html — relevant sections
40. Hypothesis API reference. https://hypothesis.readthedocs.io/en/latest/reference/api.html — relevant sections
41. CsCheck README. https://github.com/AnthonyLloyd/CsCheck — relevant sections
42. FsCheck, Running tests. https://fscheck.github.io/FsCheck/RunningTests.html — relevant sections
43. RISE GCC Fuzz CI. https://lf-rise.atlassian.net/wiki/x/0QWD — search snippet only
44. Clippy, Adding a new lint. https://doc.rust-lang.org/nightly/clippy/development/adding_lints.html — relevant sections
