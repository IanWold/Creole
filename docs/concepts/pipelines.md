# Pipelines

A pipeline runs [layers](layers.md) over the input and a parser over the last layer's items, such as a lexer and a parser over its tokens. Build one by ending a layer in a parser, and run it with `Parse`. The samples use the JSON lexer and grammar of the whitepaper's Example A, in [WorkedExampleATests.cs](../../tests/Creole.Tests/WorkedExampleATests.cs), whose tests check the output shown here. The design is in the [whitepaper's § Running pipelines](../../whitepaper.md#running-pipelines).

```csharp
var json = Pipeline.From(JsonLexer.Lexer).Then(JsonGrammar.Document);    // Pipeline<char, JsonValue>
var result = json.Parse(File.ReadAllText(path), source: path);

if (result is not Parsed<JsonValue>)
{
    Console.Error.WriteLine(result.Diagnostics.Render());
}
```

## Building a pipeline

- `Pipeline.From(layer)` starts a pipeline with the layer that reads the input. It returns the layer itself, so `JsonLexer.Lexer.Then(JsonGrammar.Document)` builds the same pipeline.
- `Then(layer)` adds a layer, as it does for layers ([Chaining layers](layers.md#chaining-layers-then)).
- `Then(parser)` ends the pipeline in a parser over the last layer's items. The result is a `Pipeline<TIn, TOut>`: `TIn` is what the pipeline reads, and `TOut` is the parser's value.
- Between each layer and the next, and between the last layer and the parser, sits a buffer. So the parser can rewind over tokens without the lexer running again.
- `From` and `Then` throw `GrammarException` for a null argument.

A pipeline is data, as its layers and parser are: building one runs nothing. It is immutable, so one pipeline can be run from several threads at once. Each run has its own buffers and state.

## `Parse`

| Overload | On | Input |
| --- | --- | --- |
| `Parse(string input, string? source = null, ParseOptions? options = null)` | `Pipeline<char, TOut>` | A string. |
| `Parse(ReadOnlyMemory<char> input, string? source = null, ParseOptions? options = null)` | `Pipeline<char, TOut>` | Characters in memory, such as part of a buffer, or a `char[]`. |
| `Parse(TextReader input, ParseOptions? options = null)` | `Pipeline<char, TOut>` | A reader, read as the layers pull ([Readers](#readers)). |
| `Parse(ReadOnlyMemory<byte> input, string? source = null, ParseOptions? options = null)` | `Pipeline<byte, TOut>` | Bytes in memory, or a `byte[]`, which the first layer decodes ([Bytes in memory](#bytes-in-memory)). |

- `source` and `options` work as they do for a parser ([Running a parser](parsing.md#parse)). Pass options by name: `Parse(text, options: limits)`.
- The depth limit applies to each layer's steps and to the parser, each counting its own depth.
- `Parse` succeeds when the parser does, even if input remains. To require the whole input, end the grammar with `End`, as `JsonGrammar.Document` does with `Value.Before(End)`.

## What it returns

- **`Parsed<T>`** with the parser's value when the parser succeeds.
- **`Failed`** otherwise, with one error:
  - from a layer that could not produce its next item, with the layer's name, such as `lexer`, as its `Layer`. A layer's error is the furthest failure across all its steps ([Layers](layers.md#layertokens));
  - or from the parser, with the layer name `parser`.

  Either way, its spans are character offsets in the input, in the source `SourceId.Unnamed`.
- `Parse` never returns `Recovered<T>` yet.

A failed result from a string or memory holds the text it parsed, so `result.Diagnostics.Render()` shows the error against it:

```csharp
var result = json.Parse("[\n{\n  \"port\": 8080 \"host\": \"x\"\n}\n]", source: "config.json");
Console.Error.WriteLine(result.Diagnostics.Render());
```

```text
error[parser]: expected ',' or '}'
 --> config.json:3:16
  |
3 |   "port": 8080 "host": "x"
  |                ^^^^^^ unexpected string "host"
  = note: in object starting at 2:1
```

The parser read tokens, but the error points at the characters of the token it did not expect, because every token's span is in the source ([Source spans](spans.md#spans-of-a-layers-items)). An error from the lexer renders the same way, at the characters it could not read.

## `ParseMany`

`ParseMany` parses input that holds a sequence of items, such as log entries or JSON values one after another. It runs the pipeline's parser at the start of the input, and again after each item until no input remains, and returns each item's result as you ask for it. With a lexer of words and `Grammar<string>.Any` as the parser, from [ParseManyTests.cs](../../tests/Creole.Tests/ParseManyTests.cs):

```csharp
var word = Pipeline.From(words).Then(Grammar<string>.Any);

foreach (var result in word.ParseMany(" a bc  d "))
{
    // Parsed "a", then Parsed "bc", then Parsed "d"
}
```

| Overload | Input |
| --- | --- |
| `ParseMany(string input, string? source = null, ParseOptions? options = null)` | A string. |
| `ParseMany(ReadOnlyMemory<char> input, string? source = null, ParseOptions? options = null)` | Characters in memory. |
| `ParseMany(TextReader input, ParseOptions? options = null)` | A reader ([Readers](#readers)). |
| `ParseMany(ReadOnlyMemory<byte> input, string? source = null, ParseOptions? options = null)` | Bytes in memory, on a `Pipeline<byte, TOut>` ([Bytes in memory](#bytes-in-memory)). |

- **It is lazy.** Nothing runs until you enumerate the results, and each item is parsed when you ask for it. A reader is read only as far as that item needs.
- **A failed item is the last.** Each result is `Parsed<T>` or `Failed`. After a failure nothing says where the next item would start, so the sequence ends there.
- **A layer's failure ends it too.** If a layer fails while the next item is being looked for, that next item's result is `Failed` with the layer's error.
- **Each item starts afresh.** A failure recorded while parsing one item never shapes a later item's error.
- **It records as it goes.** `ParseMany` records failures while it parses, rather than running again when an item fails, so a failed item never parses the items before it again. Each item pays the cost of recording.
- **The parser must consume input.** A parser that succeeds without consuming input would never reach the end, so `ParseMany` throws `GrammarException`.
- **Each enumeration parses from the start.** Ending an enumeration early, as `break` in a `foreach` does, returns its buffers' memory to their pools.

Over a string or memory, a failed result holds the whole text, so its error renders at its line in the input. The arguments are checked when `ParseMany` is called; a grammar that is not valid throws when the enumeration starts.

## Readers

`Parse` and `ParseMany` over a `TextReader` read as the layers pull, blocking until the reader has characters or ends. They keep only what the layers may still go back to, so the whole input is never held.

- Offsets count the characters read, from 0.
- Failures are recorded as the parse goes, since the input cannot be read again, so a failed parse does not run twice.
- A failed result holds no text, so `Render()` writes its error on one line, as `ToString()` does: `error[lexer] at 4..5: unexpected '1'; expected whitespace or letter`.
- Slices, such as `Slice` values in tokens, are copies.
- The reader is yours: a parse leaves it open. Enumerate `ParseMany` over a reader once, since a second enumeration reads on from wherever the reader is, counting offsets from 0 again.

## Bytes in memory

A pipeline whose first layer reads bytes, such as one that starts with [`Layer.Utf8()`](layers.md#decoding-bytes-layerutf8), parses bytes held in memory with `Parse` and `ParseMany`. From [ByteSourceDiagnosticsTests.cs](../../tests/Creole.Tests/ByteSourceDiagnosticsTests.cs), with a lexer over the decoded characters and a parser of `name = value` settings:

```csharp
var settings = Pipeline.From(Layer.Utf8().Then(lexer)).Then(parser);
var result = settings.Parse(File.ReadAllBytes(path), source: path);
```

```text
error[parser]: expected name or end of input
 --> settings.txt:2:13
  |
2 | emoji = "😀" 7
  |              ^ unexpected 7
```

- Spans hold byte offsets in `Start` and `End`, and offsets in the decoded text in `CharStart` and `CharEnd` ([Source spans](spans.md#lines-and-columns)).
- When the first layer is `Layer.Utf8()`, a failed result holds the text that `Encoding.UTF8` decodes from the bytes, after any byte-order mark, so `Render()` shows the error at its line and column. The text is decoded only when the parse fails. It matches the decoder's characters up to the first invalid sequence, where the decoder ends the parse, so an error from any layer renders in the right place.
- A pipeline from bytes whose first layer is anything else, even a layer before `Layer.Utf8()`, holds no text, and its errors render on one line.
- `Parse` over bytes runs twice when it fails, as over text. `ParseMany` records as it goes.
- Slices are copies, since the decoded characters are not the caller's memory.

## Input in memory

Over a string or memory, `Parse` and `ParseMany` copy none of their input. The first layer reads the string or memory it was given, so slices a lexer takes, such as `Slice` values in tokens, are views of it, and a failed result refers to the same text to render it. If the input is a buffer you reuse, finish with the slices and render the errors before you reuse it.

Items between layers are held in buffers, which let an item go once nothing can go back to it ([Chaining layers](layers.md#chaining-layers-then)). When a parse ends, however it ends, its buffers return their memory to their pools.

## A failed parse runs twice

As a parser's `Parse` does, a pipeline's `Parse` over a string or memory runs without recording failures. If it fails, it runs the whole pipeline again from the start, with new buffers and every layer recording, and reports the error from that second run. So a successful parse pays nothing for error reporting, and a failed one costs up to about two and a half times as much. The functions in every layer and in the parser run again in the second run (see [How often your functions run](sequencing.md#how-often-your-functions-run)). The reasons are in [ADR-0027](../../work/docs/decisions/0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md) and [ADR-0038](../../work/docs/decisions/0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md).

## Exceptions

`Parse` and `ParseMany` throw `GrammarException` if the pipeline is null, or if a grammar in it is not valid, which is found when it first runs. They throw `ArgumentNullException` for a null string or reader. Exceptions from the reader, such as an `IOException`, pass through. Exceptions from your own code, such as a `Select` function in a layer or in the parser, pass through.
