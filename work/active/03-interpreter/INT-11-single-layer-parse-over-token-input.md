# INT-11: Single-layer Parse over token input

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Building |

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

## Progress notes

- 2026-10-25 (Build): Started. Blocker INT-08 is Done. Build, since the card and ADR-0024 point 1 (non-text input holds no source) pin the behaviour; this reverses INT-08's agreed call to leave token input to LAY-05. Approach: a `Parse` over `ReadOnlyMemory<TIn>` for any `Parser<TIn, TOut>` that shares `Run` with the character overloads, holds no source, and keeps a spare failure log per thread and item type. Next: `/build` plans the increments.
- 2026-10-25 (Build): Plan approved: one increment; `Parse(ReadOnlyMemory<TIn> input, ParseOptions? options = null)` with no `source:`; a test pins that character parsers keep the character overloads; criterion 1 uses a hand-written lexer. Increment done. `Parsing` has an extension block on `Parser<TIn, TOut>` with that overload. The character and token overloads share a generic `TryRun`, and only the character path holds a `SourceText`. On RC 2 the generic overload made every `Parse(ReadOnlyMemory<char>)` call ambiguous (CS0121, checked in a scratch project), so that overload carries `[OverloadResolutionPriority(1)]`. The spare failure log is now one per thread and item type (`file static class SpareLog<T>`). Tests (15 new): `WorkedExampleATests` gains a hand-written lexer, `TheGrammarParsesTokensFromAHandWrittenLexer` and `AMalformedDocumentReportsTheTokenError`, which renders `error[parser] at 4..5: unexpected string "host"; expected ',' or '}'; in object starting at 0` (criterion 1). `ParsingTests` has token mirrors of the five depth tests, a nested parse, parses on 8 threads, memory slices of an array, token offsets with one-line rendering, and character memory and `char[]` still rendering against the text. `AllocationTests` has a successful token parse allocating only its result and a failed one allocating the same at 1,000 and 100,000 tokens (criterion 2). `ParseResultTests` covers the new overload. `TokenPrimitiveTests` runs the docs' `Kind` and `Case` samples. The failed-token allocation test read high only in the full parallel run: GCs forced from another thread add about 440 bytes per collection to the test thread's count whatever the input length. It now measures one parse per attempt over 20 attempts, and `Allocation.Fewest` takes an `attempts` count. Token slices were dropped from the plan: `Slice()` exists only for character parsers. Four mutations each failed tests or the build: a new log for every token parse, `MaxDepth` ignored for tokens, the priority attribute removed (CS0121), and the spare log left in place while in use. `dotnet build` with 0 warnings; `dotnet test` with 1,513 passed (1,484 in Creole.Tests) three runs in a row; the AOT smoke publishes with 0 warnings and prints OK, now including a token parse that succeeds and one that fails. Docs: parsing.md (overload table, Token input section), errors.md (offsets count items; a token parse holds no text), primitives.md (running a token rule), interpreter.md (one set of buffers per item type). Both criteria appear met; none ticked until `/complete`. Next: maintainer review, then `/complete INT-11`.
