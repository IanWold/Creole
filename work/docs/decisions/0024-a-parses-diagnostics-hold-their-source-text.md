# ADR-0024: A parse's diagnostics hold the source text they point into, and `Parse` names its source with a `source:` parameter

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-25 |
| Cards | [INT-08](../../complete/03-interpreter/INT-08-single-layer-parse-api.md), [LAY-05](../../active/06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md), [STRM-03](../../active/08-streaming/STRM-03-streaming-diagnostics.md), [ERR-03](../../active/04-errors-and-recovery/ERR-03-diagnostic-rendering.md) |
| Whitepaper | [§ Results](../../../whitepaper.md#results-1), [§ The error model](../../../whitepaper.md#the-error-model), [§ Running pipelines](../../../whitepaper.md#running-pipelines), [§ A. JSON in two layers](../../../whitepaper.md#a-json-in-two-layers), [§ Streaming and async input](../../../whitepaper.md#streaming-and-async-input) |
| Research | — (library documentation read for this record by a subagent on 2026-10-25, cited inline) |
| Supersedes | — |

## Context

The whitepaper renders a parse's errors in one call. [§ A. JSON in two layers](../../../whitepaper.md#a-json-in-two-layers) ends with:

```csharp
var result = json.Parse(File.ReadAllText(path), source: path);
if (result is not Parsed<JsonValue>) Console.Error.WriteLine(result.Diagnostics.Render());
```

It expects the block shown in [§ The error model](../../../whitepaper.md#the-error-model). [§ Running pipelines](../../../whitepaper.md#running-pipelines) also passes `source: "config.json"`. [§ Streaming and async input](../../../whitepaper.md#streaming-and-async-input) says that a streamed diagnostic captures its line, column and a snippet when it is raised, and that "whole-input parses still resolve lazily".

What ERR-01 and ERR-03 built does not support that call:

- `SourceSpan` holds a `SourceId`, not a name.
- `Diagnostics` is a list of errors, and two collections are equal when their errors are equal.
- `Render()` writes one line per error. `Render(params ReadOnlySpan<SourceText>)` writes the block, and throws if an error points into a source it was not given ([docs/concepts/errors.md](../../../docs/concepts/errors.md#one-line-per-error)).
- `SourceText(id, name, text)` holds a `string`. `LineMap.Create` takes a `ReadOnlySpan<char>`.

So the example prints one line per error. ERR-02's completion review found the gap. INT-08 and LAY-05 both carry a scope line that asks which way to close it.

Three constraints apply:

- **No copies of in-memory input.** `Parse` builds its cursor over the caller's memory, and slices are views of it ([ADR-0013](0013-slices-view-in-memory-input-and-copy-streamed-input.md)).
- **Streams cannot be handed back.** No caller holds a stream's text after the parse, so a streamed diagnostic must carry what it needs to render (STRM-03).
- **`ParseOptions` is shared with pipelines** (INT-08's scope), so the same options can configure many parses.

Prior art, read on 2026-10-25:

| Library | Where the source comes from when an error is rendered |
| --- | --- |
| megaparsec 9.8.3 | From the error. `runParser :: Parsec e s a -> String -> s -> Either (ParseErrorBundle s e) a` takes a file name. `ParseErrorBundle` has `bundlePosState :: PosState s`, and `PosState` has `pstateInput :: s`. `errorBundlePretty :: ... => ParseErrorBundle s e -> String` takes only the bundle. The docs do not say whether `pstateInput` holds the whole input or only what follows the error ([Text.Megaparsec](https://hackage-content.haskell.org/package/megaparsec-9.8.3/docs/Text-Megaparsec.html), [Text.Megaparsec.Error](https://hackage-content.haskell.org/package/megaparsec-9.8.3/docs/Text-Megaparsec-Error.html), [Text.Megaparsec.State](https://hackage-content.haskell.org/package/megaparsec-9.8.3/docs/Text-Megaparsec-State.html)). |
| ariadne 0.6.0 | From the caller. `Report::print`, `eprint` and `write` each take a `cache: C` where `C: Cache<S::SourceId>` ([Report](https://docs.rs/ariadne/latest/ariadne/struct.Report.html)). |
| Roslyn | From the error. `Diagnostic.Location` returns a `Location`, and `Location.SourceTree` is "The syntax tree this location is located in or `null` if not in a syntax tree." ([Diagnostic.Location](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnostic.location), [Location.SourceTree](https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.location.sourcetree)). Those pages do not show the step from the tree to its text. |

## Options for where the source text comes from (fork 1)

### 1A. The result holds its source (chosen by the maintainer)

`Parse` attaches a `SourceText` for its input to the `Diagnostics` of a `Recovered` or `Failed` result. `Render()` writes the block when the collection holds every source its errors point into, and one line per error otherwise. This is the whitepaper's current position.

- **For.** The whitepaper's examples work as written. One call renders both kinds of parse: in-memory parses now, and streamed parses once STRM-03 makes their diagnostics carry captured lines. The full block is the default output, not the second choice. megaparsec and Roslyn work this way.
- **Against.**
  - `Diagnostics` carries state that equality ignores, so what `Render()` writes depends on how the collection was built. A collection rebuilt from a result's errors renders one line per error.
  - A failed result keeps its input alive.
  - A caller who reuses a `ReadOnlyMemory<char>` buffer must render before reusing it. ADR-0013 already requires the same for slices.
  - `SourceText` must hold memory without copying it.
- **Cost.** Nothing on success. On failure, one `SourceText`; its `LineMap` is built only when the result is rendered. No effect on AOT or the analyzers.

### 1B. The caller passes the source

`Diagnostics` stays as it is. The examples change to `result.Diagnostics.Render(new SourceText(text, path))`.

- **For.** No change to `Diagnostics`. What `Render` writes depends only on its arguments. A result holds nothing. ariadne works this way.
- **Against.**
  - The whitepaper's examples change and get longer.
  - `source:` loses its purpose, because spans carry ids, not names. It would be dropped, or `Parse` would take a `SourceText`.
  - Streamed results cannot take this path. `Render()` would then show the block for a streamed parse and one line per error for an in-memory one.
  - The terse output is the default.

### 1C. Every parse captures lines when it ends

Every parse resolves its errors when it ends, as streamed parses will. The diagnostics carry line, column and a capped snippet, and hold nothing else.

- **For.** Diagnostics are self-contained values. Nothing pins the input. One mechanism serves every source.
- **Against.**
  - It contradicts "whole-input parses still resolve lazily".
  - It brings STRM-03's captured-line renderer into Phase 0.
  - It scans the input for lines on every failed parse, even when nobody renders the result.
  - The snippet cap cuts long lines that the block could otherwise show whole. Multi-line spans and context starts need more to be captured.

### 1D. Defer to LAY-05

INT-08 ships `Parse` without a source name, and the example stays wrong until pipelines exist.

- **For.** No decision now.
- **Against.** `Parse`'s public signature would change when LAY-05 decides. The whitepaper and the docs disagree until then.

## Options for where `Parse` takes the source's name (fork 2)

### 2A. A `source:` parameter (chosen by the maintainer)

`Parse(input, string? source = null, ParseOptions? options = null)`. Spans use `SourceId.Unnamed`, and the held `SourceText` carries the name.

- **For.** It matches the whitepaper. Options do not depend on the input, so one instance, such as a `static readonly` field, can serve every parse.
- **Against.**
  - Every single-source parse uses `SourceId.Unnamed`. If diagnostics from two parses are merged into one collection, the renderer cannot tell their sources apart.
  - A positional `Parse(text, options)` does not compile. Callers name the argument: `Parse(text, options: o)`.

### 2B. `ParseOptions.Source`

`Parse(input, ParseOptions? options = null)`, with the name as a property of the options.

- **For.** One optional parameter.
- **Against.** Options that are otherwise shared become per-input, so callers write `options with { Source = path }` on every call.

### 2C. `Parse` takes a `SourceText`

`Parse(SourceText source, ParseOptions? options = null)` is the only form that takes a name.

- **For.** Callers that issue ids from a `SourceRegistry` get distinct ids now.
- **Against.** The common path needs a wrapper object. `SourceText` holds a `string`, so `ReadOnlyMemory<char>` input cannot be named without changing `SourceText`.

## Decision

1. **The result holds its source.** For a `Recovered` or `Failed` result, `Parse` attaches a `SourceText` for its input to the result's `Diagnostics`. A `Parsed` result holds nothing.
   - The `SourceText` has the id `SourceId.Unnamed`, the name given as `source:` (or none), and the input as its text.
   - Holding the input copies nothing. A `string` is held as it is. `ReadOnlyMemory<char>` is held as memory, so `SourceText` gains a constructor over `ReadOnlyMemory<char>`, and the renderer reads it without making a string.
   - Input that is not text holds no source, so its errors render one line each.
2. **`Render()`.** If the held sources include every source that the errors and their context lines point into, `Render()` writes what `Render(sources)` would write with them. Otherwise it writes one line per error, as it does now. It never throws. `Render(sources)` uses only the sources it is given.
3. **Equality and construction.** Two collections are equal when they hold equal errors in the same order. Held sources do not count. `new Diagnostics(errors)` holds no sources. For now only Creole attaches them, through an internal member. A public way waits for a caller that needs one.
4. **The name.** For `string` and `ReadOnlyMemory<char>` input, the signature is `Parse(input, string? source = null, ParseOptions? options = null)`. `ParseOptions` holds only settings that do not depend on the input. A `Parse(SourceText)` overload, for callers that issue ids from a `SourceRegistry`, waits until a caller needs one.
5. **Pipelines and streams.** A pipeline's result holds its root source in the same way (LAY-05). A streamed parse holds its diagnostics' captured lines instead of the text, and `Render()` uses them (STRM-03). So the same call shows the block for every kind of source.
6. **Cards.**
   - INT-08 builds points 1 to 4 for the single-layer `Parse`, and updates docs/concepts/errors.md.
   - LAY-05 applies points 1 and 5 to pipelines. This settles the question in its scope.
   - STRM-03 makes `Render()` use captured lines.

Deciding reasons:

- **Fork 1.** 1A keeps the whitepaper's examples true. It is also the only option in which one `Render()` call serves both in-memory and streamed parses without capturing lines on every failed parse.
- **Fork 2.** The maintainer chose 2A. It keeps options shareable and matches the whitepaper.

What would change this record:

- If holding the input pins large buffers in real use, for example where results are kept long after the parse, 1C's capture becomes the better default.
- If callers often merge diagnostics from several single-source parses, the remedy is point 4's `Parse(SourceText)` overload, not a change to the default.

## Consequences

- **Easier.**
  - The whitepaper's examples render the block as written.
  - Callers learn one call for every source.
  - LAY-05's open question is settled before pipelines exist.
- **Harder.**
  - `Diagnostics` carries state that equality ignores. The docs must say when `Render()` shows the block and when it shows one line per error.
  - `Render()` with no arguments no longer always means one line per error, as docs/concepts/errors.md says now. A caller who wants single lines from a result writes `string.Join('\n', result.Diagnostics)`.
  - `SourceText` gains storage over memory, and the renderer reads that memory.
- **Ruled out.** Capturing snippets at the end of every in-memory parse (1C), unless a later record supersedes this one.
- **Risks.** A `ReadOnlyMemory<char>` caller who reuses the buffer before rendering gets a wrong snippet, not an exception. The docs state the rule next to ADR-0013's rule for slices.
- **Watched by** INT-08's tests:
  - A `Failed` result's `Render()` equals `Render(new SourceText(text, name))`.
  - A collection built from the same errors renders one line per error.
  - Equality ignores the held sources.
  - A `Parsed` result holds nothing.
  - A parse over `ReadOnlyMemory<char>` copies nothing for the held source (an allocation test).

## Changes made

- Whitepaper:
  - [§ Results](../../../whitepaper.md#results-1): the collection holds the text of the sources its errors point into, so `Diagnostics.Render()` shows the block; `source:` names the source. Nothing is copied, so a buffer is rendered before it is reused. A collection without those texts renders one line per error, and equality ignores the held texts. Links this record.
  - [§ Streaming and async input](../../../whitepaper.md#streaming-and-async-input): whole-input parses resolve lazily from the text their result holds, and `Diagnostics.Render()` shows the same format for both kinds of parse.
  - [§ A. JSON in two layers](../../../whitepaper.md#a-json-in-two-layers) and [§ Running pipelines](../../../whitepaper.md#running-pipelines) are unchanged; they already read this way.
- Cards:
  - [INT-08](../../complete/03-interpreter/INT-08-single-layer-parse-api.md): its rendering scope item is settled. Its signature is now `parser.Parse(input, source, options)`, and a new criterion covers rendering.
  - [LAY-05](../../active/06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md): its rendering scope line is settled. A pipeline's result holds its root source.
  - [STRM-03](../../active/08-streaming/STRM-03-streaming-diagnostics.md): its scope gains `Render()` over captured lines.
- Docs: docs/concepts/errors.md changes with INT-08's build, since the docs describe what is built.
