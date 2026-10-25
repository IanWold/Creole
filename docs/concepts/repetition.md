# Repetition

Repetition combinators run a parser several times. They are extension members of `Parser<TIn, TOut>` in the `Repetition` class, so they read left to right: `p.AtLeastOnce()`, `p.SepBy(comma)`. The design is in the [whitepaper's § Repetition and structure](../../whitepaper.md#repetition-and-structure).

> The interpreter runs these combinators as this page describes. Run a parser with `Parse` ([Running a parser](parsing.md)).

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

## How repetition stops

Each run of `p` is one attempt, and so is each separator and each terminator.

- An attempt that fails without consuming input ends the repetition. The repetition then succeeds if it has its minimum number of items, and fails otherwise. `Many` and `SkipMany` have no minimum, so they always succeed this way, possibly with nothing.
- An attempt that fails after consuming input fails the whole repetition. On `abac`, `Seq(Char('a'), Char('b')).Many()` reads `ab`, then fails after reading the second `a`.
- `Repeat(min, max)` is greedy: it stops after `max` items even if more would match.
- In `SepBy`, `SepBy1` and `SepEndBy`, a separator that fails without consuming input ends the list. A separator must be followed by an item, so `Char('a').SepBy(Char(','))` fails on `a,` after consuming the comma. `SepEndBy` accepts the trailing separator instead, so on `a,a,` it produces two items and consumes all four characters.
- `Until` tries the terminator before each item. If the terminator matches, it is consumed and the repetition succeeds. If it fails without consuming input, `p` must match, or the whole repetition fails. So `Char('a').Until(Char(';'))` fails on `aa`, which has no terminator.

### Items that consume nothing

A repetition with no maximum would loop forever on an item that succeeds without consuming input, such as `Whitespace.SkipMany().Many()`. When that happens, the parse throws a `GrammarException` instead. The same applies to a `SepBy` round in which both the separator and the item consume nothing, and to an `Until` item that consumes nothing.

`Repeat(n)` and `Repeat(min, max)` stop after their maximum, so they do not throw: `Return('x').Repeat(3)` produces three items and consumes nothing.

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

The behaviour matches too. On every row of the repetition table in the tests, Creole and Pidgin 3.5.1 give the same result, consume the same input and fail the same way. One difference: where Pidgin throws `InvalidOperationException` for an item that consumes nothing, Creole throws `GrammarException`.
