# CORE-12: Custom primitives

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

A hand-written scanner escape hatch for tokens no regular expression can describe, such as C# raw strings.

## Scope

- `IScanner<TIn, TOut>` and `ScannerInfo(bool Nullable, CharSet? First = null, string? Name = null)` ([ADR-0012](../../docs/decisions/0012-custom-scanner-metadata.md)). A null `First` means any item; the node is then opaque, like `Satisfy`.
- Make `CharSet` public. [CORE-03](CORE-03-primitive-nodes.md) built it as an internal type with the API `ScannerInfo` needs; this card changes its visibility, reviews the public surface, and adds its `docs/` page.
- `Custom(scanner)` as the single node case for user code.
- `Custom` reads `Info` once, when the node is built, and throws `GrammarException` if `Info` is null, if `First` is given and `TIn` is not `char`, or if `Name` is empty or white space.
- Document the contract: the scanner must fail without consuming input. Document the pattern for detailed errors: the scanner succeeds with a value marking malformed text, and the grammar binds it and calls `Fail(message)` (ADR-0012 point 5). Document that a token crossing a segment boundary must be copied, since `Cursor<T>` exposes spans.
- `Custom` null-checks its scanner with its own `[CallerArgumentExpression]` parameter, passed on to `Guard`, so the `GrammarException` names the caller's expression (see `Grammar.Deterministic`, CORE-02). Scanners usually live in `static readonly` fields, so a null one is likely a field declared later in the class. The message should say to declare the scanner before the rules that use it, not suggest `Rec`.

## Acceptance criteria

- [ ] A raw-string scanner sample compiles and is usable inside `Longest`.
- [ ] Tests cover each `GrammarException` in ADR-0012 point 2, and the FIRST set, nullability and opacity analysis reads from a `Custom` with and without `First`.

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

- [ADR-0012: Custom scanner metadata](../../docs/decisions/0012-custom-scanner-metadata.md) (Accepted)

## Progress notes

- 2026-10-22 (Design): Started. `ScannerInfo.First` is a `CharSet`, but `IScanner<TIn, TOut>` is generic over its input, so the whitepaper does not say how a token-layer scanner declares its FIRST set. Settle that, and how a failed `Custom` names what it expected, in a decision record before building.
- 2026-10-22 (Design): Drafted ADR-0012 with the maintainer's choices: `First` is a `CharSet` or null for any item, an optional `Name`, and `Scan` stays `bool` with an error-token pattern documented. Next: maintainer accepts the record, then fold it into the whitepaper and cards and move to Build.
- 2026-10-22 (Build): ADR-0012 accepted; whitepaper and the INT-04, ANA-01, ANA-08, ERR-02 and FUS-03 cards updated, and a criterion added for the construction checks and analysis properties. Next: `/build` to plan the code.
- 2026-10-22 (Build): Increment 1 done. `CharSet` is public; `Format` stays internal; XML docs on every public member; `Union` null-checks its argument. Tests: `CharSetTests` gains equality, null `Union`, and a compile check from another assembly that sees the public members but not `Format` or `Keys`. Docs: a "Character sets" section in docs/concepts/primitives.md. Next: increment 2, `IScanner`, `ScannerInfo`, `Custom` and the raw-string sample.
- 2026-10-22 (Build): Increment 2 done. `IScanner<TIn, TOut>`, `ScannerInfo`, `Grammar<TIn>.Custom` and `CustomNode` (`NodeKind.Custom`), with the construction checks of ADR-0012 point 2 and `Guard.ScannerNotNull`. Tests: `CustomNodeTests` (10) for the checks, FIRST, nullability, opacity and reading `Info` once; `RawStringSampleTests` (18) run a Rust-style raw-string scanner on cursors, across segments, and build it inside `Longest` with an unterminated check bound after the `Longest`. Docs: new docs/concepts/custom-scanners.md, a line in grammars-as-data.md. Found while writing the sample: `Longest` gives back the input of a failed alternative, so the ADR-0012 error pattern must bind after the `Longest`, not inside it; documented. Both criteria appear met; next: `/complete`.
