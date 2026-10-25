# CORE-06: Longest

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

Maximal munch as an ordinary combinator: every alternative runs atomically from the same position, the longest wins, and ties go to the earliest.

## Scope

- `Longest(p1, ..., pn)` node.
- Expose alternatives so engines can compile regular ones to a single DFA (INT-06).
- Each public combinator null-checks its parser arguments by taking its own `[CallerArgumentExpression]` parameter per parser argument and passing it to `Guard.NotNull`, so the `GrammarException` names the caller's expression (see `Grammar.Deterministic`, CORE-02). A `params` array cannot capture each element's expression, so `Longest`'s overloads take fixed-arity overloads, each with one expression parameter per operand, rather than `params`.

## Acceptance criteria

- [x] `Longest` builds one node whose alternatives are reachable through the structural view, for analysis and INT-06.
- [x] The semantics are documented in `docs/` with the `<=` vs `<` and `iffy` vs `if` examples, and those examples exist as a shared table of test cases. Running Creole against the table moved to [INT-03](../../complete/03-interpreter/INT-03-execute-the-core-combinators.md), the first card that executes `Longest` (agreed with the maintainer on 2026-10-19).

## Blocked by

- [CORE-02 Grammar base classes and authoring surface](../02-core-combinators/CORE-02-grammar-base-classes-and-authoring-surface.md)

## Unblocks

- [CORE-12 Custom primitives](../02-core-combinators/CORE-12-custom-primitives.md)
- [INT-03 Execute the core combinators](../../complete/03-interpreter/INT-03-execute-the-core-combinators.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)

## Whitepaper references

- [§ Longest match](../../../whitepaper.md#longest-match)

## Progress notes

- 2026-10-19 (Building): Added `LongestNode` (`NodeKind.Longest`, at the end of the enum) and `Grammar<TIn>.Longest` for 2 to 16 alternatives in `GrammarOfTIn.Longest.cs`. Any node can be an alternative, so CORE-12's `Custom` node needs no change here. The examples are in `tests/Creole.Tests/LongestTable.cs` with `LongestTable.ToParser` for INT-03; `LongestTableTests` only builds each row. Semantics documented in `docs/concepts/choice.md` § Longest match, including two calls the whitepaper leaves implicit: when every alternative fails, `Longest` fails without consuming input (follows from "atomically"), and beyond 16 alternatives you nest calls, which is equivalent because ties go left at both levels. 574 tests pass. Next: maintainer review, then `/complete`.

## Outcome

Delivered:

- `LongestNode<TIn, T>` in [src/Creole/Nodes/LongestNode.cs](../../../src/Creole/Nodes/LongestNode.cs), with `NodeKind.Longest`. Its alternatives are exposed in order as `Alternatives`, for INT-06, and as `Children`, for analysis. `Lower` is a `NotYetExecutable` stub until INT-03.
- `Grammar<TIn>.Longest` for 2 to 16 alternatives in [src/Creole/GrammarOfTIn.Longest.cs](../../../src/Creole/GrammarOfTIn.Longest.cs), each alternative null-checked with its own `[CallerArgumentExpression]` parameter. There are no extension members.
- [tests/Creole.Tests/LongestNodeTests.cs](../../../tests/Creole.Tests/LongestNodeTests.cs): every arity, null reporting, node shape, nesting kept as written, and an arbitrary node type as an alternative (the check CORE-12's `Custom` needs).
- [tests/Creole.Tests/LongestTable.cs](../../../tests/Creole.Tests/LongestTable.cs): 11 rows covering `<=` vs `<`, `iffy` vs `if`, ties, atomic alternatives, total failure and empty matches, with `LongestTable.ToParser` for INT-03. `LongestTableTests` builds each row and runs none.
- [docs/concepts/choice.md](../../../docs/concepts/choice.md) § Longest match, and a Pidgin row (Pidgin 3.5.1 has no longest-match combinator).

Two semantics the whitepaper leaves implicit are stated in the docs and XML docs, not decided separately: when every alternative fails, `Longest` fails without consuming input (this follows from "atomically"), and beyond 16 alternatives you nest calls, which picks the same winner because ties go left at both levels. No deviations from the card or whitepaper, and no follow-up cards.
