# MODE-03: Rollback rule

| | |
| --- | --- |
| Epic | [Mode channels](../../README.md#mode--mode-channels) |
| Phase | Phase 1 — Layers |
| Size | M |
| Status | Not started |

## Summary

Rewinding any mark undoes mode changes made after it.

## Scope

- Bookmarks and light marks record the mode log length.
- Undo log entries newest first, applying invalidation at each entry's index.
- `Or` undoes zero-width mode changes when an alternative fails without consuming.

## Acceptance criteria

- [ ] Property tests with random `SetMode` placement inside `Atomic`, `Lookahead` and `Or` agree with a from-scratch re-lex.

## Blocked by

- [MODE-02 Invalidation rule](../07-mode-channels/MODE-02-invalidation-rule.md)

## Unblocks

- [MODE-06 Model-based tests for mode rollback](../07-mode-channels/MODE-06-model-based-tests-for-mode-rollback.md)
- [REL-01 Worked-example samples](../14-release/REL-01-worked-example-samples.md)
- [INC-03 Layer item reuse](../15-incremental-reparsing/INC-03-layer-item-reuse.md)

## Whitepaper references

- [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels)
- [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention)
