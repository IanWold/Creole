# MODE-06: Model-based tests for mode rollback

| | |
| --- | --- |
| Epic | [Mode channels](../../README.md#mode--mode-channels) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

The whitepaper names mode rollback corner cases as a top risk; test against a simple reference re-lexer.

## Scope

- Reference model that re-lexes from scratch for the current mode history.
- Random grammars and inputs with mode changes, backtracking, and multi-layer cascades.
- Run in CI.

## Acceptance criteria

- [ ] Thousands of generated cases agree with the reference model.

## Blocked by

- [FND-04 Test infrastructure](../01-foundations/FND-04-test-infrastructure.md)
- [MODE-03 Rollback rule](../07-mode-channels/MODE-03-rollback-rule.md)
- [MODE-04 Multi-layer invalidation cascade](../07-mode-channels/MODE-04-multi-layer-invalidation-cascade.md)

## Unblocks

- [REL-05 Ship 1.0](../14-release/REL-05-ship-10.md)

## Whitepaper references

- [§ Risks](../../../whitepaper.md#risks)
