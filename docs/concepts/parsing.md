# Running a parser

`parser.Parse(input)` runs a character parser on text and returns a [`ParseResult<T>`](errors.md): the value, or the error that stopped the parse. The samples use `CharJson`, the character-level JSON grammar in [ParsingTests.cs](../../tests/Creole.Tests/ParsingTests.cs), whose tests check the output shown here.

```csharp
var result = CharJson.Document.Parse(File.ReadAllText(path), source: path);

string report = result switch
{
    Parsed<JsonValue> parsed       => $"ok: {parsed.Value}",
    Recovered<JsonValue> recovered => recovered.Diagnostics.Render(),
    Failed failed                  => failed.Diagnostics.Render(),
};
```

## `Parse`

`Parse` is an extension member on `Parser<char, TOut>`, so it runs any rule of a `CharGrammar`.

| Overload | Input |
| --- | --- |
| `Parse(string input, string? source = null, ParseOptions? options = null)` | A string. |
| `Parse(ReadOnlyMemory<char> input, string? source = null, ParseOptions? options = null)` | Characters in memory, such as part of a buffer. |

- `source` is the name to show the input under when errors are rendered, such as a file name. It may be omitted or null.
- `options` holds settings such as the depth limit. Omitted or null, it is `ParseOptions.Default`.
- The second parameter is `source`, so pass options by name: `Parse(text, options: limits)`.

Token parsers, such as the rules of a `TokenGrammar`, run on the output of a lexer. Pipelines that connect the two arrive with LAY-05.

A parser can be run from several threads at once. Each parse keeps its own state.

## What it returns

- **`Parsed<T>`** when the parser succeeds. The parse does not have to reach the end of the input. To require that, end the grammar with `End`, as in `Value.Before(End)`.
- **`Failed`** otherwise, with one error: the parse's furthest failure, as described in [Errors](errors.md#what-a-failure-reports). Its layer is `parser`. Its spans are character offsets into the input, in the source `SourceId.Unnamed`.
- `Parse` never returns `Recovered<T>` yet. Recovery arrives with ERR-05.
- `Parse` never returns `default(ParseResult<T>)`, so a switch over its result needs no `null` arm.

`Parse` throws `GrammarException` if the parser is null, which happens when a rule refers to one declared later in the class without `Rec`, or if the grammar is not valid, which is found when it first runs. It throws `ArgumentNullException` for a null string. Exceptions from your own code, such as a `Select` function or a custom scanner, pass through.

## Showing errors

A failed result holds the text it parsed, so `result.Diagnostics.Render()` shows each error against its line:

```csharp
var result = CharJson.Document.Parse("{\n  \"port\": 8080 \"host\": \"x\"\n}", source: "config.json");
Console.Error.WriteLine(result.Diagnostics.Render());
```

```text
error[parser]: expected whitespace, ',' or '}'
 --> config.json:2:16
  |
2 |   "port": 8080 "host": "x"
  |                ^ unexpected '"'
  = note: in object starting at 1:1
```

This grammar reads characters, so its error names a character and the whitespace its tokens may be followed by. Give rules a name with `Named` to say what was expected in your own words ([Backtracking and labels](control.md)). The format is described in [Errors § Rendering against the source](errors.md#rendering-against-the-source).

## Input in memory

`Parse` copies none of its input. `Slice` values are views of the string or memory it was given, and a failed result refers to the same text to render it. If the input is a buffer you reuse, finish with the slices and render the errors before you reuse it.

Beyond its result and what the grammar builds, a parse allocates nothing: each thread reuses the buffers that record failures. [The interpreter § What allocates](../engines/interpreter.md#what-allocates) lists what does allocate.

## Options

`ParseOptions` is an immutable record of settings that do not depend on the input, so one instance can serve every parse:

```csharp
static readonly ParseOptions Limits = ParseOptions.Default with { MaxDepth = 200 };

var result = CharJson.Document.Parse(untrusted, options: Limits);
```

| Option | Default | Meaning |
| --- | --- | --- |
| `MaxDepth` | 1,000 | The deepest a parse may recurse, in levels. It must be positive; `int.MaxValue` leaves only the stack check. |

[The interpreter § Deep input](../engines/interpreter.md#deep-input) says what counts as a level. A parse that would go deeper than `MaxDepth`, or runs short of the thread's stack first, returns `Failed` with one error:

- Its span covers the item where the level would have started, or is empty at the end of the input.
- Its `Unexpected` is a `CustomMessage` that names the cause: `nesting exceeds ParseOptions.MaxDepth of 1000`, or `nesting is too deep for the thread's stack`.
- It expects nothing.
- Its context lines are the [`Context`](control.md) rules in force there.

```text
error[parser] at 4..5: nesting exceeds ParseOptions.MaxDepth of 3
```
