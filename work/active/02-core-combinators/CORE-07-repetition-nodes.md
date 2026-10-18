# CORE-07: Repetition nodes

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

Repetition combinators with Pidgin-compatible behavior.

## Scope

- `Many`, `AtLeastOnce`, `Repeat(min, max)`.
- `SepBy`, `SepBy1`, `SepEndBy`, `Until(terminator)`.
- Allocation-free `SkipMany` and `SkipAtLeastOnce`.
- Each public combinator null-checks its parser arguments by taking its own `[CallerArgumentExpression]` parameter per parser argument and passing it to `Guard.NotNull`, so the `GrammarException` names the caller's expression (see `Grammar.Deterministic`, CORE-02).

## Acceptance criteria

- [ ] Behavior matches Pidgin's equivalents on a shared test table.

## Blocked by

- [CORE-01 Grammar node model](../02-core-combinators/CORE-01-grammar-node-model.md)

## Unblocks

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [TOOL-05 EBNF and railroad diagram export](../12-grammar-author-tooling/TOOL-05-ebnf-and-railroad-diagram-export.md)

## Whitepaper references

- [§ Repetition and structure](../../../whitepaper.md#repetition-and-structure)
