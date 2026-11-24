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

A failed result holds the text it parsed, so `result.Diagnostics.Render()` shows the error against it:

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

## Input in memory

`Parse` copies none of its input. The first layer reads the string or memory it was given, so slices a lexer takes, such as `Slice` values in tokens, are views of it, and a failed result refers to the same text to render it. If the input is a buffer you reuse, finish with the slices and render the errors before you reuse it.

Items between layers are held in buffers, which let an item go once nothing can go back to it ([Chaining layers](layers.md#chaining-layers-then)). When a parse ends, however it ends, its buffers return their memory to their pools.

## A failed parse runs twice

As a parser's `Parse` does, a pipeline's `Parse` runs without recording failures. If it fails, it runs the whole pipeline again from the start, with new buffers and every layer recording, and reports the error from that second run. So a successful parse pays nothing for error reporting, and a failed one costs up to about two and a half times as much. The functions in every layer and in the parser run again in the second run (see [How often your functions run](sequencing.md#how-often-your-functions-run)). The reasons are in [ADR-0027](../../work/docs/decisions/0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md) and [ADR-0038](../../work/docs/decisions/0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md).

## Exceptions

`Parse` throws `GrammarException` if the pipeline is null, or if a grammar in it is not valid, which is found when it first runs. It throws `ArgumentNullException` for a null string. Exceptions from your own code, such as a `Select` function in a layer or in the parser, pass through.
