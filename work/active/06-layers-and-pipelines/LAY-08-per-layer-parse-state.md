# LAY-08: Per-layer parse state

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

List every piece of state a layer's parse carries, and decide what each mechanism that skips, rewinds or replays work must save and restore, before the cards that build those mechanisms start.

## Scope

- Design stage: a decision record (`/decide`), no code.
- The decisions added per-layer state one record at a time:
  - the cut flag ([ADR-0011](../../docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md));
  - the depth count ([ADR-0020](../../docs/decisions/0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md));
  - the furthest-failure record and its context frames ([ADR-0021](../../docs/decisions/0021-how-failures-are-recorded-and-merged.md));
  - the mode log, the diagnostic count and layer state (§ Bookmarks and retention);
  - the starts of open filters and chained operators, and the items and spans copied when the buffer releases them ([ADR-0028](../../docs/decisions/0028-an-errors-items-are-copied-when-the-buffer-releases-them.md)). Under that record, a furthest failure kept across a mode change at or before its position reads the item lexed under the new mode.
- Each consumer saves a different subset today, and none restores the failure record:
  - `LightMark`: the position and the cut flag;
  - LAY-02's light mark: five fields;
  - `Recover` (ERR-05): the mode stack, layer state and the cut flag;
  - the restart bookmark (STRM-02): layer states, mode logs and the diagnostic bag;
  - the reuse memo (INC-04): the result, indexes, examined extent, entry state and the cut flag, but not depth.
- For each mechanism (light mark, pinning bookmark, mode invalidation and rollback, `Recover`, the restart driver, rule reuse), decide what it saves and restores. Decide in particular whether the failure record is restored, scoped or reset, and what a reuse memo must store, for example the rule's deepest level relative to its entry and the failures it left at or past its end.
- Decide whether a parse that reuses earlier results must report the same errors as a fresh parse. The whitepaper implies it but does not say so.
- The [Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) (finding 15) gives three counterexamples worked by hand:
  - reuse lets a parse through that a fresh parse ends at `MaxDepth`, against § Limits for untrusted input;
  - a reused rule records nothing, so the error drops an expectation a fresh parse reports;
  - after a mode change and rewind, a stale furthest failure from the old token stream hides the real error.

## Acceptance criteria

- [ ] An accepted decision record lists every piece of per-layer parse state and, for each mechanism above, what it saves and restores.
- [ ] The scopes of LAY-02, MODE-02, ERR-05, STRM-02 and INC-04 match the record, and each card names the review's counterexample that it must turn into a test.

## Blocked by

- [CORE-08 Control nodes](../02-core-combinators/CORE-08-control-nodes.md)
- [INT-01 Cursor and in-memory input](../03-interpreter/INT-01-cursor-and-in-memory-input.md)

## Unblocks

- [LAY-02 Marks and retention](LAY-02-marks-and-retention.md)
- [MODE-02 Invalidation rule](../07-mode-channels/MODE-02-invalidation-rule.md)
- [STRM-02 Restart protocol and ParseManyAsync](../08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md)
- [ERR-05 Recover combinator and strategies](../04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md)
- [INC-04 Reusable parser rules](../15-incremental-reparsing/INC-04-reusable-parser-rules.md)
- [GEN-11 Runtime support surface for generated code](../11-source-generator/GEN-11-runtime-support-surface-for-generated-code.md)

## Whitepaper references

- [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention)
- [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels)
- [§ Recovery](../../../whitepaper.md#recovery)
- [§ The restart protocol](../../../whitepaper.md#the-restart-protocol)
- [§ Reusing parser results](../../../whitepaper.md#reusing-parser-results)
- [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input)

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
