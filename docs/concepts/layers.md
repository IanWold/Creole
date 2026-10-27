# Layers

A layer turns a stream of one item type into a stream of another, such as characters into tokens, or tokens into tokens with INDENT and DEDENT added. The design is in the [whitepaper's § Layers, streams and pipelines](../../whitepaper.md#layers-streams-and-pipelines).

## A step's outputs: `Emit<T>`

A layer runs in steps. A step that can produce any number of items returns them as an `Emit<T>`: an ordered list of outputs, each either derived from the input the step consumed or synthetic.

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
