# CORE-07: Repetition nodes

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

Repetition combinators with Pidgin-compatible behavior.

## Scope

- `Many`, `AtLeastOnce`, `Repeat(min, max)`.
- `SepBy`, `SepBy1`, `SepEndBy`, `Until(terminator)`.
- Allocation-free `SkipMany` and `SkipAtLeastOnce`.
- Each public combinator null-checks its parser arguments by taking its own `[CallerArgumentExpression]` parameter per parser argument and passing it to `Guard.NotNull`, so the `GrammarException` names the caller's expression (see `Grammar.Deterministic`, CORE-02).

## Acceptance criteria

- [ ] Each combinator builds one node, and its child parsers are reachable through the structural view.
- [ ] A shared table of repetition cases exists, and a test checks each row's expected outcome against Pidgin 3.5.1. Running Creole against the table moved to [INT-03](../03-interpreter/INT-03-execute-the-core-combinators.md), the first card that executes repetition (agreed with the maintainer on 2026-10-18).

## Blocked by

- [CORE-01 Grammar node model](../02-core-combinators/CORE-01-grammar-node-model.md)

## Unblocks

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [TOOL-05 EBNF and railroad diagram export](../12-grammar-author-tooling/TOOL-05-ebnf-and-railroad-diagram-export.md)

## Whitepaper references

- [§ Repetition and structure](../../../whitepaper.md#repetition-and-structure)

## Progress notes

- 2026-10-18 (Build): Started. Add the repetition node kinds per ADR-0001 and check them against Pidgin on a shared test table.
- 2026-10-18 (Build): Plan agreed: four node kinds (`Repeat`, `SkipRepeat`, `Separated`, `Until`); results are `IReadOnlyList<T>`; `Repeat(n)` is `Repeat(n, n)` and `Repeat(min, max)` is greedy; bad counts throw `GrammarException`. The Pidgin criterion is split with INT-03, because nothing executes nodes yet. Increment 1 done: nodes, API, structural tests, `docs/concepts/repetition.md`. The combinators are C# 14 extension members in `Repetition`, not instance methods: instance methods failed the AOT smoke publish with IL3054 (generic recursion), and a scratch app that did not root Creole crashed with `TypeLoadException` at nesting depth 6. Next: increment 2, the shared table and its Pidgin check.
- 2026-10-18 (Build): Increment 2 done: the shared table in `tests/Creole.Tests/RepetitionTable.cs` (52 rows, written once as shapes) and `RepetitionTablePidginTests.cs`, which translates each shape to Pidgin 3.5.1 and checks the hand-written outcome (value and consumed offset, failure with or without consuming, or Pidgin's throw on a repeated parser that consumes nothing). `Repeat(min, max)` rows are checked against a Pidgin composition, because Pidgin has no equivalent. All rows agree; flipping two expectations made exactly those two fail. `dotnet build` with 0 warnings; `dotnet test` with 183 passed; the AOT smoke publish with 0 warnings. Both criteria appear met. Next: `/complete`. INT-03 adds the Creole translator once CORE-03 and CORE-04 exist.
