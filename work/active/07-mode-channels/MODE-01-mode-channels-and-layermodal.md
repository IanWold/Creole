# MODE-01: Mode channels and Layer.Modal

| | |
| --- | --- |
| Epic | [Mode channels](../../README.md#mode--mode-channels) |
| Phase | Phase 1 — Layers |
| Status | Building |

## Summary

The parser can switch an upstream layer's lexing mode through a typed channel.

## Scope

- `ModeChannel<TMode>` as a typed key with an initial mode. Each run keeps a stack per channel, starting as the initial mode alone, in the parse state of the layer that writes it ([ADR-0041](../../docs/decisions/0041-a-channel-has-one-reader-below-one-writer-and-a-write-nothing-reads-or-a-pop-of-the-last-mode-throws.md) points 1 and 2).
- `Layer.Modal` in three forms: `(channel, selector, name)`, `(channel, selector, skip, name)` and `(initialState, channel, selector, skip, name)`, with one `skip` for every mode. A step reads the top of the stack and otherwise runs as a `Layer.Tokens` step does. The stateful form follows [ADR-0037](../../docs/decisions/0037-lexers-take-an-initial-state-parsers-set-theirs-and-a-mistyped-read-throws.md) points 2 to 5. ADR-0041's spike found no CS0121; the tests compile its calls (points 5 and 7).
- The selector runs at most once per distinct mode for the life of the layer, also under parses on several threads, and a null result throws `GrammarException` naming the mode (point 6).
- `SetMode`, `PushMode` and `PopMode` on `Grammar<TIn>` as zero-width nodes producing `Unit`. `PopMode` of the last mode throws `GrammarException` (points 2 and 8).
- One reader per chain, checked when `Then` builds it. One writing layer per run, the first to write. In a pipeline, a write to a channel that no layer below the writer reads throws `GrammarException`. A parser run on its own keeps a stack nothing reads (points 3 and 4).
- Under `ParseMany`, the parser's channels start each item in their initial modes (point 9).
- Compare a `Layer.Modal` with one mode against the same `Layer.Tokens` with `scripts/bench-compare.py` (ADR-0041 § Consequences).
- Out: discarding items already lexed ([MODE-02](MODE-02-invalidation-rule.md)) and undoing changes on a rewind ([MODE-03](MODE-03-rollback-rule.md)). A change affects the steps run after it (point 10).

## Acceptance criteria

- [ ] Selector caching verified: each mode's item parser is planned once.
- [ ] Each `GrammarException` case of ADR-0041 points 2 to 4 throws, naming the channel: a pop of the last mode, two readers in a chain, a second writing layer, and a write in a pipeline that no layer below reads. A parser that writes a channel parses on its own over a list of tokens. Under `ParseMany`, an item that follows one that changed the mode starts in the initial mode.

## Blocked by

- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)

## Unblocks

- [MODE-02 Invalidation rule](../07-mode-channels/MODE-02-invalidation-rule.md)
- [MODE-05 Mode payloads](../07-mode-channels/MODE-05-mode-payloads.md)
- [ANA-06 Deterministic mode](../09-static-analysis/ANA-06-deterministic-mode.md)
- [RSLN-01 Roslyn grammar reader](../10-roslyn-analyzers/RSLN-01-roslyn-grammar-reader.md)

## Whitepaper references

- [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels)
- [§ Defining layers](../../../whitepaper.md#defining-layers)

## Artifacts

- [ADR-0041](../../docs/decisions/0041-a-channel-has-one-reader-below-one-writer-and-a-write-nothing-reads-or-a-pop-of-the-last-mode-throws.md) (Accepted): one reader below one writing layer per channel, a write nothing reads in a pipeline or a pop of the last mode throws, each `ParseMany` item starts in the initial mode, one shared `skip`.

## Progress notes

- 2026-11-25 (Design): Started. The whitepaper fixes the API shape but not several run-time behaviours users will see: how a writer reaches its reader and what a write with no reader below does, `PopMode` on the last mode, two readers or two writers on one channel, whether each `ParseMany` item starts in the initial mode, and whether `skip` can differ by mode. Next: a decision record via `/decide`.
- 2026-11-25 (Design): Drafted ADR-0041 (Proposed). The maintainer chose A1, B1, C1 and E1; D1 is recommended as following ADR-0030 point 1. The overload spike on SDK 11.0.100 found no CS0121 across the three `Modal` forms. Next: the maintainer accepts, rejects or revises it; on acceptance, `/decide accept 0041`.
- 2026-11-25 (Build): ADR-0041 accepted with A1, B1, C1, D1 and E1. Whitepaper updated (§ 3. Mode channels, § Defining layers, § 2. Layer-local state, § Running pipelines); MODE-02 and MODE-05 gained scope lines. Scope rewritten and a second criterion added. Next: plan the build against both criteria.
- 2026-11-25 (Build): Plan approved: four increments (a refactor so layers open with a run context; mode channels and nodes for a parser on its own; `Layer.Modal` and the wiring in pipelines; a benchmark of a one-mode `Layer.Modal` against `Layer.Tokens`). Calls: an immutable linked stack, with a prebuilt one-mode stack per `SetMode` plan; a `SetMode` to the mode on top still replaces it, and MODE-02 decides whether that is a change; messages name the channel by its mode type and the layer by the `LayerStateException` route; a `Layer.Modal` that writes its own channel throws; marks save no mode state until MODE-03. Increment 1 done (refactor): `Layer.Open(input, options, recordFailures)` became `Open(input, PipelineRun run)` in the abstract method, the seven layer types, `LayeredPipeline` and three test callers; `LayerRetentionTests` and `BoundedMemoryTests` open each chain with one run. Named `PipelineRun`, not the planned `LayerRun`, because the tests already have a `LayerRun` helper. `dotnet build` 0 warnings; `dotnet test` 1,890 passed, no expected value changed. Next: increment 2, channels and mode nodes for a parser on its own.
- 2026-11-25 (Build): Increment 2 done: channels and mode nodes for a parser run on its own. Public `ModeChannel<TMode>` (initial mode; `ToString` names it by its mode type) and `SetMode`, `PushMode` and `PopMode` on `Grammar<TIn>`, each a leaf node of a new kind with a zero-width plan. Internals: an immutable `ModeStack<TMode>`; `ModeLink<TMode>`, one channel's stack in one run; `ModeWriter`, the channels one layer writes, which a parser on its own makes on its first change of mode, kept on a new `Cursor.Modes`. `SetMode` over a stack of one mode reuses a stack built with its plan. Popping the last mode throws `GrammarException` naming the layer and channel (ADR-0041 point 2). Analysis: the three kinds are not regular and give an alternative no dispatch keys. Marks save no mode state (MODE-03). Tests: `ModeNodeTests` (8) and `ModeChangeTests` (9), plus three `RegularTests` cases; they cover stack semantics through `PopMode`'s check, separate stacks per channel instance, a fresh stack per parse, a token parser that writes a channel parsing on its own over tokens, and no allocation for `SetMode` over one mode. Five mutations each failed a test: `SetMode` pushing, `SetMode` allocating, a pop of the last mode ignored, channels of one mode type sharing a stack, and `SetMode` counted as regular. Docs: new `docs/concepts/mode-channels.md`, its sample checked by `ModeChangeTests`; the docs index; `interpreter.md`'s regular list and allocation table. `dotnet build` 0 warnings; `dotnet test` 1,910 passed; AOT smoke publishes and prints OK. Next: increment 3, `Layer.Modal` and the wiring in pipelines.
- 2026-11-25 (Build): Increment 3 done: `Layer.Modal` and channels in pipelines. Three public forms, `(channel, selector, name)`, `(channel, selector, skip, name)` and `(initialState, channel, selector, skip, name)`, backed by `ModalLayer`, which runs through the step loop `Layer.Tokens` uses (`TokensUpstream` now asks for its item plan at each step). The selector runs under a lock at most once per distinct mode for the life of the layer, and each run keeps an unlocked list of the plans it has used. `PipelineRun` collects the channels read as layers open from the bottom up; each layer that runs a grammar, and the parser, gets a `ModeWriter` holding the channels read below it. A write to any other channel throws, the first writing layer claims a channel and a second throws, `Then` rejects two readers of one channel, a null selector result throws, and `ParseMany` resets the parser's channels before each item. Tests: `ModalLayerTests` (13), covering criterion 1 (64 parallel and 16 sequential parses: two selector calls, each mode's `CountingNode` lowered once) and the pipeline parts of criterion 2, plus a test that binds each form of ADR-0041's spike to its overload. Seven mutations each failed a test: the layer cache never hitting, the reader registered before its own writer, no reset under `ParseMany`, no second-writer check, no two-reader check, the run cache returning one plan for every mode, and a write with no reader keeping its own stack. Docs: `mode-channels.md` (reading a channel, who reads and writes, where each parse starts), `layers.md`, `layer-state.md`, `pipelines.md` and the docs index; the AOT smoke runs a `Layer.Modal` pipeline. `dotnet build` 0 warnings; `dotnet test` 1,923 passed; AOT smoke publishes and prints OK. Both criteria appear met; not ticked until `/complete`. Next: increment 4, the benchmark of a one-mode `Layer.Modal` against `Layer.Tokens`.
