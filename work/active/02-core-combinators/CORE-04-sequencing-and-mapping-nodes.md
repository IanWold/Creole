# CORE-04: Sequencing and mapping nodes

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

Sequencing and maps: the shapes that make up most grammar code.

## Scope

- `Seq` overloads up to at least 8 operands (Example B uses 5).
- `Select` and `As` as instance methods, allowing an explicit result type so union conversions apply.
- `Before`, `Then`, `Between`, `Ignore`, `Optional`. `Optional` produces `Maybe<T>`, a hand-written non-boxing union of `T` and `Nothing` ([ADR-0007](../../docs/decisions/0007-maybe-as-a-non-boxing-union.md)).
- `Slice()` and `Text()` capture nodes.
- Each public combinator null-checks its parser arguments by taking its own `[CallerArgumentExpression]` parameter per parser argument and passing it to `Guard.NotNull`, so the `GrammarException` names the caller's expression (see `Grammar.Deterministic`, CORE-02). A `params` array cannot capture each element's expression, so the `Seq` overloads take fixed-arity overloads, each with one expression parameter per operand, rather than `params`.
- A combinator whose result type is built from `TOut`, such as `Optional`, is an extension member in a static class, not an instance method of `Parser<TIn, TOut>`. As an instance method it makes Native AOT expand the parser type without end (IL3054), as CORE-07 found with `Many`.

## Acceptance criteria

- [ ] `Seq(p, q, r).Select((a, b, c) => ...)` builds a single analyzable sequence node.

## Blocked by

- [CORE-02 Grammar base classes and authoring surface](../02-core-combinators/CORE-02-grammar-base-classes-and-authoring-surface.md)

## Unblocks

- [FND-09 Move to the .NET 11 GA SDK](../01-foundations/FND-09-move-to-the-net-11-ga-sdk.md)
- [CORE-10 Bind and LINQ query syntax](../02-core-combinators/CORE-10-bind-and-linq-query-syntax.md)
- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [TOOL-05 EBNF and railroad diagram export](../12-grammar-author-tooling/TOOL-05-ebnf-and-railroad-diagram-export.md)

## Whitepaper references

- [§ Types](../../../whitepaper.md#types)
- [§ Small API consequences](../../../whitepaper.md#small-api-consequences)
- [§ Text capture](../../../whitepaper.md#text-capture)

## Artifacts

- Docs: [Sequencing and mapping](../../../docs/concepts/sequencing.md)
- Decision: [ADR-0007 `Maybe<T>` is a hand-written non-boxing union](../../docs/decisions/0007-maybe-as-a-non-boxing-union.md)

## Progress notes

- 2026-10-19 (Build): Started. Plan agreed. `SequenceNode<TIn, T1, …, Tn, TOut>` for 2 to 8 operands holds typed operands and a `Func<T1, …, Tn, TOut>` selector; all arities share `NodeKind.Sequence`. `Seq(...)` produces a value tuple through a cached selector. N-ary `Select` is an extension member in `Sequencing` on `Parser<TIn, (T1, …, Tn)>` and replaces the tuple selector, so `Seq(...).Select(...)` is one node; it cannot take an explicit result type (CS1593, measured on RC 2), so union targets cast in the lambda. Unary `Select`, `As`, `Ignore` (a `Map` node) and `Before`, `Then`, `Between` (sequence nodes) are instance methods. `Optional` returns `Option<T>`, a new struct in Pidgin's `Maybe<T>` shape. `Slice` and `Text` are separate nodes on `Parser<char, TOut>`. Next: increment 1, sequencing and mapping; increment 2, `Optional`, `Slice`, `Text`.
- 2026-10-19 (Build): Increment 1 done: `SequenceNode` (2 to 8 operands, one file) and `MapNode`, with `NodeKind` members `Sequence` and `Map`; `Seq` in `GrammarOfTIn.Sequence.cs`; N-ary `Select` in `Sequencing.cs`; `Select`, `As`, `Ignore` in `Parser.Mapping.cs`; `Before`, `Then`, `Between` in `Parser.Sequencing.cs` (`Parser` is now `partial`). Tests in `SequencingNodeTests.cs` pin the criterion at every arity; turning off the `Select` fusion fails exactly those two tests. `NodeModelTests` now allows one sequence node type per arity. `dotnet build` with 0 warnings; `dotnet test` with 275 passed; the AOT smoke publish, which now uses every new combinator, with 0 IL warnings. Pidgin names on the docs page checked against Pidgin 3.5.1 by reflection. The criterion appears met. Next: increment 2, `Optional` with `Option<T>`, `Slice` and `Text`.
- 2026-10-19 (Build): Increment 2 done: `OptionalNode`, `SliceNode` and `TextNode`, with `NodeKind` members `Optional`, `Slice` and `Text`. `Optional` is an extension member in `Maybe.cs`. It returns `Maybe<T>`, not the planned `Option<T>`: CA1716 rejects `Option` as a Visual Basic keyword, and the maintainer chose Pidgin's name. `Maybe<T>` has `HasValue`, `Value`, `GetValueOrDefault`, `TryGetValue`, equality, and the `Maybe.Just` and `Maybe.Nothing` factories, but none of Pidgin's LINQ-style members. `Slice` and `Text` are extension members in `TextCapture` on `Parser<char, TOut>`. All three guard their receiver. Tests in `MaybeTests.cs` and `TextCaptureNodeTests.cs`; the docs page now covers `Optional` and text capture, with Pidgin names checked against Pidgin 3.5.1 by reflection. `dotnet build` with 0 warnings; `dotnet test` with 285 passed; the AOT smoke publish with 0 IL warnings. Every scoped item is built and the criterion appears met. Follow-ups: `Repetition`'s extension members do not guard their receiver; re-check the CS1593 limit on N-ary `Select<T>` on the GA compiler. Next: `/complete`.
- 2026-10-19 (Designing): Back to Designing: the maintainer wants `Maybe<T>` to be a C# 15 union and accepts breaking Pidgin compatibility. RC 2 spike (scratchpad, not checked in), bytes per create and match of a `Maybe<int>`: declared `union Maybe<T>(T, Nothing)` 24 (boxes) and `default` throws; hand-written `[Union]` struct 0, `default` matches `Nothing`, a missing arm gives CS8509. A `[Union]` struct must have a public `object? Value` (CS9386), and giving `HasValue` Pidgin's meaning makes `Nothing` read as null (`m is null` true). A `readonly record struct` works as a `[Union]` struct, with implicit conversions and `default == Nothing`. Generic `switch { T x => …, Nothing => … }` warns CS8655. Drafted ADR-0007 (Proposed): a hand-written union with cases `T` and `Nothing`, no `HasValue`, no factories. Next: the maintainer accepts, rejects or revises ADR-0007; on acceptance, update the whitepaper and rebuild `Maybe<T>`.
- 2026-10-19 (Building): ADR-0007 accepted by the maintainer (option C). Whitepaper § Using C# 15 unions and § Where unions are not used updated; FND-09 and REL-03 scopes updated. Next: rebuild `Maybe<T>` as the `[Union]` record struct with a public `Nothing` case, drop `HasValue`, the typed `Value` and the factories, and update `MaybeTests`, the docs page and the AOT smoke app.
- 2026-10-19 (Build): Increment 3 done: `Maybe<T>` rebuilt per ADR-0007 as a `[Union] readonly record struct` with cases `T` and `Nothing` (new `Nothing.cs`); `HasValue`, the typed `Value` and the `Maybe.Just`/`Maybe.Nothing` factories removed. `Nothing` suppresses CA1716 (a Visual Basic keyword), by the maintainer's choice, to keep the ADR's name. Found while building: `maybe.TryGetValue(out var v)` is ambiguous (CS0121) between the two overloads, so callers write the type; documented on the docs page. `MaybeTests` pins conversions, `default` as `Nothing`, equality, CS8509 for a missing `Nothing` arm, and zero allocation per create and match (adding one boxing `Value` read fails it). The docs sample compiles against the built library. `dotnet build` with 0 warnings; `dotnet test` with 288 passed; the AOT smoke publish, which now matches a `Maybe<int>`, with 0 IL warnings. Every scoped item is built and the criterion appears met. Next: `/complete`.
- 2026-10-19 (Build): By the maintainer's direction, callers match on `Maybe<T>` rather than call `TryGetValue`. RC 2: an `internal` `TryGetValue` still matches but falls back to the boxing `Value` (24 bytes per match), so both overloads stay public with `[EditorBrowsable(Never)]`, and the docs and XML docs steer to a switch or `GetValueOrDefault`. This also removes the CS0121 `out var` problem from the documented API. A switch over `Maybe<string?>` needs a `null` arm (CS8655), and a maybe holding null matches the `string` arm, pinned in `MaybeTests`. `dotnet build` with 0 warnings; `dotnet test` with 288 passed. Next: `/complete`.
