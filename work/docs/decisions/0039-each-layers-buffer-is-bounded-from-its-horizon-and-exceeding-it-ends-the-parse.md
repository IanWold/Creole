# ADR-0039: Each layer's buffer is bounded from its horizon, 16,777,216 items by default, and exceeding it ends the parse

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-11-24 |
| Cards | [LAY-06](../../active/06-layers-and-pipelines/LAY-06-buffer-size-limit.md), [LAY-05](../../active/06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md), [LAY-09](../../active/06-layers-and-pipelines/LAY-09-hold-only-the-input-the-reader-can-still-go-back-to.md), [STRM-02](../../active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md), [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md), [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md) |
| Whitepaper | [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input), [§ Guarantees](../../../whitepaper.md#guarantees), [§ Recovery](../../../whitepaper.md#recovery), [§ Equivalence](../../../whitepaper.md#equivalence), [§ The restart protocol](../../../whitepaper.md#the-restart-protocol) |
| Research | — (prior art read for this record by a research subagent on 2026-11-24, cited inline; Parlot's streaming guide at v2.5.0 read first-hand) |
| Supersedes | — |

## Context

[§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input) says "`ParseOptions.MaxBufferedItems` bounds each layer's buffer" and that exceeding it "produces an ordinary diagnostic at the offending span". It does not say what is counted, how the parse stops, what the default is, or where the diagnostic points. [ADR-0020](0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md) answered the same questions for `MaxDepth`.

How buffers hold items today ([LAY-01](../../active/06-layers-and-pipelines/LAY-01-streambuffer-and-pulling-cursor.md), [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md), [ADR-0031](0031-constructs-that-read-their-input-back-pin-their-start.md)):

- **What a buffer holds.** A `StreamBuffer<T>` holds the positions from `Start` to `Filled`. Its reader pulls an upstream step only when it reads past `Filled` (`StreamBuffer.Fill`), so nothing is produced ahead of need. One step may add several items, or none.
- **The horizon.** It is the minimum of the reader's cursor, its floor, its own pin when it produces items, and the restart bookmark. Every one of these belongs to the layer that reads the buffer.
- **Release lags the horizon.** `StreamBuffer.Release` frees whole segments, keeps the segment holding the item before the horizon, and runs only when the reader's cursor enters a new segment or a producing layer starts a step. A buffer therefore holds up to about two segments more than its horizon needs. With 1,024-item segments, that is 2,048 items.
- **A producing layer's pin.** It is the `stepStart` of the oldest output still in the layer's own output buffer (`StreamBuffer.OldestStepStart`), which is the first slot of that buffer's first segment. So it keeps the input behind up to a segment of outputs its reader has already passed. [LAY-09](../../active/06-layers-and-pipelines/LAY-09-hold-only-the-input-the-reader-can-still-go-back-to.md) narrows it to outputs the reader can still go back to.
- **A lower layer keeps input for the layer above.** A parser's `Lookahead` over *N* tokens keeps those tokens, and the lexer keeps the characters of all *N*, so that any of them can be produced again. The character buffer usually holds several times as many items as the token buffer above it.
- **Cost per item.** A slot is 32 bytes beside the item, so a buffered character costs 34 bytes ([ADR-0031](0031-constructs-that-read-their-input-back-pin-their-start.md) option 1B; LAY-05's scope).
- **Input held whole in memory has no buffer.** A cursor over the caller's text or token array releases nothing, because it holds nothing of its own.
- **Options reach each layer.** `Layer.Open(input, options, recordFailures)` already receives `ParseOptions`, and `TokensLayer` reads `MaxDepth` from it.

Prior art, read for this record by a research subagent on 2026-11-24. The Parlot row was also read first-hand.

| Source | Counts | Default | When exceeded |
| --- | --- | --- | --- |
| Parlot 2.5.0 `MaxBufferedCharacters` ([streaming guide](https://github.com/sebastienros/parlot/blob/v2.5.0/docs/streaming.md)) | Characters retained by its compacting buffer, or one value's window under `ParseManyAsync` | `int.MaxValue`; "Set it for untrusted input." Generated parsers cannot set it | "throws a `ParseException` at the start of the token, value or white space which doesn't fit" |
| `XmlReaderSettings.MaxCharactersInDocument` ([docs](https://learn.microsoft.com/en-us/dotnet/api/system.xml.xmlreadersettings.maxcharactersindocument)) | Characters, including expanded entities | 0, no limit | `XmlException` |
| Kestrel `MaxRequestBodySize`, `MaxRequestHeadersTotalSize` ([docs](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.server.kestrel.core.kestrelserverlimits)) | Bytes | 30,000,000 and 32,768 | The request is rejected with 413 or 431 |
| Google.Protobuf C# `CodedInputStream` ([source](https://github.com/protocolbuffers/protobuf/blob/main/csharp/src/Google.Protobuf/CodedInputStream.cs)) | Bytes read | `Int32.MaxValue` now; 64 MB at v3.0.0 | `InvalidProtocolBufferException`: "Protocol message was too large.  May be malicious." |
| System.Text.Json ([Utf8JsonReader guide](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-utf8jsonreader)) | — | No option; its stream sample "sets no limit to how large the buffer can grow" | `OutOfMemoryException` |
| ANTLR 4.13.2 `UnbufferedCharStream` ([source](https://github.com/antlr/antlr4/blob/4.13.2/runtime/Java/src/org/antlr/v4/runtime/UnbufferedCharStream.java)) | Characters while a mark exists | No limit; the buffer doubles | — |
| Go `bufio.Scanner` ([docs](https://pkg.go.dev/bufio)) | Bytes in one token | `MaxScanTokenSize`, 64 KB | `ErrTooLong`; "Scanning stops unrecoverably" |
| Jackson 2.18 `StreamReadConstraints` ([source](https://github.com/FasterXML/jackson-core/blob/jackson-core-2.18.0/src/main/java/com/fasterxml/jackson/core/StreamReadConstraints.java)) | Characters or bytes in one string; whole document | 20,000,000 for a string ("2.15.0 initially used a lower setting (5_000_000)"); no limit for a document | `StreamConstraintsException`, naming the limit |

Every limit counts the input's own unit, and every one ends the read; the subagent found no library where a backtracking construct can catch it. Finite defaults bound one token or one message. Limits on everything retained, Parlot's among them, default to no limit. Jackson raising its string limit fourfold shows that a finite default rejected valid input.

The choice has five forks.

## Options for what counts (fork 1)

### 1A. The items from the reader's horizon to the buffer's filled end, checked at each pull (chosen)

Each time the layer reading a buffer pulls an item it does not have, it compares the items from its horizon to `Filled` with the limit. The horizon is the one ADR-0031 defines, so it counts the reader's pins, its own step pin when it produces items, and the restart bookmark.

- **Pros.**
  - It counts what the layer may still read, not how the buffer happens to store it, so a limit of any positive size works.
  - Pulls happen once per upstream step, not per item read, and never over input held in memory. The check costs one comparison where a step already runs.
  - Memory at the limit is bounded: the limit, plus about two segments, plus one step's output, at each boundary.
- **Cons.**
  - Until LAY-09, a producing layer's horizon includes the input of outputs its reader has passed, up to one output segment. So where the limit trips in a lower layer depends on the segment size of the buffer above it.
  - The layer must know its horizon when it pulls, not only when it releases. LAY-09 needs the same thing, so whichever card lands first builds it.

### 1B. What the buffer holds, checked when it adds a segment

Compare `Filled − Start` with the limit in `StreamBuffer.Add`, once per segment.

- **Pros.**
  - One comparison per segment, in one place.
  - It measures memory exactly, in whole segments.
- **Cons.**
  - A buffer always holds the segment before its horizon and the one it is filling. Any limit below about two segments, 2,048 items with the default size, fails every streamed parse.
  - Segment size is internal, so users cannot predict where a parse fails.
  - Where it trips depends on when the reader last entered a segment.

### 1C. Only what the layer's own constructs hold

Count the layer's own pins and running step, but not the input a producing layer keeps so that its reader can go back.

- **Pros.**
  - The error always names the layer whose construct opened the window.
- **Cons.**
  - It does not bound memory. A reader may hold *N* outputs, each produced by a step of up to *N* input items, so the layer below keeps up to *N*² items.

### 1D. One budget across all of a pipeline's buffers

- **Cons.**
  - No single layer or position to report.
  - One layer's legitimate window shrinks another's.
  - The whitepaper bounds "each layer's buffer".

## Options for how exceeding stops the parse (fork 2)

### 2A. The whole parse ends, as for `MaxDepth` (chosen)

An internal exception, thrown at most once per parse, unwinds past every combinator to the parse entry point, which returns `Failed`. In a producing layer it becomes the `LayerFailedException` that carries the layer's error, as a depth error does in `TokensLayer`.

- **Pros.**
  - What a grammar accepts never depends on the limit.
  - Nothing runs on the hot path.
  - Every library in the table ends the read.
- **Cons.**
  - `Recover` cannot continue past it.

### 2B. An ordinary committed failure

- **Cons.**
  - `Not(p)` turns it into a success.
  - An `Or` tries its next alternative, which can reach the limit again from the same start.
  - What a grammar accepts would depend on the limit. The subagent found no library that does this.

## Options for the default (fork 3)

### 3A. 16,777,216 items (chosen by the maintainer)

- **Pros.**
  - On by default, as `MaxDepth` is.
  - Near Jackson's revised 20,000,000-character string limit, so valid input rarely reaches it.
  - A runaway pin over a streamed input ends cleanly instead of exhausting memory.
- **Cons.**
  - At the limit, a character buffer holds about 544 MiB, and a token buffer more. It stops growth without input bounds; it does not make a parse small.
  - A token, pinned window or `ParseManyAsync` record of more than 16,777,216 items fails until the caller raises the limit.
  - Parlot's limit is off by default and Creole's is on.

### 3B. 1,048,576 items

- **Pros.**
  - About 34 MiB for a character buffer at the limit.
- **Cons.**
  - A string token or `ParseManyAsync` record of more than about 2 MiB of text fails, which is the kind of limit Jackson had to raise.

### 3C. No limit, `int.MaxValue`, as Parlot does

- **Pros.**
  - Nothing valid ever fails.
- **Cons.**
  - A streamed parse with a runaway pin grows until it runs out of memory, unless the caller knew to set the limit.
  - Unlike `MaxDepth`, which is on by default.

## Options for the error (fork 4)

### 4A. The item at the horizon (chosen)

The error covers the oldest item the layer still had to keep, and its message names `ParseOptions.MaxBufferedItems` and its value.

- **Pros.**
  - It points at what opened the window: a `Lookahead`'s first item, the start of a step that never ends, such as an unterminated comment, or a record's start under `ParseManyAsync`.
  - Parlot reports the same place: "the start of the token, value or white space which doesn't fit".
- **Cons.**
  - Until LAY-09, a producing layer's horizon can lie up to one output segment before the step that grew.
  - When a reader's pin makes a lower layer keep its input, the lower layer usually reaches the limit first and reports it. The error then names the lexer for a parser's `Lookahead`, though at the right place in the source.

### 4B. From the horizon to the item that went over

- **Pros.**
  - It shows the whole window held.
- **Cons.**
  - An editor would underline up to 16,777,216 items. A rendered excerpt shows only the start anyway (§ Limits for untrusted input, "Rendered errors").

### 4C. The item where the limit was reached, as the depth error does

- **Cons.**
  - It lands an arbitrary distance into the input and says nothing about the cause.

## Options for where the setting lives (fork 5)

### 5A. One value in `ParseOptions` for every buffer (chosen)

Each layer reads it from the options `Open` receives.

- **Pros.**
  - The whitepaper's design, and no new API.
- **Cons.**
  - Character and token buffers differ in item size and in the windows they need, and one number fits both loosely.

### 5B. A per-layer override

- **Cons.**
  - More API with no measured need. LAY-05's pipeline builder can add one later without changing this record's rule.

## Decision

1. **What counts.** A layer may hold at most `ParseOptions.MaxBufferedItems` items of the buffer it reads: the items from its horizon (ADR-0031: the minimum of its cursor, its floor, its own pin when it produces items, and the restart bookmark) to the buffer's filled end. The layer checks each time it pulls an item it does not have, after the pull, so the count includes the items that step produced. Input held whole in memory has no buffer and is not counted. Copies an error or a span combinator keeps after release ([ADR-0028](0028-an-errors-items-are-copied-when-the-buffer-releases-them.md), [ADR-0035](0035-span-combinators-commit-like-sequences-and-hold-their-spans.md)) are not counted.
2. **The limit.** `MaxBufferedItems` is a positive `int` and defaults to 16,777,216. Zero or a negative value throws `ArgumentOutOfRangeException`, as `MaxDepth` does. `int.MaxValue` lifts the limit in practice.
3. **How the parse ends.** As point 4 of ADR-0020 says for depth: the parse unwinds past every combinator, `Recover` included, and returns `Failed`, with one error added to any already recorded. When the layer that exceeded is a producing layer, its error ends the parse through `LayerFailedException`, as any failed step does.
4. **The error.**
   - **Span:** the item at the layer's horizon when the limit was exceeded.
   - **Layer:** the layer that reads the buffer.
   - **Unexpected:** a `CustomMessage` naming `ParseOptions.MaxBufferedItems` and its value.
   - **Expected:** empty.
   - **Context:** the context lines in force where the limit was reached.
5. **Where the setting lives.** One value in `ParseOptions`, applied to each buffer separately.
6. **Cards.**
   - LAY-06 builds points 1 to 5, and tests them through internal layers and test upstreams, since no public API runs a layer until LAY-05.
   - LAY-05 catches the limit at the pipeline entry points and documents it beside its sources.
   - LAY-09's narrower pin changes the horizon, and with it where a producing layer's limit trips and what its error points at.
   - STRM-02 counts the restart bookmark, so under `ParseManyAsync` the limit also caps one record. It decides how data its driver has read ahead counts, and its doubling stops at the limit.
   - ERR-05's `Recover` does not catch it.
   - GEN-06 checks the same horizon at the same pulls.

Deciding reasons:

- **Fork 1.** 1A is the only option that both bounds memory (unlike 1C) and works for any limit a user sets (unlike 1B).
- **Fork 2.** As in ADR-0020, only 2A keeps the grammar's meaning independent of the limit. Prior art agrees.
- **Fork 4.** The horizon is where the cause starts, and it is also the place Parlot reports.
- **Fork 3.** The maintainer chose it.

What would change this record:

- If a real pipeline needs very different windows at different boundaries, add a per-layer override (5B).
- If layered benchmarks show the check at each pull costs measurably, check once per segment pulled instead. The rule stays the same; only how far past the limit a buffer may get before it trips changes.

## Consequences

- **Easier.**
  - A streamed parse with a runaway pin, a step that never ends, or a record that never ends fails with an error that names the setting to raise.
  - Guarantee 2's bound gets a number a caller can set.
- **Harder.**
  - The limit names a different layer from the one whose construct opened the window when a lower layer keeps input for the layer above. The span is still the right place in the source.
  - Until LAY-09, a producing layer's count includes input behind outputs its reader has passed, so where a lower layer trips depends on the segment size above it.
  - The default lets each buffer reach about 544 MiB of characters. Callers with tight budgets set a lower limit.
- **Ruled out.**
  - `Recover` cannot continue past a window that is too large.
  - The limit never makes a choice try another alternative.
- **Engines.** Both engines count the same horizon at the same pulls. A generated engine may skip node pins inside a producing step (ADR-0031 point 4) without changing the count, because the step pin lies below them. Until LAY-09, equivalence tests that reach the limit use the same segment sizes in both engines.
- **Not covered.**
  - Memory a layer's own code holds: layer state, values a parser builds, and a scanner's own buffers.
  - Input held whole in memory, which is the caller's already.
- **Watched by.** LAY-06's tests:
  - A `Lookahead` over a long streamed input ends at the limit with the error at the `Lookahead`'s first item. The buffer never held more than the limit plus two segments and one step.
  - Wrapping the pinned part in `Not`, `Or` and `Atomic` does not change that outcome.
  - A producing layer whose step never ends, such as an unterminated comment, ends at the limit, and the error names that layer.
  - A limit of 1 lets a parse with no pins run to the end over a one-item-per-step upstream.
  - A parse over input held in memory ignores the limit.

## Changes made

- Numbering: drafted as 0037 and renumbered 0039 on acceptance, because another branch had already taken 0037.
- Whitepaper:
  - [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input): a **Buffers** part on what counts, the default and how the parse ends, with Parlot's `MaxBufferedCharacters` as prior art. Links this record. The opening sentence adds a buffer that grows until memory runs out to what the limits prevent.
  - [§ Guarantees](../../../whitepaper.md#guarantees): the paragraph after the list says `MaxBufferedItems` caps each boundary's window. Point 2 is unchanged.
  - [§ The restart protocol](../../../whitepaper.md#the-restart-protocol): the bookmark counts toward the limit, which caps one item, and the driver stops doubling at it.
  - [§ Recovery](../../../whitepaper.md#recovery): `Recover` cannot catch a buffer limit either.
  - [§ Equivalence](../../../whitepaper.md#equivalence): the engines check the same horizon at the same pulls, so they agree on a parse the limit ends.
- Cards:
  - [LAY-06](../../active/06-layers-and-pipelines/LAY-06-buffer-size-limit.md): scope restated from the decision; the criterion made precise, and three added from "Watched by".
  - [LAY-05](../../active/06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md): catch the limit at the pipeline entry points and document it beside the sources.
  - [STRM-02](../../active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md): the restart bookmark counts; decide how read-ahead counts and stop doubling at the limit.
  - [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md): `Recover` does not catch the buffer limit.
  - [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md): check the same horizon at the same pulls.
  - [LAY-09](../../active/06-layers-and-pipelines/LAY-09-hold-only-the-input-the-reader-can-still-go-back-to.md): unchanged. Its narrower pin moves the horizon this limit counts from, so it changes where a producing layer trips and what its error points at, without changing this record's rule.
