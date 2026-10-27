# LAY-02: Marks and retention

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Done |

## Summary

Pinning bookmarks keep what may be rewound or read back; light marks pin nothing, so `Or` and repetition do not grow memory.

## Scope

- Pinning bookmarks taken by `Atomic`, `Lookahead`, `Not` and every `Longest`, compiled or not: a compiled `Longest` re-runs the winner or the interpreter from its token's start ([ADR-0017](../../docs/decisions/0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md) Consequences; § Bookmarks and retention).
- Pinning bookmarks taken by constructs that read their input back or rewind into it: `Slice`, `Text`, a `String` or `CIString` of more than one character, and `Custom`, whose scanner may keep the cursor at its start while a copy reads ahead. A one-character `String` needs none ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 1; [Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) finding 17).
- Pinning bookmarks nest, so the layer keeps one floor that each lowers to its start and restores on exit, not a count ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 3).
- An `Atomic` releases its bookmark at its exit. It may release earlier once a cut inside it is permanent: no open attempt between the cut and the `Atomic` can still fail without consuming, and no `Lookahead`, `Not` or `Longest` alternative lies between them. The engine gets no call when `Cut` runs, so earlier release needs the stack of open bookmarks and light marks; build it only if a measurement shows release at exit holds too much ([ADR-0010](../../docs/decisions/0010-cuts-under-dispatch-and-across-layers.md) point 2, which stands under [ADR-0011](../../docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md); an attempt is as ADR-0011 point 3 defines it; [Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) finding 20).
- A producing layer pins its input at the start of the step it is running, from the moment the step begins, and then at the `stepStart` of its oldest retained output. The pin belongs to the layer, not to a node, and a restart keeps it ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 2; [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 1).
- The horizon is the minimum of the cursor, the floor, the layer's pin and the restart bookmark. The layer calls `Release` with its failure log ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 3).
- Engines may skip node pins inside a producing layer's step and over input held whole in memory, so in-memory parses keep Phase 0's hot path ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 4).
- Light marks, taken before each attempt of `Or`, `Optional`, repetition (items, separators, terminators), `Expression.Build` (operators and atoms) and `Recover`, keep their two fields: the cursor position and the cut flag. The cards that add the other parts add them to marks: MODE-03 the mode log length, ERR-04 the diagnostic count and LAY-04 the layer-state reference, each with [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 9's rule for leaving it out and point 6's re-run of a `Longest` winner ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 6).

## Acceptance criteria

- [x] Property test of guarantee 2: deeply nested `Or` without `Atomic` keeps a constant number of items per boundary.
- [x] A layer with no modes, layer state or diagnostics takes two-field light marks, and the Phase 0 tests still pass. None of [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md)'s counterexamples can arise before those parts exist (point 11).
- [x] Over a stream buffer with segments of one and two items, `Slice`, `Text`, `String`, `CIString` and a `Custom` scanner that scans a copy give the in-memory result when their range crosses a released segment, and a producing layer's horizon never passes the start of the step it is running ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md)).

## Blocked by

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [LAY-01 StreamBuffer and pulling cursor](../06-layers-and-pipelines/LAY-01-streambuffer-and-pulling-cursor.md)
- [LAY-08 Per-layer parse state](LAY-08-per-layer-parse-state.md)

## Unblocks

- [ERR-04 Diagnostic withdrawal](../04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md)
- [LAY-04 Layer-local state](../06-layers-and-pipelines/LAY-04-layer-local-state.md)
- [LAY-06 Buffer size limit](../06-layers-and-pipelines/LAY-06-buffer-size-limit.md)
- [MODE-02 Invalidation rule](../07-mode-channels/MODE-02-invalidation-rule.md)
- [STRM-01 Starved outcome](../08-streaming/STRM-01-starved-outcome.md)
- [ANA-06 Deterministic mode](../09-static-analysis/ANA-06-deterministic-mode.md)
- [LAY-09 Hold only the input the reader can still go back to](LAY-09-hold-only-the-input-the-reader-can-still-go-back-to.md)

## Whitepaper references

- [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention)
- [§ Guarantees](../../../whitepaper.md#guarantees)

## Artifacts

- [Research: Parlot 2.0 baselines and generator](../../docs/research/2026-10-24-parlot-2-0-baselines-and-generator.md) Prior art: Parlot 2.0.0 pins only where a parser can move back, and `Commit()` releases buffered text (finding 23)
- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
- [ADR-0031: Constructs that read their input back pin their start, and a producing layer pins the step it is running](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) Accepted. `Slice`, `Text` and longer `String` and `CIString` pin; the layer pins its running step; node pins may be skipped in producing layers and over in-memory input

## Progress notes

- 2026-10-24 (Not started): FND-10 research found Parlot 2.0.0 already bounds a streamed parse's buffer with pins and a backtrack floor (note, finding 23). Read it before designing retention. Next: unchanged.
- 2026-10-27 (Designing): Started. Blockers INT-03, LAY-01 and LAY-08 are Done. Design first because the scope's read-behind retention (Phase 0 review finding 17) changes § Guarantees point 2 and needs a decision record before the build. Next: draft the ADR with `/decide`.
- 2026-10-27 (Designing): Drafted ADR-0031 (Proposed): read-back constructs pin their start (1B over copy at release), a producing layer pins the step it is running from its start (2C, closing a gap where a mid-step release could free an emitted item's `stepStart`), one floor per layer instead of a count, pins skippable in producing layers and over in-memory input, guarantee 2 restated, and the ADR-0030 mark parts moved to MODE-03 and ERR-04. Next: the maintainer accepts, rejects or revises it; on acceptance, apply its Changes made and move to Building.
- 2026-10-27 (Building): The maintainer accepted ADR-0031 as written. The whitepaper (§ Bookmarks and retention, § Guarantees point 2, § Text capture), this card's scope and criteria, MODE-03, ERR-04 and GEN-06 now follow it. LAY-03 is not done, so the build gives the layer's step pin a hook tested with test upstreams, as LAY-01's tests do; whichever of LAY-02 and LAY-03 lands second wires `Layer.Tokens` and `Transduce` to it. Next: `/build` to plan against the three criteria.
- 2026-10-27 (Building): Plan approved, four increments: (1) ADR-0031 edited in place to pin `Custom`, since the documented scanner pattern keeps the cursor at its start while a copy reads ahead; (2) pins with `Cursor.Floor`, release on each segment load, then the speed gate; (3) the producing layer's step pin; (4) guarantee 2's property test. Increment 1 done: ADR-0031, the whitepaper (§ Bookmarks and retention, § Guarantees point 2, § Custom primitives), GEN-06 and this card's scope and third criterion. Next: increment 2.
- 2026-10-27 (Building): Increment 2, pins and release. `Cursor.Floor` with `Pin` and `Unpin`. Each pin saves the floor, lowers it to the cursor and puts it back, so there is no count. When a cursor enters a new segment, its source releases below the minimum of the cursor and the floor (`ICursorSource.Release`). Pins in `Atomic`, `Lookahead`, `Not`, `Longest`, the compiled `Longest`, `Slice`, `Text`, `String` (which covers `CIString`) and `Custom`. One-character strings pin too: ADR-0031 allows skipping it, and nothing measured asks for it. `RetentionTests` runs each construct after a run of input already released, over segments of 1 and 2, with a pool that overwrites returned arrays, and compares it with in-memory input. LAY-01's depth-3 control-shape test now uses that pool and releases as it goes. Three buffer tests that read back after a bare cursor run start with `Floor = 0`. Mutation check: dropping any one pin, or the release call, fails at least one test. A first version released on every `LoadSegment`, which runs once per item when the upstream fills one item per step. Two allocation tests then failed every full run: the shared pool trimmed itself on gen-2 collections, at about 86% memory load on this machine, so returned arrays were lost before reuse. The base commit fails the same two tests now and then under that load: one miss each, 63 gen-2 collections. Releasing only on entering a segment brought this branch back to the base's rate. A floor added to `TryPeekAt`'s copy was dead code, because that copy moves with `Reset` and never releases, so it was removed with its sentence in ADR-0031 point 1. Docs: sequencing.md (capture memory), custom-scanners.md (copies may read ahead). Evidence: `dotnet build` with 0 warnings; `dotnet test` with 1,600 passed and 0 failed; the same change applied to main, with LAY-03, passes 1,634 of 1,634. Speed gate on d0dd1ad plus this change, three runs (20261027-204831, -205610, -210019): passes, at 0.24× Pidgin and 1.39× Parlot geometric mean (BENCH-01: 0.24 and 1.37). JSON small 1.56 (BENCH-01 1.46), medium 1.45 (1.45), large 1.66 (1.70), arithmetic 1.00 (1.00). Next: merge main, which now has LAY-03, then increment 3: the step pin in `TokensLayer` and `TransduceLayer`.
- 2026-10-27 (Building): Increment 3, the producing layer's pin, built on LAY-03's layers after merging main. `StreamBuffer.OldestStepStart` gives the `stepStart` of the oldest item still held. Each step's cursor in `TokensLayer` and `TransduceLayer` starts with its floor there, or at the step's own start when nothing is held. Found while building: `Select` and `Where` read their input with `TryGetSegment` and no cursor, so nothing released their input, and a boundary below them would have grown with the input. Each now releases its input on reaching a new segment, down to the oldest held output's `stepStart`. `LayerRetentionTests` runs `Tokens`, `Transduce`, `Select` and `Where` over segments of 1 and 2 with overwriting pools, with and without a reader that pins the first item. It checks that every held item's step start is still held after each read, that spans equal the unreleased run's, and that input was released. Mutation check: dropping or narrowing each layer's floor or release fails it (8 of 8). Docs: layers.md, on what the buffer and the layer below keep. Evidence: `dotnet build` with 0 warnings; `dotnet test` with 1,635 passed and 0 failed. The gate was not re-run: its benchmarks are single-layer parses and never reach the layers. Criterion 3 now has evidence for both halves (`RetentionTests`, `LayerRetentionTests`). Next: increment 4, guarantee 2's property test and criterion 2.
- 2026-10-27 (Building): Increment 4, guarantee 2. `BoundedMemoryTests` uses CsCheck, 50 samples each, over seeds, nesting depths 1 to 64, inputs of 1,000 to 20,000 characters and segments of 1, 2 and 16. Each sample is a random grammar of nested choices with no pinning construct, and a long input it matches to the end. A single-layer parse holds at most two segments and one item, 2S + 1. Under a one-character lexer (`Tokens` and `Transduce`), the token boundary holds 2S + 1 and the character boundary 3S + 2. Found by the test: a producing step reaches its first segment through `Reset`, which never releases, so a lexer whose steps each start a new segment released nothing. `TokensLayer` and `TransduceLayer` now release their input below the floor at the start of each step. Mutation check: removing the cursor's release, either layer's step-start release, or wrapping the grammar in `Atomic` each fails the test. Criterion 2: `LightMark.cs` is unchanged on this branch (`git diff 5b2e40a` is empty); it holds the position and the cut flag. Evidence: `dotnet build` with 0 warnings; `dotnet test` with 1,637 passed and 0 failed. The gate was not re-run, since only the layers changed. All three criteria now appear met: 1 by `BoundedMemoryTests`, 2 by `LightMark.cs` and the suite, 3 by `RetentionTests` and `LayerRetentionTests`. Next: the maintainer opens the PR and runs `/complete`.
- 2026-10-27 (Building): Completion review. The critic found all three criteria met, and its mutation checks agreed with these. It also found three weaknesses in the tests, all fixed. Choices now use `OneOf` over 2 to 5 alternatives, since chained `.Or` never reached the dispatch threshold; making `DispatchPlan` pin now fails the test. Half the sentences take each choice's deepest alternative, where before the inputs reached only about 16 levels. The `Lookahead` case no longer contains a `Text` that hid a missing pin; dropping `Lookahead`'s pin now fails `RetentionTests`. Significant finding: guarantee 2's wording from ADR-0031 point 5 promised less than a producing layer keeps when its steps produce nothing. The maintainer chose to restate it, in ADR-0031 in place and the whitepaper, and to add LAY-09 for the narrower pin.

## Outcome

Pinning bookmarks and retention for streamed input, as [ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) decides. The design stage produced ADR-0031; the build had four increments.

- **Pins**, in [Cursor.cs](../../../src/Creole/Cursor.cs):
  - `Floor`, with `Pin` and `Unpin`. Each pin saves the floor, lowers it to the cursor and restores it on exit. Pins nest, so no count is kept.
  - These plans pin their start while they run: `Atomic`, `Lookahead`, `Not`, `LongestPlan`, `DfaLongestPlan`, `Slice`, `Text`, `String` (which covers `CIString`) and `Custom`. A copy of the cursor carries the floor, so a scanner that scans a copy is safe.
  - `Atomic` releases at its exit; earlier release was not built, as the scope allows.
  - One-character strings pin too: ADR-0031 lets them skip it, and nothing measured asks for it.
- **Release:**
  - A cursor releases its source below the minimum of its position and its floor when it enters a new segment (`ICursorSource.Release`).
  - Producing layers, in [src/Creole/Layers/](../../../src/Creole/Layers/):
    - `TokensLayer` and `TransduceLayer` start each step's cursor with its floor at `StreamBuffer.OldestStepStart`, the `stepStart` of the oldest output still held, or at the step's own start if none is held. Each step starts by releasing below that floor.
    - `Select` and `Where` read without a cursor, so each releases its input itself on reaching a new segment.
  - Restart bookmarks are STRM-02's. The skipping that ADR-0031 point 4 allows was not built, because the speed gate shows no need.
- **Light marks** are unchanged: two fields, the position and the cut flag. Mode log length, diagnostic count and layer state go to MODE-03, ERR-04 and LAY-04, which now carry ADR-0030 points 6 and 9 for their parts.
- **Tests:**
  - [BoundedMemoryTests.cs](../../../tests/Creole.Tests/BoundedMemoryTests.cs) (CsCheck, 2 tests):
    - It generates random grammars of nested choices, 1 to 64 deep, with dispatched and undispatched choices and no pinning construct. Inputs run from 1,000 to 20,000 characters, over segments of 1, 2 and 16.
    - A single layer holds at most 2S + 1 items, where S is the segment size.
    - Under a one-character `Tokens` or `Transduce` lexer, the token boundary holds at most 2S + 1 and the character boundary 3S + 2.
  - [RetentionTests.cs](../../../tests/Creole.Tests/RetentionTests.cs) (4): every pinning construct, after input already released, over segments of 1 and 2. It uses a pool that overwrites returned arrays ([OverwritingPool.cs](../../../tests/Creole.Tests/Utilities/OverwritingPool.cs)), compares against in-memory input, and covers a cursor copy keeping the floor.
  - [LayerRetentionTests.cs](../../../tests/Creole.Tests/LayerRetentionTests.cs) (1): `Tokens`, `Transduce`, `Select` and `Where`, with and without a reader that pins the first item. After each read, every held item's step start is still held.
  - LAY-01's control-shape test now releases as it goes.
  - Mutation checks by this card and by an independent critic: dropping any pin, the cursor's release, any layer's floor or release, or making `Or` or `DispatchPlan` pin each fails a test.
- **Evidence, 2026-10-27, at a35e062 plus the completion fixes:**
  - `dotnet build`: 0 warnings. `dotnet test`: 1,637 passed, 0 failed.
  - Speed gate after increment 2: three runs, passes at 0.24× Pidgin and 1.39× Parlot (BENCH-01: 1.37×). Increments 3 and 4 changed only layers, which the gate's single-layer benchmarks do not reach.

  | Criterion | Evidence |
  | --- | --- |
  | 1. Guarantee 2 property test | `AParseOfDeeplyNestedChoicesHoldsTwoSegmentsAndOneItem` and `ALexerAndAParserOfDeeplyNestedChoicesHoldAConstantNumberOfItemsAtEachBoundary`. The bounds depend only on segment size, never on depth or length. Wrapping the grammar in `Atomic`, or pinning in `OrPlan` or `DispatchPlan`, fails them |
  | 2. Two-field light marks; Phase 0 tests pass | `LightMark.cs` is unchanged on this branch. No mode, diagnostic or layer-state node exists in `src`, so none of ADR-0030's counterexamples can arise. The full suite passes |
  | 3. Read-back constructs and the producing layer's horizon | `EveryConstructThatGoesBackGivesTheInMemoryResultOverReleasedSegments` and `EveryProducingLayerKeepsTheInputOfItsRunningStepAndOfEveryItemItsReaderHolds`. Each named construct's and each layer's mutation fails them |
- **Docs:** [sequencing.md](../../../docs/concepts/sequencing.md) (what a capture holds), [custom-scanners.md](../../../docs/concepts/custom-scanners.md) (a copy may read ahead), [layers.md](../../../docs/concepts/layers.md) (what a layer keeps). No public API changed. XML docs cover the new internal members.
- **Deviations:**
  - ADR-0031 was edited in place before merge three times: `Custom` pins; a dead `TryPeekAt` floor was removed; guarantee 2 was restated. Its closing note records each.
  - § Guarantees point 2 now says that a run of input producing nothing is held while an earlier item is held.
- **Found and fixed during the build:**
  - Releasing on every segment load freed pooled arrays early.
  - `Select` and `Where` never released their input.
  - A step reaching a new segment through `Reset` never released.
- **Follow-ups:**
  - [LAY-09](LAY-09-hold-only-the-input-the-reader-can-still-go-back-to.md): hold only the input the reader can still go back to, so a filter that drops most of its input stays bounded.
  - Two allocation tests, `ReadingAndReleasingAsItGoesAllocatesNothingPerItem` and `ReadingSpansPast4GBAllocatesNothingPerItem`, fail now and then in full runs on this machine at high memory load: the shared pool trims itself on gen-2 collections. They do on the base commit too.
