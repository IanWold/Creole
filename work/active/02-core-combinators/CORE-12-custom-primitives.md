# CORE-12: Custom primitives

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Size | S |
| Status | Not started |

## Summary

A hand-written scanner escape hatch for tokens no regular expression can describe, such as C# raw strings.

## Scope

- `IScanner<TIn, TOut>`, `ScannerInfo(Nullable, First)`, `CharSet`.
- `Custom(scanner)` as the single node case for user code.
- Document the contract: the scanner must fail without consuming input.

## Acceptance criteria

- [ ] A raw-string scanner sample compiles and is usable inside `Longest`.

## Blocked by

- [CORE-01 Grammar node model](../02-core-combinators/CORE-01-grammar-node-model.md)

## Unblocks

- [INT-04 Execute Bind, Expression.Build and Custom](../03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [RSLN-01 Roslyn grammar reader](../10-roslyn-analyzers/RSLN-01-roslyn-grammar-reader.md)
- [FUS-03 Fusion with modes and custom scanners](../16-layer-fusion/FUS-03-fusion-with-modes-and-custom-scanners.md)

## Whitepaper references

- [§ Custom primitives](../../../whitepaper.md#custom-primitives)
