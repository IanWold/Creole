# CORE-07: Repetition nodes

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

Repetition combinators with Pidgin-compatible behavior.

## Scope

- `Many`, `AtLeastOnce`, `Repeat(min, max)`.
- `SepBy`, `SepBy1`, `SepEndBy`, `Until(terminator)`.
- Allocation-free `SkipMany` and `SkipAtLeastOnce`.
- Each public combinator null-checks its parser arguments by taking its own `[CallerArgumentExpression]` parameter per parser argument and passing it to `Guard.NotNull`, so the `GrammarException` names the caller's expression (see `Grammar.Deterministic`, CORE-02).

## Acceptance criteria

- [x] Each combinator builds one node, and its child parsers are reachable through the structural view.
- [x] A shared table of repetition cases exists, and a test checks each row's expected outcome against Pidgin 3.5.1. Running Creole against the table moved to [INT-03](../03-interpreter/INT-03-execute-the-core-combinators.md), the first card that executes repetition (agreed with the maintainer on 2026-10-18).

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

## Outcome

- **Nodes** in `src/Creole/Nodes/`: `RepeatNode`, `SkipRepeatNode`, `SeparatedNode` and `UntilNode`, with `NodeKind` members `Repeat`, `SkipRepeat`, `Separated` and `Until`.
- **API** in [src/Creole/Repetition.cs](../../../src/Creole/Repetition.cs): `Many`, `AtLeastOnce`, `Repeat(n)`, `Repeat(min, max)`, `SepBy`, `SepBy1`, `SepEndBy`, `Until`, `SkipMany` and `SkipAtLeastOnce`, returning `IReadOnlyList<T>` or `Unit`. `Repeat(n)` is `Repeat(n, n)`; `Repeat(min, max)` is greedy; bad counts throw `GrammarException`.
- **C# 14 extension members, not instance methods.** Called as `p.Many()`, as the whitepaper writes them. Instance methods failed the AOT smoke publish with IL3054 (generic recursion), and a scratch app that did not root Creole crashed with `TypeLoadException`. The rule is recorded in [CORE-04](CORE-04-sequencing-and-mapping-nodes.md) for `Optional`.
- **Tests:**
  - [tests/Creole.Tests/RepetitionNodeTests.cs](../../../tests/Creole.Tests/RepetitionNodeTests.cs): each combinator's node, kind, children and argument checks.
  - [tests/Creole.Tests/RepetitionTable.cs](../../../tests/Creole.Tests/RepetitionTable.cs): the shared table, 52 rows written as shapes.
  - [tests/Creole.Tests/RepetitionTablePidginTests.cs](../../../tests/Creole.Tests/RepetitionTablePidginTests.cs): checks every row against Pidgin 3.5.1. `Creole.Tests` now references Pidgin.
  - `NodeCompletenessTests` builds its every-kind switch from the enum.
- **Evidence, 2026-10-18, commit cb2c33d:** `dotnet build` with 0 warnings; `dotnet test` with 183 passed and 0 failed (15 node tests, 54 table tests); the AOT smoke publish with 0 IL warnings.
- **Docs:** [docs/concepts/repetition.md](../../../docs/concepts/repetition.md), indexed in docs/README.md. Execution semantics are in the XML docs; the page gains them when INT-03 runs them.
- **Scope changes agreed with the maintainer:**
  - The Pidgin criterion was split: running Creole against the table moved to [INT-03](../03-interpreter/INT-03-execute-the-core-combinators.md), with the rows where Pidgin throws.
  - Results are `IReadOnlyList<T>`, not Pidgin's `IEnumerable<T>`.
- **Follow-ups:**
  - The maintainer plans to rename the public combinators to consistent full-word names (`SepBy1` sits beside `AtLeastOnce`). That changes the whitepaper, so it needs `/decide`.
  - Pidgin names `SepBy`, `SepBy1` and `SepEndBy` differently, and has no `Repeat(min, max)`; the docs page maps the names.
