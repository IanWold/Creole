# Which stable Parlot should the baselines use, and do the whitepaper's Parlot facts hold on 2.0.0?

| | |
| --- | --- |
| Date | 2026-10-24 |
| Cards | [FND-10](../../active/01-foundations/FND-10-move-the-parlot-baselines-to-200.md), [BENCH-03](../../active/13-benchmarks/BENCH-03-phase-3-speed-gate.md), [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md), [STRM-02](../../active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md), [STRM-04](../../active/08-streaming/STRM-04-huge-single-documents.md) |
| Whitepaper | [§ Background and motivation](../../../whitepaper.md#background-and-motivation), [§ Source-generated engine](../../../whitepaper.md#source-generated-engine), [§ Considered and deferred](../../../whitepaper.md#considered-and-deferred), [§ Streaming and async input](../../../whitepaper.md#streaming-and-async-input), [§ Comparison with existing .NET libraries](../../../whitepaper.md#comparison-with-existing-net-libraries), [§ Roadmap](../../../whitepaper.md#roadmap), [§ Risks](../../../whitepaper.md#risks) |
| Confidence | High for the facts: the API, string and `Separated` behaviour were run on each version, and the generator and streaming claims were read first-hand at the v2.0.0 commit. Medium for the speed results: two loops of three interleaved runs on one laptop. 1.5.12 is clearly slower on JSON; 1.6.0 and 2.0.0 overlap within noise. |

## Short answer

Pin Parlot 2.0.0. Parlot shipped two stable releases on 2026-10-20: 2.0.0 from `main` and 1.6.0 from `release/1.x`. FND-10 names only 1.5.12 and 2.0.0, but under ADR-0003's "fastest stable release" rule 1.6.0 had to be measured too. On the FND-05 corpora, 2.0.0's interpreted parsers have the lowest geometric mean. Against 1.5.12 it is 0.92×, mostly from JSON, which is 7–14% faster. Against 1.6.0 it is 0.98–0.99×, within run-to-run noise. Arithmetic is level across all three versions. Moving the harness to 2.0.0 needs one grammar change besides dropping `Compile()`: `ZeroOrOne` now returns a list, so the JSON grammar uses `Separated(comma, x, 0, 0)`. Both FND-05 findings still hold on 1.6.0 and 2.0.0. Parlot's generator is stable only as 2.0.0. It still compiles and runs the user's factory at build time, so the whitepaper's contrast with Creole holds, but the `[IncludeFiles]` clause and every "preview packages" phrase are stale. Parlot 2.0.0 also parses `TextReader` input with bounded memory, a restart-on-`HitEnd` window driver and `Commit()` cuts. That is close to Creole's one-layer streaming design, and the whitepaper does not mention it.

## Context

[FND-10](../../active/01-foundations/FND-10-move-the-parlot-baselines-to-200.md) must bring the Parlot baseline and the whitepaper's facts about Parlot up to date before BENCH-01 measures the Phase 0 gate. [ADR-0003](../decisions/0003-numeric-gate-thresholds.md) defines the Phase 0 baseline as "the fastest stable Parlot release's interpreted (Fluent) parser", and switches the Phase 3 gate to Parlot's generated parsers once `Parlot.SourceGenerator` is stable. The [DFA research note](2026-10-23-dfa-compilation-under-commit-semantics.md) (findings 21–22) found that 2.0.0 was stable. This note answers six questions: which version is faster, what breaks in the harness, whether FND-05's two Parlot findings still hold, how the 2.0.0 generator obtains a grammar, whether the speed claims in § Risks still hold, and which whitepaper statements are stale.

## Findings

### Versions

1. **Two stable releases on the same day.** Parlot 1.6.0 (tag `v1.6.0`, `release/1.x`, 02:00 UTC) and 2.0.0 (tag `v2.0.0`, `main`, 02:44 UTC) were both published on 2026-10-20 with `prerelease: false` [1]. NuGet lists 2.0.0 as the latest `Parlot` and the latest `Parlot.SourceGenerator`. `Parlot.SourceGenerator` has no stable 1.x: its 1.6.0 versions are all previews [2].
2. **1.6.0 backports 2.0's runtime work.** Its release notes describe "a 1.x maintenance release" that "brings the fixes and runtime optimizations from the 2.0 line to the 1.x branch, and makes compiled parsers behave like runtime parsers". It keeps `Compile()`, and adds `ParseContext.MaxRecursionDepth` [1]. 2.0.0 removes compiled parsers [1]. Both versions ship `net10.0`, `net8.0`, `net472` and `netstandard2.0` builds (read from the restored packages).

### Speed of the interpreted parsers (question 1)

3. **Setup.** Clean copies of `HEAD` (f2b6215) in the scratchpad, one per Parlot version, each built and checked with `tests/Creole.Benchmarks.Tests`. Each run used the default job and the filter `*JsonBenchmarks.Parlot`, `*JsonBenchmarks.SystemTextJson`, `*ArithmeticBenchmarks.Parlot`, `*ArithmeticBenchmarks.HandWritten`, so every run also measured its own ratio baseline. Two loops alternated versions, three runs each: 1.5.12 with 2.0.0, then 1.6.0 with 2.0.0. Machine: i9-13900H laptop, Ubuntu 25.10, on AC power, `powersave` governor, .NET 11 RC 2 (11.0.0-rc.2.26504.105), BenchmarkDotNet 0.16.0-preview.2. The JSON grammar on 2.0.0 uses `Separated(comma, x, 0, 0)` (finding 9); otherwise the grammars are the same.

4. **1.5.12 against 2.0.0** (loop 1). Mean per operation, three runs and their median:

   | Benchmark | 1.5.12 runs | 1.5.12 median | 2.0.0 runs | 2.0.0 median | 2.0.0 / 1.5.12 |
   | --- | --- | --- | --- | --- | --- |
   | Arithmetic, per expression | 314.7, 306.0, 295.2 ns | 306.0 ns | 361.8, 333.4, 315.9 ns | 333.4 ns | 1.09 |
   | JSON small (1.3 KB) | 5.44, 5.52, 5.58 µs | 5.52 µs | 5.47, 5.08, 5.54 µs | 5.47 µs | 0.99 |
   | JSON medium (104 KB) | 540, 581, 553 µs | 553 µs | 471, 486, 493 µs | 486 µs | 0.88 |
   | JSON large (1.03 MB) | 6.60, 6.55, 6.35 ms | 6.55 ms | 5.61, 5.59, 5.68 ms | 5.61 ms | 0.86 |

   Geometric mean of the four ratios: 0.95. Ratios to each run's own baseline (System.Text.Json or hand-written) give the same picture: 1.10, 1.00, 0.87 and 0.89, geometric mean 0.96. The baselines themselves stayed within 9% across the six runs.

5. **1.6.0 against 2.0.0** (loop 2):

   | Benchmark | 1.6.0 runs | 1.6.0 median | 2.0.0 runs | 2.0.0 median | 2.0.0 / 1.6.0 |
   | --- | --- | --- | --- | --- | --- |
   | Arithmetic, per expression | 312.5, 309.7, 305.6 ns | 309.7 ns | 309.9, 294.4, 310.0 ns | 309.9 ns | 1.00 |
   | JSON small (1.3 KB) | 5.12, 5.10, 5.56 µs | 5.12 µs | 5.22, 4.97, 4.99 µs | 4.99 µs | 0.97 |
   | JSON medium (104 KB) | 501, 485, 501 µs | 501 µs | 486, 482, 508 µs | 486 µs | 0.97 |
   | JSON large (1.03 MB) | 6.00, 5.83, 5.54 ms | 5.83 ms | 5.67, 5.60, 5.77 ms | 5.67 ms | 0.97 |

   Geometric mean 0.98. The two versions' ranges overlap on every benchmark. In this loop the hand-written baseline was noisy in the 2.0.0 runs (95.3, 78.7 and 100.4 ns), so ratios to it are not used.

6. **All three versions.** Taking each version's median over all its runs (three for 1.5.12 and 1.6.0, six for 2.0.0), which mixes the two loops:

   | Benchmark | 1.5.12 | 1.6.0 | 2.0.0 | 2.0.0 / 1.5.12 | 2.0.0 / 1.6.0 |
   | --- | --- | --- | --- | --- | --- |
   | Arithmetic | 306 ns | 310 ns | 313 ns | 1.02 | 1.01 |
   | JSON small | 5.52 µs | 5.12 µs | 5.15 µs | 0.93 | 1.01 |
   | JSON medium | 553 µs | 501 µs | 486 µs | 0.88 | 0.97 |
   | JSON large | 6.55 ms | 5.83 ms | 5.64 ms | 0.86 | 0.97 |
   | Geometric mean | | | | 0.92 | 0.99 |

   1.6.0 against 1.5.12 is 0.93. Inference: the JSON gain comes from the runtime work both 1.6.0 and 2.0.0 contain (finding 2). Between 1.6.0 and 2.0.0 the measured difference is smaller than the noise the [2026-10-18 note](2026-10-18-numeric-gate-thresholds.md) reports for this machine.

7. **One loop can mislead.** In loop 1, all three 2.0.0 arithmetic runs (361.8, 333.4, 315.9 ns) were slower than all three 1.5.12 runs (314.7, 306.0, 295.2 ns), which suggested a 9% regression. In loop 2, the same 2.0.0 build ran at 294.4–310.0 ns. The slowdown did not replicate, so this note does not claim one. It is consistent with Parlot's statement that "Parsing a `string` is unchanged" by the streaming support [6].

8. **Allocations.** Per parse, 2.0.0 allocates 320 bytes per expression, 1.5.12 allocates 328 and 1.6.0 allocates 336. On JSON small the figures are 6,944, 6,952 and 6,960 bytes, and the larger documents show the same 8- and 16-byte steps. Allocations did not vary between runs.

### Harness changes on 2.0.0 (question 2)

9. **Two breaks.** `Compile()` is gone. `ZeroOrOne(p)` now returns `Parser<IReadOnlyList<T>>`, a list of zero or one item, so `ZeroOrOne(Separated(comma, json))` became a list of lists and did not compile (two CS1503 errors in `ParlotJson.cs`). The `ZeroOrOne(p, defaultValue)` overload is gone too. `Separated` gains `min` and `max` parameters; its documentation says "An explicit minimum of zero allows an empty list" (read from the package's XML documentation). With `Separated(comma, json, 0, 0)` and `Separated(comma, member, 0, 0)` and without the `ParlotCompiled` benchmarks, the harness built with 0 warnings and all 22 agreement tests passed (26 before, less the four `ParlotCompiled` cases). On 1.6.0 the unchanged harness built with 0 warnings and passed all 26.
10. **Other behaviour changes** listed in the 2.0.0 and 1.6.0 notes: repetitions and `Separated` stop when the inner parser makes no progress, only `'\n'` ends a line, and empty `ZeroOrOne`, `ZeroOrMany` and `Separated` results are reported at the cursor [1]. None changed an agreement test.

### FND-05's Parlot findings (question 3)

11. **`\/` is still rejected.** A probe parsed `"a\/b"` with `Terms.String(StringLiteralQuotes.Double).Eof()`. It failed on 1.5.12, 1.6.0 and 2.0.0. `"a\\b"` and `"a\u0041b"` parsed on all three. The researcher read the escape list in `Scanner.cs` at both tags and found it unchanged; 2.0.0 only adds a flag that skips decoding when a string has no escapes [4].
12. **`Separated` still fails on an empty list by default.** `Between('[', Separated(',', Integer()), ']')` rejected `[]` on all three versions. On 2.0.0, `Separated(',', Integer(), 0, 0)` accepted `[]`, `[1]` and `[1,2]` and rejected `[1,]`. The two-argument overload chains to a minimum of 1 [4]. So the FND-05 workaround changes form on 2.0.0 but is still needed.

### How the 2.0.0 generator obtains a grammar (question 4)

13. **It still runs the user's code.** `ParserSourceGenerator.cs` at 2a34f3d emits a temporary compilation, built from the host compilation plus the `.parlot.cs` trees (lines 537 and 544). It loads the image with `Assembly.Load` (755), calls the factory by reflection (796), and calls `GenerateSource` on the resulting parser graph (936) [3]. The guide says "`[GenerateParser]` factories are executable build code, not declarative metadata. They run inside the compiler host with the build process's permissions" [3, `source-generation.md`:506].
14. **Callback bodies come from syntax.** The generator takes each lambda's source text, reparses it with `SyntaxFactory.ParseExpression` (line 1725) and emits it with `#line` directives [3]. Inference: the graph's structure comes from execution and the callback code from syntax.
15. **Dependencies in the project are free.** The factory "is compiled against the consuming project's references" (`source-generation.md`:38). `[IncludeFiles]` is now for "build-time grammar helpers that are not already part of the consuming compilation" (:463) [3].
16. **What the user writes.** Each `[GenerateParser(nameof(...))]` factory "must have one matching partial method declaration" (`source-generation.md`:132). The factory lives in a `*.parlot.cs` file, which the package's MSBuild targets add as `AdditionalFiles` and remove from `Compile` (`Parlot.SourceGenerator.targets`:22–24), so it never ships. Generated parsers embed their own copy of the runtime, and "application code does not need a runtime reference to `Parlot`" (`source-generation.md`:3–5) [3].
17. **No fallback, and fewer options.** "There is no runtime fallback for unsupported standalone code" (`source-generation.md`:599). Generated parsers take a `string` or a `TextReader` only, return no consumed offset, and accept "No custom `ParseContext`, recursion-depth setting, or other execution-context option" (:240–247) [3].
18. **The IDE does not run factories.** "IDE/design-time analysis does not execute grammar factories. The generator emits throwing stubs so partial method declarations remain available to IntelliSense" (`source-generation.md`:552) [3].
19. **Caching.** The output step combines with `context.CompilationProvider` (`StandaloneParserGeneration.cs`:92) [3]. Inference: any edit to the project re-runs generation, which then emits and loads the whole compilation.

### Speed claims in § Risks (question 5)

20. **The 8–23% is still Parlot's claim at the tag.** The README at 2a34f3d reports generated parsers at 0.80, 0.77 and 0.81 of Fluent on expressions and 0.81–0.92 on JSON. The tables were "refreshed on 2026-10-02" with ShortRun on an Apple M4 Pro under .NET 10, and the README calls short runs "indicative" [5]. It also says "Parlot Fluent is about 12-15 times faster than Pidgin" on expressions [5].
21. **The README predates later Fluent speedups.** PR #429, merged 2026-10-19, reports `ExpressionFluentBig` 1,307.9 → 1,213.6 ns and `DeepJson_Parlot` 33.96 → 28.62 µs, measured in-process with ShortRun on Apple Silicon [7]. Inference: at the tag the generated-over-Fluent gap is probably smaller than the README shows.
22. **`Compile()` lives on in 1.x.** 1.6.0 keeps compiled parsers and makes them match runtime parsers [1]. The "1–13%" in § Risks comes from Parlot's 1.x tables (the [2026-10-18 note](2026-10-18-numeric-gate-thresholds.md), finding 7).

### Streaming in Parlot 2.0.0 (found while answering question 5)

23. **Parlot 2.0.0 streams text.** Its streaming guide [6] describes two modes:
    - **Compacting buffer** (`Parse(TextReader)`, `ParseAsync`). "The parse runs once over a buffer which is refilled in place, and which only retains the text a parser can still move back to. Memory is bounded by the largest token or backtracking region." A token that reaches the end of the window is "parsed again from its start" after a refill. Choices, options and repetitions pin their start with `ParseContext.Pin()`; a choice with a lookup table "pins nothing". `Commit()` is a cut that releases the text before it.
    - **Window driver** (`ParseManyAsync`). A value that sets `HitEnd` is "inconclusive", and the driver "reads more text, so that the window at least doubles, and parses the value again from its start". Callbacks "can run more than once for the same text".
    - "The parse is synchronous": when the input does not fit in the first block, "the parse runs on a thread pool thread whose refills block on `TextReader.ReadAsync`".
    - Limits: offsets are `int`, so inputs are capped at `int.MaxValue` characters. Generated parsers support only the compacting buffer, not `ParseManyAsync`.
24. **Parlot measures the cost.** Its own table shows `Parse(TextReader)` at 1.07–1.17× the time of `ReadToEnd` plus `Parse` on an access log, with about half the allocation [6].

### Stale statements (question 6)

25. Statements about Parlot in the whitepaper at `HEAD`, checked against findings 1–24:

    | Line | Section | Statement | On 2.0.0 |
    | --- | --- | --- | --- |
    | 24 | § Background | "Its generator, so far only in preview packages, runs `[GenerateParser]` methods at build time and emits the body of a `partial` parse method the user declares." | "so far only in preview packages" is stale (finding 1). The rest holds (13, 16). |
    | 28 | § Background | "Parlot compiles ahead of time by running grammar methods inside its generator" | Holds (13). |
    | 594 | § Limits for untrusted input | "Parlot's recursion limit is the model." | Holds for the runtime, where the limit is now configurable through `ParseContext.MaxRecursionDepth` (2). Generated parsers take no recursion-depth setting (17). |
    | 745 | § Source-generated engine | "Parlot instead has users declare a `partial` parse method for its generator to implement" | Holds (16). |
    | 766 | § Considered and deferred | "executes `[GenerateParser]` methods at build time to build the grammar graph, then emits code for it" | Holds (13). |
    | 766 | § Considered and deferred | "at the cost of compiling the grammar's dependencies inside the generator, which Parlot asks users to list with `[IncludeFiles]`" | Stale. The generator compiles the whole project with the grammar files; `[IncludeFiles]` is only for helpers outside the project (13, 15). |
    | 766 | § Considered and deferred | "executing user code inside the compiler is fragile in the IDE and works against incremental caching" | An argument, not a fact. Parlot avoids the IDE half by not executing at design time (18). The caching half agrees with finding 19. |
    | 1072 | § Comparison | "Parlot's generator, so far only in preview packages, already generates code at build time" | "so far only in preview packages" is stale (1). |
    | 1082 | § Comparison, table | "Executes grammar methods, implements declared `partial` methods (preview packages only)" | "(preview packages only)" is stale (1). |
    | 1117 | § Roadmap | "Parlot means stable Parlot's interpreted parsers until `Parlot.SourceGenerator` ships a stable release; the Phase 3 speed gate then compares against Parlot's generated parsers." | The condition is met (1). The sentence should be in the present tense. |
    | 1130 | § Risks, fusion row | "Parlot reports its 2.0 generated parsers 8–23% faster than its runtime ones, and `Compile()` 1–13% faster" | Holds as Parlot's claim (20, 22); the gap is probably smaller at the tag (21). |
    | 1146 | § Questions raised in review | "Should the generator execute grammar construction, as Parlot does" | Holds (13). |

    The § Comparison table has no row for streaming input, and § Streaming and async input does not mention Parlot (finding 23).

    In [benchmarks/README.md](../../../benchmarks/README.md), the second Parlot row of "Known differences" ("ships only as preview packages … only stable Parlot 1.5.12 … is measured") is stale. The first Parlot row still holds, but its `Separated` workaround changes form on 2.0.0 (12). The "Parlot compiled" baselines in the "What is measured" table depend on the version pinned.

## Disconfirming evidence

- **The card's premise was incomplete.** It names 1.5.12 and 2.0.0. 1.6.0, released the same day with the same runtime optimizations, also had to be measured (findings 1–2).
- **2.0.0 is barely faster than 1.6.0.** Its lead is 1–3%, inside this machine's noise (findings 5–6). The pin follows ADR-0003's rule on the medians measured, but another set of runs could put 1.6.0 ahead. One loop of three runs even suggested an arithmetic regression that a second loop did not reproduce (finding 7).
- **Parlot answers one of Creole's reasons for reading syntax only.** The whitepaper says executing user code in the compiler "is fragile in the IDE". Parlot does not execute at design time and emits stubs instead (finding 18). The other reason, incremental caching, still holds (finding 19), and Parlot has no fallback where Creole falls back to the interpreter (finding 17).
- **Parlot already streams.** Bounded-memory `TextReader` parsing, restart on `HitEnd`, `ParseManyAsync` and synchronous parsing on a pool thread are all in Parlot 2.0.0 (finding 23). Creole's § Streaming makes the same choices. What Parlot lacks is n layers, cross-layer backtracking, feedback, byte sources, and offsets above `int.MaxValue`.
- **Searched without finding a regression report.** The researcher searched Parlot's issues for "regression" and "slower" and read every item created on or after 2026-10-19. None reports Fluent getting slower in 2.0; #428 slows a `TextBefore` case the harness does not use (researcher's search; not repeated first-hand).
- **Searched for problems with build-time execution.** The researcher searched Parlot's issues for "parlot.cs", "generator" and "source generator" and found no issue reporting trouble with factory execution (researcher's search; not repeated first-hand).

## Implications for Creole

- **Pin 2.0.0.** Interleaved with 1.6.0 it was faster on all three JSON sizes and level on arithmetic (finding 5), and it has the lowest geometric mean of the three versions (finding 6). So ADR-0003's rule picks it, by a margin over 1.6.0 that is within noise. It is also the only version with a stable generator, so one Parlot version serves both the Phase 0 baseline and BENCH-03, and the `ParlotCompiled` baselines go. If a later gate run puts 1.6.0 ahead, the Phase 0 bar moves by about 1–3%; ADR-0003 already requires each gate run to record the Parlot version.
- **The Phase 0 bar against Parlot is harder.** Against 1.5.12, which ADR-0003 measured, 2.0.0 is about 8% faster overall and 12–14% faster on medium and large JSON (finding 6). The 1.5× threshold does not change; the interpreter has to be that much faster to meet it.
- **Card scope.** FND-10's Measure bullet and first criterion should name 1.6.0 as well as 1.5.12 and 2.0.0, and its title ("Move the Parlot baselines to 2.0.0") may not describe the outcome. The maintainer decides.
- **Whitepaper facts** (FND-10's Build stage, then `/check-sources`): drop "preview packages" at lines 24, 1072 and 1082; restate line 1117 in the present tense; fix the `[IncludeFiles]` clause at line 766; optionally note `MaxRecursionDepth` at line 594. These change facts, not arguments.
- **One argument changed.** Line 766's "fragile in the IDE" no longer separates Creole from Parlot. The contrast between executing and reading syntax still holds, so FND-10's fourth criterion does not force a decision on it. But rewording that reason, or replacing it with Parlot's lack of fallback, changes an argument and needs a short decision record (`/decide`).
- **Streaming.** § Comparison should get a streaming row, and § Streaming and async input should cite Parlot as prior art. A row in § Comparison is a fact. Whether Parlot's streaming changes the case for goal G2 is an argument for `/decide` or `/challenge`. The design ideas are worth reading before [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md), [STRM-02](../../active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md) and [STRM-04](../../active/08-streaming/STRM-04-huge-single-documents.md) start: pinning only where a parser can move back, a sticky `HitEnd`, and doubling the window to bound retries.
- **BENCH-03.** With 2.0.0 pinned, the generated baselines need only `Parlot.SourceGenerator` 2.0.0 and `.parlot.cs` grammars beside the Fluent ones. If a later pin goes back to 1.x, generated parsers have no Parlot runtime dependency (finding 16), so a separate class library could hold Parlot 2.0.0 and the generator while the benchmark project uses the 1.x runtime. This is untested.
- Accepted decision records and earlier notes stay as they are. ADR-0003's line "Parlot's source generator is preview-only" was true when written.

## Open questions

- Whether 2.0.0 stays ahead of 1.6.0 in the maintainer's gate runs, which run the full benchmark set in one invocation. The measured gap is within noise.
- Exact generator build-time cost on a real project: not measured.

## Sources

1. sebastienros/parlot GitHub releases v2.0.0 and v1.6.0 (API `repos/sebastienros/parlot/releases`). https://github.com/sebastienros/parlot/releases/tag/v2.0.0, https://github.com/sebastienros/parlot/releases/tag/v1.6.0 — v1.6.0 notes read in full; v2.0.0 metadata read, notes via researcher summary
2. NuGet flat-container indexes. https://api.nuget.org/v3-flatcontainer/parlot/index.json, https://api.nuget.org/v3-flatcontainer/parlot.sourcegenerator/index.json — full, read 2026-10-24
3. sebastienros/parlot at v2.0.0 (2a34f3d): `docs/source-generation.md`, `src/Parlot.SourceGenerator/ParserSourceGenerator.cs`, `StandaloneParserGeneration.cs`, `build/Parlot.SourceGenerator.targets`. https://github.com/sebastienros/parlot/tree/2a34f3db9f36a55a31d3984be493ff193310bbb0 — quoted lines read first-hand; the rest via researcher summary
4. sebastienros/parlot at v2.0.0 (2a34f3d) and v1.5.12 (fec7a7b): `src/Parlot/Scanner.cs`, `Fluent/StringLiteral.cs`, `Fluent/Separated.cs`, `Fluent/ZeroOrOne.cs`, `Fluent/Parsers.cs` — researcher summary; behaviour confirmed by probes
5. Parlot README at v2.0.0 (2a34f3d). https://github.com/sebastienros/parlot/blob/2a34f3db9f36a55a31d3984be493ff193310bbb0/README.md — benchmark sections read first-hand
6. Parlot streaming guide at v2.0.0 (2a34f3d). https://github.com/sebastienros/parlot/blob/2a34f3db9f36a55a31d3984be493ff193310bbb0/docs/streaming.md — sections Compacting buffer, Asynchronous parsing, Generated parsers, Window driver, Semantics, Limitations, Performance read first-hand
7. Parlot PR #429, Remove operator adapter boxing and tighten scanning hot paths, merged 2026-10-19. https://github.com/sebastienros/parlot/pull/429 — description, benchmark rows spot-checked
8. Parlot 1.5.12, 1.6.0 and 2.0.0 NuGet packages: `lib/net10.0/Parlot.xml` (XML documentation) — `ZeroOrOne`, `Separated`, `Commit`, `ParseAsync`, `ParseManyAsync` entries read
9. Measurements and probes in this note: scratchpad copies of `HEAD` (f2b6215), not kept
