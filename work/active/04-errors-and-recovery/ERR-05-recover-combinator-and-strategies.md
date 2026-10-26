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
- Repair from the cursor, not from the reported position: a committed failure may be reported before the cursor, as a rejected filter is ([ADR-0014](../../docs/decisions/0014-a-rejected-filter-reports-at-the-start-of-the-value.md)).
- Starved propagates through `Recover` unchanged.
- `Recover` does not catch a depth abort. It passes through, and the parse ends ([ADR-0020](../../docs/decisions/0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md)).
- `Recover` takes a light mark before running `p`: if `p` fails without consuming, the cut flag is restored ([ADR-0011](../../docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md)). `Recover` is not a cut boundary.

## Acceptance criteria

- [ ] Inputs with several independent errors produce one diagnostic per real error and a `Recovered` result.

## Blocked by

- [ERR-02 Furthest-failure tracking and labels](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)
- [ERR-04 Diagnostic withdrawal](../04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md)
- [LAY-04 Layer-local state](../06-layers-and-pipelines/LAY-04-layer-local-state.md)
- [LAY-08 Per-layer parse state](../06-layers-and-pipelines/LAY-08-per-layer-parse-state.md)
- [MODE-02 Invalidation rule](../07-mode-channels/MODE-02-invalidation-rule.md)
- [STRM-01 Starved outcome](../08-streaming/STRM-01-starved-outcome.md)

## Unblocks

- [ERR-07 Lexer recovery and cascade suppression](../04-errors-and-recovery/ERR-07-lexer-recovery-and-cascade-suppression.md)
- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)

## Whitepaper references

- [§ Recovery](../../../whitepaper.md#recovery)

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
