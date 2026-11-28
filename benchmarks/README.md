# Benchmarks

`Creole.Benchmarks` is the BenchmarkDotNet harness behind every phase gate (§ [Benchmark plan](../whitepaper.md#benchmark-plan)). It holds the shared inputs and the competitor baselines. Creole's interpreter runs in `JsonBenchmarks` and `ArithmeticBenchmarks` for the Phase 0 gate ([BENCH-01](../work/active/13-benchmarks/BENCH-01-phase-0-speed-gate.md)). Its other engines join each benchmark class as they are built ([BENCH-02](../work/active/13-benchmarks/BENCH-02-full-benchmark-suite.md)). `RecordBenchmarks` runs `ParseMany` over 1,000 JSON lines held in memory, as bytes through `Layer.Utf8()` and as text. It is where ADR-0045's fork 6 trial ran ([STRM-02](../work/active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md)).

## Running

| Task | Command |
| --- | --- |
| Everything, default job | `dotnet run -c Release --project benchmarks/Creole.Benchmarks` |
| One class | `dotnet run -c Release --project benchmarks/Creole.Benchmarks -- --filter '*Json*'` |
| Quick, less precise | add `--job short` |
| Check that it runs | add `--job dry` (one cold iteration; the numbers mean nothing) |
| Only the gate benchmarks | `dotnet run -c Release --project benchmarks/Creole.Benchmarks -- --anyCategories Gate` |
| Compare two runs | `python3 scripts/bench-compare.py <old-run> <new-run>` |
| Check the Phase 0 gate | `python3 scripts/bench-gate.py <run> <run> <run>` |

Without `--filter` the program runs every benchmark. Other arguments go to BenchmarkDotNet unchanged.

Each run writes to a new folder under the current directory, `BenchmarkDotNet.Artifacts/runs/<UTC yyyyMMdd-HHmmss>/`, with full JSON, GitHub Markdown, CSV and HTML reports per class. Its `run.json` records the commit, whether the working tree had uncommitted changes, the Parlot and Pidgin versions, and the arguments. The folder is git-ignored. `bench-compare.py` matches benchmarks by full name and prints mean time and allocation with new/old ratios. It warns when the two runs used different hardware or runtimes.

## Gate runs

Phase gates compare Creole with its baselines as ratios ([ADR-0003](../work/docs/decisions/0003-numeric-gate-thresholds.md); thresholds in § [Roadmap](../whitepaper.md#roadmap)). A gate run:

- runs on the maintainer's laptop, on AC power, with no other heavy load, from a working tree with no uncommitted changes;
- runs the gate benchmarks three times with the default job, Creole and its baselines in the same invocation:

  ```sh
  for i in 1 2 3; do dotnet run -c Release --project benchmarks/Creole.Benchmarks -- --anyCategories Gate; done
  ```

- is checked with `python3 scripts/bench-gate.py <run> <run> <run>`. For each benchmark the script takes the median over the three runs of Creole's ratio to each baseline, then the geometric mean over the gate's benchmarks, and compares them with the Phase 0 thresholds. It stops if the runs measured different commits or Parlot versions, and warns about uncommitted changes, a job other than the default, or a different host.

The gate benchmarks carry the `Gate` category: `Creole`, `Pidgin` and `Parlot` in `JsonBenchmarks`, and `Creole`, `PidginFolds` and `Parlot` in `ArithmeticBenchmarks`. While [BENCH-04](../work/active/13-benchmarks/BENCH-04-revisit-string-handling-in-the-json-benchmark-grammar.md) compares two ways of building JSON strings, `CreoleSliced` in `JsonBenchmarks` carries it too, so each gate run times both forms; `bench-gate.py` reads only `Creole`. Each run's `run.json` records the commit and the Parlot version, as ADR-0003 requires. The other benchmarks decide no gate, so a gate run leaves them out.

Single runs on this machine vary by up to 1.3× per benchmark, so one run decides nothing. Allocations do not vary and can be compared directly. No CI workflow runs the benchmarks.

## Memory gate

`Creole.MemoryGate` checks the Phase 1 memory gate ([LAY-07](../work/active/06-layers-and-pipelines/LAY-07-three-layer-demo-and-bounded-memory-check.md)): for Example B and a JSON-lines stream, the peak retained heap at 1 GB is at most 1.5× the peak at 10 MB ([ADR-0003](../work/docs/decisions/0003-numeric-gate-thresholds.md)). The memory diagnoser reports bytes allocated, not retained, so the gate has its own harness rather than a BenchmarkDotNet class.

| Task | Command |
| --- | --- |
| Run the gate | `python3 scripts/memory-gate.py` |
| A quicker check at other sizes | `python3 scripts/memory-gate.py --sizes 2000000 20000000` |
| One input at one size | `dotnet run -c Release --project benchmarks/Creole.MemoryGate -- example-b 10000000` |
| A huge JSON document from a pipe ([STRM-04](../work/active/08-streaming/STRM-04-huge-single-documents.md)) | `python3 scripts/memory-gate.py --inputs json-document` |
| JSON lines from a pipe with `ParseManyAsync` ([STRM-02](../work/active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md)) | `python3 scripts/memory-gate.py --inputs json-lines-async --sizes 10000000 1000000000 4400000000` |
| The same with one record of 4 MiB in the middle | `python3 scripts/memory-gate.py --inputs json-lines-async-large --sizes 10000000 1000000000` |

The script builds the harness in Release, runs each input at 10, 100 and 1,000 million characters in its own process, and prints each run's JSON line and a table. It compares the largest size with the smallest, warns if the working tree has uncommitted changes, and exits with 1 when a run fails or the threshold is missed. A gate run should measure a commit. It needs no quiet machine: a 1 GB JSON-lines run beside the test suite measured the same peak as one on a quiet machine. JSON lines grows about 9.5 KB above its baseline, so it fails on less than 5 KB more. If a run fails, log each sample before treating the failure as real.

Each run:

- generates its input as it reads it. `GeneratedReader` writes one record at a time from a fixed seed, so the process never holds the input, and each size's input is the start of the next. A record is a top-level statement in Example B and a line in JSON lines. Sizes count characters. Example B is ASCII, and JSON lines is ASCII but for an occasional `é`, so each input is at least its size in UTF-8 bytes.
- parses Example B as one streamed `Parse` through its three layers, with `MiniGrammar.Statement.SkipMany().Before(End)` as the root so that the parse keeps no statement. JSON lines runs `ParseMany` over the token lexer and `CreoleTokenJson.Value`, one value per item.
- runs the same input at 1 million characters first, then forces a full blocking compacting GC and takes the heap it retained as the baseline.
- before record 0 and every 1,000th record after it, forces the same GC and takes the heap it retained. These samples fall in the middle of the parse, when its reader asks for more input. The peak is the largest sample, and the run reports the peak less the baseline. Arrays the warm-up run returned to `ArrayPool.Shared`, such as the buffers' segments, are in the baseline, so this is growth above a warmed-up pipeline, not everything the parse holds.
- uses workstation GC with concurrent collection off.

The heap a GC retained is what that GC recorded as it ended: `HeapSizeBytes` less `FragmentedBytes` from `GC.GetGCMemoryInfo(GCKind.FullBlocking)`, which counts the large object heap. `GC.GetTotalMemory` read after the GC is not used. It also counts an allocation context that another thread takes between the GC and the read. On a busy machine that added exactly 8 KB to some samples, which made the JSON-lines peak at 1 GB 1.9× the peak at 10 MB.

**The JSON document.** `--inputs json-document` runs a third input against the same threshold. It is not part of the gate. It generates the JSON-lines records, joins them into one JSON array as it writes them as UTF-8 into a `System.IO.Pipelines.Pipe`, and parses the pipe with `Parse(Stream)` on a `LongRunning` thread, as [docs/concepts/pipelines.md § Huge documents from a pipe](../docs/concepts/pipelines.md#huge-documents-from-a-pipe) shows. The pipeline is `Layer.Utf8()`, the token lexer, and a root that reads each element with `CreoleTokenJson.Value` and skips it. The samples are taken on the writing thread, so the parse is mid-step when they are taken, and up to about 64 KiB of unread bytes, the pipe's default pause threshold, may sit between the two. Sizes count the records' characters; the array adds two bytes per record.

**JSON lines without blocking.** `--inputs json-lines-async` and `--inputs json-lines-async-large` are not part of the gate either. They write the JSON-lines records as UTF-8 into a `Pipe`, and parse the pipe with `ParseManyAsync` on the thread pool, as [docs/concepts/pipelines.md § `ParseManyAsync`](../docs/concepts/pipelines.md#parsemanyasync) shows. The pipeline is `Layer.Utf8()`, the token lexer, and `CreoleTokenJson.Value.Spanned()`.

- The run fails unless the last record's span starts at the byte where that record was written, so a run past 4 GiB checks offsets beyond `uint.MaxValue`, not only that the parse ends.
- The large variant writes one record of 4 MiB, an object whose array holds the numbers 0 to 999 over and over, in place of the record that would cross half the size. The records after it are the ones the seed gives without it.
- `ParseManyAsync` holds a record whole until its last byte arrives, so a sample between records would miss the large one. A line of 1 MiB or more is written in 16 parts, with a sample after each.
- The warm-up run also parses the large record, so the large variant's baseline holds what that record's buffers returned to the pools.
- **`json-lines-async` retains little, so its ratio is noisy.** Retention is 20 to 160 KB, and each process settles near about 25 KB or about 150 KB, whatever its size. In three full runs at 10 MB, 1 GB and 4.4 GB, every peak fell in one of those two bands, and the ratio came out 0.77×, 5.61× and 0.19×. Logging every sample of a 300 MB run showed the same peak in each third of the run. So a failure need not mean memory grows; log each sample before treating it as real. The likely cause, not yet shown: whether the buffers' arrays come from `ArrayPool.Shared`'s per-thread caches or are new, as the parse moves between pool threads. For the same reason, the quick check at 2 MB and 20 MB fails this input more often than not.

`MemoryGateTests` checks that every input parses, pins the SHA-256 hash of their first 100,000 characters, and checks where the reader ends and samples. LAY-07's progress notes record each gate run's values, STRM-04's record the JSON document's, and STRM-02's record JSON lines without blocking.

## What is measured

| Class | Plan row | Baselines | Inputs | Reported |
| --- | --- | --- | --- | --- |
| `JsonBenchmarks` | JSON documents, small to large | System.Text.Json (ratio baseline, the ceiling), Parlot, Pidgin, Superpower, Sprache | `small` (1.3 KB), `medium` (104 KB), `large` (1.03 MB) | Mean per document, MB/s of UTF-8 input, allocation |
| `ArithmeticBenchmarks` | Arithmetic expressions | Hand-written recursive descent (ratio baseline), Parlot, Pidgin with `ExpressionParser`, Pidgin with hand-built folds | 1,000 expressions | Mean per expression, allocation |
| `ErrorBenchmarks` | None: [ERR-02](../work/active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)'s evidence that recording failures allocates nothing | The token JSON parser with no failure log (ratio baseline) | `JsonTokens`, and a copy with one comma past the middle made a colon | Mean per input and allocation, with and without a log, for the plain grammar, one with `Named` and `Context`, and the invalid copy; the invalid copy with its error built (`InvalidWithError`); building the reported error alone (`BuildError`) |
| `LayerBenchmarks` | None: [MODE-01](../work/active/07-mode-channels/MODE-01-mode-channels-and-layermodal.md)'s and MODE-05's evidence for what reading a mode channel costs a lexer ([ADR-0041](../work/docs/decisions/0041-a-channel-has-one-reader-below-one-writer-and-a-write-nothing-reads-or-a-pop-of-the-last-mode-throws.md) § Consequences) | The token JSON parser behind a `Layer.Tokens` lexer (ratio baseline) | `small`, `medium` and `large`, as `JsonBenchmarks` | Mean per document and allocation, behind that lexer and behind two one-mode `Layer.Modal`s with the same item parser: `Modal`, whose modes are an `int`, and `ModalUnion`, whose modes are a union ([MODE-05](../work/active/07-mode-channels/MODE-05-mode-payloads.md), [ADR-0043](../work/docs/decisions/0043-layer-modal-plans-each-mode-value-and-keeps-the-64-most-recent.md) trial T1) |
| `LoweringBenchmarks` | None: [INT-05](../work/active/03-interpreter/INT-05-interpreter-lowering-rewrites.md)'s and [INT-06](../work/active/03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md)'s evidence for each lowering rewrite | The same grammar lowered without the rewrite (ratio baseline, one per category) | `CSharp` (100 KB of generated C#-like text, for the 138-rule `Longest` lexer in `LongestLexers`), `Digits` (100 KB of digits), `Json` (the medium JSON document, 104 KB, also tokenized by the 12-rule `Longest` lexer) and `JsonTokens` (it lexed to kinds by a hand-written lexer), `Strings` (100 KB of quoted strings, long runs), `Words` (100 KB of words and spaces, short runs) | Mean per input, allocation; the `DfaLower…` categories time lowering itself |

Every run uses the memory diagnoser.

`LoweringBenchmarks` decides no gate. It runs interpreter plans directly, through `InternalsVisibleTo`, until INT-08 adds the public `Parse`. Each category compares one rewrite on and off, with every other rewrite on in both. A rewrite stays if the median of three runs is at least 5% faster on its target and no other category is more than 5% slower. The lexer categories (`DfaCharTokens`, `DfaJsonLexer`, `DfaCSharpLexer`) return a fingerprint of each token's end and value, and `BaselineAgreementTests` checks that the compiled and interpreted lexers give the same one.

`FourKeysSkipped` and `FourKeysLast` measure dispatch at its threshold of four keyed alternatives ([INT-05](../work/active/03-interpreter/INT-05-interpreter-lowering-rewrites.md)). In the first, the keyed alternatives come first and never match, which is dispatch's best case. In the second, they come after the alternatives that match, so dispatch skips nothing and only adds its cost. The second is slower by design; re-measure both after changing `DispatchPlan`.

## Keeping the baselines comparable

- **Same result.** Every JSON baseline builds the same `JsonValue` tree, and every arithmetic baseline evaluates to the same `double`. `tests/Creole.Benchmarks.Tests` checks each one against the reference on every input, so a baseline that skips work fails the build.
- **Same input.** Inputs are generated from fixed `System.Random` seeds, and their SHA-256 hashes are pinned in the tests. Changing a generator changes every number, so it fails a test until the hashes are updated on purpose. A seeded `Random` is not guaranteed to give the same sequence on every .NET version; if an upgrade changes it, the same test fails, and results from before and after are no longer directly comparable.
- **Idiomatic grammars.** Each baseline uses its library's own tools where they exist: Parlot's `Terms`, `LeftAssociative` and `Unary`; Pidgin's `ExpressionParser`; Superpower's tokenizer followed by token-list parsers. Numbers are parsed with `double.Parse` where the library has no float parser of its own.

Known differences:

| Baseline | Difference |
| --- | --- |
| System.Text.Json | Reads UTF-8 bytes; the combinator baselines read a `string`. |
| Parlot | Its string literal rejects the JSON `\/` escape, so the inputs leave that escape out. `Separated` fails on an empty list unless given a minimum of zero, so the grammar passes `min: 0`. |
| Pidgin | Arithmetic has two Pidgin baselines. `Pidgin` uses `ExpressionParser`, the idiomatic choice; `PidginFolds` folds `Many` by hand and is faster. The Phase 0 gate uses the faster one. |
| Creole | Arithmetic uses `Expression.Build`, Creole's own tool; the Phase 0 gate compares it with `PidginFolds`. The JSON grammar is built like the Pidgin one: a string is a list of unescaped runs and escapes, concatenated. `Parse` runs with its depth limit on (the default, 1,000), while Parlot runs with its own limit off (its default) ([ADR-0020](../work/docs/decisions/0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md)). |
| Parlot | The harness pins Parlot 2.0.0 and measures its interpreted (Fluent) parsers, which the Phase 0 gate uses ([ADR-0003](../work/docs/decisions/0003-numeric-gate-thresholds.md)). 2.0.0 was the fastest stable release measured: faster than 1.5.12 on medium and large JSON (0.86–0.88×) and level elsewhere, and level with 1.6.0 within noise ([research note](../work/docs/research/2026-10-24-parlot-2-0-baselines-and-generator.md)). Parlot 2.0 removes `Compile()`. Its generated parsers, which the Phase 3 gate uses, join with [BENCH-03](../work/active/13-benchmarks/BENCH-03-phase-3-speed-gate.md). |

## Adding a benchmark

Put the inputs in `Inputs/`, generated from a fixed `Random` seed, and pin its hash in `BaselineAgreementTests`. Put each library's grammar in a folder per input language (`Json/`, `Arithmetic/`), add an agreement test for it, then add a `[Benchmark]` method. To report MB/s for a new class, give `ThroughputColumn` its input size.
