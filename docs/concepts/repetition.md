# Repetition

Repetition combinators run a parser several times. They are extension members of `Parser<TIn, TOut>` in the `Repetition` class, so they read left to right: `p.AtLeastOnce()`, `p.SepBy(comma)`. The design is in the [whitepaper's § Repetition and structure](../../whitepaper.md#repetition-and-structure).

> Nothing runs these combinators yet. They build grammar nodes, and the interpreter that executes them arrives with INT-03. Each method's XML documentation states the behaviour engines must implement; this page will describe it once it runs.

## The combinators

| Combinator | Matches | Produces |
| --- | --- | --- |
| `p.Many()` | `p` zero or more times | `IReadOnlyList<TOut>` |
| `p.AtLeastOnce()` | `p` one or more times | `IReadOnlyList<TOut>`, never empty |
| `p.Repeat(n)` | `p` exactly `n` times | `IReadOnlyList<TOut>` with `n` items |
| `p.Repeat(min, max)` | `p` at least `min` and at most `max` times | `IReadOnlyList<TOut>` |
| `p.SepBy(sep)` | zero or more `p`, with `sep` between them | the `p` results |
| `p.SepBy1(sep)` | one or more `p`, with `sep` between them | the `p` results, never empty |
| `p.SepEndBy(sep)` | zero or more `p`, with `sep` between them and optionally after the last | the `p` results |
| `p.Until(end)` | zero or more `p`, then `end` | the `p` results; `end` is consumed and its result dropped |
| `p.SkipMany()` | the same input as `p.Many()` | `Unit`, without allocating a list |
| `p.SkipAtLeastOnce()` | the same input as `p.AtLeastOnce()` | `Unit`, without allocating a list |

The separator's and terminator's results are always discarded.

`Repeat` throws a `GrammarException` if a count is negative or `max` is less than `min`. Every repetition combinator rejects a null `p`, and `SepBy`, `SepBy1`, `SepEndBy` and `Until` also reject a null separator or terminator, the same way as every combinator (see [Grammars as data](grammars-as-data.md)).

## Coming from Pidgin

The combinators follow Pidgin's. Some names differ:

| Creole | Pidgin |
| --- | --- |
| `Many`, `AtLeastOnce`, `SkipMany`, `SkipAtLeastOnce`, `Until` | Same names |
| `Repeat(n)` | `Repeat(n)` |
| `Repeat(min, max)` | No equivalent |
| `SepBy` | `Separated` |
| `SepBy1` | `SeparatedAtLeastOnce` |
| `SepEndBy` | `SeparatedAndOptionallyTerminated` |

Pidgin's combinators return `IEnumerable<T>`. Creole's return `IReadOnlyList<T>`, which code expecting `IEnumerable<T>` still accepts.
