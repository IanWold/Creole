# ADR-0005: `Match` cases infer every type from a typed lambda

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-19 |
| Cards | [CORE-05](../../active/02-core-combinators/CORE-05-choice-nodes.md), [CORE-03](../../active/02-core-combinators/CORE-03-primitive-nodes.md), [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md) |
| Whitepaper | [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions), [§ Selective choice](../../../whitepaper.md#selective-choice) |
| Research | CORE-05's spike, recorded in its progress notes (2026-10-19) |
| Supersedes | — |

## Context

[§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions) sketches selective choice over a union like this:

```csharp
HeaderParser.Match(
    Case<V1Header>(BodyV1, static (h, body) => new Record(h.Id, body)),
    Case<V2Header>(BodyV2, static (h, body) => new Record(h.Id, body)));
```

This cannot compile. A case needs three types: the union case `A`, the branch's result `B`, and the result `TOut`. C# requires a call to give all of a method's type arguments or none of them. So with one explicit type argument, `Case<A>`'s parameters can mention only `A` and the grammar's `TIn`, and it cannot take a typed branch parser or combining function.

CORE-05's spike on SDK 11.0.100-rc.2.26504.105 confirmed this. In a `TokenGrammar`-derived class, the sketch binds to CORE-03's `Case<T>()` for union tokens and fails with CS1501 ("No overload for method 'Case' takes 2 arguments").

Forces:

- **Every branch stays visible to analysis**, so CRL0105 can check that each union case has a branch. A case must therefore hold a parser, not a function that returns one.
- **Coexistence with CORE-03.** `TokenGrammar` gains `Case<T>()` with no arguments for union tokens. A `Case` on `Grammar<TIn>` must still resolve from a `TokenGrammar`-derived class.
- **Readable grammars** (§ Authoring ergonomics). A case should not need more ceremony than the branch itself.

## Options

### A. Infer every type from a typed lambda

`Case<TCase, TBranch, TOut>(Parser<TIn, TBranch> branch, Func<TCase, TBranch, TOut> combine)`, called with no type arguments:

```csharp
HeaderParser.Match(
    Case(BodyV1, static (V1Header h, Body body) => new Record(h.Id, body)),
    Case(BodyV2, static (V2Header h, Body body) => new Record(h.Id, body)));
```

- Pros:
  - Compiles on RC 2, including from a `TokenGrammar`-derived class that declares `Case<T>()`. That method is not applicable to two arguments, so overload resolution drops it and picks the base method.
  - The case type is written once, next to the variable it names.
  - Keeps the name `Case`.
- Cons:
  - Both lambda parameters need types, because C# does not allow mixing typed and untyped lambda parameters. When the branch's result type is long, such as `IReadOnlyList<Stmt>`, the lambda gets wordy.

### B. Explicit type arguments

The same method, called as `Case<V1Header, Body, Record>(BodyV1, static (h, body) => new Record(h.Id, body))`.

- Pros: the lambda is untyped.
- Cons: three type arguments on every case. `TOut` repeats the rule's type on every line.

### C. A builder under another name

A nested generic class, such as `When<V1Header>.Then(BodyV1, static (h, body) => …)`. The class fixes `A`, and `Then` infers the rest.

- Pros: the shortest call site.
- Cons:
  - It cannot be called `Case`. CORE-03's method `Case<T>()` in `TokenGrammar` would hide a nested type `Case<A>` in `Grammar<TIn>`, because a method hides non-method members of a base type with the same name.
  - It adds a public nested generic type, and a second word for one concept.

### D. Keep the whitepaper's form

Not possible in C# 15.

## Decision

Option A, chosen by the maintainer on 2026-10-19. Deciding reason: it is the only option that keeps the name `Case`, needs no extra types, and still compiles alongside CORE-03's `Case<T>()`.

The surface CORE-05 builds:

- `Grammar<TIn>.Case<TCase, TBranch, TOut>(Parser<TIn, TBranch> branch, Func<TCase, TBranch, TOut> combine)` returns a `MatchCase<TIn, TOut>`. It has a `[CallerArgumentExpression]` parameter for each of its two arguments.
- `Match` is an extension member on `Parser<TIn, TUnion>` where `TUnion : IUnion` (`System.Runtime.CompilerServices.IUnion`, which declared unions implement). Engines read the case from `IUnion.Value` without reflection.
- `Match` takes 1 to 8 cases, as fixed-arity overloads with one expression parameter per case (CORE-05 § Scope). A second case for the same `TCase` throws a `GrammarException`.
- At run time a case applies if the union's value is a `TCase`. Cases are tried in order. If none applies, `Match` fails, and that failure is committed whenever the scrutinee consumed input.
- Other caps for the same reason: `OneOf` takes 2 to 16 alternatives; `Switch` takes 1 to 8 cases plus `@default`. Larger choices nest.

What would change this: a C# version that lets a call give some type arguments and infer the rest would make the whitepaper's original form (D) possible.

## Consequences

- The whitepaper's sample changes to the typed-lambda form.
- CRL0105 (ANA-01) reads each case's `TCase` from the node's structural view, and in source from the lambda's first parameter type.
- A union's case types are not checked against `TCase` when the grammar is built, because that would need reflection over the union's constructors. A `TCase` that is not a case of the union is a branch that never runs. CRL0105's analysis is the place to report it.
- Unions are the only scrutinee type. `Match` over a class hierarchy is ruled out for now; adding it later is a new overload, not a change to this one.

## Changes made

- Whitepaper: § Using C# 15 unions has the typed-lambda sample, its table row uses the new form, and a paragraph explains the inference limit and links here. § Selective choice points to `Match`.
- Cards: CORE-05's scope states the `Case` and `Match` signatures and the overload caps. CORE-03 is unchanged: its `Case<T>()` coexists with this `Case`.
