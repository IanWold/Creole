# Choice

Choice combinators try alternatives in order. `p.Or(q)` is an extension member of `Parser<TIn, TOut>` in the `Choice` class, and `OneOf` is a member of `Grammar<TIn>`, so a grammar class calls it unqualified. The design is in the [whitepaper's § Choice and backtracking](../../whitepaper.md#choice-and-backtracking).

> Nothing runs these combinators yet. They build grammar nodes, and the interpreter that executes them arrives with INT-03. Each method's XML documentation states the behaviour engines must implement; this page will describe it once it runs.

## Ordered choice

| Combinator | Tries | Produces |
| --- | --- | --- |
| `p.Or(q)` | `p`, then `q` | the result of whichever matched |
| `OneOf(p1, …, pn)` | `p1` to `pn` in order, for 2 to 16 alternatives | the result of the first that matched |

Every alternative produces the same type. When alternatives build different cases of a syntax tree, convert each to the common type, for example with a union.

Choice uses commit semantics: the next alternative is tried only if the one before it failed **without consuming input**. An alternative that fails after consuming input fails the whole choice. `Atomic`, which arrives with CORE-08, will let the next alternative run from the same position.

`a.Or(b).Or(c)` matches the same input as `OneOf(a, b, c)`, but the grammar tree keeps the nesting you wrote: a choice inside a choice.

For more than 16 alternatives, nest `OneOf` calls. Each overload takes its alternatives as separate parameters rather than a `params` array, so a null alternative is reported by the expression you passed (see [Grammars as data](grammars-as-data.md)). `Or` also names a null receiver, such as `Later.Or(x)` where `Later` is declared further down the class.

## Coming from Pidgin

| Creole | Pidgin |
| --- | --- |
| `p.Or(q)` | `p.Or(q)` |
| `OneOf(p1, …, pn)` | `OneOf(p1, …, pn)` |

Both libraries use commit semantics. Pidgin's `OneOf` also takes a `params` array or an `IEnumerable`, so it has no limit on the number of alternatives. Pidgin's `Try` corresponds to Creole's planned `Atomic`.
