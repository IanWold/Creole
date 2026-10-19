# ADR-0006: `Switch` and `Match` take a collection beyond eight cases

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-19 |
| Cards | [CORE-05](../../active/02-core-combinators/CORE-05-choice-nodes.md), [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md) |
| Whitepaper | [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions), [§ Selective choice](../../../whitepaper.md#selective-choice) |
| Research | CORE-05's spikes, recorded in its progress notes (2026-10-19) |
| Supersedes | [ADR-0005](0005-match-cases-infer-every-type-from-a-typed-lambda.md) |

## Context

[ADR-0005](0005-match-cases-infer-every-type-from-a-typed-lambda.md) settled the shape of `Match` cases. It also capped the fixed-arity overloads: `OneOf` at 16 alternatives, and `Switch` and `Match` at 8 cases. It said "Larger choices nest."

That holds for `OneOf`, because `OneOf(a, …, p, OneOf(q, …))` matches the same input as one flat choice. It does not hold for `Switch` or `Match`. Their branches run after the scrutinee, and a branch does not receive the scrutinee's value. A nested `Switch` in `@default` would have to parse the scrutinee again, and a `Match` has no default at all. So under ADR-0005 a `Switch` over 9 opcodes cannot be written. Dispatching on a tag in a binary format is the main use of `Switch` (§ Selective choice).

CORE-05's scope requires fixed-arity overloads because a `params` array cannot capture each element's expression for null-argument messages. That reason still stands for small counts.

## Options

### A. Fixed arity up to 8, plus one overload taking a collection

```csharp
Opcode.Switch(
    [(0x01, Nop), (0x02, Push), /* … */ (0x2A, Halt)],
    @default: Fail<Instr>("unknown opcode"));

Node.Match([Case(…), Case(…), /* … */]);
```

- Pros:
  - No limit. A table built in code, such as an array of opcodes and their parsers, can be passed directly.
  - The fixed overloads keep naming the caller's expression in error messages, for the common small cases.
  - The spike on RC 2 showed that a collection expression of tuples infers `TOut`, and so does an array. A call with separate tuple arguments still binds to the fixed overload.
- Cons:
  - Errors in the collection form name a case by its index and key, not by its expression.
  - Two ways to write the same switch.

### B. Raise the caps to 16

- Pros: one form only.
- Cons: still a hard limit with no workaround past it. Twice the generated overloads.

### C. Keep the caps at 8 and defer

- Pros: no new API now.
- Cons: a known hole in a core combinator. A record would still have to correct "Larger choices nest."

## Decision

Option A, chosen by the maintainer on 2026-10-19. Deciding reason: it is the only option without a hard limit, and it keeps the better error messages for the common small cases.

This record restates ADR-0005's decision with the change, so it stands alone.

**`Match` cases** (unchanged from ADR-0005):

- `Grammar<TIn>.Case<TCase, TBranch, TOut>(Parser<TIn, TBranch> branch, Func<TCase, TBranch, TOut> combine)` returns a `MatchCase<TIn, TOut>`. It is called with no type arguments, and the typed lambda names the case: `Case(BodyV1, static (V1Header h, Body body) => …)`. ADR-0005 explains why C# cannot accept `Case<V1Header>(…)`.
- `Match` is an extension member on `Parser<TIn, TUnion>` where `TUnion : IUnion`. Cases are tried in order, and a case applies if the union's value is a `TCase`. If none applies, `Match` fails, and that failure is committed whenever the scrutinee consumed input.

**Arity:**

| Combinator | Fixed-arity overloads | Beyond that |
| --- | --- | --- |
| `OneOf` | 2 to 16 alternatives | Nest `OneOf` calls |
| `Switch` | 1 to 8 cases, plus `@default` | `Switch(IReadOnlyList<(TKey Key, Parser<TIn, TOut> Branch)> cases, Parser<TIn, TOut> @default)` |
| `Match` | 1 to 8 cases | `Match(IReadOnlyList<MatchCase<TIn, TOut>> cases)` |

The collection overloads:

- copy the collection, so later changes to it do not change the grammar;
- reject a null collection, an empty collection, a null branch, and a null case with a `GrammarException`, counting cases from 0 as `Expression.Build` does;
- reject two `Switch` cases with the same key (by `EqualityComparer<TKey>.Default`) and two `Match` cases for the same `TCase`, as the fixed overloads do.

## Consequences

- Grammars generated from tables, such as instruction sets, can use `Switch` directly.
- The node is the same whichever overload built it, so analysis, export and engines see no difference.
- An error in a long collection names the case's index, not its source text. That is a weaker message than the fixed overloads give, accepted for the rare large case.

## Changes made

- Whitepaper: § Selective choice describes the eight-case limit of the separate-argument form, the collection form, and why nesting does not work. § Using C# 15 unions links this record alongside ADR-0005.
- Records: ADR-0005 is Superseded by 0006.
- Cards: CORE-05's scope states the collection overloads.
