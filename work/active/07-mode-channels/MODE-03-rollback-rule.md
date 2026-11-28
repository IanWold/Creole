# MODE-03: Rollback rule

| | |
| --- | --- |
| Epic | [Mode channels](../../README.md#mode--mode-channels) |
| Phase | Phase 1 — Layers |
| Status | Done |

## Summary

Rewinding any mark undoes mode changes made after it.

## Scope

- Bookmarks and light marks record the mode log length.
- Marks save the mode log length only in a layer whose grammar contains `SetMode`, `PushMode`, `PopMode` or `Bind`. A `Longest` whose winner can change the mode log runs the winner again when another alternative ran after it ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) points 6 and 9; [ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 6).
- Undo log entries newest first, applying invalidation at each entry's index.
- `Or` undoes zero-width mode changes when an alternative fails without consuming.
- An undo lowers the furthest failure at the entry's index, as a change does. A rewind withdraws diagnostics, then undoes mode changes, then restores any saved failure, which is lowered by the changes still in effect ([ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) points 2 and 3).
- An undo discards and lowers only when it changes the mode on top back, as a write does only when it changes it ([ADR-0042](../../docs/decisions/0042-a-write-discards-only-when-it-changes-the-top-mode-and-the-mode-log-keeps-only-what-a-rewind-can-reach.md) point 1).
- Marks keep absolute log lengths. A full log drops its entries below the lower of the cursor and the floor (ADR-0042 point 5), so the log counts the entries it has dropped.
- A save keeps the position of the first change made after it, and its restore is lowered there (ADR-0042 point 3). Undoing that change clears the position, so the restore is not lowered by a change no longer in effect.
- Property tests check that no rewind reaches a dropped entry and that a drop removes nothing a rewind needs. Before this card nothing reads the log after a write, so MODE-02's tests cannot show a drop that removes too much (ADR-0042 § Consequences).

## Acceptance criteria

- [x] Property tests with random `SetMode` placement inside `Atomic`, `Lookahead` and `Or` agree with a from-scratch re-lex.
- [x] Tests from [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 11: `Not` around a mode change, with a failure inside it before the change, leaves the record as it found it; option 2B's example gives a failure at *i* with no expectations, which an enclosing `Named` labels; an undo after a successful `Longest`, after a filter's rejection and after a `Named` relabel each report the next alternative's expectation; `Or(Seq(SetMode(Operand), Kind(RegexLit)), Kind(Slash))` on `/x/` gives the same result and error with dispatch on and off; and counterexample 6, with a `Longest` alternative that changes mode.

## Blocked by

- [MODE-02 Invalidation rule](../07-mode-channels/MODE-02-invalidation-rule.md)

## Unblocks

- [MODE-06 Model-based tests for mode rollback](../07-mode-channels/MODE-06-model-based-tests-for-mode-rollback.md)
- [REL-01 Worked-example samples](../14-release/REL-01-worked-example-samples.md)
- [INC-03 Layer item reuse](../15-incremental-reparsing/INC-03-layer-item-reuse.md)

## Whitepaper references

- [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels)
- [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention)

## Progress notes

- 2026-11-27 (Build): Started. ADR-0030 points 2, 3, 6 and 9, ADR-0031 point 6 and ADR-0042 points 1, 3 and 5, all Accepted, pin down the design, and the whitepaper has nothing open. Approach: marks save the mode log's absolute length in grammars with mode nodes or `Bind`, rewinds undo entries newest first and discard and lower only when the top changes back, and a `Longest` winner that can change the log runs again. Next: plan the build with `/build`.
- 2026-11-27 (Build): Plan approved: three increments (undoing a change in the failure log; the undo itself, with marks, discards and ADR-0030 point 11's tests; property tests against a reference model that lexes from scratch). Calls: mode nodes select `FullMark` rather than a third mark type; `Longest`'s re-run stays decided per grammar; an undo that would reach a dropped entry throws; a test node lowers a pipeline's parser with chosen options; `ModeWriter.Return` must reset its dropped count. Increment 1 done: `FailureLog.ChangeAt` returns how many open saves a change had already followed, and `UndoChangeAt(index, followed)` lowers at the index and takes the change, and the item copied there, back from the open saves from `followed` on, so two changes at one index clear only the right saves. No callers yet: the log entry that stores the count comes in increment 2 with its reader. `FailureLoweringTests` (+6); six mutations each failed a test, after one survivor showed the copy test released too little. `dotnet build` 0 warnings; `dotnet test` 2,268 run, 3 failed, none in this change: `NodeModelTests.NodeTypesAreInternalAndSealed` fails since `fad0ba5` unsealed node classes, and two `StreamBufferTests` allocation tests fail under the full suite's load but pass alone. Next: increment 2, the undo.
- 2026-11-27 (Build): Increment 2 done: the undo. `FullMark` notes `ModeWriter.Length` (entries kept plus dropped), and its `Rewind` withdraws diagnostics, then undoes mode changes with `ModeWriter.UndoTo`, then restores state; `SetMode`, `PushMode` and `PopMode` now select `FullMark`, so `Longest` runs its winner again in modal grammars. `UndoTo` undoes entries newest first; an entry that changed the top calls `FailureLog.UndoChangeAt` with the count `ChangeAt` returned, which each log entry now keeps, and discards through `Cursor.Discard`, which now takes a position. It throws if it would reach a dropped entry. `Atomic`, `Lookahead`, `Not` and `Longest` reset the position before they rewind, so the cursor stands at or before every change undone. `ModeWriter.Return` resets the dropped count; without it, a parser on its own after one whose log dropped entries threw on its first undo. Tests: `ModeRollbackTests` (23: each kind of attempt, `Atomic`, `Lookahead`, `Not`, `Longest` both ways (counterexample 6), push and pop, newest first, an undone change not undone again, an undo that keeps the mode; ADR-0030 point 11's tests: `Not` around a change, 2B with and without `Named`, an undo after a `Longest`, a rejection and a relabel, dispatch on and off; the docs sample), each in memory and over one-item segments; `ModeChangeTests` (+1, the spare's dropped count). Changed: `ModeNodeTests` expects `FullMark` for a grammar with a mode node; `WorkedExampleCTests`' rejection case now reports the slash read again in operator mode, because its failed alternative's change is undone (ADR-0030 point 3). Twelve mutations each failed a test, after two aimed at `Restart` by mistake were re-aimed and one survivor (undone entries kept in the log) gained a test. Docs: `mode-channels.md` § Going back and two bullets under § What a change does to errors; `interpreter.md` § When a Longest compiles. `dotnet build` 0 warnings; `dotnet test` 2,292 run, the same 3 failures as increment 1 and no others; AOT smoke OK. Criterion 2 appears met; not ticked until `/complete`. Next: increment 3, the property tests.
- 2026-11-27 (Build): Increment 3 done: the property tests. `ModeModel` (tests/Creole.Tests/Utilities/ModeModel.cs) is a reference interpreter over `ModeShape` grammars whose state is the tokens consumed, the source offset and an immutable mode stack; it lexes each token from scratch in the mode on top, so its rollback is correct by construction. `ModeShapes` builds the same shapes as parsers over a two-mode lexer (one character to a token, or a run to a token). `ModeModelTests`: four hand-checked cases for the model; random grammars to depth 4 from `Seq`, `Or`, `Atomic` (also around a leading `SetMode`), `Lookahead`, `Not`, `Optional`, `Many`, `Longest`, `SetMode` and paired push and pop, 20,000 cases over up to 10 characters; and 5,000 repetitions that run to the end of 10 to 40 characters, so the log fills and drops entries while marks are open (about 40% of cases drop). Each case runs the pipeline in one segment, in one-item segments with and without a failure log, without rewrites or light marks, and with every choice dispatched, and through `Pipeline.Parse` to the end of the text, and every run must agree with the model; a rewind to before a dropped entry throws. Mutations: no undo, mode nodes on light marks, no discard on undo, oldest first, a cursor that keeps discarded items, and two drops that remove too much (ignoring the floor, one position too far) each fail the properties. Also fixed in `ModeRollbackTests`: ADR citations left in XML docs, and the dispatch theory's one-line ternaries, now two facts. `dotnet build` 0 warnings; `dotnet test` 2,299 run, 1 failed: `NodeModelTests.NodeTypesAreInternalAndSealed`, failing since `fad0ba5`. Every criterion appears met; not ticked until `/complete`. Next: the maintainer commits, then `/complete`.
- 2026-11-27 (Done): Verified on `5a10464`: `dotnet build` 0 warnings; the eight mode and failure test classes pass (`ModeModelTests` 7 and `ModeRollbackTests` 23 by name, for criteria 1 and 2); `dotnet test` 2,299 run, 1 failed, `NodeModelTests.NodeTypesAreInternalAndSealed`, which has failed since `fad0ba5` unsealed node classes and is outside this card. The card owns no Proposed record.

## Outcome

Delivered on [ADR-0030](../../docs/decisions/0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) points 2, 3, 6 and 9, [ADR-0031](../../docs/decisions/0031-constructs-that-read-their-input-back-pin-their-start.md) point 6 and [ADR-0042](../../docs/decisions/0042-a-write-discards-only-when-it-changes-the-top-mode-and-the-mode-log-keeps-only-what-a-rewind-can-reach.md) points 1, 3 and 5, all accepted before the card started. No new record was needed.

- **Marks.** [FullMark.cs](../../../src/Creole/Plans/FullMark.cs) notes the mode log's length, counting dropped entries, and going back to it withdraws diagnostics, then undoes mode changes, then restores state. `SetMode`, `PushMode` and `PopMode` select it ([PlanBuilder.Marks.cs](../../../src/Creole/Plans/PlanBuilder.Marks.cs)), so a `Longest` in a modal grammar runs its winner again when another alternative ran after it.
- **Undo.** [ModeWriter.cs](../../../src/Creole/ModeWriter.cs) `UndoTo` undoes entries newest first. An entry that changed the top lowers the furthest failure at its index, takes its change back from the saves it was the first change after, and discards the reader's items from its index through [Cursor.cs](../../../src/Creole/Cursor.cs). It throws if it would reach a dropped entry. `Atomic`, `Lookahead`, `Not` and `Longest` reset the position before they rewind. A spare writer's dropped count resets when it is given back.
- **Errors.** [FailureLog.cs](../../../src/Creole/Errors/FailureLog.cs) `UndoChangeAt`, with the count of saves that `ChangeAt` now returns, so two changes at one index clear only the right saves.
- **Tests.**
  - Criterion 1: [ModeModelTests.cs](../../../tests/Creole.Tests/ModeModelTests.cs) compares the pipeline, in five configurations and through `Pipeline.Parse`, with [ModeModel.cs](../../../tests/Creole.Tests/Utilities/ModeModel.cs), a reference interpreter that lexes each token from scratch. 20,000 random grammars, and 5,000 repetitions long enough that about 40% of them drop log entries while marks are open. A drop that removes too much makes a rewind throw.
  - Criterion 2: [ModeRollbackTests.cs](../../../tests/Creole.Tests/ModeRollbackTests.cs): `NotAroundAChangeLeavesTheFailureAsItFoundIt`, `AFailureAfterAnUndoneChangeExpectsNothingUnlessNamedLabelsIt`, `AnUndoAfterASuccessfulLongestReportsTheNextAlternative`, `AnUndoAfterAFilterRejectedReportsTheNextAlternative`, `AnUndoAfterNamedRelabelledReportsTheNextAlternative`, `DispatchGivesTheSameResultWhenAnAlternativeChangesTheMode` and `ALongestAlternativeThatChangesTheModeAndLosesLeavesTheWinnersMode`, each in memory and over one-item segments, beside token traces for every construct that goes back.
  - Mechanics: `FailureLoweringTests` (+6) and `ModeChangeTests` (+1). Twenty distinct mutations across the three increments each failed a test, and five of them, run again, fail the property tests alone.
  - Changed: `ModeNodeTests` expects `FullMark` for a grammar with a mode node, and `WorkedExampleCTests`' rejection case reports the slash read again in operator mode, because its failed alternative's change is now undone.
- **Docs.** [mode-channels.md](../../../docs/concepts/mode-channels.md) § Going back and § What a change does to errors; [interpreter.md](../../../docs/engines/interpreter.md) § When a Longest compiles.
- **Cost.** Not measured. `LightMark` is unchanged, so grammars without mode nodes, `Bind` or state run as before, and no benchmark grammar takes `FullMark`. A modal grammar now takes a full mark at each attempt.
- **Deviations.** None from the whitepaper or the records. Two calls use ADR-0030 point 9's latitude, which lets an engine leave a part out but does not require it:
  - One flag covers every part, so a grammar with `SetState` or `Scan` and no mode node also notes the log's length, which is zero, and a modal grammar also saves its diagnostics and state.
  - `Longest` decides its re-run per grammar, not per alternative, as it has for state since LAY-04. Running a winner again gives the same result.
- **Left to other cards.**
  - [MODE-04](MODE-04-multi-layer-invalidation-cascade.md): a write through a layer in between discards nothing yet, so its undo only restores the stack.
  - [MODE-06](MODE-06-model-based-tests-for-mode-rollback.md): errors against a model of the failure record, and cascades. It can extend `ModeModel`.
