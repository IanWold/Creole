# Decision records

Each record captures one design choice: the options, the choice, and why. Rules are in [CONTRIBUTING.md § Decision records](../../../CONTRIBUTING.md#decision-records). Only the maintainer marks a record Accepted.

## Index

| # | Decision | Status | Cards | Whitepaper |
| --- | --- | --- | --- | --- |
| [0001](0001-grammar-node-encoding-and-engine-dispatch.md) | Grammar nodes use an internal constructor and one abstract method per engine operation, not a `closed` hierarchy | Accepted | [CORE-01](../../active/02-core-combinators/CORE-01-grammar-node-model.md), [FND-07](../../active/01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md), [INT-02](../../active/03-interpreter/INT-02-plan-lowering-and-cache.md), [RSLN-02](../../active/10-roslyn-analyzers/RSLN-02-share-the-analysis-engine-with-the-roslyn-component.md) | [§ Types](../../../whitepaper.md#types), [§ Where unions are not used](../../../whitepaper.md#where-unions-are-not-used) |
| [0002](0002-parseresult-as-a-plain-union.md) | `ParseResult<T>` is a plain C# 15 union | Accepted | [FND-07](../../active/01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md), [ERR-01](../../active/04-errors-and-recovery/ERR-01-error-and-result-types.md), [LAY-03](../../active/06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md) | [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions), [§ Still open](../../../whitepaper.md#still-open) |
| [0003](0003-numeric-gate-thresholds.md) | Every roadmap gate has a numeric threshold; speed is set against both Pidgin and Parlot | Accepted | [FND-06](../../active/01-foundations/FND-06-fix-numeric-gate-thresholds.md) | [§ Roadmap](../../../whitepaper.md#roadmap), [§ Performance design and benchmark plan](../../../whitepaper.md#performance-design-and-benchmark-plan), [§ Risks](../../../whitepaper.md#risks) |
| [0004](0004-expression-build-operator-table-and-semantics.md) | `Expression.Build` is one node over a homogeneous operator table, parsed by precedence climbing | Accepted | [CORE-11](../../active/02-core-combinators/CORE-11-expressionbuild.md), [INT-04](../../active/03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md), [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md) | [§ Expressions](../../../whitepaper.md#expressions) |
| [0005](0005-match-cases-infer-every-type-from-a-typed-lambda.md) | `Match` cases infer every type from a typed lambda | Superseded by 0006 | [CORE-05](../../active/02-core-combinators/CORE-05-choice-nodes.md), [CORE-03](../../active/02-core-combinators/CORE-03-primitive-nodes.md), [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md) | [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions), [§ Selective choice](../../../whitepaper.md#selective-choice) |
| [0006](0006-switch-and-match-take-a-collection-beyond-eight-cases.md) | `Switch` and `Match` take a collection beyond eight cases | Accepted | [CORE-05](../../active/02-core-combinators/CORE-05-choice-nodes.md), [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md) | [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions), [§ Selective choice](../../../whitepaper.md#selective-choice) |
| [0007](0007-maybe-as-a-non-boxing-union.md) | `Maybe<T>` is a hand-written non-boxing union of `T` and `Nothing` | Accepted | [CORE-04](../../active/02-core-combinators/CORE-04-sequencing-and-mapping-nodes.md), [FND-09](../../active/01-foundations/FND-09-move-to-the-net-11-ga-sdk.md), [REL-03](../../active/14-release/REL-03-migration-guides.md) | [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions), [§ Where unions are not used](../../../whitepaper.md#where-unions-are-not-used) |
| [0008](0008-control-node-semantics.md) | Control nodes: `Cut` holds up to the nearest predicate, and `Named` labels any parse that consumes nothing | Superseded by 0010 | [CORE-08](../../active/02-core-combinators/CORE-08-control-nodes.md), [INT-03](../../active/03-interpreter/INT-03-execute-the-core-combinators.md), [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md), [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md) | [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking), [§ The error model](../../../whitepaper.md#the-error-model) |
| [0009](0009-where-rule-names-come-from.md) | Rule names come from `Rec`, the enclosing field, and a generated name table | Accepted | [CORE-09](../../active/02-core-combinators/CORE-09-recursion-with-rec.md), [GEN-01](../../active/11-source-generator/GEN-01-generator-skeleton-and-registration.md), [ANA-02](../../active/09-static-analysis/ANA-02-analysis-report-and-debug-enforcement.md), [TOOL-02](../../active/12-grammar-author-tooling/TOOL-02-tracing.md), [TOOL-05](../../active/12-grammar-author-tooling/TOOL-05-ebnf-and-railroad-diagram-export.md) | [§ LINQ query syntax](../../../whitepaper.md#linq-query-syntax), [§ Developer experience for grammar authors](../../../whitepaper.md#developer-experience-for-grammar-authors), [§ Source-generated engine](../../../whitepaper.md#source-generated-engine) |
| [0010](0010-cuts-under-dispatch-and-across-layers.md) | Cuts under predictive dispatch and across layers | Superseded by 0011 | [CORE-08](../../active/02-core-combinators/CORE-08-control-nodes.md), [INT-03](../../active/03-interpreter/INT-03-execute-the-core-combinators.md), [GEN-02](../../active/11-source-generator/GEN-02-emit-the-core-combinators.md), [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md), [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md) | [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking), [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention), [§ Source-generated engine](../../../whitepaper.md#source-generated-engine) |
| [0011](0011-a-cut-is-dropped-with-every-failed-attempt.md) | A cut is dropped with every attempt that fails without consuming | Accepted | [CORE-08](../../active/02-core-combinators/CORE-08-control-nodes.md), [INT-03](../../active/03-interpreter/INT-03-execute-the-core-combinators.md), [INT-04](../../active/03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md), [GEN-02](../../active/11-source-generator/GEN-02-emit-the-core-combinators.md), [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md) | [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking), [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention), [§ Source-generated engine](../../../whitepaper.md#source-generated-engine) |
| [0013](0013-slices-view-in-memory-input-and-copy-streamed-input.md) | Slices are views of in-memory input and copies of streamed input | Accepted | [INT-03](../../active/03-interpreter/INT-03-execute-the-core-combinators.md), [INT-08](../../active/03-interpreter/INT-08-single-layer-parse-api.md), [LAY-01](../../active/06-layers-and-pipelines/LAY-01-streambuffer-and-pulling-cursor.md), [LAY-05](../../active/06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md), [CORE-12](../../active/02-core-combinators/CORE-12-custom-primitives.md) | [§ Text capture](../../../whitepaper.md#text-capture), [§ The stream buffer](../../../whitepaper.md#the-stream-buffer), [§ Design choices](../../../whitepaper.md#design-choices) |

Note: [ADR-0004](0004-expression-build-operator-table-and-semantics.md) point 5 states the earlier `Named` rule, "only for empty failures". [ADR-0008](0008-control-node-semantics.md) point 4, which stands under [ADR-0011](0011-a-cut-is-dropped-with-every-failed-attempt.md), extends it to empty successes for every node, expressions included.

## Template

Copy into `NNNN-<slug>.md`, using the next number:

````markdown
# ADR-NNNN: <Decision, phrased as the choice made or to be made>

| | |
| --- | --- |
| Status | Proposed |
| Date | YYYY-MM-DD |
| Cards | [FND-07](../../active/01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md) |
| Whitepaper | [§ Section](../../../whitepaper.md#anchor) |
| Research | [Note](../research/YYYY-MM-DD-slug.md) |
| Supersedes | — |

## Context

The problem, the forces at play, and the constraints (goals and non-goals from the
whitepaper, performance budgets, compatibility). Link research rather than repeat it.

## Options

### A. <name>
What it is. Pros. Cons. Evidence.

### B. <name>
…

## Decision

The choice, stated so someone could implement it. Include the deciding reason.

## Consequences

What becomes easier, what becomes harder, and what is now ruled out.
Risks this creates and how they are watched (tests, gates, analyzers).

## Changes made

- Whitepaper: sections edited, in the same change as acceptance.
- Cards: created, changed or unblocked.
````
