# GEN-07: Debugging generated code

| | |
| --- | --- |
| Epic | [Source-generated engine](../../README.md#gen--source-generated-engine) |
| Phase | Phase 3 — Staging |
| Status | Not started |

## Summary

Breakpoints on grammar lines work.

## Scope

- Emit `#line` directives back to grammar declarations.
- Readable generated code with rule names.

## Acceptance criteria

- [ ] Stepping into a generated parser in the debugger lands on grammar source lines.

## Blocked by

- [GEN-02 Emit the core combinators](../11-source-generator/GEN-02-emit-the-core-combinators.md)

## Unblocks

- [REL-05 Ship 1.0](../14-release/REL-05-ship-10.md)

## Whitepaper references

- [§ Developer experience for grammar authors](../../../whitepaper.md#developer-experience-for-grammar-authors)
