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
- `[GrammarOptions]` attribute for per-class tuning.

## Acceptance criteria

- [ ] A trivial grammar runs through generated code, verified by a test hook.

## Blocked by

- [INT-02 Plan lowering and cache](../03-interpreter/INT-02-plan-lowering-and-cache.md)
- [RSLN-01 Roslyn grammar reader](../10-roslyn-analyzers/RSLN-01-roslyn-grammar-reader.md)

## Unblocks

- [GEN-02 Emit the core combinators](../11-source-generator/GEN-02-emit-the-core-combinators.md)

## Whitepaper references

- [§ Source-generated engine](../../../whitepaper.md#source-generated-engine)
