# ADR-0007: `Maybe<T>` is a hand-written non-boxing union of `T` and `Nothing`

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-19 |
| Cards | [CORE-04](../../active/02-core-combinators/CORE-04-sequencing-and-mapping-nodes.md), [FND-09](../../active/01-foundations/FND-09-move-to-the-net-11-ga-sdk.md), [REL-03](../../active/14-release/REL-03-migration-guides.md) |
| Whitepaper | [§ Using C# 15 unions](../../../whitepaper.md#using-c-15-unions), [§ Where unions are not used](../../../whitepaper.md#where-unions-are-not-used) |
| Research | CORE-04's RC 2 spike, recorded in its progress notes (2026-10-19). [ADR-0002](0002-parseresult-as-a-plain-union.md) and FND-07's spike for the earlier union findings. |
| Supersedes | — |

## Context

`p.Optional()` produces a value that is either the parser's result or nothing. CORE-04 built it as `Maybe<T>`, a plain `readonly record struct` in the shape of Pidgin's `Maybe<T>`: `HasValue`, a typed `Value`, `GetValueOrDefault`, `TryGetValue`, and `Maybe.Just` and `Maybe.Nothing` factories.

The whitepaper says Creole "uses C# 15 unions wherever a value is one of a fixed set of shapes, so the compiler checks that every consumer handles every case" (§ Using C# 15 unions). An optional result has exactly two shapes. The maintainer has said that breaking Pidgin compatibility is acceptable, and that relying on C# 15 unions is central to the library.

The forces:

- **Exhaustiveness.** A consumer should not be able to forget the empty case.
- **No allocation.** `Optional` produces a `Maybe<T>` on every match, so it is a hot-path value. § Using C# 15 unions keeps boxing unions out of the parse loop.
- **`default` must be safe.** An engine or a user can produce `default(Maybe<T>)`; it must mean "nothing", not a hidden third state. ADR-0002 found that a union's `default` throws `SwitchExpressionException`, and that a hand-written union whose empty state is a null `Value` hides that state from exhaustiveness.
- **Native AOT.** No reflection; the union pattern is compile-time only.

CORE-04's spike measured four shapes on SDK 11.0.100-rc.2.26504.105 (scratchpad, not checked in). Allocation is bytes per create and match of a `Maybe<int>`, measured with `GC.GetAllocatedBytesForCurrentThread` over 1,000 iterations:

| Shape | Allocation | `default` in a switch | `Nothing` is null to the language | Missing `Nothing` arm |
| --- | --- | --- | --- | --- |
| Declared `union Maybe<T>(T, Nothing)` | 24 bytes (boxes `int`) | `SwitchExpressionException` | No | CS8509 |
| Hand-written `[Union]` struct, `HasValue` always true | 0 | Matches `Nothing` | No | CS8509 |
| Hand-written, `HasValue` false for `Nothing` (Pidgin's meaning) | 0 | Matches `Nothing` | **Yes**: `m is null` is true | CS8509 |
| Hand-written, no `HasValue` | 0 | Matches `Nothing` | No | CS8509 |

Other spike results:

- The compiler requires a public `object? Value` property on a `[Union]` struct (CS9386 when it is missing or not public). So a union `Maybe<T>` cannot have Pidgin's typed `T Value`.
- The union pattern's `HasValue` means "the union is non-null". Giving it Pidgin's meaning ("is Just") makes `Nothing` read as null.
- A `readonly record struct` can be a `[Union]` struct. Implicit conversions from `T` and from `Nothing` work, record equality makes `default` equal to `Nothing`, and `switch { int x => …, Nothing => … }` needs no other arm.
- In generic code, `m switch { T x => …, Nothing => … }` warns CS8655 (null not covered), because an unconstrained `T` may be null. Concrete types such as `Maybe<int>` or `Maybe<string>` do not warn.

## Options

### A. Keep the plain struct (do nothing)

CORE-04's current `Maybe<T>`.

- Pros: Pidgin's shape, so Pidgin code ports unchanged. No reliance on a preview feature.
- Cons: no exhaustiveness. `m.Value` on an empty `Maybe` compiles and throws at run time. It goes against the whitepaper's rule that fixed-shape values are unions.

### B. Declared `union Maybe<T>(T, Nothing)`

- Pros: the shortest declaration, and exhaustive.
- Cons: boxes a value-type `T` on every match (24 bytes for `int`, measured), which § Using C# 15 unions rules out for hot-path values. `default` throws `SwitchExpressionException`.

### C. Hand-written `[Union]` struct with cases `T` and `Nothing`, and no `HasValue`

A `readonly record struct Maybe<T> : IUnion` marked `[Union]`, with a public empty `readonly struct Nothing` as the second case.

- Pros: exhaustive (CS8509 on a missing arm), 0 bytes allocated, and `default` is `Nothing`, so there is no hidden state. It follows the whitepaper's rule, and it is the same pattern the whitepaper uses for `Emit<T>`.
- Cons:
  - Breaks Pidgin compatibility: there is no `HasValue`, and `Value` is `object?`. Pidgin code such as `m.HasValue ? m.Value : x` stops compiling. It fails at build time, not silently.
  - Depends on C# 15's union member-provider pattern, which is preview until GA.
  - Adds a public `Nothing` type.
  - Generic code that switches on `T x` gets CS8655 and needs a `null` arm or `TryGetValue`.

### D. Hand-written `[Union]` struct that keeps Pidgin's `HasValue`

Like C, but with `HasValue` false for `Nothing`.

- Pros: closer to Pidgin.
- Cons: the language treats `Nothing` as null (`m is null` is true, measured), which defeats the point of a case type. Ruled out.

`Parser<TIn, T?>` was considered and rejected during CORE-04's planning: it needs two extension classes, cannot be called with an unconstrained or nullable `T`, and cannot tell a null result from no result.

## Decision

Option C. Accepted by the maintainer on 2026-10-19.

1. **Cases.** `public readonly struct Nothing;` is the empty case. `Maybe<T>` is `[Union] public readonly record struct Maybe<T> : IUnion` with cases `T` and `Nothing`.
2. **Union members.** Constructors `Maybe(T value)` and `Maybe(Nothing nothing)`; `public object? Value`, which boxes and is meant for the compiler and for untyped consumers; `bool TryGetValue(out T value)` and `bool TryGetValue(out Nothing value)`. No `HasValue`.
3. **Kept convenience members.** `GetValueOrDefault()` and `GetValueOrDefault(T defaultValue)`. `ToString()` returns `Just(value)` or `Nothing`. Equality is record equality, so `default(Maybe<T>)` equals a `Maybe<T>` made from `Nothing`.
4. **Dropped.** `HasValue`, the typed `Value`, and the `Maybe.Just` and `Maybe.Nothing` factories. The implicit conversions replace the factories (`Maybe<int> m = 3;`, `Maybe<int> m = new Nothing();`). The `Maybe` static class remains, holding `Optional`.
5. **`default` is `Nothing`.** Engines may produce `default(Maybe<T>)` for an empty result.

Deciding reason: it is the only shape that is exhaustive, allocation-free, and safe for `default`, and it follows the whitepaper's rule that fixed-shape values are unions. The maintainer accepts the loss of Pidgin compatibility and the preview dependency.

What would change this:

- The GA compiler changes the union member-provider pattern, such as the required `Value` property or how `TryGetValue` is matched (FND-09 re-checks this).
- A measured cost from the boxing `Value` property on a hot path. Nothing in Creole calls it, but the compiler might in some patterns.

## Consequences

- **Easier:** `sign switch { char c => c, Nothing => '+' }` is checked by the compiler, and a missing arm is an error in this repository. There is no `Value` that throws.
- **Harder:**
  - Pidgin users must rewrite `HasValue` and `Value` uses; the migration guide (REL-03) must say so.
  - Generic grammar helpers that switch over `Maybe<T>` need a `null` arm or `TryGetValue`.
- **Ruled out:** the declared `union` keyword for any hot-path value whose cases include a value type, consistent with § Using C# 15 unions.
- **Precedent:** an empty state is a case type (`Nothing`), not a null `Value`. ADR-0002 recommended this for `Emit<T>`, and LAY-03 can reuse `Nothing`.
- **Risks and how they are watched:**
  - Preview pattern: FND-09 re-checks it on the GA compiler, and `MaybeTests` pins conversions, exhaustiveness, `default` and equality.
  - `Maybe<Nothing>` makes the two `TryGetValue` overloads the same signature, so it cannot be matched. Nothing in Creole produces it.

## Changes made

- **Whitepaper:**
  - § Using C# 15 unions: a table row for optional results, linking this record.
  - § Where unions are not used: the "Hot-path values" bullet names `Maybe<T>` beside `Emit<T>`.
- **Cards:**
  - [CORE-04](../../active/02-core-combinators/CORE-04-sequencing-and-mapping-nodes.md): scope names `Maybe<T>`; back to Building to rebuild it as the union, with its tests and docs.
  - [FND-09](../../active/01-foundations/FND-09-move-to-the-net-11-ga-sdk.md): scope re-checks the `[Union]` member-provider pattern `Maybe<T>` relies on.
  - [REL-03](../../active/14-release/REL-03-migration-guides.md): scope covers Pidgin's `Maybe<T>` (`HasValue` and `Value`) against Creole's.
