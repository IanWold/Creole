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
- `Recover` does not catch a depth abort or the buffer limit. Either passes through, and the parse ends ([ADR-0020](../../docs/decisions/0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md), [ADR-0039](../../docs/decisions/0039-each-layers-buffer-is-bounded-from-its-horizon-and-exceeding-it-ends-the-parse.md) point 3).
- `Recover` takes a light mark before running `p`: if `p` fails without consuming, the cut flag is restored ([ADR-0011](../../docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md)). `Recover` is not a cut boundary.
- A parse's first run has no failure log, so the first failure a `Recover` would catch ends that run, and the parse runs again with the log ([ADR-0027](../../docs/decisions/0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md)). Tests cover a parse that recovers giving the same diagnostics as one run with the log.
- Rendering bounds each error but not how many there are ([ADR-0029](../../docs/decisions/0029-rendered-errors-bound-text-from-input-with-caller-set-limits.md) point 9). A parse that recovers can report one error per recovery, so decide here whether to cap the count.
- `Recover` builds its diagnostic from the furthest failure, then clears it. The diagnostic keeps that failure, and if a mark of the same layer withdraws the diagnostic, the failure is recorded again. If a strategy can fail, `Recover` then withdraws its own diagnostic and fails ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 8).
- A successful `Longest` puts back its saved failure record at its end. If a `Recover` in the winner took failures from that record, the record and the diagnostic both hold them, so a later error at that position can report what the diagnostic already reported. Decide whether that needs a rule ([ADR-0033](../../docs/decisions/0033-a-longest-reruns-its-winner-on-the-saved-failure-record-and-each-root-chooses-its-marks.md), Consequences).
- Add `Recover` to the nodes that give a grammar full marks in [PlanBuilder.Marks.cs](../../../src/Creole/Plans/PlanBuilder.Marks.cs). The walk checks node kinds one by one, so nothing else forces this ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 9, [ERR-04](../04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md) Outcome).
- When a withdrawn diagnostic gives its failure back, the failure counts as recorded again, so an enclosing `Named` relabels expectations recorded before it started. With a test node that reports the furthest failure as `Recover` will, `Char('x').Optional().Then(Report(Char('l')).Then(Char('i')).Or(Char('n')).Named("thing"))` on `;` expects [thing]; without the report it expects [x, thing]. ADR-0030 point 8's "recorded again" allows this. Decide whether to keep it.
- If this card is the first to let a layer below the parser raise a diagnostic, it puts layer diagnostics in a pipeline's results and builds and tests what is listed for them in [ERR-07](ERR-07-lexer-recovery-and-cascade-suppression.md) ([ADR-0038](../../docs/decisions/0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md) point 4).

## Acceptance criteria

- [ ] Inputs with several independent errors produce one diagnostic per real error and a `Recovered` result.
- [ ] Tests from [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 11: counterexample 5; Fork 3C's case, where the reported failure came from an alternative before the `Recover`; and Fork 3B's `let` case, where a withdrawn diagnostic gives its failure back.

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
