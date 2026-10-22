# ADR-0013: Slices are views of in-memory input and copies of streamed input

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-22 |
| Cards | [INT-03](../../active/03-interpreter/INT-03-execute-the-core-combinators.md), [INT-08](../../active/03-interpreter/INT-08-single-layer-parse-api.md), [LAY-01](../../active/06-layers-and-pipelines/LAY-01-streambuffer-and-pulling-cursor.md), [LAY-05](../../active/06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md), [CORE-12](../../active/02-core-combinators/CORE-12-custom-primitives.md) |
| Whitepaper | [§ Text capture](../../../whitepaper.md#text-capture), [§ The stream buffer](../../../whitepaper.md#the-stream-buffer), [§ Design choices](../../../whitepaper.md#design-choices) |
| Research | — |
| Supersedes | — |

## Context

§ Text capture says `Slice()` returns "a `ReadOnlyMemory<char>` view of the consumed source without copying", and that tokens should normally carry slices, as Worked Example A's `JTok` does. § The stream buffer makes each boundary's buffer `ArrayPool`-backed, and § Bookmarks and retention releases segments behind the retention horizon back to the pool.

The two statements conflict for streamed input.

- **In-memory input** (`string`, `ReadOnlyMemory<char>`, § Sources) is immutable and owned by the caller. The GC keeps it alive for as long as a slice refers to it, so a view is always safe.
- **Streamed input** (`TextReader`, or a `PipeReader` or `Stream` decoded through a `Layer<byte, char>`) lives in pooled segments. A token keeps its characters' segment pinned only while the token is buffered. Once `ParseMany` yields a record and the horizon moves past it, the segment returns to the pool and is refilled. A view held in that record's tokens or values then silently shows other text.
- A slice that spans two segments cannot be one `ReadOnlyMemory<char>` view at all.

INT-03 implemented `Slice` as a view when the consumed characters lie in one segment held as memory, and a copy otherwise. That is safe today only because nothing pools segments yet; LAY-01 adds pooling. The INT-03 critic review flagged both the deviation from § Text capture and the untracked hazard.

Constraints: § Guarantees promises memory at a boundary proportional to the longest pinned window, not to the input. G8 requires AOT and trim safety, which none of the options affect. The maintainer prefers the simple standard approach until a measured need appears.

## Options

### A. GC-owned root segments

Stream root segments are allocated as ordinary arrays and never returned to the pool, so a slice is a view that keeps its segment alive through the GC. A slice copies only when it spans a segment boundary.

- For: zero-copy for almost every token, streamed or not.
- Against: one small retained slice keeps its whole segment, tens of kilobytes, alive. A stream that keeps one value per record can retain close to its whole input, which breaks the spirit of § Guarantees for exactly the inputs streaming exists for. Root segments stop being pooled, which changes § The stream buffer, and allocation grows with input size.

### B. View over in-memory input, copy over streamed input (chosen)

`Slice` returns a view when the parse's input is held whole in memory, and a fresh copy of the consumed characters when it is streamed.

- For: always safe, with no lifetime rules for users. Simple to state and implement: the cursor knows whether its input is caller-owned memory. Pooling and the memory guarantee are unchanged. A later move to views for streams (A) would not change any result, only allocation and retention.
- Against: over streams, `Slice` allocates about as much as `Text`, so "slices, not strings" saves allocation only for in-memory input.

### C. Pooled views with a documented lifetime

`Slice` is always a view, valid only until the top-level item that produced it finishes. This is the model of `Utf8JsonReader.ValueSpan` and `System.IO.Pipelines` buffers (from memory, *unverified*), which are safer there because a span cannot escape or the lifetime ends at an explicit call.

- For: no copies anywhere.
- Against: `ReadOnlyMemory<char>` can escape freely and nothing enforces the lifetime. Misuse corrupts data silently, long after the parse, in user code that looks correct.

### D. Decide only the in-memory rule now

State the in-memory rule, and record streamed slices under § Still open for LAY-01 to settle with measurements.

- For: no commitment before measurements exist.
- Against: leaves a known corruption hazard as an open question, and LAY-01 could still ship pooled views by accident.

Pidgin avoids the question by passing the consumed span to a callback (`p.Slice((span, _) => …)`, docs/concepts/sequencing.md). That is safe but a different API: tokens could not carry text without allocating a string, so it was not considered further.

## Decision

Option **B**. Accepted by the maintainer on 2026-10-22.

1. When the parse's input is held whole in memory (`string`, `ReadOnlyMemory<char>`), `Slice()` returns a view of that input. It never copies.
2. When the input is streamed, so its characters live in a stream buffer, `Slice()` returns a new copy of the consumed characters, made when the slice is taken. A slice never refers to pooled memory.
3. `Text()` allocates a `string` in both cases.
4. The rule binds every engine (interpreted, generated and fused) and any public way for a custom scanner to take a slice (CORE-12).
5. Every public entry point that takes in-memory text builds its cursor so the cursor can return views (INT-08, LAY-05). Within the engine, a cursor over caller-owned memory returns views, and any other cursor copies.

Deciding reason: B is the only option that is safe without lifetime rules and keeps § Guarantees intact, and it is the simplest. Its cost falls only on streamed input, and can be removed later without changing any result.

What would change this: LAY-01 or LAY-05 benchmarks showing that slice copies are a significant share of a streamed parse's time or allocation. Option A, with its retention trade-off documented, is then the fallback.

## Consequences

- In-memory parses keep zero-copy slices. Streamed parses allocate one array per slice, about the cost of `Text()`.
- `ReadOnlyMemory<char>` stays the one token text type for both kinds of input, so grammars do not change with the source.
- Stream buffers stay pooled, and the bounded-memory guarantee is unaffected by what users keep.
- Ruled out: views into pooled segments, and any slice lifetime shorter than the GC's.
- Watched by tests: a slice over caller-owned memory shares the caller's string or array, and a slice over any other cursor does not alias the source's segments.

## Changes made

- Whitepaper: § Text capture states the rule and links this record; § The stream buffer says no value the parse hands out may view a pooled segment; § Design choices says slices save allocation on in-memory input and cost about as much as `Text()` on streams.
- Cards: [INT-08](../../active/03-interpreter/INT-08-single-layer-parse-api.md) and [LAY-05](../../active/06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md) build in-memory cursors over caller-owned memory; [LAY-01](../../active/06-layers-and-pipelines/LAY-01-streambuffer-and-pulling-cursor.md) gains the copy rule in scope and a criterion that a slice survives its segment's reuse; [CORE-12](../../active/02-core-combinators/CORE-12-custom-primitives.md) applies the rule to any public slice method for scanners; [INT-03](../../active/03-interpreter/INT-03-execute-the-core-combinators.md) returns to Build to make `Cursor.Since` copy for every cursor not built over caller-owned memory, and to fix sequencing.md.
