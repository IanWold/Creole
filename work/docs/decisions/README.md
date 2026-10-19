# Decision records

Each record captures one design choice: the options, the choice, and why. Rules are in [CONTRIBUTING.md § Decision records](../../../CONTRIBUTING.md#decision-records). Only the maintainer marks a record Accepted.

## Index

| # | Decision | Status | Cards | Whitepaper |
| --- | --- | --- | --- | --- |
| [0001](0001-grammar-node-encoding-and-engine-dispatch.md) | Grammar nodes use an internal constructor and one abstract method per engine operation, not a `closed` hierarchy | Accepted | [CORE-01](../../active/02-core-combinators/CORE-01-grammar-node-model.md), [FND-07](../../active/01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md), [INT-02](../../active/03-interpreter/INT-02-plan-lowering-and-cache.md), [RSLN-02](../../active/10-roslyn-analyzers/RSLN-02-share-the-analysis-engine-with-the-roslyn-component.md) | [§ Types](../../../whitepaper.md#types), [§ Where unions are not used](../../../whitepaper.md#where-unions-are-not-used) |
| [0002](0002-parseresult-as-a-plain-union.md) | `ParseResult<T>` is a plain C# 15 union | Accepted | [FND-07](../../active/01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md), [ERR-01](../../active/04-errors-and-recovery/ERR-01-error-and-result-types.md), [LAY-03](../../active/06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md) | [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions), [§ Still open](../../../whitepaper.md#still-open) |
| [0003](0003-numeric-gate-thresholds.md) | Every roadmap gate has a numeric threshold; speed is set against both Pidgin and Parlot | Accepted | [FND-06](../../active/01-foundations/FND-06-fix-numeric-gate-thresholds.md) | [§ Roadmap](../../../whitepaper.md#roadmap), [§ Performance design and benchmark plan](../../../whitepaper.md#performance-design-and-benchmark-plan), [§ Risks](../../../whitepaper.md#risks) |
| [0004](0004-expression-build-operator-table-and-semantics.md) | `Expression.Build` is one node over a homogeneous operator table, parsed by precedence climbing | Accepted | [CORE-11](../../active/02-core-combinators/CORE-11-expressionbuild.md), [INT-04](../../active/03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md), [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md) | [§ Expressions](../../../whitepaper.md#expressions) |
| [0007](0007-maybe-as-a-non-boxing-union.md) | `Maybe<T>` is a hand-written non-boxing union of `T` and `Nothing` | Accepted | [CORE-04](../../active/02-core-combinators/CORE-04-sequencing-and-mapping-nodes.md), [FND-09](../../active/01-foundations/FND-09-move-to-the-net-11-ga-sdk.md), [REL-03](../../active/14-release/REL-03-migration-guides.md) | [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions), [§ Where unions are not used](../../../whitepaper.md#where-unions-are-not-used) |

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
