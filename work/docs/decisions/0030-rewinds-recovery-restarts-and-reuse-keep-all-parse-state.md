# ADR-0030: Rewinds, recovery, restarts and reuse keep all of a layer's parse state, and a mode change forgets the failures past it

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-27 |
| Cards | [LAY-08](../../active/06-layers-and-pipelines/LAY-08-per-layer-parse-state.md), [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md), [LAY-04](../../active/06-layers-and-pipelines/LAY-04-layer-local-state.md), [MODE-02](../../active/07-mode-channels/MODE-02-invalidation-rule.md), [MODE-03](../../active/07-mode-channels/MODE-03-rollback-rule.md), [MODE-04](../../active/07-mode-channels/MODE-04-multi-layer-invalidation-cascade.md), [ERR-04](../../active/04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md), [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md), [ERR-06](../../active/04-errors-and-recovery/ERR-06-orinsert-for-missing-items.md), [ERR-07](../../active/04-errors-and-recovery/ERR-07-lexer-recovery-and-cascade-suppression.md), [STRM-01](../../active/08-streaming/STRM-01-starved-outcome.md), [STRM-02](../../active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md), [INC-04](../../active/15-incremental-reparsing/INC-04-reusable-parser-rules.md), [GEN-11](../../active/11-source-generator/GEN-11-runtime-support-surface-for-generated-code.md) |
| Whitepaper | [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention), [§ Failure positions](../../../whitepaper.md#failure-positions), [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels), [§ The error model](../../../whitepaper.md#the-error-model), [§ Recovery](../../../whitepaper.md#recovery), [§ The restart protocol](../../../whitepaper.md#the-restart-protocol), [§ Reusing parser results](../../../whitepaper.md#reusing-parser-results), [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input), [§ Longest match](../../../whitepaper.md#longest-match) |
| Research | [Phase 0 review](../research/2026-10-25-phase-0-review.md), finding 15; Ohm's memo table, read from source for this record (cited inline) |
| Supersedes | — |

## Context

Decisions added per-layer parse state one record at a time. Each mechanism that skips, rewinds or replays work then saved its own subset of it (review finding 15):

| Mechanism | Saves, as built or as its card plans |
| --- | --- |
| [`LightMark`](../../../src/Creole/Plans/LightMark.cs) | Position, cut flag |
| LAY-02's light mark | Position, mode log length, diagnostic count, layer-state reference, cut flag |
| `Atomic`, `Lookahead`, `Not`, `Longest` | Position and cut flag. `Lookahead`, `Not` and `Longest` also save the furthest failure ([ADR-0021](0021-how-failures-are-recorded-and-merged.md), [ADR-0022](0022-a-successful-longest-restores-the-furthest-failure.md)) |
| `Recover` (ERR-05) | Mode stack, layer state, cut flag |
| Restart bookmark (STRM-02) | Layer states, mode logs, diagnostic bag |
| Reuse memo (INC-04) | Result, indexes, examined extent, entry state, whether a cut escaped |

The review worked three counterexamples by hand. Stated concretely:

1. **Reuse and depth.** With `MaxDepth` 60, a reusable rule entered at depth 10 reaches 40 levels below its entry. An edit before the rule, outside its examined range, adds 15 enclosing levels. A fresh parse enters the rule at depth 25 and ends when it tries to enter level 61. The session reuses the memo, enters no levels and accepts the text. Whether a text parses then depends on its edit history.
2. **Reuse and errors.** A reusable `IfStatement` ends in `Optional(ElseClause)`, which records "expected `else`" where the statement ends. If the parse fails there, a fresh parse reports "expected `else` or `;`". A reused statement records nothing, so the error loses `else`.
3. **A mode change after reading ahead.** Under mode A, `Atomic(p)` reads items *i* to *i*+3 and fails at *i*+3, so the furthest failure is at *i*+3. The parse then runs `SetMode(B)` at *i*. Items from *i* on are discarded and lexed again under B, and the parse fails at *i*+1. The failure at *i*+3 still wins. It pairs what `p` expected after three A-mode items with whichever B-mode item now has index *i*+3, at some other place in the source. [ADR-0028](0028-an-errors-items-are-copied-when-the-buffer-releases-them.md) point 9 left this case to LAY-08.

Drafting this record found three more:

4. **Diagnostics withdrawn across layers.** The parser runs `Or(Kind(Number), Kind(Str))`.
   - Peeking item *i* pulls it from the lexer. The lexer meets a bad character, emits an error token and raises a diagnostic (§ Recovery).
   - The first alternative fails without consuming, so its light mark withdraws the diagnostics raised after it, the lexer's included.
   - The token stays buffered and is never lexed again (§ Guarantees, point 1), so the diagnostic is gone for good.
   - Cascade suppression drops the parser's own failure on the error token, so the failed parse reports nothing.
5. **`Recover` keeps the failure it caught.** Take `Stmt.Recover(SkipUntil(Semi), …)`.
   1. Inside `Stmt`, an `Atomic` reads past the next `;` and fails at *f*. `Stmt` then fails before that `;`.
   2. `Recover` reports the failure at *f* and skips to the `;`.
   3. The parse later fails at *g*, before *f*. The furthest failure is still the one at *f*, so the result reports *f* a second time and never reports *g*.
6. **`Longest` keeps the last alternative's state.** [`LongestPlan`](../../../src/Creole/Plans/LongestPlan.cs) runs every alternative, keeps the value and end of the longest, and then resets only the position. If a shorter alternative runs after the winner, the mode log, layer state and diagnostics are the ones the shorter alternative left.

Constraints:

- **G7.** Both engines save and restore the same state, so they agree on results and errors.
- **G2.** No new pins.
- **Failure paths allocate nothing** (ERR-02's criterion).
- **Speed.** A light mark is taken before every attempt, so it sits on the hottest path after reading an item. Review finding 18 already questions its five fields.
- **Simplicity**, the maintainer's standing preference.

**Review.** Independent critics reviewed this record twice on 2026-10-27, and the record was revised in place after each round.

- **The first round** reviewed the first draft:
  - Lowering failures when a mode change was undone broke the restores of `Lookahead`, `Not` and `Longest` (option 2D).
  - The reuse mechanism was not exact (Fork 5).
  - The tables, point 9 and point 11 had gaps.
- **The second round** reviewed the revised points 3 and 8:
  - A failure stored with a mode change went stale (option 2F).
  - A withdrawn `Recover` diagnostic lost its failure.
  - The argument that dispatch stays exact was vacuous (point 3).

**Prior art.** Ohm implements incremental packrat parsing (Dubroy and Warth, SLE 2017), the technique § Reusing parser results follows. Its source was read for this record on 2026-10-27.

What a research subagent found in [`MatchState.js`](https://github.com/ohmjs/ohm/blob/main/packages/ohm-js/src/MatchState.js) and [`PosInfo.js`](https://github.com/ohmjs/ohm/blob/main/packages/ohm-js/src/PosInfo.js), on `main`, whose last commit to `MatchState.js` is `126a282` of 2025-09-11:

- **The memo record.** It carries `examinedLength`, a `rightmostFailureOffset` counted from the record's start, and `failuresAtRightmostPosition`, as `PosInfo.memoize` and `MatchState.useMemoizedResult` read them.
- **Reuse.** `useMemoizedResult` raises the match state's `rightmostFailurePosition` to the record's. It merges the record's failures when they fall at the position being recorded.
- **Records without failures.** `hasNecessaryInfo` refuses such a record only when failures are being recorded and the record's rightmost failure falls at the position being recorded. Ohm can make that check because its first pass always tracks the rightmost failure position.

What one of this record's critics found in `pexprs-eval.js`:

- **How a rule's failures are computed.** Ohm starts a rule's failures from empty when it enters the rule, and merges them back when it leaves. That is exact for Ohm's record, which is a maximum position plus the set of failures there. ADR-0021's record is not of that shape: it keeps the first message and the first context, its expectations are ordered, and `Named` relabels. So the same method is not exact for Creole (Fork 5).
- **Failures dropped from messages.** Ohm leaves out failures inside an expression that succeeded at the failure position. It would report counterexample 2 as "expected `;`".
- **A past bug.** Ohm's commit [`1895b4e`](https://github.com/ohmjs/ohm/commit/1895b4e42d) of 2026-01-26, "fix: bug with errors from partial failures in described rules", changed `pexprs-eval.js` and its error tests. The critic reports that it fixed a memo whose failure offset was not updated after a description relabelled it. Descriptions are Ohm's counterpart of `Named`.

So Ohm also keeps errors the same under reuse (5A), by a method that fits its simpler record.

## Options

### Fork 1. How the list binds the mechanisms

#### 1A. One list, with every mechanism's effect on every part; a layer that cannot change a part leaves it out (chosen)

The record lists every part of the state once. For each mechanism it says what happens to every part. A layer whose grammar cannot change a part, such as the mode log of a layer that never changes mode, leaves that part out of its marks.

**Pros:**

- Nothing is left implicit, so a card cannot drop a part by omission, as INC-04 dropped depth.
- Layers without modes, layer state or recovery keep today's two-field `LightMark`, so the Phase 0 gate's hot path does not grow.
- The flag is computed once per layer, and both engines read it the same way.

**Cons:**

- A layer that changes mode in one rule saves the mode log length at every mark.
- A layer that contains a `Bind` saves every part.

A flag per subtree would be finer, but it needs a new analysis property for every mark in both engines. It can be added if measurements show the cost of the per-layer flag.

#### 1B. One snapshot type that every mark saves whole

**Pros:**

- The simplest rule to state and to check.

**Cons:**

- Every light mark pays for every part, including grammars that use none of them (finding 18).
- The mechanisms still differ on the failure record. A light mark must keep it, because a failed alternative's expectations are what the error reports, while `Not` must restore it. So one snapshot still needs a rule per mechanism.

#### 1C. Each card keeps its own subset (the current state)

**Cons:**

- This is how counterexamples 1, 2 and 5 arose.

### Fork 2. Failures when a mode change discards input

Mode invalidation discards the items at index *i* and later, where *i* is the first unconsumed item.

- **Position *i*** names the same place before and after: the next item after the consumed ones.
- **A position past *i*** named an item that no longer exists. Whatever item later takes that index sits somewhere else in the source.
- **An undo** puts back the items that were there before the change.

*Lowering at i*, used by several options below, turns a failure past *i* into a failure at *i* that expects nothing, as a `Not` whose parser succeeds records (ADR-0021 point 2). Point 3 defines it exactly.

#### 2A. Keep every failure (ADR-0028's reading)

**Pros:**

- No work.

**Cons:**

- Counterexample 3.

#### 2B. Drop every failure past *i*

**Cons:**

- A parse can fail with nothing to report. Take `Or(A, Atomic(Seq(SetMode(B), X, Y)))` at *i*.
  1. `A` fails at *i*.
  2. The second alternative sets B, `X` reads item *i*, and `Y` fails at *i*+1, which replaces `A`'s failure.
  3. `Atomic` rewinds, and the light mark undoes the mode change, which discards from *i*.
  4. The failure at *i*+1 is dropped. The `Or` fails, and no failure is recorded at all.
- Dropping at *i* as well would be worse. Undoing a mode change would then erase the failure of the attempt that set it.

#### 2C. Store the record with each mode-log entry, and restore it when the entry is undone

**Cons:**

- On its own, it does nothing for a change that stands, which is counterexample 3.

#### 2D. Lower at every change and undo, saves included, when the change is made (the first draft)

**Pros:**

- No stale position survives.

**Cons**, found in the first review:

- **A save comes back lowered after its change was undone.** Take `Seq(Optional(Atomic(Seq(A, B, C))), Not(Seq(SetMode(M), K)), D)`.
  1. The `Atomic` fails at *i*+2 and rewinds.
  2. `SetMode(M)` at *i* lowers the record and `Not`'s save to a bare failure at *i*.
  3. `K` fails. `Not` rewinds, which undoes the change, and restores its lowered save.
  4. `D` fails at *i*, and the error reads "at *i*, expected D".

  Under ADR-0021 point 2, `Not` leaves the record as it found it, so the error should be "at *i*+2, expected C". The same goes for `Lookahead`.
- **Saves are out of reach.** Saves are plan locals (`FailureMark`). Lowering them when the change is made would mean moving them into the log.

The first draft also claimed that 2D broke exact dispatch. The second review showed that dispatch never skips an alternative that changes mode, under any rule (point 3), so that con was wrong.

#### 2E. Keep failures past *i*, copy their items before they are discarded, and compare failures by source offset

**Pros:**

- Keeps the deeper failure, with the item it failed on.

**Cons:**

- Failures are compared by buffer index today, which is cheap and exact within one stream of items. Indexes do not line up across two streams, so every comparison would have to move to source offsets.
- Items lexed under two modes overlap. `/x/` under one mode covers the source of `/`, `x` and `/` under another, so "same position" has no clear meaning.
- It takes a copy on every discard.

#### 2F. Store the record with the change, lower it while the change stands, and merge it back on undo (the second draft)

**Pros:**

- Keeps failures that were valid before a change that is later undone.
- 2B's example reads "at *i*, expected A".

**Cons**, found in the second review:

- **The stored failure goes stale.** Between a change and its undo, an enclosing construct can finish and rewrite the record: a successful `Longest` restores its save, a filter restores before it rejects, and `Named` relabels. The stored copy never sees those rewrites, so the undo brings back what they removed.
  - Take `Or(Atomic(Seq(Longest(Seq(U, V), Seq(SetMode(M), X, X)), W)), Z)`.
  - The error reports the losing alternative's "expected V" at 1 rather than "expected Z" at 0.
  - The fix is to carry every restore, relabel and clear into the stored failure of each change still in effect.
- **It conflicts with the log's protection of saves.** A stored failure can outlive the save that protected it in `FailureLog`.
  - `Keep` lets later failures overwrite its expectations.
  - `Leave` and `Compact` free its context frames.
  - Nothing releases old entries, so the log grows with the number of mode changes.

#### 2G. Lower at every change and undo; lower a save when it is restored, by the changes still in effect (chosen)

This is 2D, with saves lowered when they are restored instead of when the change is made. A mode change, or its undo, at *i* lowers the layer's furthest failure at *i*. A save notes the mode log's length when it is taken. When it is restored, it is lowered by the changes made since that are still in effect.

**Pros:**

- **One rule:** a mode change forgets the failures past the point it changes.
- **No stale position** survives (counterexample 3).
- **A failed parse always has a failure to report.** Lowering turns a failure into a bare one but never removes the record.
- **Restores stay exact.** `Lookahead` and `Not` undo their changes before they restore, so their saves come back unlowered. The 2D trace reads "at *i*+2, expected C".
- **Nothing is stored and nothing is merged.** Saves stay plan locals that note one length.
- **No allocation.** The cost is one comparison per change, per undo and per restore.

**Cons:**

- **Lost failures.** Failures past *i* that were recorded before a change that is later undone are lost. In 2B's example, the error is a failure at *i* with no expectations, unless an enclosing `Named` labels it.
- **Possible self-contradiction.** Expectations at *i* stay, and they may have been recorded under another mode than the item now there. This can go either way:
  - After a change that stands, say mode B lexes `-1` as a number. Then `Seq(Optional(Kind(Number)), SetMode(B), Kind(Ident))` on `-1` reads "expected number or identifier, unexpected number `-1`".
  - After an undo, an expectation recorded under the undone mode sits next to the item lexed under the restored one.

  Dropping expectations at *i* instead would empty the error whenever an alternative that starts with a mode change fails on its first item. Example C's `Operand` does that on every missing operand.

### Fork 3. `Recover` and the failure it caught

#### 3A. Keep the record as the failed parser left it (the current state)

**Cons:**

- Counterexample 5.

#### 3B. Clear the furthest failure once the diagnostic is built, and give it back if the diagnostic is withdrawn (chosen)

**Pros:**

- The record holds one furthest failure, and the diagnostic was just built from it, so clearing removes exactly what was reported.
- The diagnostic keeps that failure, so a failure is never lost when an enclosing mark withdraws the diagnostic. Without the give-back, `Or(Seq(Kind(Let).Recover(SkipUntil(Semi), fallback), Kind(Ident)), Kind(Number))` on `;` runs like this:
  1. `Recover` reports "expected `let`" and clears the record.
  2. `Ident` fails without consuming, so the first alternative's light mark withdraws the diagnostic.
  3. The error never mentions `let`.
- The same rule covers `OrInsert`, and a strategy that cannot repair, if ERR-05 allows one.

**Cons:**

- An enclosing construct that restores a save taken before the `Recover` can put back a failure that the diagnostic already reported. A filter that then rejects is one such construct.
- A failure given back is recorded again after the failures recorded since. So its expectations come later in the list than in a parse without `Recover`.

#### 3C. Restore the record to its state when the parser started (the first draft)

**Cons**, found in review:

- Restoring still reports a failure twice when it came from before the parser started. Take `Or(Atomic(BlockForm), Stmt.Recover(SkipUntil(Semi), …))`.
  1. The `Atomic` reads past the `;` and fails at *f*.
  2. The diagnostic reports *f*, and the restore puts *f* back.
  3. *f* is reported a second time, and a later failure before *f* is hidden.

### Fork 4. Which diagnostics a mark withdraws

#### 4A. Every diagnostic raised after it, in any layer (the wording of § Recovery and ERR-04)

**Cons:**

- Counterexample 4.

#### 4B. Its own layer's; another layer's diagnostics go only with the step that raised them (chosen)

**Pros:**

- Diagnostics follow the work that raised them, as items do. A downstream rewind never re-runs an upstream step (§ Guarantees, point 1), so it must not withdraw what that step raised.
- It matches § Guarantees point 4, under which upstream backtracking is private.

**Cons:**

- Each layer keeps its own diagnostics, which the result merges by span.

### Fork 5. Errors after reuse

#### 5A. Reuse is invisible, errors included (chosen by the maintainer)

A parse that reuses rule results gives the same outcome, diagnostics and `MaxDepth` failure as a fresh parse.

**Pros:**

- Counterexamples 1 and 2 cannot happen.
- Ohm keeps errors the same under reuse.
- Incremental parsing can be tested against fresh parsing by comparing whole results.

**Cons:**

- A memo must hold enough to replay the rule's effect on depth, failures, modes and layer state.
- Replaying the effect on failures exactly is hard. The first draft's method failed review in three ways, all traced against `FailureLog`:
  - **Lowering does not commute with merging.** Take `Top = Or(Atomic(Seq(Kind(A), Kind(B), Kind(C))), Seq(Rule, Kind(E)))` and `Rule = Seq(Kind(A), Optional(Kind(D)), SetMode(M)).Reusable()`, on `A B X`.
    - **A plain run.**
      1. The `Atomic` fails at 2.
      2. `D`'s failure at 1 is outranked, so it is not kept.
      3. `SetMode` at 1 lowers the failure at 2 to a bare failure at 1.
      4. `E` fails, and the error reads "expected E".
    - **Run from an empty record and merged.** The rule's own failure at 1, "expected D", survives the merge, so the error reads "expected D or E".
  - **`Recover` and `OrInsert` read the record.** The diagnostic `Recover` builds includes failures recorded before the rule started, which a rule run from an empty record cannot see. This changes the diagnostics of the run that builds the memo, before anything is reused.
  - **Replay can attach context.** A lowered failure has no context stack. A replay that attached the frames open at reuse would give it one.

#### 5B. Same result, errors may differ

The memo keeps depth, so the depth outcome stays exact, but it stores no failures.

**Pros:**

- A smaller memo and a simpler replay.

**Cons:**

- Counterexample 2 stays, and the docs would have to warn about it.
- The error an editor shows would depend on the order of edits.

### Fork 6. State after a successful `Longest`

#### 6A. The state the winner leaves, as if it alone had run; the engine re-runs the winner when needed (chosen)

**Pros:**

- Exact. [ADR-0026](0026-engines-may-skip-unused-values-and-repeat-user-functions.md) already lets engines run user functions again.
- Most `Longest` nodes are tokens. Their alternatives change no mode, layer state or diagnostics, so resetting the position is enough, as now.

**Cons:**

- An alternative that does change them is run twice when a shorter alternative ran after it.

#### 6B. Forbid mode changes, layer-state changes and recovery inside `Longest` alternatives

**Cons:**

- It needs the same analysis as 6A, plus a new analysis error.
- Users would hit that error. LAY-04's template-string lexer keeps the brace depth in layer state, and a `Longest` alternative for `}` or `${` that updates it with `Scan` would be refused.

#### 6C. Keep each alternative's exit state

**Cons:**

- An alternative's mode-log entries are undone before the next alternative runs. Keeping them would mean copying the log.

## Decision

Options **1A, 2G, 3B, 4B, 5A and 6A**. Accepted by the maintainer on 2026-10-27. The choices behind it were made in conversation the same day:

- the maintainer chose 5A;
- after the first review, the maintainer agreed to revise the draft;
- after the second review, the maintainer chose 2G.

**1. The list.** Each layer holds every part below for itself. Layers share state only through buffers and mode channels.

| Part | Decided in | Kind |
| --- | --- | --- |
| Position | § Bookmarks and retention | Restorable |
| Cut flag | [ADR-0011](0011-a-cut-is-dropped-with-every-failed-attempt.md) | Restorable |
| Depth count | [ADR-0020](0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md) | Scoped |
| Furthest failure: position, expectations, message, span end, context stack, count of failures recorded | [ADR-0021](0021-how-failures-are-recorded-and-merged.md) | Accumulated |
| Saved failures, held by `Lookahead`, `Not`, `Longest` and filters, each noting the mode log's length | ADR-0021, ADR-0022, point 3 | Scoped |
| Notes held by `Named` | ADR-0021 point 3 | Scoped |
| Context frames | ADR-0021 | Scoped |
| Held starts of open filters, chained operators and custom scanners | [ADR-0028](0028-an-errors-items-are-copied-when-the-buffer-releases-them.md) | Scoped |
| Copies taken at release | ADR-0028 | Belong to the failure, frame or start they copy for |
| Mode log and mode stack of each channel the layer writes | § 3. Mode channels | Restorable, by log length |
| Diagnostics the layer raised, each with the step that raised it. A diagnostic built from the furthest failure keeps that failure | § Recovery, points 4 and 8 | Restorable, by count |
| Layer-local state | § 2. Layer-local state | Restorable, by reference |
| Pins held by `Atomic`, `Lookahead`, `Not`, `Longest` and constructs that read behind the cursor | § Bookmarks and retention | Scoped |
| The layer's pin at the `stepStart` of its oldest retained output, and the restart bookmark | § Bookmarks and retention, § The restart protocol | Held by the layer and the restart driver |
| Starved | STRM-01 | Sticky until a restart |
| Examined extent, in a session only | § Reusing layer items | Accumulated |
| Whether the run has a failure log | [ADR-0027](0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md) | Fixed for a run |

The kinds:

- **Scoped** parts are opened and closed by one node on every path that returns, so leaving the node restores them, and no mechanism saves them. A depth abort or an exception from user code ends the parse, and a restart resets them (point 7), so neither needs to unwind them.
- **Restorable** parts are saved by a mark as a value, a reference or a log length, and put back.
- **Accumulated** parts only grow. Marks keep them on purpose, except where point 2 says otherwise.
- **Held by a driver** parts outlive the nodes of a step, and a restart does not release them.

In a layer that runs in steps, each step starts with every scoped part at its base and no cut in force, because no `Atomic` spans two steps. The failure record, diagnostics, mode logs and layer state last the whole parse, or one item under `ParseMany` or `ParseManyAsync`. If MODE-01 lets two layers write one channel, each layer keeps the entries it made.

A record that adds a part adds it to this list and says what each mechanism in point 2 does with it.

**2. What each mechanism does.**

| Mechanism | When | Position | Cut flag | Mode log | Own diagnostics | Layer state | Failure record |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Light mark | Its attempt fails without consuming | Unchanged | Restored | Undone to the mark | Withdrawn to the mark | Restored | Kept |
| `Atomic` | Its item fails and no cut is in force | Rewound | Merged (ADR-0011 point 7) | Undone | Withdrawn | Restored | Kept |
| `Lookahead` | Its item succeeds | Rewound | Restored | Undone | Withdrawn | Restored | Restored |
| `Not` | Always | Rewound | Restored | Undone | Withdrawn | Restored | Restored. If the item succeeded, a failure is then recorded at its start |
| `Longest` | Between alternatives, and when every alternative fails | Rewound | Cleared between alternatives, restored at the end | Undone | Withdrawn | Restored | Kept. When all fail, their failures stand in alternative order |
| `Longest` | It succeeds | The winner's end | Restored | As the winner left it (point 6) | As the winner left it | As the winner left it | Restored |
| Filter | Its predicate rejects | Unchanged | Unchanged | Unchanged | Unchanged | Unchanged | Restored, then the rejection is recorded at its start ([ADR-0014](0014-a-rejected-filter-reports-at-the-start-of-the-value.md)) |
| `Named` | Its item consumed nothing | Unchanged | Unchanged | Unchanged | Unchanged | Unchanged | Relabelled (ADR-0021 point 3) |
| `Recover` | It catches a failure | Repaired from the cursor | Restored only if the item failed without consuming | Point 8 | One added (point 8) | Restored | Cleared once its diagnostic is built (point 8) |
| Restart | An item starves | Every layer back to the item's start | Cleared | Undone | Withdrawn | Restored | Cleared (point 7) |
| Reuse | A memo matches | The rule's end | Set if a cut escaped | As the rule left it | None to replay | The rule's exit state | As a fresh run would leave it (point 5) |

Every rewind works in this order:

1. It withdraws its layer's diagnostics raised since the mark, giving back the failures that `Recover` and `OrInsert` cleared (point 8).
2. It undoes the mode changes made since the mark, which lowers the furthest failure (point 3).
3. If the mechanism restores a saved failure, it does so last. The save is lowered by the changes still in effect (point 3).

A restart undoes the mode changes before it clears the failure records.

**3. A mode change forgets the failures past it.** *Lowering at i* changes a failure as follows:

- **Past *i*.** The failure becomes a bare failure at *i*: it expects nothing and has no message and no context stack.
  - It replaces the record, as a further failure does. Neither a later merge nor `Named` brings back expectations that were replaced.
  - Lowering does not count as recording a failure, so it never makes `Named` relabel on its own (ADR-0021 point 3).
  - The next failure recorded at *i* supplies the context stack, as if it were the first one there.
- **At or before *i*, with its span end past *i*.** The failure drops that span end, and its message if a rejection or chained operator supplied it.
- **Otherwise** the failure is unchanged. Expectations at *i* stay, because *i* still names the next item after the consumed ones. Option 2G's cons give the imprecision this accepts.

The rule, in a run with a failure log:

1. **A change or an undo.** A mode change at *i*, or the undo of one at *i*, lowers the layer's furthest failure at *i*. Nothing is stored.
   - After a change, failures past *i* name discarded items.
   - After an undo, those recorded under the undone mode past *i* do.
   - Failures past *i* that were recorded before a change stay lowered after its undo.
2. **A save.** A save held by `Lookahead`, `Not`, `Longest` or a filter notes the mode log's length when it is taken. When it is restored, it is lowered at the lowest index among the changes made since that are still in effect. The indexes of the changes in effect never decrease, because a rewind to before a change undoes it. Lowering at several indexes in turn equals lowering once at the lowest.
3. **`Recover`'s restore** of the mode stack is a change like any other.
4. **Other layers.**
   - In a cascade (MODE-04), each layer whose input is truncated at *j* applies the same rule at *j*.
   - A producing layer that re-runs a step from `stepStart` *s* applies it at *s*.

Other consequences:

- Open context frames and held starts never start past the cursor, so they need no change.
- A failure at *i* reports the item now in the buffer, lexed under the current mode (ADR-0028 point 5).
- In a cascade, an intermediate layer discards input it had consumed, which ADR-0028's argument did not foresee. Its copies stay correct anyway. Its pin at the `stepStart` of its oldest retained output kept that input from being released, so no copy of it was taken.

Why the guarantees hold:

- **Restores.**
  - `Lookahead` and `Not` undo their changes before they restore (point 2), so their saves come back unlowered, as ADR-0021 point 2 says.
  - A `Longest` winner's change stands, so the record `Longest` restores is lowered at the winner's first change. It is not exactly what ADR-0022 says, which would bring back a stale position.
  - A filter's restore before its rejection is lowered the same way.
- **Dispatch.** Dispatch peeks item *i* under the current mode. An alternative that changes mode before its leading item reads item *i* lexed under another mode, so no key set is sound for it, whatever the rule for failures. ADR-0016 point 1 already gives such an alternative no key set: a sequence has its first item's keys, and `SetMode` has none. Taking keys from analysis behind nullable prefixes (ADR-0016 point 5) must keep that: a node that can change the mode ends the walk. A skipped alternative therefore never changes mode, and lowering cannot make dispatch inexact.

**4. Diagnostics belong to the layer that raised them.**

- A mark withdraws only the diagnostics its own layer raised after it.
- Diagnostics that another layer raised are withdrawn only when the step that raised them is run again or its outputs are discarded: by mode invalidation, a cascade or a restart.
  - A step that is run again raises them again.
  - So a step whose earlier outputs were consumed reports its diagnostic once: those are the steps with a `groupIndex` above 0.
  - These withdrawals give back no failures, because the step that is run again records its own.
- An `Embed`ded sub-pipeline's diagnostics belong to the layer that ran the `Embed`. That is the "same bag as the parent's" of § Embedded languages.
- The result merges every layer's diagnostics, ordered by span (§ Results).
- Under `ParseMany`, a lexer can raise a diagnostic while producing lookahead past the end of an item. Which item's result holds it is left to LAY-05 and STRM-02.

**5. Reuse is invisible.** A parse that reuses rule results gives the same outcome, diagnostics and `MaxDepth` failure as a fresh parse of the same text with the same options. Three things are outside that promise:

- **Values** follow ADR-0026.
- **Spans** of reused nodes come from the session's table.
- **Stack-check failures** are not compared, as ADR-0020 says.

A memo must account for the following:

- **Depth.** The memo stores the deepest level the rule reached, counted from its entry, including levels reached inside rules reused within it. If the entry depth plus that level would exceed `MaxDepth`, the rule runs instead, so the parse ends where a fresh one would. This test is exact, because ADR-0020 refuses entry when the count already equals `MaxDepth`.
- **Failures.** The memo must reproduce the rule's effect on the furthest failure. That includes lowering by the rule's mode changes, which can reach failures recorded before the rule started (point 3). It also includes what a `Recover` or `OrInsert` inside the rule reads, clears and gives back.
- **Modes and layer state.** The memo stores the exit layer state, the exit mode stack and the rule's mode-log entries. A later undo then discards from where the rule first changed mode and lowers the failures past it.
- **Cuts.** The memo records whether a cut escaped, as INC-04's scope already says.
- **Nesting.** A rule reused inside another rule's run adds its stored depth, failure effect and mode entries to the outer memo.

INC-04 designs the memo and the replay. The first draft's method is not exact (Fork 5), and the design must handle four hazards:

1. Lowering does not commute with merging. A failure that a deeper one outranked stays lost after the deeper one is lowered.
2. `Recover` and `OrInsert` read and clear the record, including failures recorded before the rule started.
3. A lowered failure has no context stack, and replay must not give it one.
4. Nested memos compose.

INC-04 also decides whether a session follows ADR-0027. If it does, a run with a failure log cannot use a memo built in a run without one. A run without a log tracks nothing, so Ohm's finer check is not available. Editors re-parse broken text often, which is the workload ADR-0027's "What would change this" names.

INC-04's criteria gain a differential test: a session's whole results equal `Parse`'s, over generated grammars and edits. The generators must cover:

- mode changes after an `Atomic` has rewound past a deeper failure;
- `Recover`;
- `Context`;
- `Named`;
- nested reusable rules.

**6. `Longest`.**

- Between alternatives, and when every alternative fails, every restorable part goes back to its value at the start.
- After a success, every part except the cut flag and the failure record is as the winner left it, as if it alone had run from the start. The failure record is restored, lowered by any change the winner made (point 3).
- Suppose another alternative ran after the winner, and the winner can change the mode log, its layer's diagnostics or layer state: it contains a node from point 9's table, or a `Bind`. Then the engine runs the winner again. Otherwise it resets the position only, as `LongestPlan` does now.
- A compiled `Longest` never needs the re-run, as long as Regular subtrees exclude those nodes, as they do today ([ADR-0017](0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md)).

**7. Restart.** The restart bookmark is taken at an item's start, where every scoped part of every layer is at its base. Under `ParseMany` and `ParseManyAsync`, each item starts with no furthest failure and no cut in force. On Starved, the driver does the following:

- It rewinds every layer to the bookmark, as § The restart protocol says, undoing mode changes first.
- It clears every layer's failure record and the Starved flag.
- It resets every scoped part to its base, so Starved need not unwind scoped parts on its way out, and STRM-01 may implement it either way. The pins held by a driver (point 1) are kept.
- A layer that discards items it produced withdraws their diagnostics with them (point 4).

**8. Diagnostics built from the furthest failure.** `Recover`, and `OrInsert` if ERR-06 builds its diagnostic from the furthest failure, turn the furthest failure into a diagnostic:

- The diagnostic is built from the furthest failure as it stands. That can include failures recorded at the same position before the construct started.
- The furthest failure is then cleared. The diagnostic keeps it.
- If a mark of the same layer later withdraws the diagnostic, the failure it kept is recorded again, after the failures recorded since. That happens before the same rewind undoes mode changes, so it is lowered like any other failure (point 2).

When `Recover` catches a failure of `p`, it does the following, in order:

1. It builds its diagnostic and clears the furthest failure, as above. Saves held by enclosing constructs are untouched.
2. It restores the mode stack and layer state from `p`'s start, as an ordinary mode change at the cursor (point 3). If `p` failed without consuming, its light mark has already undone `p`'s mode changes and withdrawn its diagnostics, and this change does nothing.
3. The strategy repairs from the cursor. If ERR-05 lets a strategy fail, `Recover` withdraws its own diagnostic, which gives the failure back, and fails.

Other parts:

- Diagnostics raised inside a `p` that consumed stay.
- The cut flag follows ADR-0011: it is restored only if `p` failed without consuming.
- A depth abort and Starved pass through.
- A run without a failure log follows ADR-0027 point 3.

**9. Saving less.** The table in point 2 defines the behaviour. An engine may leave a part out of every mark in a layer whose grammar cannot change it:

| Part | Can change only if the layer's grammar contains |
| --- | --- |
| Mode log | `SetMode`, `PushMode` or `PopMode` |
| Own diagnostics | `Recover`, `OrInsert` or `Embed` |
| Layer state | `SetState` or `Scan` |

Further rules:

- "Contains" follows `Rec` references.
- A layer whose grammar contains a `Bind` saves every part, because a continuation can return any parser.
- A custom scanner cannot change these parts while `Cursor` offers no way to. REL-04 must keep it that way, or revisit this point.
- Phase 0 parses have none of these parts, so `LightMark` stays as it is.

**10. Invisible.** Each mechanism leaves the parse as it would be without it:

- A restarted item gives the result and diagnostics of the same item parsed with all its data available.
- An undone attempt leaves every restorable part as if it had not run. The failures it recorded stay (point 2), lowered where point 3 says.
- Reuse is covered by point 5.

**11. Tests.**

| Card | Test |
| --- | --- |
| [MODE-02](../../active/07-mode-channels/MODE-02-invalidation-rule.md) | Counterexample 3. A rejection whose span crossed a later mode change no longer widens the error or supplies its message |
| [MODE-03](../../active/07-mode-channels/MODE-03-rollback-rule.md) | `Not` around a mode change, with a failure inside it before the change, leaves the failure record as it found it. Option 2B's example gives a failure at *i* with no expectations, and an enclosing `Named` labels it. An undo after a successful `Longest`, after a filter's rejection, and after a `Named` relabel each report the next alternative's expectation, not one those constructs removed. `Or(Seq(SetMode(Operand), Kind(RegexLit)), Kind(Slash))` on `/x/` gives the same result and error with dispatch on and off. Counterexample 6, with a `Longest` alternative that changes mode |
| [MODE-06](../../active/07-mode-channels/MODE-06-model-based-tests-for-mode-rollback.md) | Errors as well as items, compared with a model of the failure record that applies point 3 |
| [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md) | Counterexample 5. Fork 3C's case, where the reported failure came from an alternative before the `Recover`. Fork 3B's `let` case, where the withdrawn diagnostic gives its failure back |
| [ERR-07](../../active/04-errors-and-recovery/ERR-07-lexer-recovery-and-cascade-suppression.md) | Counterexample 4. A re-run step whose earlier outputs were consumed reports its diagnostic once |
| [STRM-02](../../active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md) | An attempt that starves inside a `Lookahead` and a `Context`, after recording a failure, leaves no failure, save or frame after the retry |
| [INC-04](../../active/15-incremental-reparsing/INC-04-reusable-parser-rules.md) | Counterexamples 1 and 2, and the differential test in point 5 |
| [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md) | None of the counterexamples can arise before modes, layer state or diagnostics exist. Its test: a layer with none of them takes two-field light marks, and the Phase 0 tests still pass |

Deciding reasons:

- **1A:** it is the only option that both leaves nothing implicit and keeps the hot path as it is. A flag per layer is the simplest way to skip.
- **2G:** it is the simplest option with all three properties:
  - it never reports a stale position;
  - it never leaves a failed parse with nothing to report;
  - it keeps the restores exact.

  2F also keeps failures that are valid again after an undo, but the review showed what that costs.
- **3B:** the record held only what was just reported. Giving it back keeps a withdrawn diagnostic from losing a failure.
- **4B:** under any other rule, a downstream rewind can lose an upstream diagnostic for good.
- **5A:** the maintainer's choice. How to meet it is INC-04's design.
- **6A:** it is exact. 6B would refuse a grammar LAY-04 plans for.

What would change this:

- **Errors at mode changes.** If users find them too vague because failures past a change are forgotten, or contradictory because expectations at *i* came from another mode, a new record would choose between:
  - a save of the failure record by `Atomic` in layers that change mode, merged back on undo;
  - tagging each failure with the mode log it was recorded under;
  - 2E.
- **The per-layer flag.** If its cost shows in measurements, the flag would become a property per subtree.

## Consequences

**Easier:**

- One list for GEN-11 to expose, and one table that every engine and card follows.
- One rule for failures at mode changes, with nothing stored.
- Restarts, rollback and reuse can each be tested against a plain parse by comparing whole results.
- Phase 0 grammars keep their light mark as it is.

**Harder:**

- Each change and undo lowers the record, and each save notes the mode log's length.
- `Named` must detect that the record was replaced since its note. `FailureLog.Relabel` assumes today that the expectations before its note are intact whenever the position returns to the note's.
- A record's message must say whether a rejection or chained operator supplied it.
- `Recover` and `OrInsert` diagnostics keep the failure they reported.
- Each layer keeps its own diagnostics, tied to the step that raised them.
- `Longest` may run its winner twice.
- INC-04 carries a hard design problem, with a differential test as its criterion.

**Ruled out:**

- A mark withdrawing another layer's diagnostics.
- A failure past an item that a mode change discarded.
- A restore by `Lookahead` or `Not` that differs because a mode change inside it was undone.
- Dispatch keys behind a node that can change the mode.
- Reuse changing an error or the depth outcome.

**Risks and how they are watched:**

- **Saves left out by point 9 diverging from full saves.** GEN-08's differential tests run with point 9 off as well as on.
- **Errors at mode changes.** MODE-06 compares errors with a model of the failure record. Lost or contradictory expectations (2G's cons) are watched through user reports. The remedies are in "What would change this".
- **A filter around a `Recover` putting back a failure the `Recover` already reported** (3B's cons). This is accepted as rare.

## Changes made

Made on acceptance, 2026-10-27:

- **Whitepaper:**
  - [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking) and [§ Interpreted engine](../../../whitepaper.md#interpreted-engine): a node that can change the mode ends the walk for an alternative's leading item.
  - [§ Longest match](../../../whitepaper.md#longest-match): a restore is lowered by the winner's change, and the state after a match is the winner's, with the winner run again when needed (point 6).
  - [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention): a new paragraph links this record. It says that:
    - light marks keep the furthest failure and withdraw only their own layer's diagnostics;
    - a rewind runs in a fixed order;
    - a layer leaves out of its marks the parts it cannot change.
  - [§ Guarantees](../../../whitepaper.md#guarantees) point 4: diagnostics are private to their layer.
  - [§ Failure positions](../../../whitepaper.md#failure-positions): a new paragraph on lowering at a change and an undo, and on saves lowered when restored (point 3).
  - [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels):
    - Invalidation step 1 withdraws diagnostics per re-run step.
    - Changes and undos lower failures past *i*.
    - Each layer in a cascade lowers at its own index.
  - [§ The error model](../../../whitepaper.md#the-error-model):
    - Labels: deeper failures past an `Atomic` keep their expectations unless a mode change at or before them was made after they were recorded.
    - Lookahead, Not and Longest: a restore by these or by a filter is lowered by a change that stands.
  - [§ Recovery](../../../whitepaper.md#recovery): `Recover` clears the furthest failure once its diagnostic is built, and a withdrawn diagnostic gives it back. Withdrawal is by the raising layer's marks, or by re-running the step.
  - [§ Embedded languages](../../../whitepaper.md#embedded-languages): an `Embed`'s diagnostics belong to the layer that ran it.
  - [§ Properties](../../../whitepaper.md#properties): the per-layer property behind point 9.
  - [§ The restart protocol](../../../whitepaper.md#the-restart-protocol): each layer's diagnostics are in the bookmark, and each item starts with no failure. A rewind undoes mode changes, clears failure records, resets scoped state and keeps the pins held by drivers.
  - [§ Reusing parser results](../../../whitepaper.md#reusing-parser-results): reuse is invisible, what a memo accounts for, and the depth test.
  - [§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input): reuse does not change the depth outcome.
- **Cards:** scope and criteria changed as follows. Each test from point 11 is an acceptance criterion.

  | Card | Change |
  | --- | --- |
  | [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md) | Points 1, 2, 4, 6 and 9. A criterion for two-field light marks; none of the counterexamples applies |
  | [LAY-04](../../active/06-layers-and-pipelines/LAY-04-layer-local-state.md) | Points 6 and 9 for layer state |
  | [LAY-05](../../active/06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md) | Points 4 and 7 for `ParseMany` |
  | [MODE-02](../../active/07-mode-channels/MODE-02-invalidation-rule.md) | Points 3 and 4, and the point 11 tests |
  | [MODE-03](../../active/07-mode-channels/MODE-03-rollback-rule.md) | Points 2 and 3, and the point 11 tests |
  | [MODE-04](../../active/07-mode-channels/MODE-04-multi-layer-invalidation-cascade.md) | Point 3 in a cascade |
  | [MODE-06](../../active/07-mode-channels/MODE-06-model-based-tests-for-mode-rollback.md) | Errors compared with a failure-record model, and a criterion for it |
  | [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md) | Dispatch keys stop at a node that can change the mode. The per-layer property of point 9 |
  | [GEN-02](../../active/11-source-generator/GEN-02-emit-the-core-combinators.md) | No `switch` key behind a node that can change the mode |
  | [ERR-04](../../active/04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md) | Per-layer withdrawal by step, and the give-back of point 8 |
  | [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md) | Point 8, and the point 11 tests |
  | [ERR-06](../../active/04-errors-and-recovery/ERR-06-orinsert-for-missing-items.md) | Point 8 for `OrInsert` |
  | [ERR-07](../../active/04-errors-and-recovery/ERR-07-lexer-recovery-and-cascade-suppression.md) | Point 4, and the point 11 tests |
  | [STRM-01](../../active/08-streaming/STRM-01-starved-outcome.md) | Starved need not unwind scoped state |
  | [STRM-02](../../active/08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md) | Point 7, the lookahead-diagnostic question, and the point 11 test |
  | [INC-04](../../active/15-incremental-reparsing/INC-04-reusable-parser-rules.md) | Point 5, its hazards, the failure-log policy, and the differential test as a criterion |
  | [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md) | Points 2, 3, 6 and 8 in generated code |
  | [GEN-08](../../active/11-source-generator/GEN-08-differential-engine-agreement-tests.md) | Comparisons with point 9 off and on |
  | [GEN-11](../../active/11-source-generator/GEN-11-runtime-support-surface-for-generated-code.md) | The surface carries point 1's list and point 2's operations |
  | [LAY-08](../../active/06-layers-and-pipelines/LAY-08-per-layer-parse-state.md) | Its second criterion allows a card to say that no counterexample applies |

  INT-05 is Done and needs no change. ADR-0016 point 1 already gives an alternative that starts with `SetMode` no key set.
- **Decisions index:** a note on the records this one refines, and that it settles ADR-0028 point 9.
- **Revised before acceptance, 2026-10-27,** after two rounds of review by independent critics:
  - **First round:**
    - 2D became 2F, and 3C became 3B.
    - The reuse mechanism moved to INC-04.
    - The analysis for leaving parts out became a per-layer flag.
    - The tables gained filters, `Named`, `Longest`'s failure row and pins held by drivers.
    - The tests moved to cards that can write them.
  - **Second round:**
    - 2F became 2G.
    - 3B gained the give-back, and the rule now covers `OrInsert`.
    - The order of a rewind was fixed.
    - The dispatch constraint replaced a vacuous argument.
    - The refinements of accepted records were declared.
