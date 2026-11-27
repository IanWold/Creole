# MODE-03: Rollback rule

| | |
| --- | --- |
| Epic | [Mode channels](../../README.md#mode--mode-channels) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

Rewinding any mark undoes mode changes made after it.

## Scope

- Bookmarks and light marks record the mode log length.
- Marks save the mode log length only in a layer whose grammar contains `SetMode`, `PushMode`, `PopMode` or `Bind`. A `Longest` whose winner can change the mode log runs the winner again when another alternative ran after it ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) points 6 and 9; [ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 6).
- Undo log entries newest first, applying invalidation at each entry's index.
- `Or` undoes zero-width mode changes when an alternative fails without consuming.
- An undo lowers the furthest failure at the entry's index, as a change does. A rewind withdraws diagnostics, then undoes mode changes, then restores any saved failure, which is lowered by the changes still in effect ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) points 2 and 3).
- An undo discards and lowers only when it changes the mode on top back, as a write does only when it changes it ([ADR-0042](../../docs/decisions/0042-a-write-discards-only-when-it-changes-the-top-mode-and-the-mode-log-keeps-only-what-a-rewind-can-reach.md) point 1).
- Marks keep absolute log lengths. A full log drops its entries below the lower of the cursor and the floor (ADR-0042 point 5), so the log counts the entries it has dropped.
- A save keeps the position of the first change made after it, and its restore is lowered there (ADR-0042 point 3). Undoing that change clears the position, so the restore is not lowered by a change no longer in effect.
- Property tests check that no rewind reaches a dropped entry and that a drop removes nothing a rewind needs. Before this card nothing reads the log after a write, so MODE-02's tests cannot show a drop that removes too much (ADR-0042 § Consequences).

## Acceptance criteria

- [ ] Property tests with random `SetMode` placement inside `Atomic`, `Lookahead` and `Or` agree with a from-scratch re-lex.
- [ ] Tests from [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 11: `Not` around a mode change, with a failure inside it before the change, leaves the record as it found it; option 2B's example gives a failure at *i* with no expectations, which an enclosing `Named` labels; an undo after a successful `Longest`, after a filter's rejection and after a `Named` relabel each report the next alternative's expectation; `Or(Seq(SetMode(Operand), Kind(RegexLit)), Kind(Slash))` on `/x/` gives the same result and error with dispatch on and off; and counterexample 6, with a `Longest` alternative that changes mode.

## Blocked by

- [MODE-02 Invalidation rule](../07-mode-channels/MODE-02-invalidation-rule.md)

## Unblocks

- [MODE-06 Model-based tests for mode rollback](../07-mode-channels/MODE-06-model-based-tests-for-mode-rollback.md)
- [REL-01 Worked-example samples](../14-release/REL-01-worked-example-samples.md)
- [INC-03 Layer item reuse](../15-incremental-reparsing/INC-03-layer-item-reuse.md)

## Whitepaper references

- [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels)
- [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention)
