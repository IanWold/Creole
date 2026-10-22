# Sequencing and mapping

Most grammar code runs parsers one after another and turns their results into a value. `Seq` runs parsers in order, and `Select` combines what they produced. This page also covers `Optional` and text capture. The design is in the [whitepaper's § Authoring ergonomics](../../whitepaper.md#authoring-ergonomics) and [§ Small API consequences](../../whitepaper.md#small-api-consequences).

> The interpreter runs these combinators as this page describes. The public method that runs a parser on your input arrives with INT-08.

## `Seq` and `Select`

`Seq` takes two to eight parsers and produces their results as a value tuple. It is a static member of the grammar base class, so a grammar calls it unqualified. Follow it with `Select` and a lambda with one parameter per parser:

```csharp
Seq(key, colon, value).Select(static (k, _, v) => KeyValuePair.Create(k, v))
```

`Seq(...).Select(...)` is a single sequence node: `Select` replaces the tuple with your lambda instead of adding a node on top. Analysis and export see the parsers in order, whatever their number.

A failure after an earlier parser in the sequence consumed input is committed, so an enclosing choice does not try its next alternative.

### Choosing the result type

The one-parameter `Select` is an instance method of `Parser<TIn, TOut>`, so you can name just the result type and let a union's implicit conversion apply:

```csharp
number.Select<JsonValue>(static n => new JsonNumber(n))
```

The many-parameter `Select` on a tuple is an extension member in the `Sequencing` class, and its result type is always inferred. `Seq(...).Select<T>((a, b) => ...)` does not compile (CS1593), because `Select<T>` binds to the one-parameter method. Cast inside the lambda instead:

```csharp
Seq(ifKeyword, condition, body).Select(static (_, c, b) => (Stmt)new If(c, b))
```

A one-parameter lambda on a `Seq` receives the whole tuple: `Seq(p, q).Select(t => t.Item1)`. That is a map over the sequence, two nodes rather than one.

## The other combinators

| Combinator | Runs | Produces |
| --- | --- | --- |
| `p.Select(f)` | `p` | `f` applied to `p`'s result |
| `p.As(value)` | `p` | `value` |
| `p.Ignore()` | `p` | `Unit` |
| `p.Before(q)` | `p`, then `q` | `p`'s result |
| `p.Then(q)` | `p`, then `q` | `q`'s result |
| `p.Between(open, close)` | `open`, `p`, then `close` | `p`'s result |

`Before`, `Then` and `Between` build the same sequence node as `Seq`, so analysis treats them alike. `Select`, `As` and `Ignore` build a map node over `p`.

## `Optional`

`p.Optional()` runs `p` if it can and produces a `Maybe<TOut>`. If `p` fails without consuming input, `Optional` succeeds without consuming input and produces `Nothing`. If `p` fails after consuming input, `Optional` fails too.

`Maybe<T>` is a C# 15 union of `T` and `Nothing`, so a switch must handle both cases, and leaving one out is a compile error (CS8509):

```csharp
Seq(sign.Optional(), digits).Select(static (s, d) => s switch
{
    char c => c == '-' ? -d : d,
    Nothing => d,
})
```

It converts from either case (`Maybe<int> m = 3;`, `Maybe<int> m = new Nothing();`), and `default(Maybe<T>)` is `Nothing`. It is a struct that stores its case without boxing, so creating and matching one does not allocate. The design is in [ADR-0007](../../work/docs/decisions/0007-maybe-as-a-non-boxing-union.md).

Match on the union to find out which case you have. Where you only need a fallback value, `GetValueOrDefault(fallback)` is shorter. `Value` holds the case as a boxed `object?`; the union pattern requires it, but a switch does not use it and does not box. The `TryGetValue` methods are how the compiler matches each case, and are hidden from IntelliSense.

Two maybes are equal if both are `Nothing` or both hold equal values. When `T` is nullable, such as `Maybe<string?>`, or a type parameter in generic code, the compiler also asks for a `null` arm (CS8655). Add one to satisfy it; a maybe that holds null still matches the `T` arm.

## Text capture

At character layers, `p.Slice()` produces the characters `p` consumed as a `ReadOnlyMemory<char>` without copying them, and `p.Text()` produces them as a new `string`. Both discard `p`'s own result. Tokens should normally carry slices. If `p` fails, both fail the same way.

A slice is a view of the input when the input is held in memory as one piece, as a string is. When the input arrives in pieces and `p` read across a boundary between them, the slice is a copy, because no single piece holds all the characters. The design is in the [whitepaper's § Text capture](../../whitepaper.md#text-capture).

`Slice` and `Text` are extension members in the `TextCapture` class, so they apply to any `Parser<char, TOut>`, not only inside a `CharGrammar`.

## Null arguments

Every parser and selector argument is checked for null, and the `GrammarException` names the expression you passed (see [Grammars as data](grammars-as-data.md)). The parser that `Optional`, `Slice`, `Text` and the many-parameter `Select` are called on is checked too. For the instance methods, a null receiver such as `later.Before(end)` throws `NullReferenceException` before Creole can check it.

## Coming from Pidgin

| Creole | Pidgin |
| --- | --- |
| `Seq(p, q).Select((a, b) => ...)` | `Map((a, b) => ..., p, q)` |
| `Seq(p, q)` | `p.Then(q, (a, b) => (a, b))` |
| `p.Select(f)` | `p.Select(f)` or `p.Map(f)` |
| `p.As(value)` | `p.ThenReturn(value)` |
| `p.Ignore()` | `p.IgnoreResult()` |
| `p.Before(q)`, `p.Then(q)`, `p.Between(open, close)` | Same names |
| `p.Optional()`, producing the union `Maybe<T>` | `p.Optional()`, producing Pidgin's `Maybe<T>` |
| `p.Slice()`, `p.Text()` | `p.Slice((span, _) => ...)`, which passes the consumed span to a function |

Creole's `Maybe<T>` is a union, so it has no `HasValue` or typed `Value`: replace `m.HasValue ? m.Value : x` with a switch over `T` and `Nothing`, or with `m.GetValueOrDefault(x)`. It has none of the LINQ-style members of Pidgin's (`Select`, `Where`, `Match` and others), and no `Maybe.Just` or `Maybe.Nothing` factories.
