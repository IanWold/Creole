# ADR-0044: Huge single documents parse synchronously on a thread the caller starts, and byte pipelines read a blocking `Stream`

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-11-26 |
| Cards | [STRM-04](../../active/08-streaming/STRM-04-huge-single-documents.md), [STRM-02](../../active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md), [STRM-03](../../active/08-streaming/STRM-03-streaming-diagnostics.md) |
| Whitepaper | [§ Why not async combinators](../../../whitepaper.md#why-not-async-combinators), [§ Sources](../../../whitepaper.md#sources) |
| Research | [Parlot 2.0 baselines and generator](../research/2026-10-24-parlot-2-0-baselines-and-generator.md), finding 23. The runtime checks this record relies on are described under Context. |
| Supersedes | — |

## Context

[§ Why not async combinators](../../../whitepaper.md#why-not-async-combinators) covers huge single documents in 1.0 this way: "Run the synchronous parser on a dedicated thread over a blocking `TextReader` that wraps the pipe. This costs one thread per parse and needs no design change." STRM-04's scope repeats it: a blocking `TextReader` adapter over a `PipeReader`, run on a dedicated thread, and documented with its one-thread cost. The whitepaper does not say whether that is public API or a recipe, where the bytes are decoded, which thread runs the parse, or how it is cancelled.

What already exists:

- `Parse` and `ParseMany` over a `TextReader` read as the layers pull, block until the reader has characters, and keep only what the layers may still go back to ([ReaderUpstream.cs](../../../src/Creole/ReaderUpstream.cs); [docs/concepts/pipelines.md § Readers](../../../docs/concepts/pipelines.md#readers)). The Phase 1 memory gate measures them ([LAY-07](../../active/06-layers-and-pipelines/LAY-07-three-layer-demo-and-bounded-memory-check.md)).
- A pipeline over bytes has `Parse` and `ParseMany` over `ReadOnlyMemory<byte>` only. `Layer.Utf8()` decodes, spans carry byte and character offsets, and invalid UTF-8 ends the parse at the first bad sequence ([ADR-0038](0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md) point 5, [SPAN-06](../../active/05-source-spans/SPAN-06-byte-sources-and-the-decoding-layer.md)).
- `PipeReader` and `Stream` are planned only for `ParseManyAsync`, whose byte root must end an attempt Starved when buffered data runs out (STRM-02).

Checked in this session on the .NET 11.0.0 runtime, with a console program in a scratch directory:

- `System.IO.Pipelines.dll` loads from `Microsoft.NETCore.App` 11.0.0, so using it needs no package reference.
- `new StreamReader(pipe.Reader.AsStream()).Read(...)`, run with `TaskCreationOptions.LongRunning`, blocked until the writer wrote, returned each of five delayed chunks, and ran on a thread outside the pool. So the BCL already provides the blocking adapter the card asks for.
- With a read pending: `pipe.Reader.CancelPendingRead()` made `Read` throw `OperationCanceledException` ("Read was canceled on underlying PipeReader"). `pipe.Writer.Complete(exception)` made it throw that exception. `pipe.Reader.Complete()` from another thread left it blocked after 2 seconds.
- `Pipeline.Parse(TextReader)` has no `catch`, so an exception from the reader leaves `Parse` unchanged.

Prior art: Parlot 2.0.0 parses a large `TextReader` synchronously "on a thread pool thread whose refills block on `TextReader.ReadAsync`", and decodes a `Stream` through a `StreamReader` (research note, finding 23 and § Disconfirming evidence). The whitepaper's case against Parlot includes keeping byte offsets through decoding ([§ Streaming and async input](../../../whitepaper.md#streaming-and-async-input)).

The maintainer chose forks 1 and 2 in conversation on 2026-11-26, each time taking the recommended option. Forks 3 and 4 turn on the facts above.

## Criteria

Fixed before the build. The build checks the chosen options against them; no option is chosen by trial.

1. **Bounded memory.** A single JSON document, an array of the memory gate's JSON-lines records, is written into a `System.IO.Pipelines.Pipe` by a producer task and parsed on a dedicated thread by a pipeline that starts with `Layer.Utf8()`. Its parser reads every record and keeps none, so the value does not grow with the input. At 10 MB, 100 MB and 1 GB, each in its own process, the retained heap is sampled after a forced full blocking GC every 1,000 records, as [scripts/memory-gate.py](../../../scripts/memory-gate.py) does. The peak at 1 GB is at most 1.5 times the peak at 10 MB, the Phase 1 threshold ([ADR-0003](0003-numeric-gate-thresholds.md)).
2. **Same results as bytes in memory.** For valid input, a grammar error, invalid UTF-8 in mid-document, a byte-order mark, and multi-byte sequences split across reads (a stream that returns one byte per `Read`), `Parse(Stream)` returns the value or the error that `Parse(ReadOnlyMemory<byte>)` returns for the same bytes: same message, and spans with the same byte and character offsets. The one difference is that its failed result holds no text, as over a `TextReader`. `ParseMany(Stream)` matches `ParseMany(ReadOnlyMemory<byte>)` likewise.
3. **Cancellation and source errors.** Tests show that `CancelPendingRead()` during a parse ends `Parse` with `OperationCanceledException`, that a producer's `Complete(exception)` ends it with that exception, and that the caller's stream is left open.
4. **Nothing else changes.** Existing tests pass. In-memory and `TextReader` paths share no changed code, so the Phase 0 speed gate is not rerun. `dotnet publish tests/Creole.AotSmoke -c Release` reports no new warnings (G8).
5. **Documented.** docs/concepts/pipelines.md shows the recipe, says what it costs and why it uses a dedicated thread, how to cancel, and that bounded memory covers the input the parse holds, not the value the grammar builds.

## Options

### Fork 1. The public surface

#### 1A. A documented recipe, no new API (chosen)

The caller starts the thread:

```csharp
var result = await Task.Factory.StartNew(
    () => json.Parse(pipe.Reader.AsStream()),
    CancellationToken.None,
    TaskCreationOptions.LongRunning,
    TaskScheduler.Default);
```

- **For:**
  - Nothing new to maintain in either engine. It is what the whitepaper describes: a synchronous parse on a dedicated thread over a blocking reader.
  - The caller owns the thread, so it can choose its stack size (fork 3) and how it waits.
  - `ParseAsync` (1B) can be added later without breaking anything.
- **Against:**
  - A caller who writes `Task.Run` blocks a pool thread for the whole parse (fork 3). The docs must say why not.
  - Harder to find than a method on `Pipeline`.

**Result:** Chosen by the maintainer on 2026-11-26. Built in cce1483: [docs/concepts/pipelines.md § Huge documents from a pipe](../../../docs/concepts/pipelines.md#huge-documents-from-a-pipe) gives the recipe, and [PipeTests.cs](../../../tests/Creole.Tests/PipeTests.cs) runs it over a `Pipe` written in three parts that split two characters, which parses on a thread outside the pool.

#### 1B. `ParseAsync(PipeReader | Stream, ParseOptions?, CancellationToken)`

`Pipeline` gets a method that returns `Task<ParseResult<T>>` and starts the dedicated thread itself.

- **For:** easy to find, and the threading is right by construction.
- **Against:**
  - It looks like scalable async but holds a thread for the whole parse. That cost is the reason the whitepaper keeps parsing synchronous, and the name hides it.
  - New public API that the generated engine must also support (GEN-06), and a `CancellationToken` that can only cancel the source's reads (fork 4).

**Result:** Not chosen; the maintainer preferred the recipe. It stays open as a later convenience if callers get the recipe wrong.

#### 1C. A public adapter type, such as a `PipeTextReader`

**Result:** Ruled out by the check under Context: `StreamReader` over `PipeReader.AsStream()` is already a blocking `TextReader` over a pipe, and with 2B no `TextReader` is needed at all.

### Fork 2. Where the bytes are decoded

#### 2A. A `StreamReader`, and the existing `Parse(TextReader)` on a character pipeline

`json.Parse(new StreamReader(pipe.Reader.AsStream()))`, with `json` a `Pipeline<char, T>`.

- **For:** no new code.
- **Against:**
  - Spans hold character offsets only, so a huge document from bytes loses its byte offsets (G5).
  - `StreamReader` replaces invalid UTF-8 with U+FFFD and the parse goes on, where `Layer.Utf8()` ends it with an error. The same bytes would parse differently from a stream and from memory.
  - It is exactly what Parlot does (Context), so § Streaming's claim of byte offsets through decoding would not hold for this case.

**Result:** Not chosen; the maintainer chose 2B. It still works for callers with a character pipeline, and the docs say so for input that is not UTF-8.

#### 2B. A blocking byte source: `Parse(Stream)` and `ParseMany(Stream)` on byte pipelines (chosen)

An internal root `IUpstream<byte>` reads the caller's `Stream` synchronously, as `ReaderUpstream` reads a `TextReader`: byte *i* has \[*i*, *i* + 1) in `SourceId.Unnamed`, and the input ends when `Read` returns 0. Two overloads on `Pipeline<byte, TOut>` mirror the `TextReader` ones. A `PipeReader` reaches them through `AsStream()`.

- **For:**
  - `Layer.Utf8()` decodes, so spans keep byte and character offsets and invalid UTF-8 ends the parse as it does in memory.
  - About 60 lines, a near copy of `ReaderUpstream`, plus two overloads.
  - Useful beyond pipes: a `FileStream` or `NetworkStream` parses with bounded memory and byte offsets, on the caller's thread if blocking it is acceptable.
- **Against:**
  - Two more public overloads. § Sources gains a row.
  - Next to STRM-02's `ParseManyAsync(Stream)`, callers must tell a blocking `ParseMany(Stream)` from the async one. Their names and return types differ, as `ParseMany(TextReader)` already does.
  - A failed result holds no text until STRM-03 captures lines, as over a `TextReader`.

**Result:** Chosen by the maintainer on 2026-11-26. Built in a9b7dc7 as `StreamUpstream` and the two overloads. Criterion 2: on eight sources, through a `MemoryStream` and a stream that gives one byte per read, `Parse` and `ParseMany` over a stream return the values and errors of the in-memory overloads; shifting the root's offsets by one fails 10 of those tests. Criterion 1, measured on cce1483 with the `json-document` harness input uncommitted: 145,320, 148,952 and 153,744 bytes retained at 10 MB, 100 MB and 1 GB, 1.06×. Again on a9aafc2, with nothing uncommitted: 145,200, 148,920 and 149,536 bytes, 1.03×. The 1 GB peak moved by 4 KB between the runs, so the rise across sizes is within the samples' spread. A byte buffer that releases nothing fails the same measurement at 15.08× from 2 MB to 20 MB. Criterion 4: all 2,025 tests pass on a9b7dc7, and the AOT smoke publishes with no warnings.

#### 2C. Overloads that take a `PipeReader` directly

**Result:** Ruled out by argument: `AsStream()` adapts a `PipeReader` in one call, and `Stream` also covers files and sockets with one pair of overloads.

### Fork 3. Which thread runs the parse

#### 3A. A dedicated thread (the whitepaper's position)

`TaskCreationOptions.LongRunning`, or `new Thread(start, maxStackSize)` when the document nests deeply.

- **For:**
  - The parse blocks for as long as the producer takes. A dedicated thread leaves the pool's threads to the producer and the rest of the process.
  - The stack check ends a parse sooner on a small stack ([§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input): "the stack used per level depends on the grammar, the JIT tier and the thread"). With `new Thread(start, maxStackSize)` the caller can give a deep document more stack; a pool thread cannot.
- **Against:** creating a thread costs more than borrowing one. That is small against a document large enough to need this.

**Result:** Chosen, by argument. In cce1483, `PipeTests` shows 10,000 nested levels fail on a 512 KiB stack and parse on a 256 MiB one, which the docs' advice on `maxStackSize` rests on.

#### 3B. A pool thread, as Parlot 2.0.0 does

- **For:** no thread creation, and `Task.Run` is what callers reach for.
- **Against:** a pool thread blocked on a slow producer is unavailable for the whole parse. Several such parses at once can starve the pool, including the producer's own continuations if it writes asynchronously.

**Result:** Ruled out by argument.

### Fork 4. Cancellation

#### 4A. Cancel the source (chosen)

The parse has no cancellation check. The caller cancels the read: `cancellationToken.Register(pipe.Reader.CancelPendingRead)`, or the producer completes its writer with an exception. Either makes the blocked read throw, and the exception leaves `Parse` unchanged (Context).

- **For:** no API, and no check on the engines' hot path.
- **Against:** it cannot stop a parse that is busy rather than waiting, such as a slow grammar over data already buffered. Over a pipe the parse soon needs more data, so in practice it stops at its next read.

**Result:** Chosen, by argument and the check under Context. Criterion 3, in cce1483: `PipeTests` shows `CancelPendingRead`, registered with a token, ends `Parse` with `OperationCanceledException`, and a writer's `Complete(IOException)` ends it with that exception. a9b7dc7's tests show the stream is left open.

#### 4B. A `CancellationToken` that the parse checks

- **For:** stops a busy parse too.
- **Against:** new API for every source, and a check in both engines' loops. That is a separate decision, for a caller who needs to cancel a parse whose source cannot be cancelled.

**Result:** Ruled out for this card, by argument.

## Decision

Options **1A, 2B, 3A and 4A**. Accepted by the maintainer on 2026-11-26. The maintainer chose 1A and 2B; 3A and 4A follow from the facts under Context.

1. **No new API to run a parse on a thread.** The docs show the recipe under 1A: a dedicated thread, `LongRunning` by default or `new Thread(start, maxStackSize)` for deep documents, never `Task.Run`, with the one-thread cost stated.
2. **Byte pipelines read a blocking `Stream`.** `Pipeline<byte, TOut>` gets `Parse(Stream input, ParseOptions? options = null)` and `ParseMany(Stream input, ParseOptions? options = null)`, mirroring the `TextReader` overloads:
   - Byte *i* has the span \[*i*, *i* + 1) in `SourceId.Unnamed`. `InputEnd` is the number of bytes read once `Read` returns 0.
   - Both record failures as they go, since the stream cannot be read again (ADR-0038 point 3).
   - A failed result holds no text, so its errors render one line each, until STRM-03 captures lines.
   - Slices are copies.
   - The stream is the caller's: a parse leaves it open.
   - A blocking read never starves, so this root never ends an attempt Starved.
3. **Cancellation is the source's.** The docs show `CancelPendingRead` through a token registration, and say that the exception the read throws leaves `Parse` unchanged.

No option is open, and no trial decides between them. The build checked the chosen options against the criteria:

| Criterion | Result | Evidence |
| --- | --- | --- |
| 1. Bounded memory | Met: 1.03× from 10 MB to 1 GB | `python3 scripts/memory-gate.py --inputs json-document` on a9aafc2; 2B's Result |
| 2. Same results as bytes in memory | Met | `ByteSourceDiagnosticsTests`, a9b7dc7 |
| 3. Cancellation and source errors | Met | `PipeTests`, cce1483; `PipelineTests` and `ParseManyTests`, a9b7dc7 |
| 4. Nothing else changes | Met | 2,025 tests pass and the AOT smoke publishes cleanly on a9b7dc7; 2,031 pass on a9aafc2 |
| 5. Documented | Met | [docs/concepts/pipelines.md](../../../docs/concepts/pipelines.md) § Streams and § Huge documents from a pipe |

Deciding reasons:

- **1A:** no new API, and the recipe is the whitepaper's design; `ParseAsync` can follow without breaking anything.
- **2B:** spans keep byte offsets and invalid UTF-8 ends the parse as it does in memory, where `StreamReader` decoding would lose both.
- **3A and 4A:** a blocked pool thread can starve the pool, and cancelling the read needs no API and no check on the hot path.

What would change this:

- **1A:** callers who block pool threads with `Task.Run` despite the docs, or RES-01 adopting resumable generated parsers. Either would make `ParseAsync` (1B) worth its API.
- **2B:** STRM-02 finding that one byte root can serve both the blocking and the starving case cheaply. The overloads would stay; only the internal root would change.
- **4A:** a caller who must cancel a busy parse. That is 4B, as its own decision.

## Consequences

- **Easier:**
  - A huge document from a pipe, file or socket parses with bounded memory and keeps byte offsets, with no async in the engines.
  - STRM-02 can start from the blocking byte root's slot layout for its starving root.
- **Harder:**
  - Callers must start the thread themselves and choose it well. The docs carry that.
  - Two more public overloads to keep in step with the `TextReader` ones.
- **Ruled out:** `ParseAsync` in 1.0, a public adapter type, and cancellation checks in the engines, unless a later decision adds them.
- **Risks and how they are watched:**
  - **A single long `Parse` holds more than `ParseMany` does.** One `Parse` over a 1 GB document depends on the grammar's repetition leaving nothing pinned. Criterion 1 measures it over a stream. If it fails, the cause is a retention bug in the engine, to fix or to card, not a reason to change these options. (Corrected 2026-11-26: this record first said the memory gate measures only `ParseMany`. Its Example B input is one long `Parse` over a `TextReader`, and measured 1.02×.)
  - **The two byte roots drift apart.** Criterion 2 compares `Parse(Stream)` with `Parse(ReadOnlyMemory<byte>)` on the same bytes, including sequences split across reads.
  - **STRM-03 misses the new source.** STRM-03's scope gains `Parse` and `ParseMany` over a `Stream` when this record is accepted.

## Changes made

- **Whitepaper.**
  - [§ Why not async combinators](../../../whitepaper.md#why-not-async-combinators): the first route parses over the pipe's blocking `Stream`, with `Layer.Utf8()` decoding, on a thread the caller starts, which is a dedicated one, cancelled by cancelling the pipe's pending read. It cites criterion 1's measurement and links this record.
  - [§ Sources](../../../whitepaper.md#sources): a `Stream` row for blocking `Parse` and `ParseMany`, and a sentence saying a blocking `Stream` gives the results of the same bytes in memory but holds no text.
  - Checked and unchanged: § The restart protocol; the Resumable async item in § Questions raised in review, which already says a dedicated-thread option covers huge documents; § Comparison with existing .NET libraries, whose Creole streaming cell ("Any source at the root of a pipeline") still holds.
- **Cards.**
  - [STRM-04](../../active/08-streaming/STRM-04-huge-single-documents.md): scope states the decision, and the card is Done.
  - [STRM-03](../../active/08-streaming/STRM-03-streaming-diagnostics.md): `Parse` and `ParseMany` over a `Stream` hold captured lines too, and take `source:`, as over a `TextReader`.
  - [STRM-02](../../active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md): a progress note pointing at `StreamUpstream` and this record's "What would change this" for 2B.
- **Code and docs** were built under STRM-04 in a9b7dc7, cce1483 and a9aafc2: `StreamUpstream`, the two overloads, docs/concepts/pipelines.md § Streams and § Huge documents from a pipe, and the `json-document` memory input.
