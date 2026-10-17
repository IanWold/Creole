# TOOL-03: Explain

| | |
| --- | --- |
| Epic | [Grammar-author tooling](../../README.md#tool--grammar-author-tooling) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

Explain where an error message came from.

## Scope

- `diagnostic.Explain()` lists the alternatives tried at the furthest position and which `Named` label summarized them.

## Acceptance criteria

- [ ] Explain output for the whitepaper's JSON error is covered by a snapshot test.

## Blocked by

- [ERR-02 Furthest-failure tracking and labels](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)
- [TOOL-02 Tracing](../12-grammar-author-tooling/TOOL-02-tracing.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Developer experience for grammar authors](../../../whitepaper.md#developer-experience-for-grammar-authors)
