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
- Undo log entries newest first, applying invalidation at each entry's index.
- `Or` undoes zero-width mode changes when an alternative fails without consuming.
- An undo lowers the furthest failure at the entry's index, as a change does. A rewind withdraws diagnostics, then undoes mode changes, then restores any saved failure, which is lowered by the changes still in effect ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) points 2 and 3).

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
