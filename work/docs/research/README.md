# Research notes

Research notes record what the project learned, from where, and what it means for Creole. Rules for writing them are in [CONTRIBUTING.md § Research notes](../../../CONTRIBUTING.md#research-notes).

## Index

Newest first. One line each: date, link, the question it answers, and the cards it serves.

| Date | Note | Question | Cards |
| --- | --- | --- | --- |
| 2026-10-24 | [Parlot 2.0 baselines and generator](2026-10-24-parlot-2-0-baselines-and-generator.md) | Which stable Parlot should the baselines use, and do the whitepaper's Parlot facts hold on 2.0.0? | [FND-10](../../active/01-foundations/FND-10-move-the-parlot-baselines-to-200.md), [BENCH-03](../../active/13-benchmarks/BENCH-03-phase-3-speed-gate.md), [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md), [STRM-02](../../active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md), [STRM-04](../../active/08-streaming/STRM-04-huge-single-documents.md) |
| 2026-10-23 | [Commit-aware automaton prototypes](2026-10-23-commit-aware-automaton-prototypes.md) | Does a commit-aware automaton agree with the interpreter, and what does it cost? | [INT-06](../../active/03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md), [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md), [ANA-04](../../active/09-static-analysis/ANA-04-choice-checks.md), [ANA-07](../../active/09-static-analysis/ANA-07-linear-maximal-munch.md), [GEN-05](../../active/11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md) |
| 2026-10-23 | [DFA compilation under commit semantics](2026-10-23-dfa-compilation-under-commit-semantics.md) | How can regular subtrees compile to DFAs that agree with commit semantics, report errors and values, and bound backup? | [INT-06](../../active/03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md), [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md), [ANA-04](../../active/09-static-analysis/ANA-04-choice-checks.md), [ANA-07](../../active/09-static-analysis/ANA-07-linear-maximal-munch.md), [GEN-05](../../active/11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md) |
| 2026-10-18 | [Numeric gate thresholds](2026-10-18-numeric-gate-thresholds.md) | What numbers should the roadmap gates use, given measured noise and prior art? | [FND-06](../../active/01-foundations/FND-06-fix-numeric-gate-thresholds.md) |
| 2026-10-18 | [Grammar node encoding and dispatch](2026-10-18-grammar-node-encoding-and-dispatch.md) | How should grammar nodes encode hidden types, and how can engine dispatch over them be checked for completeness? | [CORE-01](../../active/02-core-combinators/CORE-01-grammar-node-model.md), [FND-07](../../active/01-foundations/FND-07-validate-c-15-features-and-decide-parseresult-t.md), [INT-02](../../active/03-interpreter/INT-02-plan-lowering-and-cache.md), [RSLN-02](../../active/10-roslyn-analyzers/RSLN-02-share-the-analysis-engine-with-the-roslyn-component.md) |

## Template

Copy into `YYYY-MM-DD-<slug>.md`:

````markdown
# <Question, phrased as a question>

| | |
| --- | --- |
| Date | YYYY-MM-DD |
| Cards | [CORE-04](../../active/02-core-combinators/CORE-04-sequencing-and-mapping-nodes.md) |
| Whitepaper | [§ Section](../../../whitepaper.md#anchor) |
| Confidence | High / Medium / Low — one line on why |

## Short answer

Two to five sentences a reader can stop after.

## Context

Why the question came up and what decision it feeds.

## Findings

Numbered findings. Each says what a source claims, with a citation [1], and keeps
our own inference visibly separate ("Inference: …").

## Disconfirming evidence

What argues against the whitepaper's current position, or against the short answer.
"None found" is a valid entry if the search was real; say what was searched.

## Implications for Creole

- Whitepaper sections to revisit.
- Cards to create, change or unblock.
- Decisions this note should feed.

## Open questions

What this note could not settle.

## Sources

1. Author. Title. Venue Year. [link](https://…) — read in full / skimmed / abstract only
2. … — *unverified*: from memory, not checked
````
