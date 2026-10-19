# CORE-08: Control nodes

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

Nodes that control backtracking and error labelling.

## Scope

- `Atomic(p)`, `Lookahead(p)`, `Not(p)` and the shared leaf `Cut`, as static members of `Grammar<TIn>` in `GrammarOfTIn.Control.cs` ([ADR-0008](../../docs/decisions/0008-control-node-semantics.md)).
- `p.Named(label)` and `p.Context(text)`, as extension members in `ErrorReporting`, each guarding its receiver with its own `[CallerArgumentExpression]` parameter. A null or empty label or text throws `GrammarException`.
- XML docs and a `docs/` page stating ADR-0008's points 2 to 6, and a shared table of cases (grammar, input, expected outcome) covering every row of point 2 and the cut rules of point 3, for INT-03 to run.
- Each public combinator null-checks its parser arguments by taking its own `[CallerArgumentExpression]` parameter per parser argument and passing it to `Guard.NotNull`, so the `GrammarException` names the caller's expression (see `Grammar.Deterministic`, CORE-02).

## Acceptance criteria

- [ ] Node semantics are specified precisely enough for both engines to implement (including `Cut` inside `Atomic`).

## Blocked by

- [CORE-02 Grammar base classes and authoring surface](../02-core-combinators/CORE-02-grammar-base-classes-and-authoring-surface.md)

## Unblocks

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [TOOL-05 EBNF and railroad diagram export](../12-grammar-author-tooling/TOOL-05-ebnf-and-railroad-diagram-export.md)

## Whitepaper references

- [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking)
- [§ The error model](../../../whitepaper.md#the-error-model)

## Artifacts

- Decision: [ADR-0008 Control node semantics](../../docs/decisions/0008-control-node-semantics.md)
- Docs: [Backtracking and labels](../../../docs/concepts/control.md)

## Progress notes

- 2026-10-19 (Designing): Started at Design with the maintainer's agreement, because INT-03, ERR-02 and LAY-02 depend on the cut, lookahead and label rules. Drafted ADR-0008 after reading Parsec, Megaparsec, Pidgin and FastParse; the maintainer chose that `Named` applies to an empty success. Next: the maintainer accepts, rejects or revises ADR-0008 (cut scope option A, class name for `Named` and `Context`); on acceptance fold it into the whitepaper and cards, then Build.
- 2026-10-19 (Building): The maintainer accepted ADR-0008 (cut scope option A, class `ErrorReporting`, no Pidgin differential run); folded into the whitepaper, INT-03, ERR-02 and LAY-02. Next: build the nodes, entry points, case table, docs page and tests.
- 2026-10-19 (Building): Added the six nodes (`NodeKind` group `Atomic` to `Context`), `Atomic`/`Lookahead`/`Not`/`Cut` in `GrammarOfTIn.Control.cs`, `Named`/`Context` in `ErrorReporting`, `Guard.NotNullOrEmpty`, `ControlNodeTests`, the 38-row `ControlTable` with `ControlTableTests` (rows build; no parse is run), and docs/concepts/control.md. Build clean, 578 tests pass, AOT smoke publish clean. The criterion appears met; `Longest` boundary rows wait for CORE-06. Next: maintainer review, then `/complete`.
- 2026-10-19 (Building): Merged origin/main (CORE-06, CORE-13). Added the two `Longest` cut-boundary rows to `ControlTable` (40 rows) and the cut rule to `Longest`'s XML docs and docs/concepts/choice.md. CORE-13's `ExtensionReceiverTests` passes with `ErrorReporting`; 637 tests pass. Next: maintainer review, then `/complete`.
