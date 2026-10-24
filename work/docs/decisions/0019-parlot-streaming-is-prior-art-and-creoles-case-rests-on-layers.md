# ADR-0019: Parlot 2.0's single-layer streaming is prior art, and Creole's streaming case rests on layers

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-24 |
| Cards | [FND-10](../../active/01-foundations/FND-10-move-the-parlot-baselines-to-200.md) |
| Whitepaper | [§ Background and motivation](../../../whitepaper.md#background-and-motivation), [§ Streaming and async input](../../../whitepaper.md#streaming-and-async-input), [§ Comparison with existing .NET libraries](../../../whitepaper.md#comparison-with-existing-net-libraries), [§ Risks](../../../whitepaper.md#risks), [§ References](../../../whitepaper.md#references) |
| Research | [Parlot 2.0 baselines and generator](../research/2026-10-24-parlot-2-0-baselines-and-generator.md) |
| Supersedes | — |

## Context

Parlot 2.0.0 parses `TextReader` input with bounded memory ([research note](../research/2026-10-24-parlot-2-0-baselines-and-generator.md), finding 23):

- **One value.** The parse runs once, synchronously, over a buffer that "only retains the text a parser can still move back to". When the input outgrows the first block, the parse moves to a thread-pool thread that blocks on the reads.
- **Successive values.** `ParseManyAsync` parses each value from a window. If the parse reached the end of the window, it reads more, at least doubling the window, and parses the value again.
- **Cuts.** `Commit()` is a cut that releases buffered text.
- **Limits.** Offsets are `int`. Generated parsers support only the single-value mode.

These are the choices [§ Streaming and async input](../../../whitepaper.md#streaming-and-async-input) makes: synchronous parsing over buffered data, restarting an item that runs out of data with at least double the data, and a blocking thread for huge documents. The whitepaper does not mention Parlot there. [§ Comparison](../../../whitepaper.md#comparison-with-existing-net-libraries) has no row for streaming input. [§ Background](../../../whitepaper.md#background-and-motivation) and [§ Risks](../../../whitepaper.md#risks) describe Parlot's strengths as speed and build-time generation only.

For the other libraries, read from the XML documentation of the versions the benchmark harness pins: Sprache 2.3.1 and Superpower 3.2.1 parse a `string` only. Pidgin 3.5.1 also parses a `TextReader`, a byte `Stream` or an `IEnumerator<T>`; its documentation does not say how much input it buffers.

G2 is "Streaming n-layer pipelines: lazy, pull-based, bounded memory". G3 and G4 are cross-layer backtracking and feedback.

## Options

### A. Leave the whitepaper as it is

- Pros: no change.
- Cons: it omits the closest prior art. § Streaming's choices read as Creole's own when Parlot shipped them first. § Comparison is incomplete by omission.

### B. Record Parlot's streaming as prior art, and rest the case on layers (recommended)

G2 does not change. The whitepaper states which streaming choices Parlot already made, and what Creole adds: streaming at every layer boundary, cross-layer backtracking and feedback over the buffers, byte sources through a decoding layer, and offsets beyond 2 GB.

- Pros: accurate and checkable. It also points the streaming cards at a working design.
- Cons: concedes that single-layer streaming of text does not differentiate Creole.
- Effect on goals: none. G2 is about n layers, and the Phase 1 memory gate is unchanged.

### C. Narrow G2, or move streaming out of 1.0

Since Parlot covers single-layer streaming, cut `ParseManyAsync` and the restart protocol from 1.0, or narrow G2 to the n-layer part.

- Pros: a smaller 1.0.
- Cons:
  - Streaming is not an add-on to layers. The stream buffer at each layer boundary is how layers work (G2, G3), and Phase 1's memory gate measures it.
  - Record streams (`ParseManyAsync` over a `PipeReader`) are a user need whether or not Parlot has them.
  - Little would be saved.

Adopting Parlot's mechanisms is not an option here. A compacting buffer, pins, a sticky `HitEnd` and window doubling are design questions for [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md), [STRM-02](../../active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md) and [STRM-04](../../active/08-streaming/STRM-04-huge-single-documents.md), which now link the research note.

## Decision

**B.** The whitepaper changes as follows:

- **§ Comparison** gets a row after "Input type":

  | Capability | Sprache | Superpower | Pidgin | Parlot | Creole (proposed) |
  | --- | --- | --- | --- | --- | --- |
  | Streaming input | None; `string` only | None; `string` only | `TextReader`, byte `Stream` or `IEnumerator<T>` | `TextReader`, buffering only what can be re-read; `ParseManyAsync` for successive values | Any source at the root of a pipeline, with every layer streaming; `ParseManyAsync` with restart |

- **§ Streaming and async input** gets a second sentence in its opening paragraph: "Parlot 2.0 makes the same choices for one layer of text: a synchronous parse over a buffer that keeps only what can still be re-read, and a driver that re-parses a value that reached the end of the data. Creole applies them at every layer boundary, to byte sources, and to offsets beyond 2 GB."
- **§ Background and motivation:** Parlot's "What it added" cell gains "bounded-memory parsing of `TextReader` input". The sentence "so Creole's case cannot rest on speed alone" becomes "so Creole's case cannot rest on speed, build-time generation or single-layer streaming".
- **§ Risks:** the Parlot row's risk becomes "Parlot already offers fast, build-time-generated combinators that stream text", and its impact becomes "Speed and single-layer streaming do not differentiate Creole". The mitigation is unchanged.
- **§ References:** Parlot's entry gains a link to its streaming guide.

Deciding reason: these are facts a reader can check. G2 is about n layers, and Parlot changes what Creole can claim, not what it must build.

What would change this: if Parlot adds staged pipelines, such as a parser over another parser's output stream, the § Risks assessment needs a new record.

## Consequences

- Streaming is no longer presented as new in itself. The differentiators are layers, feedback, analysis, diagnostics and incremental parsing, as § Risks already says.
- No code or card changes. LAY-02, STRM-02 and STRM-04 already link the research note as prior art.
- The § Comparison row will need re-checking when Pidgin, Superpower or Sprache release new versions.

## Changes made

- **Whitepaper.**
  - [§ Comparison](../../../whitepaper.md#comparison-with-existing-net-libraries): the "Streaming input" row after "Input type", and a sentence in the opening paragraph that links this record.
  - [§ Streaming and async input](../../../whitepaper.md#streaming-and-async-input): the opening paragraph gains the sentences under Decision and links this record.
  - [§ Background and motivation](../../../whitepaper.md#background-and-motivation): Parlot's "What it added" cell, and the sentence on what Creole's case cannot rest on.
  - [§ Risks](../../../whitepaper.md#risks): the Parlot row's risk and impact.
  - [§ References](../../../whitepaper.md#references): Parlot's entry lists streaming and links its streaming guide.
- **Cards.** None changed. LAY-02, STRM-02 and STRM-04 already link the research note.
