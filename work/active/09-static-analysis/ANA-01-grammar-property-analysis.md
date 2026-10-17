# ANA-01: Grammar property analysis

| | |
| --- | --- |
| Epic | [Static analysis](../../README.md#ana--static-analysis) |
| Phase | Phase 2 — Analysis |
| Size | L |
| Status | Not started |

## Summary

A fixpoint over the grammar graph computes the properties every check builds on.

## Scope

- Nullable, FIRST (token kinds, character ranges or ⊤) and Regular per node, through `Rec` cycles.
- `Custom` uses its declared `ScannerInfo`; union token case types count as kinds.
- `Expression.Build`, `Switch`, `Match` and layers handled precisely.
- Write the analysis so it can be shared with the Roslyn component (RSLN-02).

## Acceptance criteria

- [ ] Properties are correct on a table of known grammars, including mutual recursion.

## Blocked by

- [CORE-03 Primitive nodes](../02-core-combinators/CORE-03-primitive-nodes.md)
- [CORE-04 Sequencing and mapping nodes](../02-core-combinators/CORE-04-sequencing-and-mapping-nodes.md)
- [CORE-05 Choice nodes](../02-core-combinators/CORE-05-choice-nodes.md)
- [CORE-06 Longest](../02-core-combinators/CORE-06-longest.md)
- [CORE-07 Repetition nodes](../02-core-combinators/CORE-07-repetition-nodes.md)
- [CORE-08 Control nodes](../02-core-combinators/CORE-08-control-nodes.md)
- [CORE-09 Recursion with Rec](../02-core-combinators/CORE-09-recursion-with-rec.md)
- [CORE-10 Bind and LINQ query syntax](../02-core-combinators/CORE-10-bind-and-linq-query-syntax.md)
- [CORE-11 Expression.Build](../02-core-combinators/CORE-11-expressionbuild.md)
- [CORE-12 Custom primitives](../02-core-combinators/CORE-12-custom-primitives.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)

## Unblocks

- [ANA-02 Analysis report and Debug enforcement](../09-static-analysis/ANA-02-analysis-report-and-debug-enforcement.md)
- [ANA-06 Deterministic mode](../09-static-analysis/ANA-06-deterministic-mode.md)
- [RSLN-02 Share the analysis engine with the Roslyn component](../10-roslyn-analyzers/RSLN-02-share-the-analysis-engine-with-the-roslyn-component.md)
- [TOOL-04 Grammar fuzzing](../12-grammar-author-tooling/TOOL-04-grammar-fuzzing.md)
- [GLL-01 Define the declarative subset](../18-general-parsing-spike/GLL-01-define-the-declarative-subset.md)

## Whitepaper references

- [§ Properties](../../../whitepaper.md#properties)
