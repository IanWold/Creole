# TOOL-04: Grammar fuzzing

| | |
| --- | --- |
| Epic | [Grammar-author tooling](../../README.md#tool--grammar-author-tooling) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

Grammars are data, so inputs can be generated from them.

## Scope

- Generate random valid inputs from a grammar.
- Mutate them into near misses.
- Report which nodes are opaque and cannot be generated.

## Acceptance criteria

- [ ] Generated inputs for Example A all parse; mutated ones mostly fail.

## Blocked by

- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)

## Unblocks

- [GEN-08 Differential engine-agreement tests](../11-source-generator/GEN-08-differential-engine-agreement-tests.md)

## Whitepaper references

- [§ Developer experience for grammar authors](../../../whitepaper.md#developer-experience-for-grammar-authors)
