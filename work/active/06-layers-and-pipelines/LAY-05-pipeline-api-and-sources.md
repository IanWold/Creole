# LAY-05: Pipeline API and sources

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Building |

## Summary

Chain layers into a parser and run it over in-memory or reader input.

## Scope

- `Pipeline.From(layer).Then(...)`.
- `Parse(text, source:)` and lazy `ParseMany` returning `IEnumerable<ParseResult<T>>`. The parser is part of the pipeline: `Parse` runs it once, and `ParseMany` runs it until no input remains. A failed item is the last result, and an item that consumes nothing throws `GrammarException` until ANA-03 ([ADR-0038](../../docs/decisions/0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md) points 1 and 2).
- `TextReader` sources with blocking pulls. Each character gets the root-layer span (§ Span rules, rule 1): character *i* has \[*i*, *i* + 1).
- Each root source's upstream reports `InputStart` and `InputEnd`, the zero-width spans where its source starts and ends: 0 and the text's length in memory, and the number of characters read once a reader ends. Every layer above takes them, and `LeadingTrivia` reads them for the trivia before the first item and at the end ([ADR-0036](../../docs/decisions/0036-trivia-is-the-source-between-items-read-with-a-zero-width-parser.md) point 5, [SPAN-05](../05-source-spans/SPAN-05-trivia-attach-mode.md)).
- Slices follow [ADR-0013](../../docs/decisions/0013-slices-view-in-memory-input-and-copy-streamed-input.md): `Parse(text)` builds its root cursor over the caller's memory, so slices are views; over a `TextReader` they are copies.
- Immutable, thread-shareable pipelines; each call owns its buffers and state.
- Remove `SourceRegistry`, which had no producer and held a second copy of each source's name ([Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) finding 25). Pipelines name their source with `source:` and use `SourceId.Unnamed`. `SourceText`'s public constructors take the text, as a `string` or `ReadOnlyMemory<char>`, and an optional name; those that take an id become internal ([ADR-0038](../../docs/decisions/0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md) point 6).
- A pipeline's result holds its root source text, as [INT-08](../03-interpreter/INT-08-single-layer-parse-api.md)'s does, so Example A's `result.Diagnostics.Render()` shows the full format ([ADR-0024](../../docs/decisions/0024-a-parses-diagnostics-hold-their-source-text.md) points 1 and 5).
- A pipeline over in-memory input runs without failure logs and, when it fails, runs again whole with them ([ADR-0027](../../docs/decisions/0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md)). The second run opens every layer again in fresh buffers, so no item, span hold or state carries over from the first. `ParseMany` records as it goes over every source ([ADR-0038](../../docs/decisions/0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md) point 3).
- `Position` over a streamed input with no items gives offset 0 in `SourceId.Unnamed`, because the buffer has no span to take a source from ([ADR-0035](../../docs/decisions/0035-span-combinators-commit-like-sequences-and-hold-their-spans.md) point 4). That is right, because every pipeline's root is `Unnamed` ([ADR-0038](../../docs/decisions/0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md) point 6).
- When a parse ends, return the segments its stream buffers still hold to the pool. `StreamBuffer<T>.Release` keeps the segment below its horizon, so every finished buffer holds at least one: about 34 KB for characters with the default 1,024-item segment, most of it the 32-byte slots ([LAY-01](LAY-01-streambuffer-and-pulling-cursor.md) review, [SPAN-02](../05-source-spans/SPAN-02-out-of-band-span-storage.md)).
- Over streamed input, make sure `Parse`'s fallback errors read nothing the buffer may have released: `NothingRecorded` reads position 0, and `DepthError` the depth limit's position.
- Under `ParseMany`, each item starts with no furthest failure ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 7). Its result holds the parser's diagnostics raised while parsing it, and a lower layer's from the steps it reads. A diagnostic raised in lookahead past its end waits for the next item, and any left at the end of input go in a last `Failed` result ([ADR-0038](../../docs/decisions/0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md) point 4). No layer below the parser can raise a diagnostic until ERR-05, ERR-07 or SPAN-07, so this card builds the per-item bags and ERR-07 tests the rule.
- A layer whose step fails throws the internal `LayerFailedException`, which carries that layer's error, named for the layer, with spans in the source. `Parse` catches it and returns `Failed` with that error. `Layer.Open` takes `recordFailures`; LAY-03's tests always pass true. Pass false in a first run over in-memory input, and true when it runs again or under `ParseMany` ([LAY-03](LAY-03-layer-abstraction-and-emit.md) Outcome). Under `ParseMany`, the item running when a layer fails, or the next item if the driver was looking for it, is `Failed` with that error and is the last result ([ADR-0038](../../docs/decisions/0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md) point 3).
- Test [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 4's example once layers run in a pipeline: a parser's mark never withdraws a diagnostic its lexer raised. ERR-04 holds this by giving each layer its own failure log, without a test ([ERR-04](../04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md) Outcome).
- `Parse` and `ParseMany` over `ReadOnlyMemory<byte>` for pipelines from bytes. When the first layer is `Layer.Utf8()` ([SPAN-06](../05-source-spans/SPAN-06-byte-sources-and-the-decoding-layer.md)), a failed or recovered result holds the text `Encoding.UTF8` decodes after any leading byte-order mark, named by `source:` and decoded once per call. Other byte pipelines hold no text ([ADR-0038](../../docs/decisions/0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md) point 5).

## Acceptance criteria

- [ ] Example A's pipeline runs end to end and reports diagnostics against the file.

## Blocked by

- [ERR-01 Error and result types](../04-errors-and-recovery/ERR-01-error-and-result-types.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)

## Unblocks

- [SPAN-07 Embedded languages](../05-source-spans/SPAN-07-embedded-languages.md)
- [LAY-07 Three-layer demo and bounded-memory check](../06-layers-and-pipelines/LAY-07-three-layer-demo-and-bounded-memory-check.md)
- [STRM-02 Restart protocol and ParseManyAsync](../08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md)
- [STRM-04 Huge single documents](../08-streaming/STRM-04-huge-single-documents.md)
- [TOOL-01 Creole.Testing assertions](../12-grammar-author-tooling/TOOL-01-creoletesting-assertions.md)
- [TOOL-02 Tracing](../12-grammar-author-tooling/TOOL-02-tracing.md)
- [INC-01 ParseSession and retained buffers](../15-incremental-reparsing/INC-01-parsesession-and-retained-buffers.md)

## Whitepaper references

- [§ Running pipelines](../../../whitepaper.md#running-pipelines)
- [§ Sources](../../../whitepaper.md#sources)

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
- [ADR-0038](../../docs/decisions/0038-pipelines-end-in-a-parser-run-again-whole-and-name-sources-without-a-registry.md): pipelines end in a parser, run again whole with fresh buffers, give each `ParseMany` item the diagnostics of the steps it reads, parse bytes in memory, and name their source without a registry

## Progress notes

- 2026-11-24 (Designing): Started. Four scope items end in a decision: registry ids or no `SourceRegistry`, whether ADR-0027's second run reuses buffered items, which `ParseMany` item holds a lexer diagnostic raised past its end, and `Parse` over bytes in memory. Settle them in one decision record before Build. Next: `/decide`.
- 2026-11-24 (Designing): Drafted ADR-0038 (Proposed). The maintainer chose forks 3–6 in conversation: diagnostics by the step that raised them, `Parse` over bytes in memory, no `SourceRegistry`, and the parser inside the pipeline. Forks 1 and 2 follow from the code: fresh buffers for the second run, and `ParseMany` records as it goes. Next: the maintainer accepts, rejects or revises; on acceptance, update the whitepaper and cards and move to Building.
- 2026-11-24 (Building): ADR-0038 accepted; the whitepaper, this card's scope, STRM-02, STRM-03, ERR-07, ERR-05 and SPAN-07 updated in the same change. Next: `/build` to plan against the criterion.
- 2026-11-24 (Building): Plan approved: five increments (remove `SourceRegistry`; `refactor:` layers read any `ICursorSource<TIn>`; pipelines over in-memory text; `TextReader` and `ParseMany`; bytes in memory). Calls: `Pipeline.From(layer)` returns the layer and `Layer.Then(parser)` makes the `Pipeline<TIn, T>`; `Parse` and `ParseMany` are extension members in `Pipeline`; a fallback error points at the parser cursor, not 0; results are `Parsed` or `Failed` as single-layer `Parse`'s are, so ADR-0038 point 4's per-item bags and the ADR-0030 point 4 test move to the first card that surfaces layer diagnostics (card lines change in increment 3).
- 2026-11-24 (Building): Increment 1 done. `SourceRegistry` removed (ADR-0038 point 6). `SourceText`'s public constructors take a `string` or `ReadOnlyMemory<char>` and a name; the id constructor is internal; `SourceId`'s docs no longer mention a registry. Tests that need several sources use the internal `new SourceId(n)`. Docs: spans.md, errors.md, the index. `dotnet build` 0 warnings; `dotnet test` 1,760 passed, 0 failed (1,763 before, less four registry tests, plus `UnnamedIsDefault`); AOT smoke OK. Next: increment 2, `/refactor` `Layer.Open` to take `ICursorSource<TIn>`.
