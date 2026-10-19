# Choice

Choice combinators pick one of several parsers. `p.Or(q)` and `p.Switch(...)` are extension members of `Parser<TIn, TOut>` in the `Choice` class, and `OneOf` is a member of `Grammar<TIn>`, so a grammar class calls it unqualified. The design is in the [whitepaper's § Choice and backtracking](../../whitepaper.md#choice-and-backtracking) and [§ Selective choice](../../whitepaper.md#selective-choice).

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

## Selective choice with `Switch`

`Switch` runs a parser, the scrutinee, and uses its result to pick one of a fixed set of branches:

```csharp
// Version produces an int; RecordV1 and RecordV2 produce a Record.
public static readonly Parser<byte, Record> Record =
    Version.Switch(
        (1, RecordV1),
        (2, RecordV2),
        @default: Fail<Record>("unsupported version"));
```

The branch whose key equals the scrutinee's result runs next, from where the scrutinee stopped, and its result is the switch's result. A result that matches no key runs `@default`. Keys are compared with `EqualityComparer<TKey>.Default`. If the scrutinee or the chosen branch fails, the switch fails, and the failure is committed if any input was consumed.

Every branch is part of the grammar, so analysis sees all of them. Use `Switch` rather than a function that returns a parser whenever the branches are known in advance.

Each key must have the scrutinee's type, or C# cannot infer the branch type. For a scrutinee that produces a `byte`, write `((byte)1, RecordV1)`.

Up to eight cases can be passed as separate arguments. For more, or for a table of cases built in code, pass a collection:

```csharp
Opcode.Switch([(0x01, Nop), (0x02, Push), (0x03, Pop) /* … */], @default: Fail<Instr>("unknown opcode"));
```

Both forms build the same node. A nested `Switch` is no substitute for a long one, because it would parse the scrutinee again ([ADR-0006](../../work/docs/decisions/0006-switch-and-match-take-a-collection-beyond-eight-cases.md)). The collection is copied, so changing it afterwards does not change the grammar.

`Switch` throws a `GrammarException` when two cases have the same key, when the collection is empty, or when the scrutinee, a branch, the default or the collection is null. With separate arguments, the message names the case as you wrote it, such as `'(2, RecordV2)'`; with a collection, it names the case by index, counting from 0.

## Coming from Pidgin

| Creole | Pidgin |
| --- | --- |
| `p.Or(q)` | `p.Or(q)` |
| `OneOf(p1, …, pn)` | `OneOf(p1, …, pn)` |

Both libraries use commit semantics. Pidgin's `OneOf` also takes a `params` array or an `IEnumerable`, so it has no limit on the number of alternatives. Pidgin's `Try` corresponds to Creole's planned `Atomic`.
