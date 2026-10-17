# RSLN-04: CRL0004 forward field reference analyzer

| | |
| --- | --- |
| Epic | [Build-time analyzers and code fixes](../../README.md#rsln--build-time-analyzers-and-code-fixes) |
| Phase | Phase 2 — Analysis |
| Status | Not started |

## Summary

The most common bug in Sprache and Pidgin grammars: a field initializer reads a field declared later, which is still null.

## Scope

- Read field initializers in declaration order and flag references to later fields outside `Rec`.
- Code fix that wraps the reference in `Rec`.

## Acceptance criteria

- [ ] The whitepaper's `Value` and `ArrayValue` example is flagged and fixed.

## Blocked by

- [FND-02 Repository and solution scaffolding](../01-foundations/FND-02-repository-and-solution-scaffolding.md)
- [CORE-09 Recursion with Rec](../02-core-combinators/CORE-09-recursion-with-rec.md)

## Unblocks

- [RSLN-06 Code fixes for grammar bugs](../10-roslyn-analyzers/RSLN-06-code-fixes-for-grammar-bugs.md)
- [RSLN-07 Grammar bug corpus](../10-roslyn-analyzers/RSLN-07-grammar-bug-corpus.md)

## Whitepaper references

- [§ Diagnostics](../../../whitepaper.md#diagnostics)
