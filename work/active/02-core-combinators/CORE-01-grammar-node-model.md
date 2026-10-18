# CORE-01: Grammar node model

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

Every combinator call builds an immutable node in a grammar tree. This item establishes the node base type and the conventions every other node follows.

## Scope

- `Parser<TIn, TOut>` abstract base with `internal` constructors and `internal sealed` node classes, not `closed` ([ADR-0001](../../docs/decisions/0001-grammar-node-encoding-and-engine-dispatch.md)).
- The structural view: an internal `NodeKind` and the node's children in an untyped form, for consumers that need only structure.
- The convention for typed engine operations: an `internal abstract` member on `Parser<TIn, TOut>` that every node overrides, forwarding its typed fields to the engine. INT-02 adds the first one, lowering.
- Immutability and identity semantics for nodes.
- `Unit` and `IKinded<TKind>`.
- Null-argument checks on every combinator, throwing a `GrammarException` that names the argument expression via `[CallerArgumentExpression]` (the run-time half of CRL0004).
- Test helpers for building small throwaway grammars in `tests/Creole.Tests` (moved from [FND-04](../01-foundations/FND-04-test-infrastructure.md)).

## Acceptance criteria

- [ ] Node types cannot be subclassed outside the assembly.
- [ ] A node type that does not implement every abstract node member fails to compile, and a switch expression over `NodeKind` that misses a kind fails the build.

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

## Artifacts

- [Research: grammar node encoding and dispatch](../../docs/research/2026-10-18-grammar-node-encoding-and-dispatch.md)
- [ADR-0001: Grammar node encoding and engine dispatch](../../docs/decisions/0001-grammar-node-encoding-and-engine-dispatch.md)

## Progress notes

- 2026-10-18 (Build): Started. Probed `closed` on the pinned RC 2 SDK (11.0.100-rc.2.26504.105): a `closed` generic base compiles, and a switch missing a case gives CS8509 (an error here, since warnings are errors). `closed` is implicitly abstract, so `abstract closed` is CS9384. Not yet confirmed on the GA compiler; FND-07 does that. Derivation from another assembly is not yet tested (criterion 1). Next: `/build` to plan against the acceptance criteria.
- 2026-10-18 (Designing): Moved back from Build before any code. On RC 2 a `closed` `Parser<TIn, TOut>` cannot hold nodes with a type parameter missing from the base: `Map<TIn, TA, TOut> : Parser<TIn, TOut>` is error CS9383. Nodes that fix `TOut` (`Tok<TIn> : Parser<TIn, TIn>`) compile, but a generic engine switch over `Parser<TIn, TOut>` cannot name them and gets CS8509. Only nodes shaped exactly `X<TIn, TOut>`, with other types hidden in an internal object reached through a visitor, give a checkable switch. § Where unions are not used assumes this works directly. Next: `/decide` on how nodes encode intermediate types and how engine dispatch is made complete (closed plus inner visitors, a plain visitor interface, or an internal `NodeKind` enum switch).
- 2026-10-18 (Designing): Research note written. `closed` cannot hold nodes with hidden or fixed type arguments; the spec makes this final. The note supports an abstract `internal` method per typed operation (missing override is CS0534), a `NodeKind` enum for consumers that need only structure, and an `internal` constructor instead of `closed` (other assemblies get CS1729). Next: `/decide` on node encoding and engine dispatch.
- 2026-10-18 (Building): ADR-0001 accepted by the maintainer: option D, an internal constructor plus one abstract member per engine operation, with a `NodeKind` structural view; no `closed`. Whitepaper, scope and criterion 2 updated to match. Back to Build. Next: `/build CORE-01` to plan against the revised criteria.
