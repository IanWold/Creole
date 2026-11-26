# ERR-06: OrInsert for missing items

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

Repair a single missing item, such as a forgotten `;`, without re-running anything.

## Scope

- `p.OrInsert(span => token)` records a diagnostic and succeeds with the author's token. Its span is zero-width, which is what makes it synthetic; nothing stores a flag ([ADR-0034](../../docs/decisions/0034-a-synthetic-item-is-one-whose-span-is-zero-width.md) point 5). If a reader must tell a recovery-inserted item from a layout item, as Roslyn's `IsMissing` and tree-sitter's `ts_node_is_missing` do, that needs a stored flag and a new decision.
- Inserts only where the failure would otherwise be committed, so it never changes which alternative a choice takes.
- If its diagnostic comes from the furthest failure, `OrInsert` clears it and gives it back when the diagnostic is withdrawn, as `Recover` does ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 8).
- Add `OrInsert` to the nodes that give a grammar full marks in [PlanBuilder.Marks.cs](../../../src/Creole/Plans/PlanBuilder.Marks.cs) ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 9, [ERR-04](../04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md) Outcome).

## Acceptance criteria

- [ ] Tests show insertion never fires on an empty failure inside a choice.

## Blocked by

- [ERR-02 Furthest-failure tracking and labels](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)
- [SPAN-03 Synthetic items](../05-source-spans/SPAN-03-synthetic-items.md)

## Unblocks

- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)

## Whitepaper references

- [§ Recovery](../../../whitepaper.md#recovery)

## Artifacts

- [ADR-0034: A synthetic item is one whose span is zero-width, and no flag is stored](../../docs/decisions/0034-a-synthetic-item-is-one-whose-span-is-zero-width.md)
