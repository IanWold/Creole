# ADR-0027: A parse over in-memory input runs without a failure log, and runs again with one when it fails

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-26 |
| Cards | [BENCH-01](../../active/13-benchmarks/BENCH-01-phase-0-speed-gate.md), [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md), [LAY-05](../../active/06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md), [STRM-02](../../active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md), [GEN-11](../../active/11-source-generator/GEN-11-runtime-support-surface-for-generated-code.md), [BENCH-02](../../active/13-benchmarks/BENCH-02-full-benchmark-suite.md) |
| Whitepaper | [§ Design choices](../../../whitepaper.md#design-choices), [§ Failure positions](../../../whitepaper.md#failure-positions), [§ Recovery](../../../whitepaper.md#recovery), [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input) |
| Research | [Phase 0 review](../research/2026-10-25-phase-0-review.md), findings 1, 3 and 27; measurements in [BENCH-01](../../active/13-benchmarks/BENCH-01-phase-0-speed-gate.md)'s progress notes of 2026-10-26 |
| Supersedes | — |

## Context

`Parse` gives every parse a failure log ([`Parsing.cs`](../../../src/Creole/Parsing.cs)). Scans, loops, optional parts, dispatch gaps and expression operators record into it as they stop, so a successful parse records as much as a failed one. Only a failed parse reads the log, to build its one error ([ADR-0021](0021-how-failures-are-recorded-and-merged.md)).

Recording costs time. On the gate grammars the JSON inputs make about one record per two characters, half of them moving the furthest failure forward and half merging into it. The arithmetic grammar makes 43 per expression. The Phase 0 review measured recording at 1.16–1.44× (finding 3), and the Phase 0 speed gate misses its Parlot bar ([ADR-0003](0003-numeric-gate-thresholds.md): geometric mean ≤ 1.5×).

The whitepaper says something different from the code:

- [§ Design choices](../../../whitepaper.md#design-choices): "failures record a position and an interned expectation-set handle".
- [§ Failure positions](../../../whitepaper.md#failure-positions): "a buffer index plus an expectation-set handle".
- The code records references to expectation descriptors into reusable arrays, with linear de-duplication (review finding 27). ADR-0021 left the representation to the build.

[ADR-0026](0026-engines-may-skip-unused-values-and-repeat-user-functions.md) lets an engine run a function again and requires decision functions to be deterministic. A parse run a second time therefore takes the same path as the first.

### Measurements

BENCH-01 measured three configurations at `b4dcc38` on the gate machine. Each was given three default-job gate runs, summarized by `scripts/bench-gate.py`. The prototypes for B and C were kept only in a scratch worktree. Both passed all 1,513 tests.

| Configuration | ÷ Pidgin | ÷ Parlot | Creole's time against A |
| --- | ---: | ---: | --- |
| A. As built: always record | 0.33 | 1.93 | — |
| B. No log; on failure, run again with one | 0.26 | 1.53 | 0.73× arithmetic; 0.77×, 0.81×, 0.86× JSON small, medium, large |
| C. Cheaper recording | 0.32 | 1.86 | 0.96–0.97× |

- Allocation was 0.996× Pidgin's on JSON and 0.067× on arithmetic in all three.
- Parlot's own times stayed within 2% across the three batches.
- On the failure path, B was measured against A on the medium JSON document. It took 1.82× as long with the document broken in the middle, and 1.87× with it broken at the end. It allocated 2.00×, because the values the first run built are built again. Recording itself allocates nothing either way.

Why C gains so little: a Stopwatch ablation of the log, not the gate protocol. Calling the log costs about 1% of a parse. Moving the record forward costs 3–8%. Adding each expectation costs the rest, 14–27%, mostly in storing its reference into the log's array; the de-duplication loop is the smaller part. With no expectations added at all, recording still costs 1.03–1.10× over no log. C keeps the store and trims the rest.

An interned expectation-set handle would also store one reference per record. Avoiding that store means recording integer ids, and that needs a global registry from id to descriptor. Such a registry grows without bound when `Bind` builds parsers at each match. That is inference; no handle was prototyped.

### Safety of a run without a log

A run without a log returns the same value and position as a run with one. Every plan reaches the log through `Failures?.`, the marks it saves go only back into the log, and no plan branches on recorded state. The review's critic A found this, and BENCH-01 confirmed it by inspection. Running the parse again with the log reproduces the error, given ADR-0026's deterministic decision functions. Both depend on the input still being there to run again.

### Prior art

FastParse offers `.trace()`, which re-runs a failed parse on the original input to collect fuller error information ([docs](https://com-lihaoyi.github.io/fastparse/), read by a research subagent on 2026-10-26 for ADR-0026). Whether Parlot or Pidgin record anything on a successful parse was not checked; the review left it open.

## Options

### A. Always record (as built)

- **For:**
  - One policy for every kind of input: in memory, streamed or pipelined.
  - A failed parse costs no more than a successful one of the same length.
  - `Recover` (ERR-05) gets the log it needs without special handling.
- **Against:**
  - Every successful parse pays 1.16–1.37× for an error it never builds.
  - The gate misses Parlot at 1.93×.

### B. Run without a log; on failure, run again with one

`Parse` runs the grammar with no failure log. If the run succeeds, that is the result. If it fails, it runs the grammar again from the start with the log, and reports that run's result.

- **For:**
  - Every successful parse is 14–27% faster, with no change in allocation.
  - Errors are unchanged, and so is anything a run without a log returns.
  - About 20 lines, all in `Parsing`.
  - `Named` and `Context` cost nothing on a successful parse.
- **Against:**
  - A failed parse costs up to about twice as much time, and twice the allocation of the values it built.
  - User functions run again on a failed parse. ADR-0026 allows this.
  - It needs input that can be read again from the start. Streamed input past its bookmark horizon cannot be, so streams need their own rule.
  - Anything that needs the log during the run must end the run without a log early. `Recover` is one such case.
  - The gate still misses Parlot, at 1.53×.

### C. Make recording cheaper

Keep one policy and trim the work per record. The prototype resets the message, item and span on an advance only if one was set, and inlines the store. The other candidate was the interned expectation-set handle the whitepaper promises.

- **For:**
  - One policy for every kind of input.
  - No re-run, so no extra cost on failure.
- **Against:**
  - The prototype gained 3–4%.
  - Even without the store, recording costs 1.03–1.10×, so C cannot match B on successful parses.
  - A handle needs an id registry that leaks, or it keeps the store.

### D. B, and C for what still records

Take B, and use C's trimmed log for the re-run and for parses that cannot run again.

- **For:** each failure path is 3–4% cheaper.
- **Against:** more code in the log for a gain only on failures and streams. Nothing measured asks for it yet.

## Decision

**B.** A parse records failures only when it has failed.

1. **In-memory parses.** `Parse` over input held in memory runs the grammar without a failure log. If it succeeds, that is the result.
2. **On failure.** If the run fails, or ends at `MaxDepth` or the stack check, the parse runs again from the start with a failure log, and that run's result is returned.
   - The second run fails the same way for any grammar whose functions follow ADR-0026. The exception is the stack check, which depends on frame sizes that can change between runs ([§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input)). If the second run succeeds, its value is returned.
   - An exception from user code in the first run propagates at once. Nothing runs a second time.
3. **What else needs the log.** A combinator that needs the log during the run ends the run without a log as a failure does, and the parse runs again with the log. `Recover` (ERR-05) is the first: in a run without a log, the first failure it would catch ends that run.
4. **Input that cannot be read again.** This record covers input held in memory. A streamed parse records as it goes, unless STRM-02 shows that an item can run again from its bookmark at a measured gain. A pipeline over in-memory input runs again whole; LAY-05 decides whether the second run reuses items already buffered.
5. **The log itself.** The log keeps its design: references to descriptors that each node creates once, in arrays that each thread reuses. It holds no interned expectation-set handle. C's trimming is not adopted; a measurement that needs it can bring it back.

Deciding reason: B removes nearly all of recording's cost from every successful parse, changes no result and no error, and costs about 20 lines. C cannot reach B's speed even in principle, because the per-record store remains. B's costs fall only on failed parses, and are bounded at about twice a successful parse of the same input.

What would change this:

- Workloads where most parses fail, such as validating input that is usually wrong. A deferred re-run would then help: build the error only when the caller reads it.
- A streaming or pipeline design that cannot re-run, if its recording cost turns out to matter. Then C or D would apply there.
- A future engine change that makes a run without a log observably different from one with it. ADR-0026 and the review's inspection rule that out today.

## Consequences

- **Easier:**
  - The success path no longer pays for errors, so error features such as `Named`, `Context` and dispatch gaps are free on success.
  - The gate's Parlot ratio falls from 1.93× to 1.53×.
- **Harder:**
  - A failed parse costs up to about twice as much, and the docs must say so.
  - `Recover` must end a run without a log at its first catch.
  - Generated code must run with no log as well as with one. That is [GEN-11](../../active/11-source-generator/GEN-11-runtime-support-surface-for-generated-code.md)'s runtime surface.
  - A pipeline's re-run produces upstream items a second time. G3's "each upstream item is produced once" holds within a run, and the re-run is a second run.
- **Ruled out:**
  - The interned expectation-set handle, as § Design choices and § Failure positions describe it.
  - Trimming the log, until a measurement asks for it.
- **Risks and how they are watched:**
  - **The two runs disagree.** BENCH-01's build adds a test that `Parse`'s error equals the error of a single run with the log, across the error tests' grammars and inputs.
  - **Failure-heavy workloads.** The whitepaper's benchmark row for inputs with many errors ([BENCH-02](../../active/13-benchmarks/BENCH-02-full-benchmark-suite.md)) reports time on failing inputs.
  - **Untrusted input.** A failing input costs at most about twice a successful one of the same length, so the bound stays linear.
- **The gate.** B alone does not meet the Phase 0 Parlot threshold. BENCH-01's second criterion requires the remaining gap, 1.53× against 1.5×, to be closed or explained. It sits in JSON, at 1.54–1.80×; arithmetic is at 1.18×.

## Changes made

- **Whitepaper.**
  - [§ Design choices](../../../whitepaper.md#design-choices): "Lazy errors" says a parse over in-memory input records failures only after it has failed, and describes the log as it is built. The interned expectation-set handle is gone.
  - [§ Failure positions](../../../whitepaper.md#failure-positions): the record holds references to expectations, not a handle. In-memory parses record only in a second run, and streamed parses record as they go.
  - [§ Recovery](../../../whitepaper.md#recovery): in a first run without a log, the first failure `Recover` would catch ends that run.
  - [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input): a new "Failed parses" bullet says a failed parse runs twice and costs up to about twice a successful one.
  - Checked and unchanged: § The error model and § Equivalence, since neither the error nor what engines agree on changes. § The restart protocol is left to STRM-02.
- **Cards.**
  - [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md): `Recover` ends a run without a log.
  - [STRM-02](../../active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md): measure re-running an item before adopting it.
  - [LAY-05](../../active/06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md): in-memory pipelines run again whole, and the card decides whether buffered items are reused.
  - [GEN-11](../../active/11-source-generator/GEN-11-runtime-support-surface-for-generated-code.md): generated code runs with and without a log.
  - [BENCH-02](../../active/13-benchmarks/BENCH-02-full-benchmark-suite.md): the inputs-with-many-errors benchmark reports the failure path.
  - [BENCH-01](../../active/13-benchmarks/BENCH-01-phase-0-speed-gate.md) returns to Building, to build B with its test and docs.
- **Not yet.** `Parsing`, the test that `Parse`'s error equals one run's with the log, [docs/concepts/parsing.md](../../../docs/concepts/parsing.md) and [docs/engines/interpreter.md](../../../docs/engines/interpreter.md) change with the code, in BENCH-01's build.
