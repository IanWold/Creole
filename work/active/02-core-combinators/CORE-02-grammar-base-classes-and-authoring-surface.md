# CORE-02: Grammar base classes and authoring surface

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

C# cannot infer `TIn` from a call like `Char('a')`, so grammars inherit a base class that fixes the input type and brings combinators into scope unqualified.

## Scope

- `Grammar<TIn>`: `Any`, `Token`, `Satisfy`, `End`, `Seq`, `OneOf`, `Longest`, `Rec`, `Return`, `Fail`.
- `TokenGrammar<TToken, TKind>`: `Kind(k)`, overridable `Describe(TKind)`, and `Case<T>()` for union tokens.
- `CharGrammar`: character classes, `String`, `CIString`, `Text()` and `Slice()`.
- The static `Grammar` class entry points (for example `Grammar.Deterministic`, stubbed until ANA-06).

## Acceptance criteria

- [ ] The grammar classes in Worked Example A compile against the API.

## Blocked by

- [CORE-01 Grammar node model](../02-core-combinators/CORE-01-grammar-node-model.md)

## Unblocks

- [CORE-03 Primitive nodes](../02-core-combinators/CORE-03-primitive-nodes.md)
- [RSLN-01 Roslyn grammar reader](../10-roslyn-analyzers/RSLN-01-roslyn-grammar-reader.md)

## Whitepaper references

- [§ Authoring ergonomics](../../../whitepaper.md#authoring-ergonomics)
