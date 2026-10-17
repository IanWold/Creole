# CORE-06: Longest

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

Maximal munch as an ordinary combinator: every alternative runs atomically from the same position, the longest wins, and ties go to the earliest.

## Scope

- `Longest(p1, ..., pn)` node.
- Expose alternatives so engines can compile regular ones to a single DFA (INT-06).

## Acceptance criteria

- [ ] Semantics documented with the `<=` vs `<` and `iffy` vs `if` examples as tests.

## Blocked by

- [CORE-01 Grammar node model](../02-core-combinators/CORE-01-grammar-node-model.md)

## Unblocks

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)

## Whitepaper references

- [§ Longest match](../../../whitepaper.md#longest-match)
