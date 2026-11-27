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
- Layers are not `INode`s yet. Each layer kind exposes its parsers and parts as properties: `TokensLayer.Item` and `Skip`, `TransduceLayer.Step`, `ComposedLayer.First` and `Second` ([LAY-03](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md) Outcome). Add the walk analysis needs.
- `Expression.Build`, `Switch`, `Match` and layers handled precisely. `Expression.Build` is read through its table view; operators that succeed without consuming input are reported ([ADR-0004](../../docs/decisions/0004-expression-build-operator-table-and-semantics.md)).
- Decide union-token `Case<T>()` before building on it. § Authoring ergonomics and § Where unions are not used leave open whether union tokens need `IKinded`, and the code requires it. FIRST's separate case part makes any choice that mixes `Kind(k)` and `Case<T>()` look overlapping, a false-positive risk for the Phase 2 gate ([idea](../../ideas/union-tokens-and-ikinded.md); [Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) finding 25).
- Span combinators: `Spanned` and `WithSpan` have their parser's properties; `Adjacent` has `Seq(p, q)`'s and, like a filter, can fail after both succeed; `Position` is nullable, never fails and consumes nothing. `Spanned`, `WithSpan` and `Position` are regular when their parts are, and `Adjacent` is not ([ADR-0035](../../docs/decisions/0035-span-combinators-commit-like-sequences-and-hold-their-spans.md) point 7).
- `LeadingTrivia` has `Position`'s properties: nullable, never fails, consumes nothing and regular ([ADR-0036](../../docs/decisions/0036-trivia-is-the-source-between-items-read-with-a-zero-width-parser.md) point 10).
- List each mode channel's modes, for example every value of an enum, or the channel's initial mode plus every mode the grammar above writes as a constant. A mode written inside `Bind`, as a payload is, cannot be listed. ANA-05 reports CRL0201 at a `Layer.Modal` for each mode that cannot be listed, so a union mode without data must be listed to avoid a false positive ([ADR-0043](../../docs/decisions/0043-layer-modal-plans-each-mode-value-and-keeps-the-64-most-recent.md) point 7).
- Write the analysis so it can be shared with the Roslyn component (RSLN-02).
- Interpreter lowering may take `Or` dispatch keys from these properties instead of its leading-item walk ([ADR-0016](../../docs/decisions/0016-or-dispatches-on-each-alternatives-leading-item.md) point 5).
- Dispatch keys taken from these properties stop at any node that can change the mode, because dispatch peeks the next item under the current mode ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 3).
- For each grammar lowered on its own (a layer's grammar, or a parser that a `Bind` continuation returns), record whether it can change the mode, raise diagnostics (`Recover`, `OrInsert`, `Embed`) or change layer state (`SetState`, `Scan`), following `Rec` references. A `Bind` counts as all three ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 9, [ADR-0033](../../docs/decisions/0033-a-longest-reruns-its-winner-on-the-saved-failure-record-and-each-root-chooses-its-marks.md)). Lowering may read these properties instead of its own walk in [PlanBuilder.Marks.cs](../../../src/Creole/Plans/PlanBuilder.Marks.cs).

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
- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
- [ADR-0035](../../docs/decisions/0035-span-combinators-commit-like-sequences-and-hold-their-spans.md) `Adjacent` commits like a sequence, `Position` is a zero-width span, and span combinators hold their spans without the failure log
