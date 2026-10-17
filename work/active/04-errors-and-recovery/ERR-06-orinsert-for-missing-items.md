# ERR-06: OrInsert for missing items

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

Repair a single missing item, such as a forgotten `;`, without re-running anything.

## Scope

- `p.OrInsert(span => token)` records a diagnostic and succeeds with a synthetic, zero-width token.
- Inserts only where the failure would otherwise be committed, so it never changes which alternative a choice takes.

## Acceptance criteria

- [ ] Tests show insertion never fires on an empty failure inside a choice.

## Blocked by

- [ERR-02 Furthest-failure tracking and labels](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)
- [SPAN-03 Synthetic items](../05-source-spans/SPAN-03-synthetic-items.md)

## Unblocks

- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)

## Whitepaper references

- [§ Recovery](../../../whitepaper.md#recovery)
