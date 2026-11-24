# ADR-0038: Pipelines end in a parser, run again whole with fresh buffers, give each `ParseMany` item the diagnostics of the steps it reads, parse bytes in memory, and name their source without a registry

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-11-24 |
| Cards | [LAY-05](../../active/06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md), [STRM-02](../../active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md), [STRM-03](../../active/08-streaming/STRM-03-streaming-diagnostics.md), [ERR-07](../../active/04-errors-and-recovery/ERR-07-lexer-recovery-and-cascade-suppression.md), [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md), [SPAN-07](../../active/05-source-spans/SPAN-07-embedded-languages.md) |
| Whitepaper | [§ Running pipelines](../../../whitepaper.md#running-pipelines), [§ Sources](../../../whitepaper.md#sources), [§ Failure positions](../../../whitepaper.md#failure-positions), [§ Results](../../../whitepaper.md#results), [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input), [§ Design choices](../../../whitepaper.md#design-choices) |
| Research | [Phase 0 review](../research/2026-10-25-phase-0-review.md), finding 25. The code read for this record is cited where it is used. |
| Supersedes | — (refines [ADR-0027](0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md) point 4 for `ParseMany` and settles the question it left to LAY-05; its other points stand) |

## Context

LAY-05 builds the public API that runs layers. Its scope leaves five questions open, and the whitepaper's sketch leaves a sixth.

1. **The second run** ([ADR-0027](0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md) point 4). A pipeline over in-memory input runs first without failure logs, and again with them if it fails. Does the second run reuse the items already buffered, or produce them again?
   - A layer keeps one failure log across all its steps, and reports its furthest failure across them ([TokensLayer.cs](../../../src/Creole/Layers/TokensLayer.cs), line 8; [LAY-03](../../active/06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md) Outcome). A step can record a failure at or past the place where the next step starts, as a `Longest` alternative that loses does.
   - The first run releases segments behind the reader of each buffer ([`StreamBuffer<T>.Release`](../../../src/Creole/StreamBuffer.cs)). Items from position 0 are gone by the time it fails.
   - A run that ends in an exception leaves the span holds its span combinators took ([SPAN-04](../../active/05-source-spans/SPAN-04-span-combinators.md)).
2. **`ParseMany` and failure logs.** ADR-0027 point 4 says a pipeline over in-memory input "runs again whole". `ParseMany` over a string is such a pipeline. Running every earlier item again for each failed item costs time quadratic in the input when many items fail.
3. **Diagnostics raised past an item's end.** Under `ParseMany`, a lexer can raise a diagnostic while producing lookahead past the end of an item. [ADR-0030](0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 4 leaves to LAY-05 and STRM-02 which item's result holds it. Point 1 of that record already ties each diagnostic to the step that raised it. No layer can raise a diagnostic yet: that waits for `Recover` (ERR-05), lexer recovery (ERR-07) or `Embed` (SPAN-07).
4. **Bytes in memory.** [§ Sources](../../../whitepaper.md#sources) gives byte sources only `ParseManyAsync` (STRM-02). `Layer.Utf8()` is public ([SPAN-06](../../active/05-source-spans/SPAN-06-byte-sources-and-the-decoding-layer.md)) but nothing public runs it. If a pipeline from bytes in memory gets `Parse`, its result needs a text to render against ([ADR-0024](0024-a-parses-diagnostics-hold-their-source-text.md) points 1 and 5).
5. **The source registry.** `SourceRegistry` has no producer. `Parse` always uses `SourceId.Unnamed`, and a source's name is held both in the registry and in `SourceText.Name` (Phase 0 review, finding 25). The whitepaper never names the registry. Only [docs/concepts/spans.md](../../../docs/concepts/spans.md) and [docs/concepts/errors.md](../../../docs/concepts/errors.md) describe it.
6. **The shape of `ParseMany`.** [§ Running pipelines](../../../whitepaper.md#running-pipelines) sketches two shapes. `Pipeline.From(lexer).Then(Document)` is a `Pipeline<char, JsonValue>` that `Parse` runs. `Pipeline.From(lexer).ParseMany(Entry, reader)` takes its item parser at the call.

The maintainer chose forks 3 to 6 in conversation on 2026-11-24, each time taking the recommended option. Forks 1 and 2 turn on the facts above.

## Options

### Fork 1. What the second run over in-memory input reads

#### 1A. Produce every item again, in fresh buffers (recommended)

The second run opens every layer again over the caller's input, with new buffers and failure logs.

- **For:**
  - Every layer's error is the one a single run with a log gives, which is what ADR-0027's test holds `Parse` to.
  - The first run can release segments as it goes, so a successful parse keeps the bounded memory LAY-01 and LAY-09 give it.
  - No span hold, pin or layer state from the first run survives into the second.
  - [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input) already bounds this cost at about two and a half times a successful parse.
- **Against:** every layer runs twice on a failed parse, not only the last.

#### 1B. Reuse the items already buffered

The first run keeps every item, and the second run reads them again. Only the last layer runs again.

- **For:** a failed parse whose last layer failed costs one extra run of that layer instead of every layer.
- **Against:**
  - The first run could release nothing, so every successful parse would hold every item of every layer, each with its 32-byte slot. The first run cannot know it will fail.
  - When a layer below the last one fails, its second run must still start from position 0, or its error loses the failures its earlier steps recorded. 1B then helps only when the last layer fails.
  - Each reused buffer must drop the span holds left by a run that ended in an exception.

### Fork 2. Failure logs under `ParseMany`

#### 2A. `ParseMany` records as it goes, over any source (recommended)

`ParseMany` is a streamed parse whatever its source, so it follows ADR-0027 point 4's rule for streams. Each item starts with no furthest failure (ADR-0030 point 7).

- **For:**
  - Linear time however many items fail.
  - One rule for text and readers. STRM-02 builds the same driver for `ParseManyAsync`.
- **Against:** every item pays the recording cost ADR-0027 measured at 1.16–1.44×, even over a string.

#### 2B. Run again whole, as ADR-0027 point 4 literally says

- **Against:** quadratic when many items fail, such as a log file with one bad line in ten.

#### 2C. Run the failed item again from its start, with logs

- **For:** the success path stays free of recording.
- **Against:** it needs a bookmark across every layer at each item's start, which is STRM-02's restart protocol. ADR-0027 point 4 already leaves this to a STRM-02 measurement.

### Fork 3. Which `ParseMany` item holds a diagnostic raised past an item's end

#### 3A. By the step that raised it, read in source order (chosen)

A diagnostic from a layer below the parser belongs to the first item that reads input of the step that raised it. A diagnostic raised during lookahead past an item's end waits for the next item, as trivia between items does under [ADR-0036](0036-trivia-is-the-source-between-items-read-with-a-zero-width-parser.md).

The maintainer chose this as "by span": a diagnostic whose span starts before the item's end. Comparing spans alone is ambiguous in one case. A zero-width diagnostic exactly at an item's end could be the lexer's report of a missing closing quote in the item's last token, or of something missing before the next item. Both have the same span. The step that raised the diagnostic tells them apart, and ADR-0030 point 1 already stores it with each diagnostic. Away from an item's ends, the two agree.

- **For:**
  - Each item reports problems in its own text. An item whose own text is clean comes out `Parsed`.
  - It needs no state beyond what ADR-0030 already keeps.
- **Against:** the driver maps each item's end down through the layers to find which steps it read.

#### 3B. The item that was running

- **For:** simplest; no mapping.
- **Against:** item *k* reports item *k* + 1's bad text and comes out `Recovered`, and item *k* + 1 can come out `Parsed` despite it.

#### 3C. Defer to STRM-02

- **For:** nothing can raise such a diagnostic before ERR-05, ERR-07 or SPAN-07.
- **Against:** LAY-05 would build the per-item bags with no rule, and the rule is cheap to state now.

### Fork 4. Bytes held in memory

#### 4A. `Parse` and `ParseMany` over bytes in memory; the result holds the decoded text (chosen)

- **For:**
  - `Layer.Utf8()` gets a public runner now, and in-memory bytes keep byte offsets.
  - Rendering shows the full format, as for text.
- **Against:** a failed parse decodes the input once more to build the text it holds.

#### 4B. The same API, but the result holds the bytes and decodes them when first rendered

- **For:** no decoding unless something renders.
- **Against:** `SourceText` would need a lazily decoded form. A failed parse already runs twice, so one more decode changes its cost little.

#### 4C. Not now (the whitepaper's position)

- **For:** no new API.
- **Against:** `Layer.Utf8()` stays public with no public way to run it until STRM-02, and callers with bytes in memory lose byte offsets.

### Fork 5. The source registry

#### 5A. Remove the registry (chosen)

Pipelines name their source with `source:`, as `Parse` does, and use `SourceId.Unnamed`.

- **For:**
  - Finding 25's two problems go: no type without a producer, and the name is held once, in `SourceText`.
  - `Position` over a streamed input with no items already gives the right source, `Unnamed` ([ADR-0035](0035-span-combinators-commit-like-sequences-and-hold-their-spans.md) point 4).
- **Against:** a caller that merges diagnostics from several parses cannot tell their sources apart. ADR-0024's "What would change this" names the remedy: a `Parse(SourceText)` overload, which would now also need a way to issue ids.

#### 5B. Keep it; pipelines take a `SourceText`

`registry.Add(name, text)` returns the `SourceText`, so the name is held once, and `Parse(SourceText)` overloads carry its id into spans.

- **For:** callers can merge diagnostics across files now.
- **Against:** more public API for a need no caller has shown.

#### 5C. Defer to the API review

- **Against:** an unused public type stays, and finding 25 stays open.

### Fork 6. The shape of `ParseMany`

#### 6A. The parser is part of the pipeline (chosen)

`Pipeline.From(lexer).Then(Entry)` is a `Pipeline<char, LogEntry>`. `Parse` runs `Entry` once, and `ParseMany` runs it repeatedly until the input ends.

- **For:** one pipeline type and one way to build it.
- **Against:** running other item parsers over the same layers means building another pipeline, which is cheap: pipelines are immutable values.

#### 6B. The item parser is passed at the call (the whitepaper's sketch)

- **For:** one chain of layers can run different item parsers.
- **Against:** two shapes for one idea, and a layers-only pipeline type that only `ParseMany` uses.

## Decision

Options **1A, 2A, 3A, 4A, 5A and 6A**. Accepted by the maintainer on 2026-11-24.

**1. Building a pipeline.**

- `Pipeline.From(layer)` starts a pipeline, `Then(layer)` adds a layer, and `Then(parser)` ends it in an immutable `Pipeline<TIn, T>`. The name of the type `Pipeline.From` returns is a build-plan call.
- A pipeline can be shared across threads. Each call owns its buffers, logs and state.

**2. Running a pipeline.**

| Input | Calls | Slices | The result holds |
| --- | --- | --- | --- |
| `string`, `ReadOnlyMemory<char>` | `Parse`, `ParseMany` | Views of the caller's memory ([ADR-0013](0013-slices-view-in-memory-input-and-copy-streamed-input.md)) | The text, named by `source:` ([ADR-0024](0024-a-parses-diagnostics-hold-their-source-text.md) point 1) |
| `TextReader` | `Parse`, `ParseMany` | Copies | Captured lines once STRM-03 builds them; until then, nothing, so errors render one line each |
| `ReadOnlyMemory<byte>` | `Parse`, `ParseMany` | Copies, since decoded characters are not the caller's memory | Point 5 |
| `PipeReader`, `Stream` | `ParseManyAsync` (STRM-02) | Copies | Captured lines (STRM-03) |

- `Parse` runs the pipeline's parser once. As for one layer, it succeeds if the parser does, even if input remains.
- `ParseMany` is lazy. It runs the parser at the start of the input and again after each item until no input item remains for the parser to read. An item that consumes nothing is held to the rule `Layer.Tokens` holds its item to (CRL0003). Until ANA-03 adds that check, such an item throws `GrammarException`, as an empty `Tokens` step does.
- A failed item is the last result. Nothing says where the next item would start, so a grammar that continues past bad items catches them with `Recover` (ERR-05).
- The root gives character *i* the span \[*i*, *i* + 1) in `SourceId.Unnamed`, or byte *i* over bytes. Its `InputStart` is offset 0. Its `InputEnd` is the input's length in memory, or the number of items read once a reader ends.

**3. Failure logs and the second run.**

- `Parse` over input in memory, text or bytes, runs once without failure logs. If that run fails, a layer throws, or the depth limit or the stack check ends it, `Parse` runs the whole pipeline again from the start with every layer recording, and returns that run's result (ADR-0027 points 1 and 2).
- The second run opens every layer again with new buffers. It reuses no item, hold or state from the first run, which returns its buffers' segments to their pools as any finished run does.
- `Parse` over a `TextReader` records as it goes, as ADR-0027 point 4 says of streams.
- `ParseMany` records as it goes over every source, and each item starts with no furthest failure (ADR-0030 point 7). This refines ADR-0027 point 4: under `ParseMany`, the in-memory rule does not apply. STRM-02 may measure running a failed item again from its bookmark, as ADR-0027 point 4 already allows, and adopt it for every source if it pays.
- A layer whose step fails ends the parse with that layer's error ([LAY-03](../../active/06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md) Outcome). Under `ParseMany`, the item running when it fails gets a `Failed` result with that error and is the last result. If the driver was looking for the next item, that next item gets the result.

**4. Which `ParseMany` item holds a diagnostic.**

- The parser's own diagnostics belong to the item it is parsing.
- A diagnostic from a layer below the parser belongs to the first item that reads input of the step that raised it.
  - An item reads the items it consumes in the last buffer. Below that, it reads the input of the steps that produced what it read, layer by layer.
  - So a diagnostic raised by a step that produced lookahead past an item's end belongs to the next item. That holds whether the item's parser or the driver pulled the lookahead.
  - So does a diagnostic raised by a step that emitted nothing between two items, as trivia between items is the next item's.
- Stated as positions:
  - Item *k* ends at position *e* in the buffer the parser reads. A step of the layer that fills that buffer belongs to item *k* if fewer than *e* outputs came before it, and no earlier item took it. Steps after that belong to later items, including those that produced lookahead and those that emitted nothing.
  - One layer down, item *k* ends where the first step of the layer above that belongs to a later item began reading. If no such step has run, it ends at that layer's input position. The same test then applies there, and so on down to the root.
  - An item takes the diagnostics of every step that belongs to it.
  - The build plan chooses how the driver tracks where those steps began. A slot's `StepStart` alone is not enough, because a step that emitted nothing has no slot.
- If the input ends with diagnostics that no item took, `ParseMany` yields one more result: `Failed`, with those diagnostics.
- STRM-02's restart withdraws what ADR-0030 point 7 says. A diagnostic that waits for the next item was raised before the next item's bookmark, so a restart of that item keeps it.

**5. Bytes held in memory.**

- A pipeline whose input is bytes gets `Parse` and `ParseMany` over `ReadOnlyMemory<byte>`.
- If its first layer is `Layer.Utf8()`, a `Recovered` or `Failed` result holds a `SourceText` whose text is what `Encoding.UTF8` decodes from the bytes after any leading byte-order mark, named by `source:`.
  - It is decoded once per call, when the first result that holds it is built.
  - Its character offsets match the decoder's up to the first invalid sequence. There the decoder's error ends the parse, with a zero-width character span where `Encoding.UTF8` puts its first U+FFFD (SPAN-06). So every error renders at the right line and column.
- A pipeline from bytes whose first layer is not `Layer.Utf8()` holds no text, so its errors render one line each, as `Parse` over items does.

**6. Sources.**

- `SourceRegistry` is removed.
- `SourceId` stays as the source of every span. `SourceId.Unnamed` is its only public value. The internal constructor stays, for a later design that issues ids.
- `SourceText`'s public constructors take the text, as a `string` or `ReadOnlyMemory<char>`, and an optional name. The constructors that take an id become internal.
- Every pipeline's root is `SourceId.Unnamed`, so `Position` over a streamed input with no items needs no change.

**7. Results.** A pipeline's result holds its root source as point 2's table says, so Example A's `result.Diagnostics.Render()` shows the full format. That is ADR-0024 points 1 and 5.

Deciding reasons:

- **1A:** it is exact, and 1B would cost every successful parse its bounded memory for a gain only when the last layer fails.
- **2A:** it is linear and needs nothing STRM-02 has not built. 2C stays open to STRM-02's measurement.
- **3A, 4A, 5A, 6A:** the maintainer's choices. 3A is stated through the step that raised each diagnostic because spans alone cannot place a zero-width diagnostic at an item's end.

What would change this:

- **1A:** a measurement where failed pipeline parses dominate and the last layer is most of their cost.
- **2A:** STRM-02 finding that an item can run again from its bookmark at a measured gain.
- **5A:** a caller that merges diagnostics from several parses, or a layer that reads more than one source, such as one that follows include files.
- **6A:** grammars that want several item parsers over one costly chain of layers.

## Consequences

- **Easier:**
  - One pipeline type, one way to name a source, and one rule for failure logs in each kind of call.
  - `Layer.Utf8()` has a public runner, and SPAN-06's tests can render through `Parse`.
- **Harder:**
  - A failed pipeline parse runs every layer twice.
  - The `ParseMany` driver maps each item's end down through the layers.
- **Ruled out:**
  - Reusing buffered items across `Parse`'s two runs.
  - Running every earlier item again under `ParseMany`.
  - Ids for sources other than `Unnamed`, until a caller needs them.
- **Risks and how they are watched:**
  - **The two runs disagree.** LAY-05's build tests that a failed pipeline's error equals a single run's with every layer recording, as ADR-0027's test does for one layer, including a lexer that fails after an earlier step recorded a failure past the failing step's start.
  - **Diagnostics in the wrong item.** The first card that lets a layer below the parser raise a diagnostic tests point 4. That is ERR-07 for lexer recovery, or ERR-05 or SPAN-07 if either comes first. The tests: a diagnostic raised in lookahead past an item's end goes to the next item, a zero-width one at an item's end from that item's last step stays with it, and one left at the end of input comes out in a last `Failed` result.
  - **The decoded text drifts from the decoder.** LAY-05 tests that errors from `Parse` over bytes render at the lines and columns SPAN-06's criterion test worked out by hand.

## Changes made

- **Whitepaper.**
  - [§ Running pipelines](../../../whitepaper.md#running-pipelines): the `ParseMany` line runs the pipeline's parser, `Pipeline.From(LogLexer.Lexer).Then(LogGrammar.Entry).ParseMany(reader)`. A new paragraph says what `Parse` and `ParseMany` run, that a failed item is the last result, which item holds a diagnostic, and that a pipeline names its source with `source:` and has the root `SourceId.Unnamed`.
  - [§ Sources](../../../whitepaper.md#sources): a `ReadOnlyMemory<byte>` row, and which text a parse over bytes in memory holds.
  - [§ Failure positions](../../../whitepaper.md#failure-positions): `Parse`, not every parse, records only in a second run over in-memory input. A pipeline's second run uses fresh buffers, and `ParseMany` records as it goes.
  - [§ Results](../../../whitepaper.md#results): a pipeline's result holds its root source.
  - [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input) and [§ Design choices](../../../whitepaper.md#design-choices): "a failed parse" and "a parse" over in-memory input became `Parse`, since `ParseMany` does not run twice.
  - Checked and unchanged: Example A, which already uses `Then(Document)` and `source:`; § The restart protocol; § Embedded languages; § Comparison with existing .NET libraries. The whitepaper never named `SourceRegistry`.
- **Cards.**
  - [LAY-05](../../active/06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md): the scope lines that asked for a decision now state it, and the card moves to Building.
  - [STRM-02](../../active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md): its open question is settled; `ParseManyAsync` follows points 2 to 4.
  - [STRM-03](../../active/08-streaming/STRM-03-streaming-diagnostics.md): `Parse` and `ParseMany` over a `TextReader` hold captured lines.
  - [ERR-07](../../active/04-errors-and-recovery/ERR-07-lexer-recovery-and-cascade-suppression.md): point 4's tests. [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md) and [SPAN-07](../../active/05-source-spans/SPAN-07-embedded-languages.md) run them instead if either is first to let a layer below the parser raise a diagnostic.
- **Not yet.** `SourceRegistry`'s removal, `SourceText`'s constructors, docs/concepts/spans.md and docs/concepts/errors.md change with the code, in LAY-05's build.
