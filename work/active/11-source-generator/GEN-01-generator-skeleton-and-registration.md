# GEN-01: Generator skeleton and registration

| | |
| --- | --- |
| Epic | [Source-generated engine](../../README.md#gen--source-generated-engine) |
| Phase | Phase 3 — Staging |
| Status | Not started |

## Summary

Emit one method per rule and register it, so callers keep using the field and transparently get compiled code.

## Scope

- Emit `Parse_<Rule>(ref Cursor<T> c, out T result)` per rule.
- Register through a generated static constructor, or a module initializer if the class already has one.
- `Staging.Register` and planner lookup (INT-02 hook).
- Register every rule field's name for the node it holds, in a table keyed by node, from the same static constructor ([ADR-0009](../../docs/decisions/0009-where-rule-names-come-from.md)). Names are registered even for rules whose code falls back to the interpreter. A registered name takes precedence over a `Rec` name. Make the table public with `Staging`.
- `[GrammarOptions]` attribute for per-class tuning.

## Acceptance criteria

- [ ] A trivial grammar runs through generated code, verified by a test hook.
- [ ] Every rule field of a `partial` grammar class, `Rec` or not, has its field name registered, shown by a test over Example A's `JsonGrammar`.

## Blocked by

- [INT-02 Plan lowering and cache](../../complete/03-interpreter/INT-02-plan-lowering-and-cache.md)
- [RSLN-01 Roslyn grammar reader](../10-roslyn-analyzers/RSLN-01-roslyn-grammar-reader.md)

## Unblocks

- [GEN-02 Emit the core combinators](../11-source-generator/GEN-02-emit-the-core-combinators.md)
- [TOOL-05 EBNF and railroad diagram export](../12-grammar-author-tooling/TOOL-05-ebnf-and-railroad-diagram-export.md)

## Whitepaper references

- [§ Source-generated engine](../../../whitepaper.md#source-generated-engine)
