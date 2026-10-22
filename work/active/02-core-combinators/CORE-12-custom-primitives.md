# CORE-12: Custom primitives

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

A hand-written scanner escape hatch for tokens no regular expression can describe, such as C# raw strings.

## Scope

- `IScanner<TIn, TOut>`, `ScannerInfo(Nullable, First)`.
- Make `CharSet` public. [CORE-03](CORE-03-primitive-nodes.md) built it as an internal type with the API `ScannerInfo` needs; this card changes its visibility, reviews the public surface, and adds its `docs/` page.
- `Custom(scanner)` as the single node case for user code.
- Any public way for a scanner to take a slice of the characters it consumed follows [ADR-0013](../../docs/decisions/0013-slices-view-in-memory-input-and-copy-streamed-input.md): a view over in-memory input, a copy otherwise. The engine's internal equivalent is `Cursor<T>.Since`.
- Document the contract: the scanner must fail without consuming input.
- `Custom` null-checks its scanner with its own `[CallerArgumentExpression]` parameter, passed on to `Guard`, so the `GrammarException` names the caller's expression (see `Grammar.Deterministic`, CORE-02). Scanners usually live in `static readonly` fields, so a null one is likely a field declared later in the class. The message should say to declare the scanner before the rules that use it, not suggest `Rec`.

## Acceptance criteria

- [ ] A raw-string scanner sample compiles and is usable inside `Longest`.

## Blocked by

- [CORE-02 Grammar base classes and authoring surface](../02-core-combinators/CORE-02-grammar-base-classes-and-authoring-surface.md)
- [CORE-03 Primitive nodes](../02-core-combinators/CORE-03-primitive-nodes.md)
- [CORE-06 Longest](../02-core-combinators/CORE-06-longest.md)

## Unblocks

- [INT-04 Execute Bind, Expression.Build and Custom](../03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [RSLN-01 Roslyn grammar reader](../10-roslyn-analyzers/RSLN-01-roslyn-grammar-reader.md)
- [FUS-03 Fusion with modes and custom scanners](../16-layer-fusion/FUS-03-fusion-with-modes-and-custom-scanners.md)

## Whitepaper references

- [§ Custom primitives](../../../whitepaper.md#custom-primitives)
