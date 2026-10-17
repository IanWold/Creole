# GEN-04: Interpreter fallback and CRL0301

| | |
| --- | --- |
| Epic | [Source-generated engine](../../README.md#gen--source-generated-engine) |
| Phase | Phase 3 — Staging |
| Status | Not started |

## Summary

Staging is an optimization, never a dialect: unreadable subtrees call into the interpreter.

## Scope

- Emit calls into the interpreter for unreadable subtrees.
- Report CRL0301 with the reason (captured local, helper from another assembly, grammar built in a loop).

## Acceptance criteria

- [ ] A grammar mixing readable and unreadable rules gives identical results to pure interpretation.

## Blocked by

- [GEN-02 Emit the core combinators](../11-source-generator/GEN-02-emit-the-core-combinators.md)

## Unblocks

- [GEN-08 Differential engine-agreement tests](../11-source-generator/GEN-08-differential-engine-agreement-tests.md)
- [GEN-10 Measure the interpreter fallback rate](../11-source-generator/GEN-10-measure-the-interpreter-fallback-rate.md)
- [REL-02 Documentation](../14-release/REL-02-documentation.md)

## Whitepaper references

- [§ Source-generated engine](../../../whitepaper.md#source-generated-engine)
- [§ Why keep an interpreter](../../../whitepaper.md#why-keep-an-interpreter)
