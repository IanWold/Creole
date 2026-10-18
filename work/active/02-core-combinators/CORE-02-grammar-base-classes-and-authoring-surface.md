# CORE-02: Grammar base classes and authoring surface

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

C# cannot infer `TIn` from a call like `Char('a')`, so grammars inherit a base class that fixes the input type and brings combinators into scope unqualified.

## Scope

- `Grammar<TIn>`, `TokenGrammar<TToken, TKind>` (with `TToken : IKinded<TKind>` and an overridable `Describe(TKind)`) and `CharGrammar : Grammar<char>`.
- `Return` and `Fail` on `Grammar<TIn>`. Their nodes exist from CORE-01, so they prove the pattern.
- The static `Grammar` class, with `Grammar.Deterministic` stubbed until ANA-06.
- Document in `docs/concepts/grammars-as-data.md` that combinators reject null arguments with a `GrammarException` naming the expression. CORE-01 built the check, but nothing public used it yet.

Each node card adds its own entry points to the class that [§ Authoring ergonomics](../../../whitepaper.md#authoring-ergonomics) names, so the cards that add static members wait on this one:

| Card | Adds |
| --- | --- |
| CORE-03 | `Any`, `Token`, `Satisfy`, `End`; `Kind(k)` and `Case<T>()` for union tokens on `TokenGrammar`; character classes, `String` and `CIString` on `CharGrammar` |
| CORE-04 | `Seq`; `Text()` and `Slice()` for character layers |
| CORE-05 | `OneOf`; `Case<A>(...)` for `Match` |
| CORE-06 | `Longest` |
| CORE-08 | `Atomic`, `Lookahead`, `Not` if they are static |
| CORE-09 | `Rec` |
| CORE-12 | `Custom` |

The Worked Example A criterion moved to INT-03, the first card that has every combinator the example uses.

## Acceptance criteria

- [ ] A class deriving from each of `Grammar<TIn>`, `TokenGrammar<TToken, TKind>` and `CharGrammar` calls `Return` and `Fail` unqualified, with `TIn` fixed by the base class.

## Blocked by

- [CORE-01 Grammar node model](../02-core-combinators/CORE-01-grammar-node-model.md)

## Unblocks

- [CORE-03 Primitive nodes](../02-core-combinators/CORE-03-primitive-nodes.md)
- [CORE-04 Sequencing and mapping nodes](../02-core-combinators/CORE-04-sequencing-and-mapping-nodes.md)
- [CORE-05 Choice nodes](../02-core-combinators/CORE-05-choice-nodes.md)
- [CORE-06 Longest](../02-core-combinators/CORE-06-longest.md)
- [CORE-08 Control nodes](../02-core-combinators/CORE-08-control-nodes.md)
- [CORE-09 Recursion with Rec](../02-core-combinators/CORE-09-recursion-with-rec.md)
- [CORE-12 Custom primitives](../02-core-combinators/CORE-12-custom-primitives.md)
- [RSLN-01 Roslyn grammar reader](../10-roslyn-analyzers/RSLN-01-roslyn-grammar-reader.md)

## Whitepaper references

- [§ Authoring ergonomics](../../../whitepaper.md#authoring-ergonomics)
