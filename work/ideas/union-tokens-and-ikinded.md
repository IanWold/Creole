# Union tokens and `IKinded`

Added 2026-10-19.

**What:** `TokenGrammar<TToken, TKind>` requires `TToken : IKinded<TKind>`, and CORE-03 puts `Case<T>()` on `TokenGrammar`. So a grammar over union tokens must also declare a kind enum and a `Kind` switch on the union. A spike on the .NET 11 RC 2 SDK showed this compiles: a `union` can implement `IKinded<TKind>`, and it also implements `System.Runtime.CompilerServices.IUnion`. But [§ Where unions are not used](../../whitepaper.md#where-unions-are-not-used) reads as if union tokens are an alternative to `IKinded` tokens, not an addition.

**Why it might matter:** Union tokens are meant to be the ergonomic option. Requiring a parallel kind enum doubles the work and lets the enum and the cases drift apart. Analysis also cannot relate a case type to a kind, so a choice mixing `Kind(k)` and `Case<T>()` is treated as possibly overlapping.

**Touches:** [§ Where unions are not used](../../whitepaper.md#where-unions-are-not-used), [§ Authoring ergonomics](../../whitepaper.md#authoring-ergonomics), [CORE-03](../active/02-core-combinators/CORE-03-primitive-nodes.md), [ANA-01](../active/09-static-analysis/ANA-01-grammar-property-analysis.md), CORE-05's `Case<A>(parser, func)`.

**First question:** Where should union-token grammars live (`TokenGrammar`, `Grammar<TIn>` with `TIn : IUnion`, or a separate base class), and how should analysis treat case types: as their own kind space, or mapped to kinds? Is the cost of the current design (an extra enum) worth a new base class?
