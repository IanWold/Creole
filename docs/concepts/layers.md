# Layers

A layer turns a stream of one item type into a stream of another, such as characters into tokens, or tokens into tokens with INDENT and DEDENT added. A `Layer<TIn, TOut>` is data, as a parser is: building one runs nothing, and one layer can be shared between threads and used in any number of chains. The design is in the [whitepaper's § Layers, streams and pipelines](../../whitepaper.md#layers-streams-and-pipelines).

This version builds and chains layers, but has no public way to run them yet.

## Making layers

| Method | Produces |
| --- | --- |
| `Layer.Tokens(item)` | The value of `item`, run repeatedly to the end of the input |
| `Layer.Tokens(item, skip)` | The same, running `skip` before each item, such as whitespace between tokens |
| `Layer.Where<T>(predicate)` | The items for which `predicate` is true |
| `Layer.Select<TIn, TOut>(selector)` | `selector` applied to each item |
| `Layer.Identity<T>()` | Its input, unchanged |

A layer runs in steps. It runs a step only when the layer above it needs another item, so it never reads further ahead than that. Every item carries a span in the original source, so an error at any layer points at the characters behind it ([Source spans](spans.md#spans-of-a-layers-items)).

## `Layer.Tokens`

`Layer.Tokens` is the usual lexer. Each step runs `skip`, stops if the input has ended, and otherwise runs `item` and produces its value:

```csharp
Layer<char, string> lexer = Layer.Tokens(
    Longest(Letter.AtLeastOnce().Text(), Digit.AtLeastOnce().Text(), String("<"), String("<="), String("<<")),
    skip: Whitespace.SkipMany()
);
```

Over `"  ab 12\t<=x "` this produces `ab`, `12`, `<=` and `x`, with the spans 2..4, 5..7, 8..10 and 10..11. A token's span runs from the first character `item` consumed to the last, so it never includes the whitespace before it.

- `skip` must succeed, so it usually repeats, as `SkipMany` does. A `skip` that fails ends the parse like a failed item.
- An `item` that succeeds without consuming input, such as `Digit.Many()` before a letter, would never let the layer reach the end of its input, so the step throws `GrammarException`.
- Each step runs on its own. Backtracking, cuts and the depth count inside a step stay inside it, so the layer above sees only finished items, and rewinding over them never runs the layer again.

If `item` or `skip` fails, the parse ends with an error from this layer. The error is the furthest failure the layer's steps recorded, from all its steps together, merged as [Errors § What a failure reports](errors.md#what-a-failure-reports) describes. So `skip` stopping where `item` then failed adds its expectation:

```text
error[lexer] at 3..4: unexpected '?'; expected whitespace, letter, digit, "<", "<=" or "<<"
```

The error's `Layer` is `lexer` unless the layer is given a name: `Layer.Tokens(item, skip, name: "words")`. A step that goes deeper than `ParseOptions.MaxDepth` ends the parse with the depth error, also named for the layer.

## `Where` and `Select`

`Layer.Where` and `Layer.Select` apply a function to each item, with no parsing. Each item keeps its span. Each function runs once for each item, when the layer above first needs the result.

```csharp
Layer<char, string> noLessThan = lexer.Then(Layer.Where<string>(static token => token != "<"));
Layer<char, int> lengths = lexer.Then(Layer.Select<string, int>(static token => token.Length));
```

## Chaining layers: `Then`

`a.Then(b)` runs `b` over the items `a` produces. The items of `a` are held in a buffer between the two, so `b` can rewind over them without `a` running again.

- Chaining is associative: `a.Then(b).Then(c)` and `a.Then(b.Then(c))` produce the same items with the same spans.
- `Layer.Identity<T>()` is its unit: `a.Then(Layer.Identity<T>())` and `Layer.Identity<T>().Then(a)` return `a` itself.

## A step's outputs: `Emit<T>`

A step that can produce any number of items returns them as an `Emit<T>`: an ordered list of outputs, each either derived from the input the step consumed or synthetic.

| Output | Means | Its span |
| --- | --- | --- |
| Derived | An item made from the input the step consumed | Covers that input |
| Synthetic | An item that stands for no input, such as INDENT, DEDENT or an inserted `;` | Zero-width |

Build an emit from its outputs:

```csharp
Emit<Tok> none = [];                                  // or default
Emit<Tok> one = token;                                // an item converts to one derived output
Emit<Tok> marked = Emit.Synthetic(indent);            // one synthetic output
Emit<Tok> several = [newline, Emit.Synthetic(indent)];
Emit<Tok> spread = [newline, .. dedents];             // spreads work, and keep each output's mark
```

- An item converts to an emit only when its type is not an interface or `object`, because C# applies no user-defined conversion from those. Use `Emit.One(item)` for them, or where a lambda must return an emit: `Any.Select(static t => Emit.One(t))`.
- `Emit.Synthetic(item)` returns an `Emitted<T>`, the type of one output, so it can sit in a collection expression beside plain items.

Read an emit with `Count` and `foreach`, which yields each output as an `Emitted<T>` with `Item` and `IsSynthetic`:

```csharp
foreach (var output in emit)
{
    Console.WriteLine($"{output.Item} {(output.IsSynthetic ? "synthetic" : "derived")}");
}
```

- An emit is not a union. Its outputs are one list, so there are no cases to switch on, and `default` is simply an empty list. [ADR-0032](../../work/docs/decisions/0032-emit-is-a-list-of-outputs-each-derived-or-synthetic.md) records why.
- An emit of zero or one output holds it inline, so building and reading one allocates nothing. Several outputs are held in one array.
- Two emits are equal when they hold equal outputs in the same order, however they were built: `Emit.One('a')`, `'a'` and `['a']` are equal. `ToString()` lists the outputs and marks synthetic ones: `[a, b (synthetic)]`.
