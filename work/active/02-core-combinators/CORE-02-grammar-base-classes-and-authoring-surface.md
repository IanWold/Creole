# CORE-02: Grammar base classes and authoring surface

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

C# cannot infer `TIn` from a call like `Char('a')`, so grammars inherit a base class that fixes the input type and brings combinators into scope unqualified.

## Scope

- `Grammar<TIn>`, `TokenGrammar<TToken, TKind>` (with `TToken : IKinded<TKind>`) and `CharGrammar : Grammar<char>`.
- `Describe(TKind)` moved to [ERR-02](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md), its first consumer. How the renderer reaches a grammar's override is not settled.
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

## Progress notes

- 2026-10-18 (Build): Started. Add `Grammar<TIn>`, `TokenGrammar<TToken, TKind>`, `CharGrammar` and the static `Grammar` class, with `Return` and `Fail` proving the unqualified-call pattern.
- 2026-10-18 (Build): Moved `Describe(TKind)` to ERR-02: grammar classes are never instantiated and a class cannot have static virtuals, so how the renderer reaches an override needs a decision there. Added `Grammar<TIn>` (with `Return`, `Fail`; CA1000 suppressed on purpose), `TokenGrammar<TToken, TKind>`, `CharGrammar`, the static `Grammar` with `Deterministic` returning its argument unchecked, `GrammarBaseTests`, and the docs section. Found that `Guard.NotNull` named the combinator's parameter, not the caller's expression, so public combinators take their own `[CallerArgumentExpression]` parameter and forward it. Build, all 114 tests and the AOT publish pass. Next: maintainer review, then `/complete`.
- 2026-10-18 (Build): Recorded the `[CallerArgumentExpression]` convention in the scope of CORE-04 to CORE-08, CORE-10 and CORE-11, with fixed-arity overloads instead of `params` for `Seq`, `OneOf`, `Match` and `Longest` (agreed with the maintainer).
- 2026-10-18 (Build): Extended the convention to `Custom(scanner)` in CORE-12; other non-parser arguments keep plain parameter-name checks (agreed with the maintainer).
