# CORE-12: Custom primitives

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Designing |

## Summary

A hand-written scanner escape hatch for tokens no regular expression can describe, such as C# raw strings.

## Scope

- `IScanner<TIn, TOut>`, `ScannerInfo(Nullable, First)`.
- Make `CharSet` public. [CORE-03](CORE-03-primitive-nodes.md) built it as an internal type with the API `ScannerInfo` needs; this card changes its visibility, reviews the public surface, and adds its `docs/` page.
- `Custom(scanner)` as the single node case for user code.
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

## Artifacts

- [ADR-0012: Custom scanner metadata](../../docs/decisions/0012-custom-scanner-metadata.md) (Proposed)

## Progress notes

- 2026-10-22 (Design): Started. `ScannerInfo.First` is a `CharSet`, but `IScanner<TIn, TOut>` is generic over its input, so the whitepaper does not say how a token-layer scanner declares its FIRST set. Settle that, and how a failed `Custom` names what it expected, in a decision record before building.
- 2026-10-22 (Design): Drafted ADR-0012 with the maintainer's choices: `First` is a `CharSet` or null for any item, an optional `Name`, and `Scan` stays `bool` with an error-token pattern documented. Next: maintainer accepts the record, then fold it into the whitepaper and cards and move to Build.
