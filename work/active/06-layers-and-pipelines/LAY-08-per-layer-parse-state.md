# LAY-08: Per-layer parse state

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Designing |

## Summary

List every piece of state a layer's parse carries, and decide what each mechanism that skips, rewinds or replays work must save and restore, before the cards that build those mechanisms start.

## Scope

- Design stage: a decision record (`/decide`), no code.
- The decisions added per-layer state one record at a time:
  - the cut flag ([ADR-0011](../../docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md));
  - the depth count ([ADR-0020](../../docs/decisions/0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md));
  - the furthest-failure record and its context frames ([ADR-0021](../../docs/decisions/0021-how-failures-are-recorded-and-merged.md));
  - the mode log, the diagnostic count and layer state (§ Bookmarks and retention);
  - the starts of open filters, chained operators and custom scanners, the failures saved by constructs that may restore them, and the items and spans copied when the buffer releases them ([ADR-0028](../../docs/decisions/0028-an-errors-items-are-copied-when-the-buffer-releases-them.md)). Under that record, a furthest failure kept across a mode change at or before its position reads the item lexed under the new mode.
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
- [ ] The scopes of LAY-02, MODE-02, ERR-05, STRM-02 and INC-04 match the record, and each card names the counterexample that it must turn into a test, or says that none applies.

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
- [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) Accepted: the list of per-layer parse state, what each mechanism saves and restores, how a mode change forgets the failures past it, and exact reuse with its mechanism left to INC-04

## Progress notes

- 2026-10-27 (Design): Started. Draft one decision record that inventories per-layer parse state and gives, per mechanism, what it saves and restores, then align the five downstream cards and their counterexample tests.
- 2026-10-27 (Design): Drafted ADR-0030 (Proposed), with options 1A, 2D, 3C, 4B, 5A and 6A. The maintainer chose 5A, exact reuse. Ohm's memo table was read as prior art for 5A. Next: the maintainer accepts, rejects or revises it; on acceptance, update the whitepaper and the cards in its Changes made.
- 2026-10-27 (Design): Challenged ADR-0030 with two independent critics. They found two blocking defects: lowering on undo broke the restores and exact dispatch, and the draft's reuse mechanism was not exact. Revised in place: 2D to 2F, 3C to 3B, the reuse mechanism moved to INC-04, a per-layer flag for leaving parts out, completed tables, tests moved to cards that can write them. Next: the maintainer accepts, rejects or revises; on acceptance, LAY-08's second criterion also needs "or says none applies" for LAY-02.
- 2026-10-27 (Design): Second challenge of ADR-0030's points 3 and 8 found that 2F's stored failure goes stale, that a withdrawn `Recover` diagnostic lost its failure, and that the dispatch argument was vacuous. The maintainer chose 2G. Revised in place: 2G, the give-back in 3B covering `OrInsert`, a fixed rewind order, the dispatch-key constraint, declared refinements and new tests. Next: the maintainer accepts, rejects or revises.
- 2026-10-27 (Design): The maintainer accepted ADR-0030. The whitepaper and 20 cards were updated as its Changes made lists, and its second criterion now allows a card to say that no counterexample applies. Next: /complete, which verifies both criteria and closes the card.
