# Creole documentation

User-facing documentation for Creole, written alongside the code (see [CONTRIBUTING.md § Documentation](../CONTRIBUTING.md#documentation)). Everything here must be true of the current code. Design rationale lives in the [whitepaper](../whitepaper.md) and decision records, not here.

## Planned layout

The layout follows the scope of [REL-02](../work/active/14-release/REL-02-documentation.md). Create folders as the first page in each is written, and add each page to this index.

| Folder | Contents |
| --- | --- |
| `getting-started/` | Installing, a first grammar, running it. |
| `concepts/` | Commit semantics, layers and pipelines, the three feedback tools, spans, errors and recovery, streaming. |
| `guides/` | Task-oriented how-tos: testing grammars, deterministic mode, untrusted input, migrating from Sprache, Superpower, Pidgin or Parlot. |
| `engines/` | The interpreter, the source generator, staging rules, Native AOT and trimming. |
| `reference/diagnostics/` | One page per diagnostic, `CRLnnnn.md`, created in the change that introduces the diagnostic. |

## Pages

| Page | Contents |
| --- | --- |
| [Grammars as data](concepts/grammars-as-data.md) | The `Parser<TIn, TOut>` type, immutability and identity, `IKinded<TKind>` and `GrammarException`. |
| [Source spans](concepts/spans.md) | `SourceSpan`, `SourceId`, `SourceRegistry` and `LineMap`: where items came from in the original source, as offsets or as lines and columns. |
| [The cursor](engines/cursor.md) | `Cursor<T>`: absolute positions, reading without allocation, bulk scans over segmented input. |
