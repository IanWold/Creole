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
- `Before`, `Then`, `Between`, `Ignore`, `Optional`.
- `Slice()` and `Text()` capture nodes.
- Each public combinator null-checks its parser arguments by taking its own `[CallerArgumentExpression]` parameter per parser argument and passing it to `Guard.NotNull`, so the `GrammarException` names the caller's expression (see `Grammar.Deterministic`, CORE-02). A `params` array cannot capture each element's expression, so the `Seq` overloads take fixed-arity overloads, each with one expression parameter per operand, rather than `params`.
- A combinator whose result type is built from `TOut`, such as `Optional`, is an extension member in a static class, not an instance method of `Parser<TIn, TOut>`. As an instance method it makes Native AOT expand the parser type without end (IL3054), as CORE-07 found with `Many`.

## Acceptance criteria

- [ ] `Seq(p, q, r).Select((a, b, c) => ...)` builds a single analyzable sequence node.

## Blocked by

- [CORE-02 Grammar base classes and authoring surface](../02-core-combinators/CORE-02-grammar-base-classes-and-authoring-surface.md)

## Unblocks

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

## Progress notes

- 2026-10-19 (Build): Started. Plan agreed. `SequenceNode<TIn, T1, …, Tn, TOut>` for 2 to 8 operands holds typed operands and a `Func<T1, …, Tn, TOut>` selector; all arities share `NodeKind.Sequence`. `Seq(...)` produces a value tuple through a cached selector. N-ary `Select` is an extension member in `Sequencing` on `Parser<TIn, (T1, …, Tn)>` and replaces the tuple selector, so `Seq(...).Select(...)` is one node; it cannot take an explicit result type (CS1593, measured on RC 2), so union targets cast in the lambda. Unary `Select`, `As`, `Ignore` (a `Map` node) and `Before`, `Then`, `Between` (sequence nodes) are instance methods. `Optional` returns `Option<T>`, a new struct in Pidgin's `Maybe<T>` shape. `Slice` and `Text` are separate nodes on `Parser<char, TOut>`. Next: increment 1, sequencing and mapping; increment 2, `Optional`, `Slice`, `Text`.
- 2026-10-19 (Build): Increment 1 done: `SequenceNode` (2 to 8 operands, one file) and `MapNode`, with `NodeKind` members `Sequence` and `Map`; `Seq` in `GrammarOfTIn.Sequence.cs`; N-ary `Select` in `Sequencing.cs`; `Select`, `As`, `Ignore` in `Parser.Mapping.cs`; `Before`, `Then`, `Between` in `Parser.Sequencing.cs` (`Parser` is now `partial`). Tests in `SequencingNodeTests.cs` pin the criterion at every arity; turning off the `Select` fusion fails exactly those two tests. `NodeModelTests` now allows one sequence node type per arity. `dotnet build` with 0 warnings; `dotnet test` with 275 passed; the AOT smoke publish, which now uses every new combinator, with 0 IL warnings. Pidgin names on the docs page checked against Pidgin 3.5.1 by reflection. The criterion appears met. Next: increment 2, `Optional` with `Option<T>`, `Slice` and `Text`.
