# LAY-06: Buffer size limit

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 1 — Layers |
| Status | Building |

## Summary

Bound each layer's buffer for untrusted input.

## Scope

- `ParseOptions.MaxBufferedItems`, a positive `int` that defaults to 16,777,216. Each layer reads it from the options `Layer.Open` receives, and bounds the buffer it reads on its own ([ADR-0039](../../docs/decisions/0039-each-layers-buffer-is-bounded-from-its-horizon-and-exceeding-it-ends-the-parse.md) points 2 and 5).
- Each time a layer pulls an item its buffer does not have, it counts the items from its horizon to the buffer's filled end, after the pull. Past the limit, the parse ends. A cursor's horizon is the minimum of its position and its floor, and the floor of a producing step's cursor starts at the layer's pin. `Select`, `Where` and `Utf8`, which read without a cursor, use their own pin. Input held whole in memory is not counted (point 1).
- Exceeding the limit ends the parse past every combinator, through `LayerFailedException` when the layer produces items. The error covers the item at the horizon, names the layer that reads the buffer, and says `ParseOptions.MaxBufferedItems` and its value (points 3 and 4).
- The check uses the horizon the reader states with each pull, which LAY-09 built ([ADR-0040](../../docs/decisions/0040-a-producing-layer-keeps-input-only-for-outputs-its-reader-can-go-back-to.md) point 2).
- No public API runs a layer until LAY-05, so the tests use internal layers and test upstreams, as LAY-02's do.

## Acceptance criteria

- [ ] A pathological `Lookahead` over huge input fails cleanly instead of exhausting memory: the parse ends at the limit with the error at the `Lookahead`'s first item, and the buffer never holds more than the limit plus two segments and one step.
- [ ] Wrapping the pinned part in `Not`, `Or` or `Atomic` does not change that outcome.
- [ ] A producing layer whose step never ends, such as one inside an unterminated comment, ends the parse at the limit with an error that names that layer.
- [ ] A limit of 1 lets a parse with no pins run to the end over an upstream that produces one item per step, and a parse over input held in memory ignores the limit.

## Blocked by

- [ERR-02 Furthest-failure tracking and labels](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)
- [LAY-02 Marks and retention](../06-layers-and-pipelines/LAY-02-marks-and-retention.md)

## Unblocks

- [LAY-07 Three-layer demo and bounded-memory check](../06-layers-and-pipelines/LAY-07-three-layer-demo-and-bounded-memory-check.md)

## Whitepaper references

- [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input)
- [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention)

## Artifacts

- [ADR-0039: Each layer's buffer is bounded from its horizon, 16,777,216 items by default, and exceeding it ends the parse](../../docs/decisions/0039-each-layers-buffer-is-bounded-from-its-horizon-and-exceeding-it-ends-the-parse.md) Accepted. Counts from the reading layer's horizon at each pull; ends the parse like `MaxDepth`; error at the horizon; one value for every buffer

## Progress notes

- 2026-11-24 (Designing): Started. Blockers ERR-02 and LAY-02 are Done. Design first: § Limits for untrusted input names the option and says exceeding it gives a diagnostic, but not whether that ends the parse, the default, what counts toward the limit or the error's span, which ADR-0020 settled for `MaxDepth` by decision. Next: draft the ADR with `/decide`.
- 2026-11-24 (Designing): Drafted ADR-0039 (Proposed). A research subagent surveyed buffer limits in Parlot, XmlReader, Kestrel, protobuf, System.Text.Json, ANTLR, Go and Jackson; Parlot 2.5's `MaxBufferedCharacters` (default `int.MaxValue`) was checked first-hand. The maintainer chose a default of 16,777,216. The record counts from the reading layer's horizon at each pull (1A), ends the parse as `MaxDepth` does (2A), reports at the horizon (4A) and keeps one value for every buffer (5A). Next: the maintainer accepts, rejects or revises it; on acceptance, apply its changes to the whitepaper and cards and move to Building.
- 2026-11-24 (Building): The maintainer accepted the record and renumbered it 0039, because LAY-04's branch uses 0037. The whitepaper (§ Limits for untrusted input, § Guarantees, § The restart protocol, § Recovery, § Equivalence), LAY-05, STRM-02, ERR-05 and GEN-06 now follow it, and this card's scope and criteria are restated from the record. LAY-09's card is unchanged, to avoid conflicting with its branch. Next: `/build` to plan against the four criteria.
- 2026-11-24 (Building): Plan approved, in two increments: (1) the option, the check in `StreamBuffer.Fill` and the error for the last reader, criteria 1, 2 and 4; (2) producing layers turn the error into `LayerFailedException` under their names (`where` and `select` for the two that have none), criterion 3. The check needs the reader to state its horizon at each pull, which LAY-09 is building; the maintainer chose to wait for that and merge it in rather than build the hook twice. Done so far, in files LAY-09 does not touch: `ParseOptions.MaxBufferedItems` (positive, default 16,777,216, tested in `ParsingTests`), `BufferLimitExceededException`, `ParseErrors.BufferLimit`, and the option in parsing.md. `dotnet build` has 0 warnings. Next: once LAY-09's build is committed and merged into this branch, give `StreamBuffer` its limit and check it in `Fill`, pass the limit through `LayerRun`, and write `BufferLimitTests` for criteria 1, 2 and 4.
- 2026-11-24 (Building): Rebased this branch onto feature/lay-09 at the maintainer's request (LAY-09 complete, 7175575); the uncommitted ADR-0039 work came back with two tracker conflicts resolved, and LAY-09's hook gives the reader's horizon at each pull. Increment 1 built: `StreamBuffer` takes `maxBufferedItems` and, after each upstream step in `Fill`, throws `BufferLimitExceededException` when the items from the stated horizon to `Filled` exceed it. `BufferLimitTests` covers criteria 1, 2 and 4 for a reader over a streamed root: a `Lookahead` over 10,000,000 items with a limit of 1,000 ends after exactly 1,003 items with the error at the `Lookahead`'s first item, for segments of 1, 2, 16 and 1,024, holding at most the limit plus two segments and one item; `Not`, `Optional`, `Or` and `Atomic` around it give the same error; the context in force is kept; a limit of 1 runs a parse with no pins to the end, and fails a step that produces two items; a parse over memory ignores the limit. Mutation check: removing the check, counting from `Start` instead of the horizon, or `>=` for `>` each fails at least four tests. Docs: the option in parsing.md, XML docs on `ParseOptions` and `StreamBuffer`. Evidence: `dotnet build` with 0 warnings; `dotnet test` with 1,777 passed and 0 failed. Until increment 2, a lower buffer's exception is not yet turned into its layer's error, so it would reach the top reader as if it were its own; no test or public API reaches that. Next: increment 2, producing layers turn the exception into `LayerFailedException` under their names, criterion 3, and layers.md.
- 2026-11-24 (Building): Increment 2, producing layers. `Tokens` and `Transduce` turn `BufferLimitExceededException` into `LayerFailedException` under their names, beside the depth catch. The `try` now also covers the cursor's `Reset` at the start of a step, and `Transduce`'s `Add`, because both can pull: `Reset` when the last step did not peek, and `Add` when it peeks for a synthetic output's span. `Where` and `Select` catch around their one pull and report as `where` and `select` (new `Name` constants; they could not fail before). `Utf8` wraps its step. Its error is at the byte pin, with the character offset of the output at the reader's horizon (new `StreamBuffer.Horizon`), or its own count when it has produced none. `LayerRun` bounds both buffers it builds and `Run` takes options. `LayerBufferLimitTests`, 13 cases: a `Tokens` and a `Transduce` step inside an unterminated string fail at the step's start under the layer's name (criterion 3); a parser's `Lookahead` over a spaced lexer trips the lexer first, at the start of the step of the `Lookahead`'s first token; `Where`, `Select` and the decoder under a pinned reader report their own limit, the decoder with character offsets; the decoder's limit inside one sequence; and the rendered line layers.md shows. Mutation check: each of the five catches, `Reset` outside the `try` in either layer, `Add` outside it, and the decoder always using its own count, each fails at least one test. The first version had no test where `Reset` pulled, which the mutation check found; two tests whose steps never peek now cover it. Docs: layers.md gains § The buffer limit and notes in § Layer.Tokens, § Layer.Transduce and § Where and Select; parsing.md links to it. Evidence: `dotnet build` with 0 warnings; `dotnet test` with 1,790 passed and 0 failed. This closes the gap increment 1 left: no layer lets a lower buffer's exception through. All four criteria now appear met: 1 and 2 by `BufferLimitTests`, 3 by `LayerBufferLimitTests`, 4 by `BufferLimitTests`. Next: the maintainer reviews and commits, then runs `/complete`.
