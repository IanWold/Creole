# STRM-01: Starved outcome

| | |
| --- | --- |
| Epic | [Streaming and async input](../../README.md#strm--streaming-and-async-input) |
| Phase | Phase 1 — Layers |
| Status | Building |

## Summary

In streaming mode, running out of buffered data is neither success nor failure.

## Scope

- Sticky Starved flag on parse state, held behind the reference the cursor already holds (as `Failures` is), so a custom scanner that scans a copy of the cursor and discards it cannot lose it ([Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) finding 19).
- `Or`, `Atomic`, `Not`, `Lookahead` and `Recover` check it before acting on a failure.
- `End` and root reads report Starved at the end of buffered data before the true end of the stream.
- Starved need not unwind the state that nodes open and close, such as context frames, saved failures, held starts and depth: the restart resets it ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 7). That includes the span holds a `StreamBuffer` keeps for its reader's `Spanned`, `WithSpan` and `Adjacent`, which the restart must clear ([ADR-0035](../../docs/decisions/0035-span-combinators-commit-like-sequences-and-hold-their-spans.md) point 6, [SPAN-04](../05-source-spans/SPAN-04-span-combinators.md)).

## Acceptance criteria

- [ ] No combinator ever tries another alternative after a starved attempt.

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
