# Source spans

Every item Creole produces, at every layer, has a span: the range of the original source it came from. Errors use spans to point at the characters that caused them. The design reasons are in the [whitepaper's § Source spans and provenance](../../whitepaper.md#source-spans-and-provenance).

## `SourceSpan`

A `SourceSpan` is a source id and a half-open range of offsets: `Start` is the first unit in the span, and `End` is just past the last.

```csharp
var span = new SourceSpan(file, start: 12, end: 16);   // units 12, 13, 14 and 15
```

- Offsets are `long`, because a streamed source can be longer than 2 GB.
- `Start` and `End` are in the root layer's units: characters for text, bytes for a byte source.
- `CharStart` and `CharEnd` are offsets in the decoded text. For a text source they equal `Start` and `End`. For a byte source, use the five-argument constructor to give both pairs.
- A zero-width span (`Start == End`) marks a position, such as where a missing item was expected. An item whose span is zero-width is [synthetic](#synthetic-items).
- The constructor throws `ArgumentOutOfRangeException` if a start offset is negative or an end offset is before its start.
- Spans are values. Two spans are equal when their source and all four offsets are equal.

## `SourceId` and `SourceRegistry`

A `SourceId` says which source a span points into. A `SourceRegistry` issues ids and records each source's name, such as a file path, for diagnostics to show.

```csharp
var sources = new SourceRegistry();
SourceId config = sources.Add("config.json");

sources.GetName(config);              // "config.json"
sources.GetName(SourceId.Unnamed);    // null
```

- `SourceId.Unnamed`, which is `default(SourceId)`, is the source of a parse given no name.
- Adding the same name twice gives two different ids.
- An id means something only in the registry that issued it. Ids from different registries can compare equal, and `GetName` throws `ArgumentException` for an id it did not issue.
- A registry is safe to use from several threads.

## Spans of a layer's items

Each [layer](layers.md) gives every item it produces a span in the original source, kept beside the item rather than inside it, so token types need no position fields.

- An item from `Layer.Tokens` spans from the start of the first input item its parser consumed to the end of the last. Input that `skip` consumed is left out, so a token's span never includes the whitespace before it.
- An output of a `Layer.Transduce` step that is derived spans the input the step consumed. A synthetic one is zero-width at the start of the next input item ([Layers § `Layer.Transduce`](layers.md#layertransduce)).
- An item from `Layer.Where` or `Layer.Select` keeps the span of the input item it came from.
- A character from `Layer.Utf8` spans the bytes it was decoded from, and its `CharStart` and `CharEnd` give its place in the decoded text ([Layers § Decoding bytes](layers.md#decoding-bytes-layerutf8)). Layers above it keep both units, so their items' `Start` and `End` are byte offsets, and `CharStart` and `CharEnd` are character offsets.

### Synthetic items

An item is synthetic exactly when its span is zero-width (`Length == 0`): it covers no source text. INDENT, DEDENT and the other synthetic outputs of a `Layer.Transduce` step are synthetic in every layer above. Nothing else records it: `Emitted<T>.IsSynthetic` tells the layer where to place an output's span, and is not kept with the item ([ADR-0034](../../work/docs/decisions/0034-a-synthetic-item-is-one-whose-span-is-zero-width.md)).

- `Layer.Where` and `Layer.Select` keep each item's span, so a synthetic item stays synthetic.
- A token built only from synthetic items at one position is zero-width, so it is synthetic. So is a derived output of a step that consumed only such items, whatever its mark.
- A token built from synthetic items at two positions spans the text between them, so it is not synthetic. Over `"a\n\nb"`, a pass that turns each newline into a synthetic `;` puts them at 1..1 and 2..2, and a token made of both spans 1..2.

## Spans in grammars

A grammar can read spans as it parses, to give syntax nodes their location.

| Combinator | Produces |
| --- | --- |
| `p.Spanned()` | A `Spanned<T>`: `p`'s result as `Value`, and the span of the input `p` consumed as `Span` |
| `p.WithSpan((value, span) => …)` | What the function builds from `p`'s result and that span |
| `Position` | A zero-width `SourceSpan` where the next item starts |
| `LeadingTrivia` | The `SourceSpan` of the source before the next item that no item covers ([below](#trivia)) |
| `Adjacent(p, q)` | `p`'s and `q`'s results as a tuple, if `q` starts exactly where `p` ended ([below](#joining-split-tokens)) |

```csharp
record Name(string Text, SourceSpan Span);

class Names : CharGrammar
{
    public static readonly Parser<char, Name> Identifier =
        Letter.AtLeastOnce().Text().WithSpan((text, span) => new Name(text, span));
}
```

- A span runs from the start of the first item `p` consumed to the end of the last, in the original source. Over a layer's items it leaves out input the layer skipped, so a parser over the tokens of `  ab cd` gives `ab cd` the span 2..7.
- If `p` consumes nothing, its span is zero-width where `Position` would be.
- If `p` fails, `Spanned` and `WithSpan` fail the same way.
- `Position` lies after any input the layer skipped. At the end of the input it is the end of the last item, and in an empty input it is offset 0. It reads the next item to find where that item starts.
- Over input held whole in memory, such as a `string` or an array of tokens passed to `Parse`, offsets are item positions: character offsets for text, and token indexes for tokens.
- `WithSpan`'s function must be pure, as `Select`'s must, because an engine may run it more than once ([whitepaper § Functions in grammars](../../whitepaper.md#functions-in-grammars)).
- Span combinators keep no input alive. Over streamed input, if the buffer releases the item where a span starts while the parser is still running, it first copies that item's span.

### Joining split tokens

`Adjacent(p, q)` runs `p` and then `q`, as `Seq` does, and succeeds only if `q` starts exactly where `p` ended. It lets the lexer split what the parser may join. A lexer that reads `>>` as one token cannot close two type arguments in `List<List<int>>`. One that emits each `>` as its own token can, and the parser joins two of them where it reads a shift:

```csharp
public static readonly Parser<Tok, string> Expr = Expression.Build(Name, static op =>
[
    [op.InfixL(Atomic(Adjacent(Kind(Symbol.Gt), Kind(Symbol.Gt))), static (left, _, right) => $"({left} >> {right})")],
    [op.InfixL(Kind(Symbol.Gt), static (left, _, right) => $"({left} > {right})")],
]);
```

Over the tokens of a lexer that skips whitespace, `a >> b` gives `(a >> b)`, `a > b` gives `(a > b)`, and `a > > b` fails, because of the space the lexer skipped between the two `>`.

- The two touch when `p`'s span ends where `q`'s starts, in the same source. A `p` that consumes nothing touches whatever follows it.
- `Adjacent` commits as `Seq` does: once `p` has consumed input, a failure is committed. Wrap it in `Atomic` where the grammar should then try something shorter. Above, `a > b` reads its `>` as the start of a shift, and `Atomic` lets the comparison level read it instead.
- When the two do not touch, it fails as a rejected `Where` does ([Sequencing § `Bind`, `where` and `let`](sequencing.md#bind-where-and-let)): at `p`'s start, over the input both consumed. The message names both, as in `'>' must come right after '>', with nothing between them`, each by its label or by what its leading primitive expects. If either has no name, it is `these items must touch, with nothing between them`.
- Over input held whole in memory, consecutive items always touch, so telling `>>` from `> >` needs a layer's spans.

### Trivia

Trivia is the source that no item covers: the whitespace and comments a lexer's `skip` consumed, and any items a layer dropped. Creole stores none of it. `LeadingTrivia` works it out from the spans of the items on either side, so it costs nothing unless a grammar reads it ([ADR-0036](../../work/docs/decisions/0036-trivia-is-the-source-between-items-read-with-a-zero-width-parser.md)).

```csharp
// Each token with the trivia before it, then the trivia after the last token.
var file = Seq(Seq(LeadingTrivia, Any.Spanned()).Many(), LeadingTrivia).Before(End);
```

Over the tokens of a lexer that skips whitespace and comments, `  a /* c */ b ` gives `a` the trivia 0..2 and `b` the trivia 3..12, and the trivia at the end is 13..14.

- `LeadingTrivia` succeeds without consuming input. It produces the span from the end of the item before the cursor to the start of the next item.
- Before the first item, the trivia starts where the source starts. At the end of the input, it runs from the last item to where the source ends. With no items, that is the whole source.
- If the items are in order and do not overlap, as a lexer's tokens are, their trivia and spans cover the source exactly once. Reading them from the text, in order, reproduces the source.
- All of a gap belongs to the item after it. There is no trailing trivia, so a tool that wants a same-line comment to belong to the token before it has to split the gap itself.
- A trivia span is one range. To tell comments from whitespace, run a grammar for trivia over its text.
- Comment tokens that a `Layer.Where` drops are part of the trivia of the next token that is kept.
- A [synthetic item](#synthetic-items) takes the gap before it. An item at the same place after it gets zero-width trivia, so in a layout pass the indentation before a dedented line is the trivia of its first DEDENT.
- If an item starts before the item before it ends, its trivia is zero-width at its own start. This happens with the outputs of a `Layer.Transduce` step that emits several derived outputs over one span.
- Over input held whole in memory, items are adjacent, so trivia is always zero-width.
- Over a byte source decoded by [`Layer.Utf8`](layers.md#decoding-bytes-layerutf8), trivia has byte and character offsets, as items do. Its byte ranges reproduce the bytes and its character ranges reproduce the decoded text. A byte-order mark at the start is in the first item's trivia.
- It reads the next item to find where that item starts, as `Position` does.

## Lines and columns

A `LineMap` turns character offsets into lines and columns. Build it once per source, then look up as many offsets as you need. It records where lines start and does not keep the text.

```csharp
var map = LineMap.Create("{\r\n  \"port\": 8080\r\n}");

LinePosition quote = map.GetPosition(5);    // 2:3, the quote before "port"
long lines = map.LineCount;                 // 3
```

- Lines and columns start at 1. `LinePosition` formats as `line:column`.
- Line breaks are LF, CRLF and CR. NEL, U+2028 and U+2029 do not end a line.
- Columns count Unicode scalar values, so an emoji written as a surrogate pair is one column. A lone surrogate is also one column.
- The LF of a CRLF is on the same line as its CR. An offset on the second half of a surrogate pair has the same column as the first half.
- Any offset from 0 to the text's length is valid. The length itself is the position just past the last character. Other offsets throw `ArgumentOutOfRangeException`.

For a byte source, look up `CharStart`, the offset in the decoded text, not the byte offset `Start`.

To show errors against a source, wrap its text in a `SourceText`, which builds the `LineMap` on first use. Columns in a rendered location count scalar values as above, while the carets under the line follow what a terminal shows: grapheme clusters, wide characters and tab stops ([Errors § Rendering against the source](errors.md#rendering-against-the-source)).

For text that does not fit in one string, such as a source longer than 2 GB, feed it to a `LineMap.Builder` in chunks. A CRLF or a surrogate pair may be split across two chunks.

```csharp
var builder = new LineMap.Builder();

foreach (ReadOnlyMemory<char> chunk in chunks)
{
    builder.Append(chunk.Span);
}

LineMap map = builder.Build();
```
