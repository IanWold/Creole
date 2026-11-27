# Layers

A layer turns a stream of one item type into a stream of another, such as characters into tokens, or tokens into tokens with INDENT and DEDENT added. A `Layer<TIn, TOut>` is data, as a parser is: building one runs nothing, and one layer can be shared between threads and used in any number of chains. The design is in the [whitepaper's § Layers, streams and pipelines](../../whitepaper.md#layers-streams-and-pipelines).

To run layers, end them in a parser and parse with the [pipeline](pipelines.md) that makes: `Pipeline.From(lexer).Then(parser).Parse(text)`.

## Making layers

| Method | Produces |
| --- | --- |
| `Layer.Tokens(item)` | The value of `item`, run repeatedly to the end of the input |
| `Layer.Tokens(item, skip)` | The same, running `skip` before each item, such as whitespace between tokens |
| `Layer.Tokens(initialState, item, skip)` | The same, with a [state](layer-state.md) that starts as `initialState`, such as how deeply braces nest |
| `Layer.Modal(channel, selector)`, `Layer.Modal(channel, selector, skip)` and `Layer.Modal(initialState, channel, selector, skip)` | As `Layer.Tokens`, with each step's item parser chosen by a [mode channel](mode-channels.md)'s current mode, which the parser sets |
| `Layer.Transduce(initialState, step, atEnd)` | The outputs of `step`, run repeatedly to the end of the input, then those of `atEnd`: none, one or several per step, such as a layout pass adding INDENT and DEDENT |
| `Layer.Where<T>(predicate)` | The items for which `predicate` is true |
| `Layer.Select<TIn, TOut>(selector)` | `selector` applied to each item |
| `Layer.Identity<T>()` | Its input, unchanged |
| `Layer.Utf8()` | The characters that UTF-8 bytes encode, so layers over characters can read a byte source |

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
- The layer's [state](layer-state.md) is the one thing a step hands to the next. To give a lexer a state to start with, pass it first: `Layer.Tokens(initialState, item, skip)`. That form always takes `skip`; pass `Return(new Unit())` to skip nothing.

If `item` or `skip` fails, the parse ends with an error from this layer. The error is the furthest failure the layer's steps recorded, from all its steps together, merged as [Errors § What a failure reports](errors.md#what-a-failure-reports) describes. So `skip` stopping where `item` then failed adds its expectation:

```text
error[lexer] at 3..4: unexpected '?'; expected whitespace, letter, digit, "<", "<=" or "<<"
```

The error's `Layer` is `lexer` unless the layer is given a name: `Layer.Tokens(item, skip, name: "words")`. A step that goes deeper than `ParseOptions.MaxDepth`, or would hold more of its input than `ParseOptions.MaxBufferedItems`, ends the parse with that limit's error, also named for the layer ([The buffer limit](#the-buffer-limit)).

## `Layer.Modal`

`Layer.Modal` is a lexer whose parser tells it how to read the next token, such as whether `/` starts a regex or divides. It runs as `Layer.Tokens` does, except that each step reads the current mode of a [mode channel](mode-channels.md) and runs that mode's item parser:

```csharp
public static readonly Layer<char, Lexeme> Lexer = Layer.Modal(Modes, static mode => mode switch
{
    LexMode.Operand => _name.Or(_regex),
    _ => _name.Or(_slash),
}, Whitespace.SkipMany());
```

- The layer keeps the item plans of the 64 modes used most recently, and runs the selector only for a mode whose plan it does not keep. So a lexer with at most 64 modes runs the selector once for each, for the life of the layer, and building parsers inside it is safe: each mode's item parser is planned once. A selector that returns null throws `GrammarException` when the parse first reaches that mode.
- A mode can carry data, which the selector sees and may build its item parser from ([Modes that carry data](mode-channels.md#modes-that-carry-data)).
- Every mode shares `skip`. In a mode where whitespace is content, skip whitespace only and read it back in the parser with [`LeadingTrivia`](spans.md).
- A lexer with a [state](layer-state.md) passes it first, `Layer.Modal(initialState, channel, selector, skip)`, as `Layer.Tokens` does.
- When the parser changes the mode after it has looked at a token, the layer reads that token again in the new mode, starting the step from the state it started with before ([Tokens read ahead](mode-channels.md#tokens-read-ahead)).
- Spans, errors, the layer's name and the rule that an item must consume input are as for `Layer.Tokens`.

## `Layer.Transduce`

`Layer.Transduce` runs a step parser that returns an [`Emit<T>`](#a-steps-outputs-emitt): any number of outputs per step, each derived from the input the step consumed or synthetic. Each step stops if the input has ended, and otherwise runs `step` and produces its outputs in order. When the input ends, the layer runs `atEnd` once and produces its outputs too.

This pass turns each newline token into a synthetic `;`, passes other tokens through, and adds a synthetic `<end>`:

```csharp
Layer<string, string> semicolons = Layer.Transduce(
    initialState: 0,
    step: OneOf(Token("\n").Select<Emit<string>>(static _ => Emit.Synthetic(";")), Any.Select(static token => Emit.One(token))),
    atEnd: static _ => Emit.Synthetic("<end>")
);

Layer<char, string> lexer = statements.Then(semicolons);
```

Over `"a = 1\nb = 2\n"`, with `statements` a lexer that produces names, numbers, `=` and newlines, `lexer` produces `a = 1 ; b = 2 ; <end>`. Each `;` is zero-width where its newline was, at 5..5 and 11..11, and `<end>` is zero-width at the end, 12..12.

Each output's span follows from its kind and its place in the step:

| Output | Span |
| --- | --- |
| Derived | From the start of the first input item the step consumed to the end of the last. Several derived outputs of one step each get that whole span. |
| Synthetic, with no derived output before it in the step | Zero-width at the start of the step's first input item |
| Synthetic, after a derived output | Zero-width at the start of the first input item after the step, or at the end of the input if there is none |
| From `atEnd` | Zero-width at the end of the input. Each is synthetic, whether or not it is marked. |

So a DEDENT emitted after a NEWLINE sits at the start of the first token on the next line, not at the end of the newline. To place it, the step reads one input item past its own input. An error at the DEDENT shows on that line. Here a pass emits `[newline, Emit.Synthetic("<dedent>")]` for each newline, and a parser expects a second statement after the first line of `"a = 1\n  b = 2\n"`:

```text
error[parser]: expected statement
 --> 2:3
  |
2 |   b = 2
  |   ^ unexpected <dedent>
```

In every layer above, an output is synthetic exactly when its span is zero-width ([Source spans § Synthetic items](spans.md#synthetic-items)). A pass that emits INDENT and DEDENT from the indentation of each line keeps the open blocks in its [state](layer-state.md#a-layout-pass).

- `atEnd` is optional. It receives the layer's [state](layer-state.md), which starts as `initialState` and which `step` can read and change, as the last step left it.
- A `step` that succeeds without consuming input would never let the layer reach the end of its input, so it throws `GrammarException`. Outputs that stand for no input are synthetic outputs of a step that consumes some, or come from `atEnd`.
- If `step` fails, or reaches the depth or buffer limit, the parse ends with an error from this layer, as for `Layer.Tokens`. Its `Layer` is `layout` unless the layer is given a name: `Layer.Transduce(0, step, name: "indents")`.

## `Where` and `Select`

`Layer.Where` and `Layer.Select` apply a function to each item, with no parsing. Each item keeps its span. Each function runs once for each item, when the layer above first needs the result. An item that `Where` drops is let go as it is dropped, so a filter that keeps one item in a million holds no more of its input than one that keeps them all.

```csharp
Layer<char, string> noLessThan = lexer.Then(Layer.Where<string>(static token => token != "<"));
Layer<char, int> lengths = lexer.Then(Layer.Select<string, int>(static token => token.Length));
```

Neither can fail on its own input. The only error either can end a parse with is the [buffer limit](#the-buffer-limit)'s, named `where` or `select`.

## Decoding bytes: `Layer.Utf8`

`Layer.Utf8()` is a `Layer<byte, char>` that decodes UTF-8. Chain a lexer over characters after it to read bytes, and run it with a pipeline on bytes in memory ([Bytes in memory](pipelines.md#bytes-in-memory)) or on a stream ([Streams](pipelines.md#streams)):

```csharp
Layer<byte, string> words = Layer.Utf8().Then(Layer.Tokens(Letter.AtLeastOnce().Text(), skip: Whitespace.SkipMany()));
```

Each step decodes one Unicode scalar value into one character, or into both halves of a surrogate pair. A byte-order mark (EF BB BF) at the start of the input is skipped, so the characters are the ones `StreamReader` would read. Its bytes are in the [leading trivia](spans.md#trivia) of the first item above, which is three bytes wide and zero-width in characters. A byte-order mark anywhere else is the character U+FEFF.

Each character's span gives two units ([Source spans](spans.md#spans-of-a-layers-items)):

- `Start` and `End` cover the bytes it was decoded from.
- `CharStart` and `CharEnd` are its offsets in the decoded text, counted from 0 after any byte-order mark. Both halves of a surrogate pair get the pair's whole span.

Layers above keep both units. Over `héllo wörld` encoded after a byte-order mark, `words` produces `héllo` at bytes 3..9 and characters 0..5, and `wörld` at bytes 10..16 and characters 6..11.

Bytes that are not valid UTF-8 end the parse with an error from the layer named `decoder`. Its span covers the bytes a decoder would replace with one U+FFFD, such as a byte that cannot start a sequence, or the start of a sequence that a later byte or the end of the input cuts short. In the decoded text the span is zero-width, where those bytes stand. Over the bytes `61 0A 62 FF`:

```text
error[decoder] at 3..3: invalid UTF-8 byte FF
```

The one-line form shows character offsets. The error's `Span.Start` and `Span.End` hold the byte offsets, 3..4. The decoder does not replace invalid bytes and carry on.

## Chaining layers: `Then`

`a.Then(b)` runs `b` over the items `a` produces. The items of `a` are held in a buffer between the two, so `b` can rewind over them without `a` running again. The buffer lets an item go once `b` can no longer go back to it. `a` in turn keeps its own input only from where the step began that produced the oldest item `b` can still go back to, or from the start of the step it is running if there is none. So memory follows how far `b` can go back, not the length of the input, and input that produces no item, such as a run that `Where` drops or `Transduce` steps that emit nothing, is let go as it is read ([ADR-0040](../../work/docs/decisions/0040-a-producing-layer-keeps-input-only-for-outputs-its-reader-can-go-back-to.md)).

- Chaining is associative: `a.Then(b).Then(c)` and `a.Then(b.Then(c))` produce the same items with the same spans.
- `Layer.Identity<T>()` is its unit: `a.Then(Layer.Identity<T>())` and `Layer.Identity<T>().Then(a)` return `a` itself.
- A chain has one `Layer.Modal` for each [mode channel](mode-channels.md#who-reads-and-writes-a-channel). `Then` throws `GrammarException` if `a` and `b` both read the same channel.

## The buffer limit

[`ParseOptions.MaxBufferedItems`](parsing.md#options), 16,777,216 by default, bounds each buffer on its own. Each time a layer pulls an item it does not have, it counts the items of the buffer it reads, from the oldest one it may still need to the last one produced. It may still need an item it can go back to, the input of the step it is running, and the input behind every item the layer above can still go back to. A layer that would hold more ends the parse with one error, named for that layer, which no combinator catches ([ADR-0039](../../work/docs/decisions/0039-each-layers-buffer-is-bounded-from-its-horizon-and-exceeding-it-ends-the-parse.md)). Its span covers that oldest item, and its message names the limit. A lexer whose string literal never closes, over `x"aaa…` with a limit of 1,000:

```text
error[lexer] at 1..2: buffered input from here exceeds ParseOptions.MaxBufferedItems of 1000
```

A lower layer keeps the input behind every item the layer above can still go back to, so it usually holds more than that layer and reaches the limit first. A `Lookahead` in the parser over a long run of tokens ends the parse with an error from the lexer, at the start of the step that produced the `Lookahead`'s first token. Over bytes, the decoder holds more still, so the error comes from `decoder`. Its span covers the first byte of the character the layer above can go back to, and is zero-width in characters where that character starts.

## A step's outputs: `Emit<T>`

A `Layer.Transduce` step returns its outputs as an `Emit<T>`: an ordered list of outputs, each either derived from the input the step consumed or synthetic.

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
