# ADR-0003: Numeric thresholds for every roadmap gate, with speed set against both Pidgin and Parlot

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-18 |
| Cards | [FND-06](../../active/01-foundations/FND-06-fix-numeric-gate-thresholds.md), [BENCH-01](../../active/13-benchmarks/BENCH-01-phase-0-speed-gate.md), [BENCH-03](../../active/13-benchmarks/BENCH-03-phase-3-speed-gate.md), [LAY-07](../../active/06-layers-and-pipelines/LAY-07-three-layer-demo-and-bounded-memory-check.md), [RSLN-07](../../active/10-roslyn-analyzers/RSLN-07-grammar-bug-corpus.md), [GEN-08](../../active/11-source-generator/GEN-08-differential-engine-agreement-tests.md), [INC-07](../../active/15-incremental-reparsing/INC-07-incremental-re-parsing-gate.md), [FUS-01](../../active/16-layer-fusion/FUS-01-fusion-feasibility-measurement.md), [FUS-05](../../active/16-layer-fusion/FUS-05-fusion-gate.md), [RES-01](../../active/17-resumable-async/RES-01-resumable-async-measurement.md), [RES-02](../../active/17-resumable-async/RES-02-resumable-generated-parsers.md) |
| Whitepaper | [§ Roadmap](../../../whitepaper.md#roadmap), [§ Performance design and benchmark plan](../../../whitepaper.md#performance-design-and-benchmark-plan), [§ Risks](../../../whitepaper.md#risks) |
| Research | [What numbers should the roadmap gates use?](../research/2026-10-18-numeric-gate-thresholds.md) |
| Supersedes | — |

## Context

§ Roadmap states its gates in words: "speed near Pidgin", "memory stays bounded", "classic grammar bugs caught", "engines agree", and for 1.x "each ships behind its own gate", with fusion needing "a measured win". It says the numbers will be fixed once the benchmark harness exists. FND-05 built the harness, and the research note measured it and surveyed how other projects set thresholds. Four findings shape the choice:

- On the maintainer's laptop, time per benchmark moves by up to 1.3× between identical runs, including the Pidgin baseline. Allocations are identical run to run.
- Parlot is 5–7× faster than Pidgin on JSON and about 4× on arithmetic, so a Pidgin-only bar does not test the aim in § Risks to "match Parlot on speed".
- No parser library publishes a numeric competitor gate, so there is nothing to copy. The numbers below are judgment informed by measured noise.
- Parlot's source generator is preview-only, and Parlot 2.0 removes `Compile()`.

Phase 0 speed is set against both Pidgin and Parlot; the interpreter must be within 1.5× of Parlot; the generated engine must match Parlot; one record covers every gate; the gates run on the maintainer's laptop with pinned settings.

## Options

### A. Keep qualitative gates (the whitepaper's current position)

Each gate card judges "near" or "measured win" when it runs.

- Pros: no work now; no risk of picking a wrong number before engines exist.
- Cons: a gate decided after the measurement is not a gate. FND-06's acceptance criterion requires numbers. "Near Pidgin" is also the wrong bar (finding above).

### B. Numeric gates, speed set against Pidgin only

As C below, but Phase 0 compares with Pidgin only, and Parlot stays an aim in § Risks.

- Pros: Pidgin is the closest design (commit semantics, generic tokens), and the bar is easy to pass.
- Cons: the interpreter could ship 5× slower than Parlot and pass. The strategic risk in § Risks goes untested.

### C. Numeric gates, speed set against Pidgin and Parlot (chosen)

Every gate gets a threshold (table below). Phase 0 speed has a bar against each library; Phase 3 adds a speed gate for the generated engine against Parlot.

- Pros: tests the strategic risk directly; covers every gate, which FND-06 requires; thresholds sit above measured noise.
- Cons: harder to pass. A Phase 0 miss against Parlot delays Phase 1 unless the gap is recorded as understood (BENCH-01 already allows that). Parlot's numbers will move as Parlot changes.

### D. Absolute thresholds (MB/s, nanoseconds per expression)

- Pros: simple to state.
- Cons: tied to one machine and runtime version, and they go stale when either changes. Ratios measured in the same run cancel machine drift. Rejected.

## Decision

Option C. Every gate in § Roadmap uses the threshold below.

### How speed gates are measured

- **Machine.** The maintainer's laptop with pinned settings: on AC power, no other heavy load. benchmarks/README.md records the settings and the machine.
- **Runs.** BenchmarkDotNet default job. Creole and its baselines run in the same process invocation. The suite runs three times, and each benchmark's ratio is the median of its three ratios.
- **Summary.** "Geometric mean" is the geometric mean of those median ratios over the gate's benchmarks.
- **Baselines.** Pidgin means `PidginFolds` for arithmetic and `Pidgin` for JSON. Parlot means the fastest stable Parlot release's interpreted (Fluent) parser. When `Parlot.SourceGenerator` ships a stable release, the Phase 3 gate switches to Parlot's generated parsers.

### Thresholds

| Gate | Benchmarks | Threshold |
| --- | --- | --- |
| Phase 0 speed | Interpreter, character-level JSON (small, medium, large) and arithmetic | Time: geometric mean ≤ 1.0× Pidgin, and no benchmark above 1.25× Pidgin; geometric mean ≤ 1.5× Parlot. Allocations: ≤ Pidgin's on every benchmark |
| Phase 1 memory | Example B (three layers) and a JSON-lines stream, generated at 10 MB, 100 MB and 1 GB | Peak retained managed heap at 1 GB ≤ 1.5× the peak at 10 MB. Retained heap is sampled after a forced full blocking GC at fixed item intervals, minus a warm-up baseline, in a separate process per size |
| Phase 2 analysis | One corpus grammar per CRL error and warning in § Static analysis that should trigger it, and one near miss that should not | 100% of triggering grammars report their diagnostic at build time; 0 near misses do |
| Phase 3 agreement | Random grammar and input pairs from a fixed seed | 0 mismatches across 10,000 pairs on every CI run. A nightly run with fresh seeds files issues and does not block |
| Phase 3 speed | Generated engine, the Phase 0 benchmarks | Time: geometric mean ≤ 1.0× Parlot |
| Incremental re-parsing (1.x) | At least 500 single-character edits replayed on a file of about 300 KB, and on a 10× copy, in JSON and Example B | Median re-parse ≤ 2% of a full parse; p99 ≤ 100 ms; median on the 10× file ≤ 2× the median on the base file |
| Layer fusion (1.x) | Fused against the best unfused generated code: JSON, arithmetic, Example B | Geometric mean ≥ 1.2× faster, and no benchmark slower |
| Resumable async (1.x) | Resumable generated parser against the restart driver on JSON lines | ≥ 1.2× throughput when the median record is at least one pipe segment; ≥ 0.95× when records are smaller |

Deciding reason: speed is Parlot's strength (§ Risks), so a speed gate that ignores Parlot measures the wrong thing. The interpreter gets room (1.5×) because Parlot interpreted is close to System.Text.Json, and the generated engine closes the gap. The other thresholds are set above measured noise and tied to what each gate is for.

What would change this: if Phase 0 shows the interpreter's design (inspectable plans) cannot get within 1.5× of Parlot without giving up analysis, the bar should move in a new record, not quietly. If a quiet machine shows much lower noise, the per-benchmark bound can tighten.

## Consequences

- Gate cards apply a fixed number instead of judging after the fact.
- Phase 0 is harder. A miss against Parlot needs the gap explained and recorded (BENCH-01's criterion already allows this) or a new record.
- Parlot is a moving target. Each gate run records the Parlot version. When `Parlot.SourceGenerator` is stable, the Phase 3 bar gets harder, since Parlot reports its generated parsers 8–23% faster than its interpreted ones.
- Gate results depend on one laptop. Hosted runners only catch regressions of 10% or more and do not decide gates.
- The memory gate needs its own harness, because BenchmarkDotNet's memory diagnoser reports allocated, not retained, bytes.
- The incremental gate tests growth with file size, which catches the linear memo-shift cost GPeg found in Dubroy and Warth's method.
- Thresholds are judgment, not measured from comparable projects. Each gate card records the measured value, not just pass or fail, so a later record can revise a number with evidence.

## Changes made

- Whitepaper:
  - § Roadmap: the intro's fusion sentence gives the 1.2× bar; the gate column gives each phase's number; the sentence promising numbers later is replaced by a paragraph on how speed is measured, a table of every threshold and the rule for which Parlot variant is the baseline. Links this record.
  - § Performance design and benchmark plan: the targets paragraph states the Phase 0 and Phase 3 bars; § Benchmark plan says how gate runs work and that the memory gate needs its own test.
  - § Layer fusion and § Why not async combinators: point at their gates.
  - § Risks: the fusion row gives the 1.2× bar; the Parlot row gates speed on Parlot instead of only aiming to match it.
- Cards:
  - BENCH-01, LAY-07, RSLN-07, GEN-08, INC-07, FUS-01, FUS-05, RES-01 and RES-02: scope and criteria now state this record's thresholds.
  - New: BENCH-03 Phase 3 speed gate, blocked by FND-06 and BENCH-02, blocking REL-05.
- benchmarks/README.md: a "Gate runs" section with the machine, settings and method; the Parlot and Pidgin notes now say which baseline the gates use.
