# Decision records

Each record captures one design choice: the options, the choice, and why. Rules are in [CONTRIBUTING.md § Decision records](../../../CONTRIBUTING.md#decision-records). Only the maintainer marks a record Accepted.

## Index

| # | Decision | Status | Cards | Whitepaper |
| --- | --- | --- | --- | --- |
| — | No decisions yet. | — | — | — |

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
