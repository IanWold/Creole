# GEN-03: Lambda and helper inlining

| | |
| --- | --- |
| Epic | [Source-generated engine](../../README.md#gen--source-generated-engine) |
| Phase | Phase 3 — Staging |
| Size | M |
| Status | Not started |

## Summary

No delegates on the hot path in generated code.

## Scope

- Inline non-capturing lambdas passed to `Select`.
- Inline expression-bodied static helpers such as `Lexeme`.
- Remove LINQ transparent-identifier allocations.

## Acceptance criteria

- [ ] Generated JSON parser allocates nothing beyond the result values.

## Blocked by

- [CORE-10 Bind and LINQ query syntax](../02-core-combinators/CORE-10-bind-and-linq-query-syntax.md)
- [GEN-02 Emit the core combinators](../11-source-generator/GEN-02-emit-the-core-combinators.md)

## Unblocks

- [GEN-08 Differential engine-agreement tests](../11-source-generator/GEN-08-differential-engine-agreement-tests.md)

## Whitepaper references

- [§ Source-generated engine](../../../whitepaper.md#source-generated-engine)
- [§ LINQ query syntax](../../../whitepaper.md#linq-query-syntax)
- [§ Design choices](../../../whitepaper.md#design-choices)
