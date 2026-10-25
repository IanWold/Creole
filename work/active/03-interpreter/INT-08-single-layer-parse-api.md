# INT-08: Single-layer Parse API

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

Phase 0 on its own is a modern single-layer library, so parsers need a public entry point before pipelines exist.

## Scope

- `parser.Parse(input, source, options)` returning `ParseResult<T>`. `source` is an optional name, and spans use `SourceId.Unnamed` ([ADR-0024](../../docs/decisions/0024-a-parses-diagnostics-hold-their-source-text.md)).
- `ParseOptions` type, shared later with pipelines.
- Attach a `FailureLog<T>` to the cursor and build the reported error from it with `ToError`, naming the layer and mapping positions to spans; ERR-02 built both. Reuse the log between parses, so that a parse allocates nothing for it.
- A `Recovered` or `Failed` result's `Diagnostics` holds a `SourceText` for the input, without copying it, so `Render()` shows the block in [§ The error model](../../../whitepaper.md#the-error-model) ([ADR-0024](../../docs/decisions/0024-a-parses-diagnostics-hold-their-source-text.md) points 1 to 4). `SourceText` gains a constructor over `ReadOnlyMemory<char>`. `Render()` writes one line per error when the collection lacks a source, and equality ignores held sources. Update [docs/concepts/errors.md](../../../docs/concepts/errors.md#one-line-per-error).
- `ParseOptions.MaxDepth`, a positive `int` that defaults to 1,000 ([ADR-0020](../../docs/decisions/0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md)). `Parse` catches INT-07's depth abort and returns `Failed`. The depth diagnostic covers the item where the level would have started (zero-width at the end of input) and names the layer. Its `CustomMessage` names `MaxDepth` and its value, or the thread's stack, and it has no expectations.
- Build the cursor for `string` and `ReadOnlyMemory<char>` input over the caller's memory (`Cursor<T>.FromMemory`), so `Slice()` returns views of it ([ADR-0013](../../docs/decisions/0013-slices-view-in-memory-input-and-copy-streamed-input.md)).
- Add each public member that returns or exposes `ParseResult<T>` to `_coveredMembers` in `tests/Creole.Tests/ParseResultTests.cs`, with a test that it never returns `default(ParseResult<T>)` ([ADR-0002](../../docs/decisions/0002-parseresult-as-a-plain-union.md); ERR-01's Outcome).

## Acceptance criteria

- [x] A character-level JSON parser can be parsed and its result switched on exhaustively.
- [x] A parse that exceeds `MaxDepth` returns `Failed` with one depth diagnostic at the offending position.
- [x] A failed parse's `Diagnostics.Render()` shows the block in [§ The error model](../../../whitepaper.md#the-error-model), naming the source given as `source:`, and copies nothing from `ReadOnlyMemory<char>` input.

## Blocked by

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [INT-07 Recursion depth limit](../03-interpreter/INT-07-recursion-depth-limit.md)
- [ERR-02 Furthest-failure tracking and labels](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)

## Unblocks

- [BENCH-01 Phase 0 speed gate](../13-benchmarks/BENCH-01-phase-0-speed-gate.md)

## Whitepaper references

- [§ Roadmap](../../../whitepaper.md#roadmap)
- [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input)
- [§ Results](../../../whitepaper.md#results-1)

## Artifacts

- [ADR-0024](../../docs/decisions/0024-a-parses-diagnostics-hold-their-source-text.md) A parse's diagnostics hold the source text they point into, and `Parse` names its source with a `source:` parameter (Accepted)

## Progress notes

- 2026-10-25 (Design): Started. Blockers INT-03, INT-07 and ERR-02 are Done. Design first: the scope asks to settle how a parse's errors render against their source, which changes either the result's shape or § A. JSON in two layers, so it needs a decision record. The rest (`ParseOptions`, the depth diagnostic, cursors over the caller's memory) is pinned by ADR-0020 and ADR-0013 and goes to the Build plan.
- 2026-10-25 (Design): Drafted ADR-0024 (Proposed). The maintainer chose 1A, so the result holds its source and `Render()` shows the block, and 2A, so `Parse(input, source, options)` takes the name as a parameter and spans use `SourceId.Unnamed`. Prior art: megaparsec and Roslyn reach the source from the error; ariadne takes it from the caller. Next: the maintainer accepts or revises the record; on acceptance, edit the whitepaper's § Results and § Streaming and async input, settle the scope lines in INT-08, LAY-05 and STRM-03, then move to Build.
- 2026-10-25 (Build): ADR-0024 accepted. Whitepaper § Results and § Streaming and async input updated. Scope gains the held source and the `source` parameter, and a third criterion covers rendering. LAY-05 and STRM-03 scope lines settled. Next: `/build` plans the increments.
- 2026-10-25 (Build): Plan approved: increment 1, diagnostics that hold their source; increment 2, `Parse`, `ParseOptions` and the depth diagnostic (criteria 1 to 3); increment 3, a failure log reused per thread, with allocation tests and the AOT smoke. Calls agreed: `SourceText.Text` becomes `ReadOnlyMemory<char>`; no token input (pipelines bring it); `Parse` adds no end check; a failing parse with nothing recorded reports at position 0. Increment 1 done. `SourceText` takes `ReadOnlyMemory<char>` and holds text without copying. `Diagnostics` has an internal constructor that holds source texts, and `Render()` uses them when they cover every error and context line, otherwise one line per error; equality ignores them. Tests: six in DiagnosticRenderingTests, one in DiagnosticsTests. Two mutations (`Render()` ignoring held sources; skipping the span-length check) each failed tests. `dotnet build` with 0 warnings; `dotnet test` with 1,463 passed; AOT smoke OK. Docs: errors.md (`Diagnostics`, Rendering against the source, `SourceText`, One line per error). Next: increment 2.
- 2026-10-25 (Build): Increment 2 done (the `Parse` API). `Parsing` has extension members on `Parser<char, TOut>`: `Parse(string, source, options)` and `Parse(ReadOnlyMemory<char>, source, options)`, each with the receiver's `[CallerArgumentExpression]`. They build the cursor with `Cursor<char>.FromMemory`, attach a new `FailureLog`, report the furthest failure from layer `parser` in `SourceId.Unnamed`, and hold the input in the `Failed` result's diagnostics. `ParseOptions` is a sealed record whose `MaxDepth` defaults to 1,000 and must be positive. A depth abort becomes one error with the item's span (zero-width at the end), the message "nesting exceeds ParseOptions.MaxDepth of N" or "nesting is too deep for the thread's stack", no expectations, and the context in force (`FailureLog.ContextAt`, which shares a loop with `ToError`). Tests: `ParsingTests` (15), including a character-level JSON grammar switched on exhaustively (criterion 1), depth errors at an item, at the end, with context and from the stack, and 1,000 levels parsing under the default while 1,001 fail (criterion 2), and rendering against the input with its name (criterion 3, rendering half). `ParseResultTests` covers both overloads, with a test that neither returns `default`. Its member scan now skips the special-name types the compiler emits for extension blocks, because their members only mark the static methods it scans, and a fixture test shows this. Four mutations each failed tests: `MaxDepth` ignored, the depth span one item late, no held source, and context read from the furthest failure. `dotnet build` with 0 warnings; `dotnet test` with 1,480 passed; AOT smoke OK. Docs: new concepts/parsing.md (indexed), the INT-08 notes on nine pages replaced by links, errors.md and interpreter.md's depth section. Criteria 1 and 2 appear met; criterion 3's no-copy half waits for increment 3's allocation test. Next: increment 3.
- 2026-10-25 (Build): Increment 3 done (the log allocates nothing). `Parsing` keeps a spare `FailureLog<char>` per thread (`[ThreadStatic]`). A parse takes it and empties the field, so a parse inside another makes its own; a `finally` clears it and puts it back. Tests: `AllocationTests.ASuccessfulParseAllocatesOnlyItsResult` (`Parse` allocates the same as `new Parsed<int>`); `AFailedParseAllocatesTheSameWhateverTheLengthOfItsInput` for `string` and `ReadOnlyMemory<char>`, at 1,000 and 100,000 characters (criterion 3's no-copy half); `ParsingTests.ANestedParseKeepsItsOwnFailures`; `ParsesOnManyThreadsAgreeWithParsesOnOne` (4,000 parses on 8 threads). Four mutations each failed tests: a new log every parse, the spare left in place while in use, the input copied on failure, and the log not cleared. The AOT smoke runs `Parse` on a `Rec` grammar: a value, a render with a source name over memory, and a depth failure. `dotnet build` with 0 warnings; `dotnet test` with 1,485 passed; AOT smoke OK. Docs: interpreter.md (What allocates) and parsing.md. Every criterion appears met on this evidence; none ticked until `/complete`. Next: maintainer review, then `/complete INT-08`.
- 2026-10-25 (Done): `/complete` verified the three criteria at commit ec2f1d7 and closed the card. BENCH-01 can start.

## Outcome

- **API** in [src/Creole/Parsing.cs](../../../src/Creole/Parsing.cs): `Parse(string input, string? source = null, ParseOptions? options = null)` and the same over `ReadOnlyMemory<char>`, as extension members on `Parser<char, TOut>`.
  - The cursor is built over the caller's memory with `Cursor<char>.FromMemory`, so slices are views of it.
  - A successful parse returns `Parsed<T>`; it need not reach the end of the input.
  - A failed parse returns `Failed` with one error: the furthest failure from layer `parser`, with spans in `SourceId.Unnamed`. Its `Diagnostics` holds the input, so `Render()` shows the full block.
  - Each thread keeps one failure log and reuses it. A parse inside another gets its own.
- **Options** in [src/Creole/ParseOptions.cs](../../../src/Creole/ParseOptions.cs): a sealed record. `MaxDepth` is positive and defaults to 1,000.
- **Depth errors.** `Parse` turns INT-07's abort into one error:
  - the span covers the item where the level would have started, zero-width at the end of input;
  - a `CustomMessage` "nesting exceeds ParseOptions.MaxDepth of N" or "nesting is too deep for the thread's stack";
  - no expectations;
  - the context lines in force there, from the new `FailureLog.ContextAt`.
- **Held sources** ([ADR-0024](../../docs/decisions/0024-a-parses-diagnostics-hold-their-source-text.md)):
  - `Diagnostics` has an internal constructor that holds `SourceText`s.
  - `Render()` uses the held texts when they cover every error and context line, and otherwise writes one line per error. It never throws.
  - Equality ignores held texts.
  - `SourceText.Text` is now `ReadOnlyMemory<char>`, with a constructor over memory; nothing is copied.
- **Tests:**
  - [ParsingTests.cs](../../../tests/Creole.Tests/ParsingTests.cs) (17): a character-level JSON grammar; depth errors at an item, at the end, with context and from a 256 KB stack; 1,000 levels under the default limit; rendering; memory input; slices as views; a nested parse; parses on 8 threads.
  - [AllocationTests.cs](../../../tests/Creole.Tests/AllocationTests.cs): a successful `Parse` allocates only its `Parsed<T>`; a failed parse allocates the same at 1,000 and 100,000 characters, from a `string` or from memory.
  - [ParseResultTests.cs](../../../tests/Creole.Tests/ParseResultTests.cs): both overloads in `_coveredMembers`, with a test that neither returns `default`. The member scan now skips the special-name types the compiler emits for extension blocks, whose members only mark the static methods it scans; a fixture test pins this.
  - [DiagnosticRenderingTests.cs](../../../tests/Creole.Tests/DiagnosticRenderingTests.cs) and [DiagnosticsTests.cs](../../../tests/Creole.Tests/DiagnosticsTests.cs): held sources.
  - The AOT smoke runs `Parse`.
  - Ten mutations across the three increments each failed tests (progress notes).
- **Evidence, 2026-10-25, commit ec2f1d7:** `dotnet build` with 0 warnings; `dotnet test` with 1,485 passed and 0 failed; the AOT smoke publish with 0 warnings, printing OK.

  | Criterion | Evidence |
  | --- | --- |
  | 1. Character-level JSON, switched on exhaustively | `ParsingTests.ACharacterLevelJsonDocumentParses`: a switch with no default arm, which would fail the build with CS8509 if a case were missing |
  | 2. `MaxDepth` exceeded gives `Failed` with one depth error at the position | `ATooDeepParseFailsWithOneDepthError`, `ADepthErrorAtTheEndOfInputIsZeroWidth`, `ADepthErrorKeepsTheContextInForce`, `RunningShortOfStackFailsWithADepthError`, `TheDefaultLimitAllowsAThousandLevels` |
  | 3. `Render()` shows the block with the source name and copies nothing from memory | `AFailedParseRendersAgainstItsInput`; `AFailedParseAllocatesTheSameWhateverTheLengthOfItsInput(inMemory: True)`; `SourceTextHoldsItsTextWithoutCopying` |
- **Docs:** new [docs/concepts/parsing.md](../../../docs/concepts/parsing.md) (indexed in docs/README.md); [errors.md](../../../docs/concepts/errors.md) (held sources, `SourceText`, one line per error); [interpreter.md](../../../docs/engines/interpreter.md) (setting the depth limit, what allocates). The "arrives with INT-08" notes on nine pages became links to the new page.
- **Decisions and agreed calls:**
  - [ADR-0024](../../docs/decisions/0024-a-parses-diagnostics-hold-their-source-text.md): results hold their source, and `Parse` takes a `source:` name. The whitepaper's § Results and § Streaming and async input were updated on acceptance.
  - Agreed with the maintainer in the build plan: `SourceText.Text` becomes `ReadOnlyMemory<char>` (ADR-0024 asks only for a constructor over memory); no token input until pipelines (LAY-05); `Parse` adds no end check; a failed parse that recorded nothing reports at position 0.
- **Known gaps:**
  - No test reaches the position-0 fallback, because no plan fails without recording a failure.
  - ERR-02 measured recording at about 40% of the time on token JSON, and `Parse` always records, so that cost is in BENCH-01's numbers. Parsing without a log and re-running on failure would avoid it, but it runs user code twice and would need a decision.
- **Follow-ups:**
  - [BENCH-01](../13-benchmarks/BENCH-01-phase-0-speed-gate.md) can start.
  - [LAY-05](../06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md) applies ADR-0024 points 1 and 5 to pipelines and brings token input.
  - [STRM-03](../08-streaming/STRM-03-streaming-diagnostics.md) makes `Render()` use captured lines.
