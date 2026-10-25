# Sequencing and mapping

Most grammar code runs parsers one after another and turns their results into a value. `Seq` runs parsers in order, and `Select` combines what they produced. This page also covers `Optional`, text capture and query syntax. The design is in the [whitepaper's § Authoring ergonomics](../../whitepaper.md#authoring-ergonomics), [§ LINQ query syntax](../../whitepaper.md#linq-query-syntax) and [§ Small API consequences](../../whitepaper.md#small-api-consequences).

> The interpreter runs these combinators as this page describes. Run a parser with `Parse` ([Running a parser](parsing.md)).

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

At character layers, `p.Slice()` produces the characters `p` consumed as a `ReadOnlyMemory<char>`, and `p.Text()` produces them as a new `string`. Both discard `p`'s own result. Tokens should normally carry slices. If `p` fails, both fail the same way.

When the input is held whole in memory, as a `string` is, a slice is a view of it and copies nothing. When the input is streamed, a slice is a copy, because the buffer that held those characters is reused once the parse moves on. Either way, a slice keeps its text for as long as you hold it. The design is in the [whitepaper's § Text capture](../../whitepaper.md#text-capture) and [ADR-0013](../../work/docs/decisions/0013-slices-view-in-memory-input-and-copy-streamed-input.md).

`Slice` and `Text` are extension members in the `TextCapture` class, so they apply to any `Parser<char, TOut>`, not only inside a `CharGrammar`.

## Query syntax

C# query syntax works on parsers. Each `from` clause runs a parser and names its result, and `select` builds the value:

```csharp
static readonly Parser<JTok, KeyValuePair<string, JsonValue>> Member =
    from key   in Kind(J.String)
    from colon in Kind(J.Colon)
    from value in Value
    select KeyValuePair.Create(key.Text, value);
```

Each `from` clause after the first is classified once, when the grammar is built:

- **A clause that does not use an earlier range variable** is evaluated once, and its parser becomes the next step of a sequence. `Member` builds the same kind of node as `Seq`, so analysis sees every step.
- **A clause that uses an earlier range variable** is a bind: the parser to run depends on a parsed value, so it is chosen once per match. Analysis cannot see past a bind.

```csharp
static readonly Parser<byte, byte[]> Blob =
    from length in UInt16
    from bytes  in Any.Repeat(length)   // uses length: a bind
    select bytes.ToArray();
```

A clause that only chooses between known parsers is better written with `Switch` or `Match`, which keep every branch visible (see [Choice](choice.md)).

The clause's lambda is compiled once when the grammar is built, so parsing never touches an expression tree. Things to know:

- A `from` clause must be valid in an expression tree. A parser reference or a combinator call always is.
- A query with three or more clauses builds nested two-step sequences, not one flat `Seq`. Analysis treats them the same. At run time, C# allocates an object per extra clause per match to carry the earlier range variables, so a hot rule may prefer `Seq(...).Select(...)`.
- Under Native AOT, expressions are interpreted rather than compiled. A clause evaluated once costs nothing extra, but a bind clause runs on the expression interpreter at every match. `p.Bind(...)` takes a plain delegate and avoids this.

### `Bind`, `where` and `let`

| Combinator | Runs | Produces |
| --- | --- | --- |
| `p.Bind(f)` | `p`, then the parser `f` returns for `p`'s result | that parser's result |
| `p.Where(pred)`, `p.Where(pred, message)`, or a `where` clause | `p`, then fails if `pred` rejects its result | `p`'s result |
| a `let` clause | nothing more | a map, as `Select` builds |

`Bind` is the fluent form of a dependent clause. It always builds a bind node. The parser `f` returns is prepared on first use and reused whenever `f` returns it again, as it does for a rule field. A parser that `f` builds afresh at each match, like `Any.Repeat(length)` above, is prepared at each match. If `f` returns null, the parse throws `GrammarException` naming `f`.

If `p` consumed input before `Where`'s predicate rejects its result, the failure is committed: an enclosing choice does not try its next alternative, and the input stays consumed. Wrap it in `Atomic` to let the choice backtrack.

A rejection is reported at the start of the value it rejected, with that value underlined, not at the item after it. Failures recorded while `p` ran are dropped first, so `p`'s own expectations, such as "expected digit" where a number ends, cannot outrank the rejection. Pass a message to say what is wrong:

```csharp
static readonly Parser<char, int> Byte = Number.Where(n => n <= 255, "number out of range");
```

Without a message, the error shows the rejected input as unexpected. A `where` clause cannot take a message, so call `.Where(pred, message)` on the query instead. Under `Atomic`, `Named` labels a rejection like any other failure at its start:

```csharp
// Reports "expected identifier" at a keyword, and lets an enclosing choice try its next alternative.
static readonly Parser<char, string> Name = Atomic(Identifier.Where(name => !Keywords.Contains(name))).Named("identifier");
```

`Where` judges a value, so its error points at the value. `Bind` with `Fail(message)` fails where `p` stopped, which suits a construct that goes wrong at its end, such as an unterminated string (see [Custom scanners](custom-scanners.md#reporting-errors-from-a-scanner)). The reasons are in [ADR-0014](../../work/docs/decisions/0014-a-rejected-filter-reports-at-the-start-of-the-value.md).

## Null arguments

Every parser and selector argument is checked for null, and the `GrammarException` names the expression you passed (see [Grammars as data](grammars-as-data.md)). In a query, a clause that gives a null parser is named by its own text, such as `'Value' is null`. The parser that `Optional`, `Slice`, `Text` and the many-parameter `Select` are called on is checked too. For the instance methods, a null receiver such as `later.Before(end)` throws `NullReferenceException` before Creole can check it.

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
| `p.Bind(f)` | `p.Bind(f)` |
| `p.Where(pred)`, `p.Where(pred, message)` | `p.Where(pred)` or `p.Assert(pred)`, `p.Assert(pred, message)`. Pidgin reports a failed assertion where `p` ended; Creole reports it where `p` started. |
| Query syntax, with independent clauses analyzed as a sequence | Query syntax, where every clause is a bind |

Creole's `Maybe<T>` is a union, so it has no `HasValue` or typed `Value`: replace `m.HasValue ? m.Value : x` with a switch over `T` and `Nothing`, or with `m.GetValueOrDefault(x)`. It has none of the LINQ-style members of Pidgin's (`Select`, `Where`, `Match` and others), and no `Maybe.Just` or `Maybe.Nothing` factories.
