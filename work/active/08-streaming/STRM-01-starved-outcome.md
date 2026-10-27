# STRM-01: Starved outcome

| | |
| --- | --- |
| Epic | [Streaming and async input](../../README.md#strm--streaming-and-async-input) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

In streaming mode, running out of buffered data is neither success nor failure.

## Scope

- Sticky Starved flag on parse state, held behind the reference the cursor already holds (as `Failures` is), so a custom scanner that scans a copy of the cursor and discards it cannot lose it ([Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) finding 19).
- `Or`, `Atomic`, `Not`, `Lookahead` and `Recover` check it before acting on a failure.
- `End` and root reads report Starved at the end of buffered data before the true end of the stream.
- Starved need not unwind the state that nodes open and close, such as context frames, saved failures, held starts and depth: the restart resets it ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 7).

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
