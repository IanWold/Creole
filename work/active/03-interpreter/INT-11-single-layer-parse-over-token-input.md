# INT-11: Single-layer Parse over token input

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

Let a token parser run over tokens held in memory, so Phase 0's token grammars can be used before pipelines exist.

## Scope

- `Parse` exists only for `Parser<char, TOut>` (`src/Creole/Parsing.cs`). `TokenGrammar`, `Kind`, `Case<T>()` and `IKinded` are public, but nothing public runs them.
  - § Roadmap says "Phase 0 alone is a modern single-layer library", and G1 covers tokens and any `T`.
  - A fresh user's attempt to run a token grammar failed with CS1929 (the [Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md), finding 22).
- Add `Parse` over in-memory input of any item type, such as `ReadOnlyMemory<TIn>`. It takes the same options as character input and reports failures and depth limits the same way. `FailureLog<T>`, `Cursor<T>.FromMemory` and `Display<T>` are already generic.
- Token input has no source text. Under [ADR-0024](../../docs/decisions/0024-a-parses-diagnostics-hold-their-source-text.md) a non-text input holds no source, so an error carries item positions and renders without a source line. Document that spans count tokens until SPAN-02 gives tokens spans of their own.
- Make the token samples in docs/concepts/primitives.md, errors.md and parsing.md run.
- Keep the name and shape consistent with the pipeline `Parse(text, source:)` that LAY-05 adds.

## Acceptance criteria

- [ ] Example A's `JsonGrammar` runs through the public API over tokens from a hand-written lexer. A malformed input reports the token error the docs show.
- [ ] The depth limit and the allocation guarantees hold for token input, shown by tests that mirror the character-input ones.

## Blocked by

- [INT-08 Single-layer Parse API](INT-08-single-layer-parse-api.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Design goals and non-goals](../../../whitepaper.md#design-goals-and-non-goals)
- [§ Roadmap](../../../whitepaper.md#roadmap)
- [§ A. JSON in two layers](../../../whitepaper.md#a-json-in-two-layers)

## Artifacts

- [Research: Phase 0 review](../../docs/research/2026-10-25-phase-0-review.md) Eight reviewers and two critics on what is unnecessary, what could be simpler, where decisions drifted and what was missed, with gate measurements
