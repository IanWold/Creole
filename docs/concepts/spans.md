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
