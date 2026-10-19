# Sequencing and mapping

Most grammar code runs parsers one after another and turns their results into a value. `Seq` runs parsers in order, and `Select` combines what they produced. The design is in the [whitepaper's § Authoring ergonomics](../../whitepaper.md#authoring-ergonomics) and [§ Small API consequences](../../whitepaper.md#small-api-consequences).

> Nothing runs these combinators yet. They build grammar nodes, and the interpreter that executes them arrives with INT-03. Each method's XML documentation states the behaviour engines must implement; this page will describe it once it runs.

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

Every parser and selector argument is checked for null, and the `GrammarException` names the expression you passed (see [Grammars as data](grammars-as-data.md)). The parser these methods are called on is checked too for the many-parameter `Select`. For the instance methods, a null receiver such as `later.Before(end)` throws `NullReferenceException` before Creole can check it.

## Coming from Pidgin

| Creole | Pidgin |
| --- | --- |
| `Seq(p, q).Select((a, b) => ...)` | `Map((a, b) => ..., p, q)` |
| `Seq(p, q)` | `p.Then(q, (a, b) => (a, b))` |
| `p.Select(f)` | `p.Select(f)` or `p.Map(f)` |
| `p.As(value)` | `p.ThenReturn(value)` |
| `p.Ignore()` | `p.IgnoreResult()` |
| `p.Before(q)`, `p.Then(q)`, `p.Between(open, close)` | Same names |
