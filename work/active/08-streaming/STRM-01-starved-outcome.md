# STRM-01: Starved outcome

| | |
| --- | --- |
| Epic | [Streaming and async input](../../README.md#strm--streaming-and-async-input) |
| Phase | Phase 1 — Layers |
| Status | Done |

## Summary

In streaming mode, running out of buffered data is neither success nor failure.

## Scope

- Sticky Starved flag on parse state, held behind the reference the cursor already holds (as `Failures` is), so a custom scanner that scans a copy of the cursor and discards it cannot lose it ([Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) finding 19).
- Every combinator that acts on a failure checks it first: choices, `Optional`, repetitions, `Expression.Build`, `Longest`, `Atomic`, `Not`, `Lookahead` and `Recover` (`Recover` in [ERR-05](../04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md)).
- `End` and root reads report Starved at the end of buffered data before the true end of the stream.
- Starved need not unwind the state that nodes open and close, such as context frames, saved failures, held starts and depth: the restart resets it ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 7). That includes the span holds a `StreamBuffer` keeps for its reader's `Spanned`, `WithSpan` and `Adjacent`, which the restart must clear ([ADR-0035](../../docs/decisions/0035-span-combinators-commit-like-sequences-and-hold-their-spans.md) point 6, [SPAN-04](../05-source-spans/SPAN-04-span-combinators.md)).

## Acceptance criteria

- [x] No combinator ever tries another alternative after a starved attempt.

## Blocked by

- [LAY-02 Marks and retention](../06-layers-and-pipelines/LAY-02-marks-and-retention.md)

## Unblocks

- [ERR-05 Recover combinator and strategies](../04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md)
- [STRM-02 Restart protocol and ParseManyAsync](../08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md)
- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)

## Whitepaper references

- [§ Results](../../../whitepaper.md#results)
- [§ The restart protocol](../../../whitepaper.md#the-restart-protocol)

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements

## Progress notes

- 2026-11-25 (Building): Started. Plan: keep the Starved flag on each layer's cursor source (the stream buffer), so cursor copies share it and reads through `Available` set it; a buffer whose upstream starves stays unended; choices and repetitions check it through `IMark.FailedEmpty`, and `Atomic`, `Not`, `Lookahead`, `Longest` (both engines), dispatch, `End` and each layer's step check it directly. No source starves until STRM-02, so tests use an internal starving upstream. Next: `/build` plan.
- 2026-11-25 (Building): Increment 1 done: the flag, set by reads and passed up through layers. `StreamBuffer` keeps a `Starved` flag behind `ICursorSource`, so cursor copies and reads through `Available` set it; an upstream that starves leaves its buffer unended and stepping stops. Tokens, Transduce, Utf8, Select and Where pass their input's flag up, and a starved step adds nothing, raises no error and does not run `atEnd`; `End` fails where the input received so far runs out. Found and fixed a spurious starve: `Cursor.Reset` fetched new input when a `Longest` (interpreted, light marks) moved forward to a winner that ended exactly at the end of the input received so far; it now moves to the segment holding the item before the position, so a rewind never fetches. Tests in `StarvedTests`, including every control shape to depth 3 on every starving prefix, under three lowerings (mutation-checked). All 1,914 tests pass. Next: increment 2, the combinator checks and the gate benchmark before and after.
- 2026-11-25 (Building): Increment 2 done: no combinator acts on a starved attempt. `LightMark` and `FullMark` treat a starved attempt as consumed, which covers `Or`, `Optional`, every repetition, separators, `Until` and `Expression.Build`. `Atomic` does not rewind it; `Lookahead` and `Not` fail without rewinding or inverting; the interpreted `Longest` stops at the first starved alternative; the compiled `Longest` fails where its scan starves and does not run its winner again after an earlier read starved (that re-run could disagree with the automaton, which a test reproduced through a debug assertion); dispatch fails on a starved look at the next item; `ScanPlan` fails where the input runs out, as the repetition it replaces does. Scoped state unwinds as on any failure. A primitive may still succeed after a starved read (a custom scanner, a span or trivia peek); the attempt then ends Starved however it ends. Tests: 18 grammars each put a counting probe where a combinator would go next; the probe runs at the true end and never after a starved attempt, under three lowerings. Reverting each changed plan file in turn fails at least one test. 1,952 of 1,952 tests pass. Whitepaper § Results and § The restart protocol, and this card's scope, now say every combinator that acts on a failure checks the flag (editorial: the criterion already said so). Gate before (4617dc7, clean, runs 20261125-163232, -163723, -164438) and after (uncommitted, runs 20261125-165545, -170234, -170644): 0.25× Pidgin both; 1.74× and 1.73× Parlot. Creole's median times moved by a factor of 1.03 while Parlot and Pidgin moved about as much, so the change costs nothing measurable. Both batches fail the 1.5× Parlot threshold because the harness now builds against Parlot 2.4.0, not the 2.0.0 the gate pins; this predates STRM-01 (reported to the maintainer). `Recover`'s check belongs to ERR-05. Every acceptance criterion appears met. Next: the maintainer reviews, commits and runs `/complete`.
- 2026-11-25 (Building): Rebased on feature/mode-01. Conflicts in `Cursor.cs`, `TokensLayer.cs` and work/README.md kept both sides; the Tokens step's starved check now follows `Layer.Modal`'s per-step item plan. `StarvedTests`' layer helper takes the new `PipelineRun`. 1,988 of 1,988 tests pass at a572cf2. Next: `/complete`.

## Outcome

Starved exists as a per-layer flag that reads set, layers pass up, and no combinator acts on. No source starves yet: [STRM-02](STRM-02-restart-protocol-and-parsemanyasync.md) adds the async roots and the restart that clears it.

- **The flag.** [StreamBuffer.cs](../../../src/Creole/StreamBuffer.cs) keeps `Starved`, exposed through [ICursorSource.cs](../../../src/Creole/ICursorSource.cs) and read by `Cursor.Starved` ([Cursor.cs](../../../src/Creole/Cursor.cs)) through the source reference, so a copy of the cursor and a read through `Available` set it for the original too (Phase 0 review finding 19). An upstream reports it through `IUpstream.Starved` ([IUpstream.cs](../../../src/Creole/IUpstream.cs)); its buffer then stays unended and runs no more steps. Input held in memory never starves.
- **Layers.** Tokens (and so `Layer.Modal`), Transduce, Utf8, Select and Where pass their input's flag up. A starved step adds nothing and raises no error, Transduce does not run `atEnd`, and Utf8 does not report a sequence cut off where its input starved.
- **`End`** fails where the input received so far runs out.
- **Combinators.** `LightMark` and `FullMark` treat a starved attempt as consumed, which covers `Or`, `Optional`, every repetition, separators, `Until` and `Expression.Build`. `Atomic` does not rewind it; `Lookahead` and `Not` fail without rewinding or inverting; the interpreted `Longest` stops at the first starved alternative; the compiled `Longest` fails where its scan starves and does not run its winner again once an earlier read has starved; dispatch fails on a starved look at the next item; `ScanPlan` fails where the input runs out, as the repetition it replaces does.
- **`Cursor.Reset`** no longer fetches input: it moves to the segment holding the item before the position. Before, a `Longest` that moved forward to a winner ending exactly at the end of the input received so far starved spuriously.
- **Tests.** [StarvedTests.cs](../../../tests/Creole.Tests/StarvedTests.cs), 62 tests. The criterion: 18 grammars each put a counting probe where a combinator would go next; it runs at the true end of the input and never after a starved attempt, under three lowerings (both mark kinds, dispatch, the compiled `Longest`, the scan rewrite). Every control shape to depth 3, on every prefix of every input up to length 3, gives the whole input's outcome whenever it does not starve. Reverting each changed plan file, and making the buffer never starve, each failed a test.
- **Speed.** Three default-job gate runs before increment 2 (4617dc7, clean: 20261125-163232, -163723, -164438) and after (uncommitted: 20261125-165545, -170234, -170644): 0.25× Pidgin both times, 1.74× and 1.73× Parlot. Creole's median times moved by a factor of 1.03, about as much as Parlot's and Pidgin's.
- **Docs.** No `docs/` change: nothing public starves yet. Whitepaper § Results and § The restart protocol now say every combinator that acts on a failure checks the flag (editorial; the criterion already said so).
- **Deviations.** None needing a decision. Scoped state unwinds as on any failure, which ADR-0030 point 7 allows. A primitive may still succeed after a starved read (a custom scanner, a span or trivia peek); the flag stays set, so the attempt still ends Starved. Dispatch can starve an attempt that trying alternatives in order would finish, when an alternative that reads nothing succeeds before the keyed ones: one extra restart, same result.
- **Left to other cards.** `Recover`'s check ([ERR-05](../04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md)). Clearing the flag on restart, and documenting for users what a cursor sees where streamed input runs out (docs/engines/cursor.md, docs/concepts/custom-scanners.md), in [STRM-02](STRM-02-restart-protocol-and-parsemanyasync.md).
- **Found, not fixed.** The benchmark harness now builds against Parlot 2.4.0 (Dependabot), not the 2.0.0 that benchmarks/README.md says the gate pins, and Parlot 2.4.0's net10.0 assembly reports its version as `2.0.0-preview`, so `run.json` and `bench-gate.py` cannot see the change. Against 2.4.0 the Phase 0 gate measures 1.74× Parlot, over its 1.5× threshold; BENCH-01's 1.37× was against 2.0.0. No card yet.
