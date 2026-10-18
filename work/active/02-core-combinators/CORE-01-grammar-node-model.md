# CORE-01: Grammar node model

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

Every combinator call builds an immutable node in a grammar tree. This item establishes the node base type and the conventions every other node follows.

## Scope

- `Parser<TIn, TOut>` abstract base as a `closed` hierarchy with an internal `NodeKind`.
- Immutability and identity semantics for nodes.
- `Unit` and `IKinded<TKind>`.
- Null-argument checks on every combinator, throwing a `GrammarException` that names the argument expression via `[CallerArgumentExpression]` (the run-time half of CRL0004).
- Test helpers for building small throwaway grammars in `tests/Creole.Tests` (moved from [FND-04](../01-foundations/FND-04-test-infrastructure.md)).

## Acceptance criteria

- [ ] Node types cannot be subclassed outside the assembly.
- [ ] Adding a node kind without handling it in an engine's dispatch switch produces a build warning.

## Blocked by

- [FND-02 Repository and solution scaffolding](../01-foundations/FND-02-repository-and-solution-scaffolding.md)

## Unblocks

- [CORE-02 Grammar base classes and authoring surface](../02-core-combinators/CORE-02-grammar-base-classes-and-authoring-surface.md)
- [CORE-04 Sequencing and mapping nodes](../02-core-combinators/CORE-04-sequencing-and-mapping-nodes.md)
- [CORE-05 Choice nodes](../02-core-combinators/CORE-05-choice-nodes.md)
- [CORE-06 Longest](../02-core-combinators/CORE-06-longest.md)
- [CORE-07 Repetition nodes](../02-core-combinators/CORE-07-repetition-nodes.md)
- [CORE-08 Control nodes](../02-core-combinators/CORE-08-control-nodes.md)
- [CORE-09 Recursion with Rec](../02-core-combinators/CORE-09-recursion-with-rec.md)
- [CORE-11 Expression.Build](../02-core-combinators/CORE-11-expressionbuild.md)
- [CORE-12 Custom primitives](../02-core-combinators/CORE-12-custom-primitives.md)
- [INT-02 Plan lowering and cache](../03-interpreter/INT-02-plan-lowering-and-cache.md)

## Whitepaper references

- [§ Core model: parsers as data](../../../whitepaper.md#core-model-parsers-as-data)
- [§ Types](../../../whitepaper.md#types)
- [§ Where unions are not used](../../../whitepaper.md#where-unions-are-not-used)
- [§ Diagnostics](../../../whitepaper.md#diagnostics)

## Progress notes

- 2026-10-18 (Build): Started. Probed `closed` on the pinned RC 2 SDK (11.0.100-rc.2.26504.105): a `closed` generic base compiles, and a switch missing a case gives CS8509 (an error here, since warnings are errors). `closed` is implicitly abstract, so `abstract closed` is CS9384. Not yet confirmed on the GA compiler; FND-07 does that. Derivation from another assembly is not yet tested (criterion 1). Next: `/build` to plan against the acceptance criteria.
