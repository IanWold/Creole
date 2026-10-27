# LAY-02: Marks and retention

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Building |

## Summary

Pinning bookmarks keep what may be rewound or read back; light marks pin nothing, so `Or` and repetition do not grow memory.

## Scope

- Pinning bookmarks taken by `Atomic`, `Lookahead`, `Not` and every `Longest`, compiled or not: a compiled `Longest` re-runs the winner or the interpreter from its token's start ([ADR-0017](../../docs/decisions/0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md) Consequences; § Bookmarks and retention).
- Pinning bookmarks taken by constructs that read their input back or rewind into it: `Slice`, `Text`, a `String` or `CIString` of more than one character, and `Custom`, whose scanner may keep the cursor at its start while a copy reads ahead. A one-character `String` takes none ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 1; [Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) finding 17).
- Pinning bookmarks nest, so the layer keeps one floor that each lowers to its start and restores on exit, not a count ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 3).
- An `Atomic` releases its bookmark at its exit. It may release earlier once a cut inside it is permanent: no open attempt between the cut and the `Atomic` can still fail without consuming, and no `Lookahead`, `Not` or `Longest` alternative lies between them. The engine gets no call when `Cut` runs, so earlier release needs the stack of open bookmarks and light marks; build it only if a measurement shows release at exit holds too much ([ADR-0010](../../docs/decisions/0010-cuts-under-dispatch-and-across-layers.md) point 2, which stands under [ADR-0011](../../docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md); an attempt is as ADR-0011 point 3 defines it; [Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) finding 20).
- A producing layer pins its input at the start of the step it is running, from the moment the step begins, and then at the `stepStart` of its oldest retained output. The pin belongs to the layer, not to a node, and a restart keeps it ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 2; [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 1).
- The horizon is the minimum of the cursor, the floor, the layer's pin and the restart bookmark. The layer calls `Release` with its failure log ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 3).
- Engines may skip node pins inside a producing layer's step and over input held whole in memory, so in-memory parses keep Phase 0's hot path ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 4).
- Light marks, taken before each attempt of `Or`, `Optional`, repetition (items, separators, terminators), `Expression.Build` (operators and atoms) and `Recover`, keep their two fields: the cursor position and the cut flag. The cards that add the other parts add them to marks: MODE-03 the mode log length, ERR-04 the diagnostic count and LAY-04 the layer-state reference, each with [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 9's rule for leaving it out and point 6's re-run of a `Longest` winner ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 6).

## Acceptance criteria

- [ ] Property test of guarantee 2: deeply nested `Or` without `Atomic` keeps a constant number of items per boundary.
- [ ] A layer with no modes, layer state or diagnostics takes two-field light marks, and the Phase 0 tests still pass. None of [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md)'s counterexamples can arise before those parts exist (point 11).
- [ ] Over a stream buffer with segments of one and two items, `Slice`, `Text`, `String`, `CIString` and a `Custom` scanner that scans a copy give the in-memory result when their range crosses a released segment, and a producing layer's horizon never passes the start of the step it is running ([ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md)).

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
