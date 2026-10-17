# FND-07: Validate C# 15 features and decide `ParseResult<T>`

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

The design relies on unions, hand-written `[Union]` structs and `closed` hierarchies. Confirm each on the GA compiler and settle the open `ParseResult<T>` question.

## Scope

- Confirm `union` declarations, implicit conversions and switch exhaustiveness.
- Confirm the hand-written `[Union]` struct pattern (`HasValue` plus `TryGetValue`) is exhaustive and does not box.
- Confirm `closed` class hierarchies and the completeness warning that engine dispatch relies on (G7).
- ParseResult test: does a switch on a union returned from a method need a null arm, and does a hand-written `[Union]` struct with a non-nullable `Value` remove it?
- Choose in order: plain union, then hand-written union, then closed hierarchy. Record the decision in the whitepaper's Still open section.

## Acceptance criteria

- [ ] Decision recorded in the whitepaper.
- [ ] A spike project under `tests/` demonstrates each feature on the GA compiler.

## Blocked by

- [FND-02 Repository and solution scaffolding](../01-foundations/FND-02-repository-and-solution-scaffolding.md)
- External: .NET 11 / C# 15 GA (expected November 2026)

## Unblocks

- [ERR-01 Error and result types](../04-errors-and-recovery/ERR-01-error-and-result-types.md)
- [REL-04 Public API review and freeze](../14-release/REL-04-public-api-review-and-freeze.md)

## Whitepaper references

- [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions)
- [§ Where unions are not used](../../../whitepaper.md#where-unions-are-not-used)
- [§ Still open](../../../whitepaper.md#still-open)
