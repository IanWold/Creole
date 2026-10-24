# ANA-01: Grammar property analysis

| | |
| --- | --- |
| Epic | [Static analysis](../../README.md#ana--static-analysis) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

A fixpoint over the grammar graph computes the properties every check builds on.

## Scope

- Nullable, FIRST (token kinds, character ranges or ⊤) and Regular per node, through `Rec` cycles.
- Regular comes from INT-06's predicate in `src/Creole/Analysis/`, not a second implementation ([ADR-0017](../../docs/decisions/0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md) point 7).
- `Custom` uses its declared `ScannerInfo`; a null `First` is ⊤ and makes the node opaque ([ADR-0012](../../docs/decisions/0012-custom-scanner-metadata.md)). Union token case types count as kinds.
- `Expression.Build`, `Switch`, `Match` and layers handled precisely. `Expression.Build` is read through its table view; operators that succeed without consuming input are reported ([ADR-0004](../../docs/decisions/0004-expression-build-operator-table-and-semantics.md)).
- Write the analysis so it can be shared with the Roslyn component (RSLN-02).
- Interpreter lowering may take `Or` dispatch keys from these properties instead of its leading-item walk ([ADR-0016](../../docs/decisions/0016-or-dispatches-on-each-alternatives-leading-item.md) point 5).

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
- [INT-06 DFA compiler for regular subtrees](../03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md)

## Unblocks

- [ANA-02 Analysis report and Debug enforcement](../09-static-analysis/ANA-02-analysis-report-and-debug-enforcement.md)
- [ANA-06 Deterministic mode](../09-static-analysis/ANA-06-deterministic-mode.md)
- [RSLN-02 Share the analysis engine with the Roslyn component](../10-roslyn-analyzers/RSLN-02-share-the-analysis-engine-with-the-roslyn-component.md)
- [TOOL-04 Grammar fuzzing](../12-grammar-author-tooling/TOOL-04-grammar-fuzzing.md)
- [GLL-01 Define the declarative subset](../18-general-parsing-spike/GLL-01-define-the-declarative-subset.md)

## Whitepaper references

- [§ Properties](../../../whitepaper.md#properties)

## Artifacts

- [Research: DFA compilation under commit semantics](../../docs/research/2026-10-23-dfa-compilation-under-commit-semantics.md) How regular subtrees can compile to DFAs that agree with commit semantics; construction, errors, alphabet, backup and prior art
- [Research: commit-aware automaton prototypes](../../docs/research/2026-10-23-commit-aware-automaton-prototypes.md) Differential prototypes: the commit rule, lexer sizes, speed, proof sketches, Option B's condition, backup, blow-ups and a wider eager fragment
