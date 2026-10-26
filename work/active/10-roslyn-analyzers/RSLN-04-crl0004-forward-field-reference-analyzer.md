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
- A `DiagnosticSuppressor` for the nullable warnings C# gives on the safe form. With nullable enabled, the compiler treats a field declared later as maybe null inside a lambda in an earlier initializer, although `Rec` calls the lambda only after the class is initialized. `Rec(() => Later)` itself no longer warns, since `Rec` takes `Func<Parser<TIn, TOut>?>` (CORE-09). A later field used inside a larger `Rec` lambda, as in Example A's `Value = Rec(() => OneOf(..., ArrayValue, ObjectValue))`, still warns CS8604 (measured on RC 2 during CORE-09). Suppress CS8603 and CS8604 only where the expression is a later field of the same grammar class read inside a `Rec` lambda, the case CRL0004 accepts as safe.

## Acceptance criteria

- [ ] The whitepaper's `Value` and `ArrayValue` example is flagged and fixed.
- [ ] Example A, as written in the whitepaper, builds with nullable enabled and warnings as errors, and the same later-field reference outside `Rec` still warns. CORE-09's `RecTests` pins Example A with a `#pragma warning disable CS8604`, and INT-03's `WorkedExampleATests` writes `_arrayValue!` and `_objectValue!` for the same reason; remove both once the suppressor ships.

## Blocked by

- [FND-02 Repository and solution scaffolding](../01-foundations/FND-02-repository-and-solution-scaffolding.md)
- [FND-09 Move to the .NET 11 GA SDK](../01-foundations/FND-09-move-to-the-net-11-ga-sdk.md)
- [CORE-09 Recursion with Rec](../02-core-combinators/CORE-09-recursion-with-rec.md)

## Unblocks

- [RSLN-06 Code fixes for grammar bugs](../10-roslyn-analyzers/RSLN-06-code-fixes-for-grammar-bugs.md)
- [RSLN-07 Grammar bug corpus](../10-roslyn-analyzers/RSLN-07-grammar-bug-corpus.md)

## Whitepaper references

- [§ Diagnostics](../../../whitepaper.md#diagnostics)

## Progress notes

- 2026-10-26 (Not started): Now blocked by FND-09, by the maintainer, so the analyzer is written against the GA Roslyn packages rather than RC 2. Moved from Phase 2, wave 1, to wave 2. Next: start when FND-09 is done.
