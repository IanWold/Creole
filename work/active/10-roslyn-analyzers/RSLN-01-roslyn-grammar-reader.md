# RSLN-01: Roslyn grammar reader

| | |
| --- | --- |
| Epic | [Build-time analyzers and code fixes](../../README.md#rsln--build-time-analyzers-and-code-fixes) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

Build-time analysis and code generation both start by reading grammars from syntax. Creole reads syntax only, by design.

## Scope

- Discover classes deriving from `Grammar<TIn>`, partial or not.
- Walk static field initializers with the semantic model into a value-equatable build-time grammar model.
- Recognize Creole API calls, `Rec`, LINQ queries (classifying clauses by range-variable use), static lambdas, layers and mode channels.
- Inline expression-bodied static helpers from the same compilation like macros.
- Mark unreadable subtrees with the reason (for CRL0301).
- Incremental-generator pipeline that caches well in the IDE.

## Acceptance criteria

- [ ] Examples A to C are read completely; a captured local is marked unreadable with a reason.

## Blocked by

- [FND-02 Repository and solution scaffolding](../01-foundations/FND-02-repository-and-solution-scaffolding.md)
- [CORE-02 Grammar base classes and authoring surface](../02-core-combinators/CORE-02-grammar-base-classes-and-authoring-surface.md)
- [CORE-10 Bind and LINQ query syntax](../02-core-combinators/CORE-10-bind-and-linq-query-syntax.md)
- [CORE-11 Expression.Build](../02-core-combinators/CORE-11-expressionbuild.md)
- [CORE-12 Custom primitives](../02-core-combinators/CORE-12-custom-primitives.md)
- [CORE-14 Choices beyond 16 alternatives](../02-core-combinators/CORE-14-choices-beyond-16-alternatives.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)
- [MODE-01 Mode channels and Layer.Modal](../07-mode-channels/MODE-01-mode-channels-and-layermodal.md)

## Unblocks

- [RSLN-02 Share the analysis engine with the Roslyn component](../10-roslyn-analyzers/RSLN-02-share-the-analysis-engine-with-the-roslyn-component.md)
- [RSLN-05 Grammar class hygiene analyzers](../10-roslyn-analyzers/RSLN-05-grammar-class-hygiene-analyzers.md)
- [GEN-01 Generator skeleton and registration](../11-source-generator/GEN-01-generator-skeleton-and-registration.md)

## Whitepaper references

- [§ Source-generated engine](../../../whitepaper.md#source-generated-engine)
- [§ Considered and deferred](../../../whitepaper.md#considered-and-deferred)
- [§ LINQ query syntax](../../../whitepaper.md#linq-query-syntax)
