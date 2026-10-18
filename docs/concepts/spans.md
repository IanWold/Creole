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
- A zero-width span (`Start == End`) marks a position, such as where a missing item was expected.
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

For text that does not fit in one string, such as a source longer than 2 GB, feed it to a `LineMap.Builder` in chunks. A CRLF or a surrogate pair may be split across two chunks.

```csharp
var builder = new LineMap.Builder();

foreach (ReadOnlyMemory<char> chunk in chunks)
{
    builder.Append(chunk.Span);
}

LineMap map = builder.Build();
```
