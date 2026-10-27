# INC-04: Reusable parser rules

| | |
| --- | --- |
| Epic | [Incremental re-parsing (1.x)](../../README.md#inc--incremental-re-parsing-1x) |
| Phase | Phase 4 — 1.x releases |
| Status | Not started |

## Summary

Reuse whole rule results, incremental-packrat style, for rules that opt in.

## Scope

- `.Reusable()` memoizes result, indexes, examined extent and entry state.
- Reuse requires matching entry mode stack; `Bind` disables reuse; recovered items are never reused.
- A reused result runs none of its rule's functions, which [ADR-0026](../../docs/decisions/0026-engines-may-skip-unused-values-and-repeat-user-functions.md) allows: unused value functions may be skipped, and a reused result holds the outcome of each decision function.
- The memo records whether a cut escaped the rule, and a reuse replays it, or an enclosing `Atomic` could rewind on re-parse where a fresh parse keeps the failure. A cut escapes a rule that succeeds, that fails after consuming, or that fails without consuming outside any attempt ([ADR-0011](../../docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md)).
- Reuse is invisible: a session gives the same outcome, diagnostics and `MaxDepth` failure as a fresh parse of the same text ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 5).
- A memo accounts for: the deepest level the rule reached from its entry, including inside rules reused within it, and is not reused when the entry depth plus that level would exceed `MaxDepth`; the rule's effect on the furthest failure, including lowering by its mode changes and what a `Recover` or `OrInsert` inside it reads, clears and gives back; its exit layer state, exit mode stack and mode-log entries; and nested memos.
- Design the replay against the four hazards in ADR-0030 point 5. Running the rule from an empty failure record and merging afterwards is not exact.
- Decide whether a session follows [ADR-0027](../../docs/decisions/0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md). If it does, a run with a failure log cannot use a memo built without one.

## Acceptance criteria

- [ ] Editing one statement in a large file re-parses only that statement's rule.
- [ ] A differential test shows a session's whole results equal `Parse`'s over generated grammars and edits. It covers mode changes after an `Atomic` has rewound past a deeper failure, `Recover`, `Context`, `Named`, nested reusable rules, and counterexamples 1 and 2 of [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md).

## Blocked by

- [LAY-08 Per-layer parse state](../06-layers-and-pipelines/LAY-08-per-layer-parse-state.md)
- [INC-03 Layer item reuse](../15-incremental-reparsing/INC-03-layer-item-reuse.md)

## Unblocks

- [INC-05 Session span table](../15-incremental-reparsing/INC-05-session-span-table.md)
- [INC-06 CRL0205 stale span analyzer](../15-incremental-reparsing/INC-06-crl0205-stale-span-analyzer.md)

## Whitepaper references

- [§ Reusing parser results](../../../whitepaper.md#reusing-parser-results)
- [§ Limits](../../../whitepaper.md#limits)

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
