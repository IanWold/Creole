# TOOL-05: EBNF and railroad diagram export

| | |
| --- | --- |
| Epic | [Grammar-author tooling](../../README.md#tool--grammar-author-tooling) |
| Phase | Phase 3 — Staging |
| Status | Not started |

## Summary

Document a language straight from its grammar.

## Scope

- Export any rule as EBNF.
- Export railroad diagrams (SVG).
- Nonterminals use rule names ([ADR-0009](../../docs/decisions/0009-where-rule-names-come-from.md)). Example A's `ArrayValue` and `ObjectValue` are not `Rec`s, so their names come from GEN-01's name table; this card moved to Phase 3 for that reason.

## Acceptance criteria

- [ ] Example A exports to readable EBNF and a railroad diagram.

## Blocked by

- [CORE-03 Primitive nodes](../02-core-combinators/CORE-03-primitive-nodes.md)
- [CORE-04 Sequencing and mapping nodes](../02-core-combinators/CORE-04-sequencing-and-mapping-nodes.md)
- [CORE-05 Choice nodes](../02-core-combinators/CORE-05-choice-nodes.md)
- [CORE-07 Repetition nodes](../02-core-combinators/CORE-07-repetition-nodes.md)
- [CORE-08 Control nodes](../02-core-combinators/CORE-08-control-nodes.md)
- [CORE-11 Expression.Build](../02-core-combinators/CORE-11-expressionbuild.md)
- [GEN-01 Generator skeleton and registration](../11-source-generator/GEN-01-generator-skeleton-and-registration.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Core model: parsers as data](../../../whitepaper.md#core-model-parsers-as-data)
- [§ Developer experience for grammar authors](../../../whitepaper.md#developer-experience-for-grammar-authors)
