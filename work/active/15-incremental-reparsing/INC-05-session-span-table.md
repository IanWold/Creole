# INC-05: Session span table

| | |
| --- | --- |
| Epic | [Incremental re-parsing (1.x)](../../README.md#inc--incremental-re-parsing-1x) |
| Phase | Phase 4 — 1.x releases |
| Status | Not started |

## Summary

Reused nodes are the same objects, so positions live on the side.

## Scope

- `WithSpan` records spans in a session table keyed by node identity. It is a node of its own, not `Spanned` followed by `Select`, so the session can find it. Its function is a value function that an engine may run more than once, so a node it builds may not be the one the result holds ([ADR-0035](../../docs/decisions/0035-span-combinators-commit-like-sequences-and-hold-their-spans.md) point 2).
- Shift through the same offset table; `session.SpanOf(node)`.
- Require tracked nodes to be reference types.

## Acceptance criteria

- [ ] Spans of reused nodes are correct after edits before them.

## Blocked by

- [SPAN-04 Span combinators](../05-source-spans/SPAN-04-span-combinators.md)
- [INC-04 Reusable parser rules](../15-incremental-reparsing/INC-04-reusable-parser-rules.md)

## Unblocks

- [INC-07 Incremental re-parsing gate](../15-incremental-reparsing/INC-07-incremental-re-parsing-gate.md)

## Whitepaper references

- [§ Reusing parser results](../../../whitepaper.md#reusing-parser-results)

## Artifacts

- [ADR-0035](../../docs/decisions/0035-span-combinators-commit-like-sequences-and-hold-their-spans.md) `Adjacent` commits like a sequence, `Position` is a zero-width span, and span combinators hold their spans without the failure log
