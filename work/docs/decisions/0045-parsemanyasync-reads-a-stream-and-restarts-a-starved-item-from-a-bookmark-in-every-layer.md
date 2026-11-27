# ADR-0045: `ParseManyAsync` reads a `Stream`, and a starved item restarts from a bookmark in every layer once it has twice the data or its source is idle, up to the buffer limit

| | |
| --- | --- |
| Status | Proposed |
| Date | 2026-11-27 |
| Cards | [STRM-02](../../active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md), [STRM-03](../../active/08-streaming/STRM-03-streaming-diagnostics.md) |
| Whitepaper | [§ The restart protocol](../../../whitepaper.md#the-restart-protocol), [§ Sources](../../../whitepaper.md#sources), [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input) |
| Research | [Parlot 2.0 baselines and generator](../research/2026-10-24-parlot-2-0-baselines-and-generator.md), finding 23. Parlot's source and the runtime checks this record relies on are described under Context. |
| Supersedes | — |

## Context

[§ The restart protocol](../../../whitepaper.md#the-restart-protocol) fixes the shape. The driver bookmarks every layer at each item's start, runs the item over the data buffered, and on Starved awaits at least double the data, rewinds every layer and retries. Accepted records fix most of the rest:

- [ADR-0030](0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md):
  - Point 7: the restart undoes mode changes first, then clears every layer's failure record and the Starved flag, resets scoped parts to their base, and keeps the pins the layers and the driver hold.
  - Point 10: "A restarted item gives the result and diagnostics of the same item parsed with all its data available."
  - Point 11: the test this card owns.
- [ADR-0038](0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md):
  - Points 2 to 4: `ParseManyAsync` follows `ParseMany`, which item holds a diagnostic, and that a restart keeps a diagnostic raised before the item's bookmark.
  - Point 3: STRM-02 may measure running a failed item again from its bookmark.
- [ADR-0039](0039-each-layers-buffer-is-bounded-from-its-horizon-and-exceeding-it-ends-the-parse.md) point 6: the restart bookmark is part of each layer's horizon, so `MaxBufferedItems` caps one item. STRM-02 decides how data its driver has read ahead counts, and its doubling stops at the limit.
- [ADR-0027](0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md) point 4: a streamed parse records as it goes, unless STRM-02 shows a measured gain from running a failed item again.
- [ADR-0026](0026-engines-may-skip-unused-values-and-repeat-user-functions.md) lets any user function run more than once, "as when a parse runs again", so a restart needs no new rule for user code.
- [ADR-0044](0044-huge-documents-parse-on-a-caller-started-thread-and-byte-pipelines-read-a-blocking-stream.md):
  - It gave byte pipelines `Parse(Stream)` and `ParseMany(Stream)` over a blocking root that never starves.
  - It ruled out `PipeReader` overloads because `AsStream()` adapts in one call.
  - It cancels through the source, with no check in the engines.

What the whitepaper and those records leave open is the card's own list:

- the public surface;
- what exactly a bookmark holds in each layer;
- where bytes the driver has received wait, and how they count toward the limit;
- how much the driver reads before a retry;
- when an item is yielded relative to the look for the next one;
- whether a failed item runs again with a failure log.

What exists, at 36bedbf:

- **Starved.** STRM-01 made it a per-buffer flag that stays set until a restart. A starved step adds nothing and leaves its layer's position and state as they were ([TokensLayer.cs](../../../src/Creole/Layers/TokensLayer.cs), [Utf8Layer.cs](../../../src/Creole/Layers/Utf8Layer.cs)). Scoped parts unwind as on any failure. Nothing clears the flag, and no source starves.
- **The `ParseMany` driver** ([LayeredPipeline.cs](../../../src/Creole/LayeredPipeline.cs)). For each item it forgets every layer's furthest failure, resets the channels the parser writes, peeks for an item, and runs the parser on a new cursor. It yields lazily: it looks for item *k* + 1 only when the caller asks for it. The parser's cursor has no floor at the item's start, so a cut inside an item may release the item's start.
- **The buffer limit** is checked in `StreamBuffer.Fill` after each upstream step ([StreamBuffer.cs](../../../src/Creole/StreamBuffer.cs)). So the error is raised inside the reading layer's step, which turns it into its own error with its context lines (ADR-0039 point 4).
- **Each layer's diagnostics** live in its failure log (`FailureLog.Diagnostics.cs`). No combinator raises one yet; `Recover` is ERR-05.
- **The blocking byte root** `StreamUpstream` ([StreamUpstream.cs](../../../src/Creole/StreamUpstream.cs)) reads up to 1,024 bytes per step with a blocking `Read`.

Checked in this session on the .NET 11.0.0 runtime, with a console program in a scratch directory:

- `new PipeOptions()` has `PauseWriterThreshold` 65,536 and `ResumeWriterThreshold` 32,768.
- A reader that called `AdvanceTo(buffer.Start, buffer.End)` after each read, so it examined everything and consumed nothing, did not stall a writer that wrote 1 MB in 4 KB writes. The writer finished, and the reader saw all 1,048,576 bytes unconsumed after 27 reads.
- `pipe.Reader.AsStream().ReadAsync` stays pending until the writer writes. It returned 10 after a 10-byte write and 0 once the writer completed. With a token cancelled after 100 ms, it threw `OperationCanceledException`.

`Stream`'s documentation, read on learn.microsoft.com for .NET 11 in this session, says two things:

- A read's result "can be less than the length of the buffer if that many bytes are not currently available".
- "An implementation is free to return fewer bytes than requested even if the end of the stream has not been reached" (`Stream.Read`, Remarks).

So a short read alone does not prove the source is idle.

**Prior art.** A researcher read Parlot's `ParseManyAsync` at the v2.0.0 tag (2a34f3d) and compared it with v2.4.0 (8000c7b). The line numbers below are as GitHub's file and blame views reported them.

- **Source and results.** It reads only a `TextReader`, and returns `IAsyncEnumerable<T>` (`Parser.Streaming.cs`:71–130). A failure throws a `ParseException`, "The input could not be parsed.", at the value's start (`StreamingDriver.cs`:454–456). Nothing records what was expected.
- **When an attempt is inconclusive.** A sticky `HitEnd` is set by any read that reaches the end of a window that is not final, even one that looks no further (`Cursor.cs`:78, 182–195, 397–401).
  - Combinators ignore it. An attempt runs to completion, and the driver discards it if the flag is set (`StreamingDriver.cs`:438–454).
  - Creole's Starved is set only by a read past the data received, and it aborts the attempt at once (STRM-01).
- **How far it reads.** Before a retry it grows the window, counted from the value's start, to `available + max(available, BufferSize)`, and reads until it holds that or the input ends (`StreamingDriver.cs`:77–131).
  - `BufferSize` is 4,096 by default, and the window is capped at `MaxBufferedCharacters`.
  - A value still inconclusive at the cap fails at its start.
- **Request and response.** It yields each value before reading again (`Parser.Streaming.cs`:137–141). But each grow reads to its target, so its guide says interactive sources stall. For those it offers an overload that splits the input on a delimiter character and parses each frame as final (`docs/streaming.md`, Limitations; `StreamingDriver.cs`:164–254).
- **Cancellation and threads.** The token is checked before each read, and about every 64 parser entries inside the parse (`ParseContext.cs`:547–573). The parse runs synchronously inside `MoveNextAsync`.
- **Changes in 2.4.0.** Parlot PRs #564 and #569 changed only what happens between values. An error in white space at a window's end is now inconclusive too, and white space between values is no longer kept in the window. The growth rule, the yield and the overloads are unchanged.
- **A correction to the research note.** Finding 23 says Parlot's `int` offsets cap inputs at `int.MaxValue` characters. That cap is the compacting buffer's. For `ParseManyAsync`, the driver keeps a `long` offset, and reported positions saturate at `int.MaxValue` (`StreamingDriver.cs`:29, 284–287).

The maintainer chose fork 1 on 2026-11-27, taking the recommended option. Later that day, shown Parlot's stall, the maintainer chose fork 4C. The other forks turn on the facts above and on the criteria.

## Criteria

Fixed before the build. Forks 1 and 4 are the maintainer's choices, and forks 2, 3 and 5 are settled by argument. The build checks the chosen options against these criteria. Fork 6 is decided by criterion 8.

1. **Same results as bytes in memory** (ADR-0030 point 10). The inputs are:
   - JSON lines: valid; a grammar error in a middle record; invalid UTF-8 in a middle record; a byte-order mark; multi-byte characters.
   - Example B's three layers behind `Layer.Utf8()`.
   - A pipeline whose parser changes its lexer's mode inside each record.

   Each input is delivered three ways:
   - one byte per read, with each next read pending until the attempt has run;
   - 1 to 64 bytes per read, from a fixed seed;
   - all at once.

   `ParseManyAsync` must yield the results `ParseMany(ReadOnlyMemory<byte>)` yields: the same number, the same values, and the same errors, meaning message, expected set, context lines, and spans with the same byte and character offsets. Failed results hold no text until STRM-03, as over a blocking `Stream`. With one byte per read, an internal counter shows a restart inside every record longer than one byte, so the restart path runs.
2. **No stray state** (the card's second acceptance criterion, ADR-0030 point 11). An attempt starves inside a `Lookahead` and a `Context` after recording a failure. After the retry, no layer holds a failure, save or context frame left by the starved attempt.
3. **Bounded work.** An item whose last attempt reads *n* bytes runs:
   - at most ⌈log₂ *n*⌉ + 2 attempts over a stream that returns every byte asked for;
   - at most one attempt more than the reads it took, over a stream whose every read is short and whose next read pends.
4. **The limit caps one item.** With `MaxBufferedItems` at 1,000:
   - A 5,000-byte record is the last result, `Failed` with the limit's error as ADR-0039 point 4 states it, after at most ⌈log₂ 1,001⌉ + 2 attempts.
   - The driver never holds more than 1,001 bytes past the root buffer's horizon.
   - A 900-byte record parses.
5. **Request and response.** The grammar's records end in a terminator that the item consumes. A producer writes each record in two writes, flushing between them, and writes record *k* + 1 only after the consumer has received item *k*'s result. Every record parses within a 10-second timeout.
6. **Cancellation and source errors.**
   - Cancelling the token while the driver awaits a read ends the enumeration with `OperationCanceledException`, both when the token is passed as an argument and through `WithCancellation`.
   - A pipe writer that completes with an exception makes the enumeration throw that exception.
   - The caller's stream is left open.
7. **Bounded memory** (the card's first acceptance criterion). The memory harness parses JSON lines with `ParseManyAsync` from a `Pipe` written by a producer task. Each size runs in its own process, and the retained heap is sampled as [scripts/memory-gate.py](../../../scripts/memory-gate.py) samples it.
   - With uniform records at 10 MB, 1 GB and 4 GB, the peak at 4 GB is at most 1.5 times the peak at 10 MB, the Phase 1 threshold ([ADR-0003](0003-numeric-gate-thresholds.md)). At 4 GB, root offsets pass both `int.MaxValue` and `uint.MaxValue`.
   - The same inputs at 10 MB and 1 GB, with one 4 MB record in the middle: the 1 GB peak is at most 1.5 times the 10 MB peak, and both exceed the uniform 10 MB peak by at least 4 MB. So the peak tracks the largest record, not the input.
8. **Fork 6's trial.** A BenchmarkDotNet benchmark runs `ParseMany` over JSON lines held in memory as bytes. Option 6B is adopted only if it meets both conditions in each of three default-job runs:
   - its mean time is at least 10% below 6A's;
   - its allocation is no higher.

   Otherwise 6A stays. The measurements are recorded in either case.
9. **Nothing else changes.**
   - Existing tests pass.
   - If the build changes code the Phase 0 gate runs (`Cursor`, a plan, or `StreamBuffer`'s read path), three gate runs before and three after show a change no larger than the runs' spread.
   - `dotnet publish tests/Creole.AotSmoke -c Release` reports no new warnings (G8).
10. **Documented.**
    - [docs/concepts/pipelines.md](../../../docs/concepts/pipelines.md) covers `ParseManyAsync`: that a starved item restarts, so user functions can run more than once for one record; that `MaxBufferedItems` caps one record; when an item waits for data past its end (fork 5); cancellation; and `AsStream()` for a `PipeReader`.
    - docs/engines/cursor.md and docs/concepts/custom-scanners.md say what a cursor sees where streamed input runs out (the card's 2026-11-25 note).

## Options

### Fork 1. The public surface

#### 1A. One overload over a `Stream` (chosen)

`Pipeline<byte, TOut>` gets `ParseManyAsync(Stream input, ParseOptions? options = null, CancellationToken cancellationToken = default)`, returning `IAsyncEnumerable<ParseResult<TOut>>`. A `PipeReader` caller passes `reader.AsStream()`.

- **For:**
  - It is ADR-0044's rule for the blocking overloads: `AsStream()` adapts a `PipeReader` in one call, and a `Stream` also covers files and sockets.
  - One overload for GEN-06 to mirror.
  - The check under Context shows `AsStream().ReadAsync` returns as soon as any data arrives, ends at 0 and honours the token, which is all the driver needs.
- **Against:**
  - It is less easy to find for a caller holding a `PipeReader`, such as ASP.NET's `Request.BodyReader`; the docs show `AsStream()`.
  - § Sources and ADR-0038 point 2's table list `PipeReader` as a source, and change on acceptance.

**Result:** Chosen by the maintainer on 2026-11-27.

#### 1B. Overloads over a `PipeReader` and a `Stream`

The whitepaper's current position. The `PipeReader` overload forwards to `AsStream()`, or reads the pipe's buffers directly.

- **For:** easier to find for pipe users.
- **Against:**
  - One more overload, for no capability `AsStream()` lacks.
  - A different rule from ADR-0044's blocking overloads, which take only a `Stream`.
  - Reading the pipe directly saves no copy: the root copies each byte into its buffer's segments either way.

**Result:** Not chosen; the maintainer chose 1A.

Either way, the signature follows .NET's convention for async streams:

- The token can also come through `WithCancellation`, via `[EnumeratorCancellation]` on the iterator.
- It is passed to each `ReadAsync` and checked before each attempt, never inside one. That follows ADR-0044's option 4A: no check on the engines' hot path.
- Awaits do not capture the synchronization context.
- The stream is the caller's, and is left open.
- `source:` arrives with STRM-03, as for the other streamed overloads.

### Fork 2. What a restart rewinds

#### 2A. Every layer back to a bookmark taken at the item's start (the whitepaper's position; recommended)

At each item's start, after every layer has forgotten its furthest failure and the parser's channels are back at their initial modes, the driver bookmarks every layer:

| Where | The bookmark holds |
| --- | --- |
| The parser | The item's start in the last buffer, held as the parser cursor's floor. The last buffer then keeps the item, and through [ADR-0040](0040-a-producing-layer-keeps-input-only-for-outputs-its-reader-can-go-back-to.md)'s pins every buffer below keeps the input needed to produce it again. |
| Each layer | Its position in its input, its layer state, its mode log's length, its diagnostic count, and how many items its output buffer holds. Its failure record is empty there, and every scoped part is at its base. |
| The root | Nothing. It keeps every byte received from its buffer's horizon on, and is never rewound. |

On Starved, the driver does the following, in ADR-0030's order:

1. It undoes the mode changes made since the bookmark, the parser's and each layer's.
2. In every buffer but the root's, it discards the items past the bookmarked count. It puts each layer back at its bookmarked position and state, which withdraws the diagnostics raised since.
3. It clears every layer's failure record, including saves, notes, frames and held starts, and every buffer's Starved flag.
4. It awaits more data (fork 4), then runs the item again.

Items that layers produced before the bookmark are kept, and so are diagnostics they raised there, which wait for this item (ADR-0038 point 4).

- **For:**
  - Exact by construction. Every layer is where it was when the item started, so the retry is the run a parse with all its data makes, which is point 10.
  - It is what the whitepaper and ADR-0030 point 7 say, so no accepted record changes.
  - Parlot does the same for its one layer: it discards the attempt and parses the value again from its start (Context).
  - The bookmark costs nothing per item that fits in the data already received, and holds nothing those pins do not already hold.
- **Against:**
  - Every layer runs its steps for the item again on each retry, not only the parser. The doubling bounds the total to a constant factor of the item's size (fork 4), and only items that straddle a read pay it.
  - Each layer kind needs a way to take and restore its bookmark.

**Result:** Recommended, by argument.

#### 2B. Keep what the layers produced, and restart only the parser

A starved step already adds nothing and leaves its layer's position and state alone (Context). The restart would clear the parser's failure record and every Starved flag, make each layer's starved step withdraw what it recorded since it began, undo the parser's mode changes, and run the parser again over the buffered items.

- **For:**
  - Layers below the parser do not run their completed steps again.
  - No bookmark per layer.
- **Against:**
  - Its exactness is unproven where the parser changes modes. Undoing the parser's changes discards and re-runs steps of the layers below, which withdraws, lowers and re-records failures in records that 2B keeps. A parse with all its data never makes that undo. Whether it always ends in the same record turns on lowering, merging and withdrawal composing across layers. That is the hazard ADR-0030 point 5 names for reuse, and no one has shown it here.
  - It changes ADR-0030 point 7, an accepted record, which says the restart rewinds every layer and clears every record.
  - It saves a constant factor, and only on items that starve.

**Result:** Ruled out by argument.

### Fork 3. Where received bytes wait, and how they count toward the limit

#### 3A. The root holds them until a step pulls them, and they count toward the limit (recommended)

The driver reads with `ReadAsync` into pooled chunks that the async root holds. The root's step moves bytes from them into the root buffer when the lowest layer asks, as the blocking roots' steps do. When it has none left and the stream has not ended, the step reports Starved.

Bytes received and not yet pulled count toward `MaxBufferedItems`. The driver never holds more than `MaxBufferedItems` + 1 bytes past the root buffer's horizon. So at the cap, the next attempt either finishes or pulls past the limit, and the existing check in `StreamBuffer.Fill` raises the limit's error inside the lowest layer's step, with its span, layer and context lines.

- **For:**
  - The limit's error is raised where it is today, at a pull, so ADR-0039 points 3 and 4 hold unchanged.
  - `MaxBufferedItems` bounds what the root holds under `ParseManyAsync`, received bytes included. That is the bound a server sets it for.
  - An item that fits in the data already received never waits.
- **Against:**
  - The root counts bytes no layer has asked for, which the other roots never hold. Between items, a large read can be cut short to stay under the cap.

**Result:** Recommended, by argument.

#### 3B. As 3A, but only pulled bytes count

As every other root. Read-ahead is bounded by fork 4's cap and one read's size, not by the limit.

- **For:** the same rule for every root.
- **Against:** the root can hold up to one read beyond the limit. The difference from 3A is at most one read, so 3B buys a uniform wording at the cost of a limit that does not quite bound memory.

**Result:** Ruled out by argument.

#### 3C. The driver adds received bytes straight to the root buffer

No staging: the root buffer is the window, and read-ahead counts as buffered items without a rule.

- **For:** one place holds the bytes.
- **Against:** the root's step never adds anything, so `StreamBuffer.Fill`'s check after each step never sees the root grow. The driver would have to raise the limit's error itself, outside any layer's step. That error would have no context lines, unlike ADR-0039 point 4's, or the check would have to move onto the read path.

**Result:** Ruled out by argument.

**Open for the build:** whether the async root is `StreamUpstream` with a second mode or a class of its own. ADR-0044's "What would change this" expects the overloads to stay either way. Both meet the criteria. The build tries the shared class first, and keeps it only if both modes read as plainly as one class each.

**Result:** a class of its own, `AsyncStreamUpstream`, decided on a sketch of the shared class during the build on 2026-11-27. The two share only the loop that adds bytes to the buffer with their spans. A shared class would branch in its step on where the bytes come from. It would also carry the staged bytes and their two ends, the pending read and how much it asked for, the end of the stream, and the cancellation source, none of which the blocking mode uses. The blocking overloads are unchanged.

### Fork 4. How much the driver reads before a retry

The attempt could read the bytes from the root buffer's horizon to the end of the data received. Call that what it saw.

*Revised 2026-11-27 during the build:* this first said "from the lowest layer's bookmarked position". The root buffer's horizon is where the item's input starts, because the bookmark pins the item's start through every layer. The lowest layer's bookmark can lie ahead of it, when that layer produced the item's first items as lookahead before the item started. Counting from the horizon measures the item, and it is the same horizon the limit counts from (fork 3).

#### 4A. At least twice what the starved attempt saw (the whitepaper's position, and Parlot's)

Before retrying, the driver reads until it holds at least twice what the attempt saw, and at least one read's size more. It stops early when the stream ends or fork 3's cap is reached.

- **For:**
  - The attempts form a geometric series, so the work per item is linear in its size, even against a peer that trickles bytes.
  - It ends: each retry has twice the data, sees the true end, or reaches the cap, where the limit's error ends the item.
- **Against:** a record that arrives split across reads waits for twice its starved prefix. In a request-and-response protocol the rest never comes, because the peer is waiting for the reply. Parlot documents this stall and adds a delimited overload for interactive sources (Context).

**Result:** Not chosen; the maintainer chose 4C.

#### 4B. Retry after each read

- **For:** a record completes as soon as its last byte arrives.
- **Against:** with small reads, an item of *n* bytes runs about *n* / *r* attempts over its growing prefix, which is quadratic. A pipe that delivers 4 KB at a time would parse a 16 MB record about 4,000 times.

**Result:** Ruled out by argument.

#### 4C. As 4A, or as soon as the source has nothing more ready (chosen)

Each read asks for the whole shortfall. The driver retries in either of two cases:

- it holds 4A's target;
- a read returned fewer bytes than asked, and the next read does not complete at once.

That pending read is kept, and awaited if the retry starves again. The driver stops at the stream's end and at the cap, as 4A does.

- **For:**
  - Request and response stays live. Once a peer has sent a whole record and waits, the next read pends, and the item is retried with what arrived. Criterion 5 checks it.
  - Sources that fill each read, such as memory and files, keep 4A's linear bound, and so do sources whose next read completes at once because data is ready.
  - `Stream` allows short reads while data is ready (Context), which is why a short read alone does not trigger a retry.
- **Against:**
  - A peer that sends an item in many pieces with pauses makes the driver retry after each pause. The work is then about the item's size times its pauses. An item of *n* bytes trickled one byte per pause costs about *n*² / 2 byte reads, so a hostile peer can make a server's work grow with the square of `MaxBufferedItems`. The docs tell servers to set a small limit and to cancel slow reads.
  - The driver keeps one read pending across an attempt, into a chunk it must not return to the pool until that read completes.
  - § The restart protocol's Cost paragraph changes on acceptance.

**Result:** Chosen by the maintainer on 2026-11-27.

### Fork 5. When an item is yielded

#### 5A. As soon as its parser returns; the look for the next item waits for the caller (recommended)

As `ParseMany` does today: item *k* is yielded when the parser returns, and the driver looks for item *k* + 1 only when the caller asks for the next result. So an item waits for data past its end only when something reads past it:

- its own grammar, such as a trailing repetition or option;
- a layer's step that produces its last item, such as a number's scan, which must see the next character;
- a dispatched choice at the item's end, which peeks first. STRM-01 found that this can cost one extra restart where trying the alternatives in order would not.

The docs tell request-and-response protocols to end each record with a terminator that the item consumes. Criterion 5 checks that such a protocol never waits for the next request.

- **For:**
  - A server can reply to record *k* before record *k* + 1 is sent, given fork 4C.
  - Parlot yields the same way (Context).
  - Nothing needs the look ahead: the extra result for diagnostics that no item took (ADR-0038 point 4) is found when the input ends, after the last item.
- **Against:** a grammar that reads past its record's end waits for the next record or the stream's end. That is inherent in streaming the grammar, so the docs cover it.

**Result:** Recommended, by argument.

#### 5B. Look for the next item before yielding

- **For:** the caller would know whether an item is the last.
- **Against:** every request-and-response protocol deadlocks, because the reply to record *k* is what prompts record *k* + 1.

**Result:** Ruled out by argument.

### Fork 6. Failure logs for streamed items

#### 6A. Record as it goes (today's rule; chosen)

Every layer keeps a failure log for every item, as ADR-0038 point 3 says for `ParseMany`.

- **For:** no second run, and no extra code.
- **Against:** every successful item pays for recording. ADR-0027 measured that cost at 14–27% of a successful in-memory `Parse`.

**Result:** Chosen. 6B did not meet criterion 8 (below), and on 2026-11-27 the maintainer kept the criterion as fixed rather than measure text instead.

#### 6B. Run each item without logs, and run a failed item again from its bookmark with them

ADR-0027's rule, applied per item. If an item fails, a layer throws, or the depth limit or the stack check ends it, the driver rewinds to the bookmark as for Starved, turns on every layer's log, and runs the item again. That run's result is returned. If adopted, the rule applies to `ParseMany` over every source and to `ParseManyAsync` (ADR-0038 point 3).

- **For:** successful items skip recording. The restart's bookmark is the only machinery it needs.
- **Against:**
  - Layers must switch their logs on and off between items, where today a run fixes them when it opens.
  - `ParseMany` over every source would take bookmarks, holding each item whole as `ParseManyAsync` does.
  - A failed item costs about two runs.

**Trial, 2026-11-27.** Built in a separate worktree on 337d676: a run-wide recording switch that each step's cursor reads, and an item run that, under 6B, takes bookmarks, runs each item with the switch off, and runs a failed item again from its bookmark with it on. With `ParseMany` routed through 6B, all 2,240 tests passed, so 6B gives the same values and errors. `RecordBenchmarks` parses 1,000 JSON lines held in memory, each the benchmarks' small JSON document on one line, through the same loop over `PipelineItems` for every variant. Three default-job runs (20261127-220743, -221002 and -221224) held every variant in each invocation:

| Run | Bytes, 6A | Bytes, 6A with bookmarks | Bytes, 6B | 6B / 6A | Text, 6A | Text, 6B | 6B / 6A |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 220743 | 70.96 ms | 73.42 ms | 63.94 ms | 0.901 | 25.04 ms | 19.16 ms | 0.765 |
| 221002 | 71.43 ms | 72.78 ms | 66.09 ms | 0.925 | 25.73 ms | 19.20 ms | 0.746 |
| 221224 | 72.94 ms | 73.12 ms | 63.75 ms | 0.874 | 25.50 ms | 18.57 ms | 0.728 |

Allocation was equal in every pair: 41.99 MB over bytes, 2.01 MB over text. Bookmarks alone cost 0 to 4%.

Read against criterion 8, which measures bytes: 6B is 9.9%, 7.5% and 12.6% faster, so it misses the 10% bar in two of the three runs and is not adopted by the criterion as fixed. Over text it is 23 to 27% faster in every run. The difference has a cause outside fork 6. `Layer.Utf8()` keeps no failure log, so nothing in it gets faster, and it takes most of the time over bytes (BENCH-02's progress note of 2026-11-27). The bytes pairs allocate 42 MB for 1 MB of input, against 2 MB over text, which is the same layer's cost. Criterion 8 was fixed before the trial, so changing it now is the maintainer's decision.

**Result:** Ruled out by trial: 7.5% and 9.9% faster over bytes in two of three runs, short of criterion 8's 10%. The maintainer kept criterion 8 as fixed on 2026-11-27, so the gain over text does not adopt it. The trial's code was not merged.

## Decision

Proposed, with one option chosen in every fork: **1A, 2A, 3A, 4C, 5A and 6A**. 1A and 4C are the maintainer's choices; 2A, 3A and 5A are settled by argument; 6A stands because 6B failed criterion 8's trial. The root is a class of its own (fork 3's Result). The record is ready for the maintainer to accept when STRM-02 completes.

1. **Surface.** `Pipeline<byte, TOut>.ParseManyAsync(Stream input, ParseOptions? options = null, CancellationToken cancellationToken = default)` returns `IAsyncEnumerable<ParseResult<TOut>>`. Its token is checked between attempts and passed to every read. The stream is left open. A `PipeReader` passes `AsStream()`.
2. **Bookmark and restart.** Each item's start bookmarks every layer as fork 2A's table says. On Starved, the driver does four things in order:
   - it undoes mode changes;
   - it discards items produced since the bookmark and puts each layer back at its bookmarked state;
   - it clears every failure record, every scoped part and every Starved flag;
   - it reads more, and runs the item again.

   The root is never rewound.
3. **Received bytes and the limit.** The root holds received bytes until a step pulls them, and they count toward `MaxBufferedItems` from the root buffer's horizon. The driver holds at most `MaxBufferedItems` + 1 bytes past that horizon. An item that needs more then ends with the limit's error, raised at the pull as today.
4. **When to retry.** Each read asks for the shortfall. The driver retries in any of four cases:
   - it holds at least twice what the starved attempt saw, and one read's size more;
   - a short read is followed by a read that does not complete at once;
   - the stream ends;
   - the cap is reached.
5. **Yielding.** Each item is yielded when its parser returns, and the next is looked for only when the caller asks.
6. **Failure logs.** 6A: every layer records failures as it goes, for every item, as ADR-0038 point 3 says for `ParseMany`. Items take no bookmark under `ParseMany`, so `ParseMany` holds no more than its parser can still go back to.

What would change the recommendation:

- **2A:** if retries prove costly in a measurement, and someone shows 2B exact under mode changes. Then 2B saves the layers' re-runs.
- **3A:** if a caller needs read-ahead beyond the limit for throughput. Then 3B, with the limit's wording qualified.
- **4C:** a common source whose reads pend while data is ready would make retries frequent; that needs a measurement. Or a hostile-peer case may need a hard bound, in which case a per-item budget of idle retries would cap the work at the cost of liveness past it.
- **6A:** a cheaper decoding layer. If LAY-10 or BENCH-02 cuts `Layer.Utf8()`'s cost, rerun the trial against criterion 8, rebuilding 6B beside `RecordBenchmarks`. Over text, 6B was 23 to 27% faster.
- **5A:** none expected. Dispatch's extra restart at an item's end would become a fallback to trying the alternatives in order on a starved peek, if request-and-response users hit it.

## Consequences

- **Easier:**
  - JSON lines, logs and protocols parse from a socket or pipe with memory bounded by the largest record, keeping byte offsets past 4 GB.
  - Request-and-response servers can reply per record.
  - STRM-03 has one streamed driver to add captured lines and `source:` to.
- **Harder:**
  - Each layer kind takes and restores a bookmark, and GEN-06's generated layers must do the same.
  - A record larger than `MaxBufferedItems` bytes, 16 MiB by default, fails under `ParseManyAsync`. The docs say so.
- **Ruled out:**
  - `PipeReader` overloads.
  - Async combinators. [§ Why not async combinators](../../../whitepaper.md#why-not-async-combinators) is unchanged.
  - A cancellation check inside an attempt.
  - `ParseManyAsync` over a `TextReader` on character pipelines: an async text source goes through bytes and `Layer.Utf8()`, as § Sources says.
- **Risks and how they are watched:**
  - **A bookmark misses a part.** Criterion 1 compares every chunking with the in-memory result on grammars with modes and three layers, and criterion 2 inspects the failure log after a retry. ADR-0030's list in point 1 is the checklist for what a bookmark must cover.
  - **Diagnostics cannot be tested yet.** No combinator raises one until ERR-05, so the bookmark's diagnostic count is checked through an internal test hook, and ERR-05 and ERR-07 add restart cases to their tests.
  - **The cap and the doubling disagree.** Criterion 4 checks that an item over the limit ends after a bounded number of attempts rather than retrying.
  - **A pending read outlives its chunk.** Under 4C, a read can be in flight when the enumeration ends or is disposed. Disposing cancels it and does not wait for it, and its memory goes to the garbage collector instead of back to the pool, since the stream may still write into it. *Revised 2026-11-27 during the build:* this first said the driver awaits the read, or cancels and then awaits it. Awaiting would hang disposal on a stream that ignores its token. `StoppingEarlyWithAReadPendingCancelsItWithoutWaitingForIt` disposes an enumeration whose pending read never completes.
  - **White space between items counts toward the next item.** A `Layer.Tokens` step skips, then reads its item, so white space between records is held as the next item's, and counts toward its limit. Parlot 2.4.0 stopped keeping white space between values (Context). Creole matches that only if a measurement shows long gaps between records matter.

## Changes made

None yet. On acceptance:

- **Whitepaper.**
  - § Sources: the async row lists `Stream` only, with `AsStream()` for a `PipeReader`.
  - § The restart protocol: step 1 says what a bookmark holds and that the root is not rewound. Step 4 and the Cost paragraph say the driver retries once it has twice the data or the source has nothing more ready, what a trickling peer costs, and that received bytes count toward `MaxBufferedItems`, with at most one more held.
  - Fork 6's outcome, if 6B is adopted.
- **Records.** ADR-0038 point 2's table, through this record, as ADR-0038 refined ADR-0027.
- **Cards.**
  - [STRM-02](../../active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md): scope states the decision.
  - [STRM-03](../../active/08-streaming/STRM-03-streaming-diagnostics.md): `ParseManyAsync(Stream)` takes `source:` and holds captured lines.
  - GEN-06: generated layers take and restore bookmarks.
- **Research.** Finding 23 of the Parlot note gets a dated correction: the `int.MaxValue` cap is the compacting buffer's, and `ParseManyAsync`'s positions saturate instead.
