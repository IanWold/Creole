# ERR-05: Recover combinator and strategies

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

One parse produces many diagnostics without cascades.

## Scope

- `p.Recover(strategy, fallback)`: records a diagnostic, repairs position, returns `fallback`, never re-runs `p`.
- Restore the mode stack and layer state from `p`'s start as an ordinary mode change.
- Strategies `SkipUntil(sync)` and `SkipNested(open, close)`.
- Starved propagates through `Recover` unchanged.

## Acceptance criteria

- [ ] Inputs with several independent errors produce one diagnostic per real error and a `Recovered` result.

## Blocked by

- [ERR-02 Furthest-failure tracking and labels](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)
- [ERR-04 Diagnostic withdrawal](../04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md)
- [LAY-04 Layer-local state](../06-layers-and-pipelines/LAY-04-layer-local-state.md)
- [MODE-02 Invalidation rule](../07-mode-channels/MODE-02-invalidation-rule.md)
- [STRM-01 Starved outcome](../08-streaming/STRM-01-starved-outcome.md)

## Unblocks

- [ERR-07 Lexer recovery and cascade suppression](../04-errors-and-recovery/ERR-07-lexer-recovery-and-cascade-suppression.md)
- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)

## Whitepaper references

- [§ Recovery](../../../whitepaper.md#recovery)
