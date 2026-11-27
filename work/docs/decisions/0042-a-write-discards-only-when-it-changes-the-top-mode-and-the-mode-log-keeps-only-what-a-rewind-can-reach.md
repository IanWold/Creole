# ADR-0042: A write discards only when it changes the top mode, the mode log keeps only what a rewind can still reach, and CRL0008 moves to cascades

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-11-26 |
| Cards | [MODE-02](../../active/07-mode-channels/MODE-02-invalidation-rule.md), [MODE-03](../../active/07-mode-channels/MODE-03-rollback-rule.md), [MODE-04](../../active/07-mode-channels/MODE-04-multi-layer-invalidation-cascade.md), [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md) |
| Whitepaper | [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels), [§ Failure positions](../../../whitepaper.md#failure-positions), [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention), [§ Guarantees](../../../whitepaper.md#guarantees) |
| Research | MODE-02's reading of the layer code, recorded under Context (2026-11-26) |
| Supersedes | — |

## Context

§ 3. Mode channels gives the invalidation rule in four steps. Earlier records settle most of what it needs:

- [ADR-0030](0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) says how a change lowers failures (point 3) and withdraws diagnostics (point 4).
- [ADR-0040](0040-a-producing-layer-keeps-input-only-for-outputs-its-reader-can-go-back-to.md) says which input a producing layer keeps. It leaves to MODE-02 how CRL0008's check reaches consumed outputs that the buffer may have released (point 7).
- [ADR-0041](0041-a-channel-has-one-reader-below-one-writer-and-a-write-nothing-reads-or-a-pop-of-the-last-mode-throws.md) gives the stack, one `Layer.Modal` reader and one writing layer per channel (point 3), and the reset at each `ParseMany` item (point 9).

MODE-01 built channels, the mode nodes and `Layer.Modal`. A change affects only the steps run after it (ADR-0041 point 10). Four questions remain open for MODE-02.

1. **Which writes are changes.** MODE-01's `SetMode` replaces the top even with an equal mode, and its build left to MODE-02 whether that is a change (MODE-01's progress note of 2026-11-25). The whitepaper's steps 1 and 2 read as if every write discards. A `SetMode` to the mode on top, or a `PushMode` of it, leaves `Layer.Modal` choosing the same item parser, because its selector cache compares modes with `EqualityComparer<TMode>.Default` (ADR-0041 point 1, [`ModalLayer.cs`](../../../src/Creole/Layers/ModalLayer.cs)).
2. **CRL0008 cannot occur in MODE-02.** Reading the code shows that a single boundary never drops outputs:
   - A channel's reader is a `Layer.Modal` (ADR-0041 point 3). It runs through the step loop of `Layer.Tokens`, which adds one output per step with `groupIndex` 0 ([`TokensLayer.cs`](../../../src/Creole/Layers/TokensLayer.cs)).
   - `Then` puts a stream buffer between every pair of layers ([`ComposedLayer.cs`](../../../src/Creole/Layers/ComposedLayer.cs)). A writer reads the reader's buffer directly only when no layer sits between them. Otherwise a change truncates an intermediate layer's buffer, which is MODE-04's cascade.
   - Only `Transduce` adds outputs with a `groupIndex` above 0 ([`TransduceLayer.cs`](../../../src/Creole/Layers/TransduceLayer.cs)). So does the root reader, but a mode change never re-runs the root.

   So in MODE-02's case the re-run drops nothing, the check has nothing to compare, and neither CRL0008 nor ADR-0040 point 7's problem can arise.
3. **How long the mode log keeps an entry.** Step 3 appends an entry per change. Marks record the log's length, and the rollback rule undoes the entries made after a mark. Nothing says when an entry can go. ADR-0041 point 9 empties the parser's logs at each `ParseMany` item, but a single document keeps every entry until its parse ends. Example C writes twice per operand, so the log of a streamed script grows with the script. § Guarantees point 2 bounds memory at a boundary by the longest window a pin holds open. A log that grows with the input breaks that bound for modal grammars.
4. **Writes across a layer before MODE-04.** A writer with a layer between it and the reader needs a cascade to discard anything. MODE-04 builds cascades.
5. **A restore lowered where the input was released.** Found in MODE-02's build on 2026-11-26, at `f087aac`. A save without a pin, a filter's or `Adjacent`'s, stays open while the cursor and the horizon move past the first change made after it. When it is restored, it is lowered there (ADR-0030 point 3, rule 2), and the error then reads the item at that position. ADR-0028 point 1 copies a saved failure's own position and end before the buffer releases them, but not the position its restore is lowered to. So the error reads a released item. Reproduced: `Atomic(OneOf(Kind(Name), Kind(Semi)).Many().Then(Kind(If))).Optional().Then(Statement.Many().Where(_ => false))` over 100 statements `x;`, streamed in segments of one item, throws `ArgumentOutOfRangeException` ("The input does not hold this position") from `FailureCopy.ToError`. The gap is in the code since MODE-02's second increment, whatever Fork C chooses. C2, which gives a save its index only when entries are dropped, does not close it either: the buffer releases the item before the entry is dropped.
6. **A log over memory that never shrinks.** Found by an independent review of MODE-02 on 2026-11-27, at `2109e7e`. Under C6 as first built, the log dropped entries only when the writing layer's cursor entered a new segment of its input. A parser run on its own over memory has one segment, so its log kept every write. Each thread's spare writer then kept the log's capacity after the parse. Measured: `SetMode(m, A).Then(Char('a')).Then(SetMode(m, B)).SkipMany()` over 1,000,000 characters left a spare log with room for 2,097,152 entries of about 32 bytes, some 64 MB against a 2 MB string. C2's claim that such a log "is no worse than holding the input itself" was wrong.

## Criteria

Fixed before any option is tried:

1. **Items.** After any sequence of writes, the writing layer reads the items that a lex from scratch under the same mode history gives. Example C lexes `/` as division in `(a) / 2` and as a regex in `if (ok) /x/.test(s)`. One case must make the parser read an item under the old mode before the change, so that a discard happens (MODE-02's first criterion).
2. **Errors.** MODE-02's second and third criteria pass. A filter and a `Context` around an operand that starts with `SetMode` each report the item lexed under the new mode. ADR-0030 point 11's tests for MODE-02 pass.
3. **Bounded log.** A streamed parse of a modal grammar over a run of statements, in segments of 16 items, holds no more log entries at its peak over 100,000 statements than over 1,000.
4. **Speed.** Three default-job runs of `LayerBenchmarks` before and after, compared with `scripts/bench-compare.py`: the median ratio for `Layer.Tokens` is at most 1.05 on each input. A one-mode `Layer.Modal` stays within 1.05 of `Layer.Tokens`, as MODE-01 measured (1.02–1.03).
5. **Allocation.** Once the log has reached its working size, a write allocates nothing. MODE-01's test that `SetMode` over a one-mode stack allocates nothing still passes after a warm-up.
6. **Hot path.** Marks get no work that MODE-02 does not need. Layers whose grammars have no mode nodes get no work at all (ADR-0030 point 9).
7. **Simplicity**, the maintainer's standing preference.
8. **Streamed errors.** Added on 2026-11-26, after the build found question 5 under Context. A filter around a whole document whose restore is lowered at a change whose item the buffer has released reports, over buffers of one item to a segment, the same error as over text in memory.

## Options

### Fork A: which writes discard

#### A1. A write that changes the top (chosen by the maintainer)

Every `SetMode`, `PushMode` and `PopMode` appends an entry to the log. A write discards the buffered items at the cursor, re-runs item *i*'s step and lowers failures at *i* (ADR-0030 point 3) only when its new top differs from its old top by `EqualityComparer<TMode>.Default`. An undo in MODE-03 uses the same test. The `ParseMany` reset discards only when a channel's top differs from its initial mode.

- Pros:
  - No item is lexed twice under the same mode.
  - Failures past the cursor that name items still current are kept, so the error loses nothing.
  - The test is exact. Equal modes share one item parser in the selector cache, so a write that keeps the top leaves every buffered item as the reader would produce it again.
- Cons:
  - One equality call per write.
  - "A mode change" in ADR-0030 point 3 and in the whitepaper must now be read as a change of the top. This record says so.

**Result:** Chosen by the maintainer on 2026-11-26.

#### A2. Every write

Every `SetMode`, `PushMode` and `PopMode` discards and lowers, as the whitepaper's steps read literally.

- Pros: no comparison, and the whitepaper's wording stands as written.
- Cons: it re-lexes a buffered item at each write that keeps the top, and lowers failures past the cursor for nothing, so an error past it loses its expectations. Example C would do this at every `if`. `IfStmt` sets Operand mode before `Statement`. A `Statement` that chooses among alternatives reads the next item first, and the operand that follows sets Operand mode again.

**Result:** Ruled out by argument. A1 does less work and keeps more of the error, for one comparison.

#### A3. A write that changes the selected item plan

A write discards only when the reader's selector would choose a different item parser for the new top.

- Pros: it re-lexes least, in the rare grammar where two modes select one parser.
- Cons: the writer must reach the reader's plan cache, and it may call the selector for a mode that never lexes.

**Result:** Ruled out by argument. It couples the writer to the reader to save work in a rare case.

### Fork B: where CRL0008 is decided and built

#### B1. Move it to MODE-04 (chosen by the maintainer)

MODE-04 takes over the scope lines for the re-run that drops the first `groupIndex` outputs, the check, how CRL0008 is reported and its docs page, and ADR-0040 point 7. MODE-02's re-run drops nothing, and it asserts that a slot it re-runs has `groupIndex` 0.

- Pros: the check is decided where a grammar can reach it and a test can try it. MODE-02 builds less.
- Cons: ADR-0040 point 7 names MODE-02. That record has merged, so this record and a note in the index carry the move.

**Result:** Chosen by the maintainer on 2026-11-26.

#### B2. Decide CRL0008 here, build it in MODE-04

This record would settle three things now: what "equal" means for the check, whether CRL0008 throws or becomes an error in the result, and how released outputs are reached.

- Pros: one record for the whole invalidation rule.
- Cons: it decides without a grammar to try. Item equality wrongly flags token classes that have no value equality. Spans alone miss a token whose kind changes but whose span does not. A real cascade is the place to weigh these.

**Result:** Ruled out by the maintainer's choice of B1.

#### B3. Decide and build it here, tested through internal calls

- Pros: MODE-04 starts with the check built.
- Cons: the tests would truncate a `Transduce` layer's buffer by hand, on a path no grammar reaches before MODE-04, and MODE-04 would likely rework it.

**Result:** Ruled out by the maintainer's choice of B1.

### Fork C: how long the mode log keeps an entry

An entry is needed while something can still undo it, or while a saved failure still needs its index (ADR-0030 point 3, rule 2). Two facts bound that:

- **Indexes never decrease along the log.** A write appends at the cursor. Every rewind undoes the entries made since its mark before the cursor goes back (ADR-0030 point 2). So the entries below any index form a prefix of the log.
- **Nothing rewinds below the writing layer's horizon on its input.** That horizon is the minimum of its cursor, its floor, its own pin and the restart bookmark (§ Bookmarks and retention, ADR-0040 point 2).
  - A light mark undoes only while its attempt has consumed nothing, so its entries lie at the cursor.
  - `Atomic`, `Lookahead`, `Not` and `Longest` pin their start, so every entry made since they started lies at or above the horizon.
  - The restart bookmark is part of the horizon.

A filter's save is the only one that can outlive the horizon. A filter holds its start without pinning it (§ Bookmarks and retention), so the cursor and the horizon move past its start while its save is open.

#### C1. Keep every entry until the parse or the `ParseMany` item ends (the whitepaper as written)

- Pros: nothing to build.
- Cons: an entry holds an index and a stack reference, so at least 16 bytes per write, for the whole parse. A streamed modal grammar holds a log that grows with its input, which § Guarantees point 2 rules out. In memory, Example C's log can outgrow the script itself.

**Result:** Ruled out by argument. It breaks § Guarantees point 2 for modal grammars.

#### C2. Drop entries below the writing layer's horizon as its input buffer releases; an open save keeps the one index it needs (first recommended)

When the writing layer's cursor releases segments of its input buffer, the writer drops the log entries whose index is below the horizon it states. Marks keep absolute log lengths, and the log counts the entries it has dropped, so lengths still compare. Before entries are dropped, each open save whose length falls among them, and that has no index yet, keeps the index of the first dropped entry that changed the top. When restored, the save is lowered at that index if it has one, since indexes never decrease and a dropped entry is never undone. Otherwise it is lowered from the log as ADR-0030 says. In practice only a filter's save ever keeps an index. A run without a failure log has no saves and drops entries without checking.

Over input held whole in memory and read without a stream buffer, as by a parser run on its own over text or a list of tokens, nothing is released. The log then grows with the writes. That is not "no worse than holding the input itself", as this draft said: an entry takes about 32 bytes per write (question 6 under Context).

- Pros:
  - The log holds only entries at or above the horizon. Its size is then bounded by the window the pins hold open, which § Guarantees point 2 already requires of the buffers.
  - The work runs once per released segment, as ADR-0028's copies do.
  - Restores stay exact.
- Cons:
  - Each open save carries one more index, and the log carries a count of the entries it has dropped.
  - Release now reaches the writer's mode log as well as the failure log.

**Result:** Ruled out by the build. It leaves question 5 under Context open: a save gets its index only when entries are dropped, after the buffer has released the item there. On 2026-11-27 the maintainer agreed to C6 in its place.

#### C3. Drop entries below the horizon or the oldest open filter start, whichever is lower

- Pros: no save needs an index of its own.
- Cons: a filter around a whole document, such as `Program.Where(valid)`, holds its start at 0 for the whole parse, and the log grows as under C1.

**Result:** Ruled out by argument. One common grammar shape loses the bound.

#### C4. Count open marks and saves, and empty the log when none is open

- Pros: no horizon reasoning.
- Cons: every mark in a layer that writes a channel gains an increment and a decrement, against criterion 6. A filter around a whole document keeps a save open for the whole parse, so the log never empties.

**Result:** Ruled out by argument.

#### C5. Leave the log's bound to MODE-03

- Pros: MODE-02 builds less.
- Cons: the log grows from MODE-02 on. How long an entry lives depends on the saves MODE-02 lowers, so MODE-03 would rework the log MODE-02 builds.

**Result:** Ruled out. On 2026-11-27 the maintainer agreed to C6, which bounds the log in MODE-02.

#### C6. As C2, but each save keeps the first change made after it when the change is made, and the buffer copies the item there (chosen, added 2026-11-26)

Entries below the writing layer's horizon are dropped, as under C2. Revised on 2026-11-27, after review: they are dropped when the log fills, before it grows, below the lower of the writing layer's cursor and its floor, over any input. Until then they were dropped when the cursor entered a new segment of its input, which a cursor over memory never does (question 6 under Context). Saves no longer note the log's length. Instead, a change at *i* gives every open save that has no index yet the index *i*, the first change made after it, and a restore is lowered there. Before a stream buffer releases a segment, it copies the item and span at each open save's index in that segment, as ADR-0028 point 1 copies the saved failure's own position. A failure lowered there takes the copy. Dropping entries then needs nothing from the saves. Marks keep absolute log lengths for MODE-03, whose undo clears the index of each save whose first change it undoes.

- Pros:
  - It closes question 5 for every save without a pin, whether or not the log has dropped anything.
  - It is simpler than C2. A restore reads one index, no save reads the log, and dropping is a count.
  - The open saves without an index are those taken since the last change, which form the top of the stack of saves, so a change visits only those.
- Cons:
  - A change does work for each open save taken since the last change.
  - ADR-0028 point 1's list of copied positions gains one per open save.
  - MODE-03's undo must clear those indexes as it undoes changes.

**Result:** Chosen. Proposed after the build reproduced question 5. On 2026-11-27 the maintainer agreed to build it in place of C2, with criterion 8. An independent review of the build then found question 6, and C6's drop moved from segment entry to a full log. The measurements below are on `69ba0e1`, the final code.

- **Criterion 3.** Each log stays as small as the window its layer can go back over, over streamed input and over memory.
  - Example C, over 1,000 and over 100,000 statements in segments of 16 items, holds the same peak number of entries (`TheModeLogHoldsNoMoreEntriesOverALongerDocument`).
  - So does a parser run on its own over 1,000 and over 100,000 characters (`ALogOverInputInMemoryHoldsNoMoreEntriesOverALongerInput`).
  - After a pinned parse with 20,000 writes, the next parse on the same thread has a log with room for at most 1,024 entries (`AParseLeavesTheNextParseOnItsThreadNoLargeLog`).
  - Without the drop, or without the cap on the spare, those tests fail.
- **Criterion 4.** `LayerBenchmarks`, against the tree before MODE-02 (`fb86fe7`). Unpinned runs earlier in the build were bimodal: the large input took about 8.9 ms or about 11.6 ms, with standard deviations up to 2.1 ms. This machine has performance cores at 5.2–5.4 GHz and efficiency cores at 4.1 GHz, so every run counted here was pinned with `taskset -c 0-11`. Three default-job runs on each side, alternating, on `69ba0e1`:

  | Input | `Layer.Tokens`, after ÷ before (medians) | `Layer.Modal` ÷ `Layer.Tokens`, after (median of runs) | The same, before |
  | --- | --- | --- | --- |
  | small | 0.99 | 1.01 | 1.02 |
  | medium | 0.98 | 1.06 | 1.01 |
  | large | 0.95 | 1.00 | 1.00 |

  `Layer.Modal` over `Layer.Tokens` on the medium input misses the 1.05 threshold in this set: its runs gave 1.08, 1.00 and 1.06. An earlier pinned set, on the tree just before `2109e7e`, gave 1.02. The two layers run the same step loop, and the benchmark writes no mode, so no change in MODE-02 runs for one and not the other. `Layer.Modal`'s own after ÷ before on that input is 1.03.
- **Criterion 5.** MODE-01's allocation test passes. Per parse, a small `Layer.Tokens` input allocates 2.82 KB against 2.79 KB before MODE-02, and `Layer.Modal` 3.03 KB against 3.00 KB. The difference is the new fields. Each writer makes its log at its first write, so a grammar that writes no mode allocates no log.
- **Criterion 6.** Marks gain no work. A layer whose grammar has no mode nodes does no work for the log, which it never makes. The failure path gains checks in every layer, mode nodes or not: one comparison in `Reaches` when a failure is recorded at the furthest position, one flag in `Reject`, and one position per open save in `Preserve`. `ErrorBenchmarks` measures recording and building errors. Three pinned runs on each side, after ÷ before (medians): 0.86 to 1.03 across its eight benchmarks. Allocation is unchanged, except 8 B more when an error is built (`BuildError`, `InvalidWithError`).
- **Criterion 8.** `AFilterAroundADocumentReportsTheFirstChangeInItAfterTheBufferReleasedThatItem` reports the same error over one-item segments as over text in memory. Without the copy at release, it throws.

## Decision

Options **A1, B1 and C6**, accepted by the maintainer on 2026-11-27, when MODE-02 completed. The maintainer chose A1 and B1 on 2026-11-26. C2 was recommended until the build found question 5 under Context, and on 2026-11-27 the maintainer agreed to C6 in its place. C6 meets criteria 3, 5, 6 and 8, and criteria 1 and 2 pass under A1 and B1. Criterion 4's `Layer.Modal` over `Layer.Tokens` on the medium input came out at 1.06 in the final set, against 1.02 in the set before it. The maintainer accepted the record with that figure as this machine's spread, because the two layers run the same code in that benchmark.

1. **What a change is.** Every `SetMode`, `PushMode` and `PopMode` appends (index *i*, previous stack) to the writing layer's mode log, where *i* is its cursor's position. A write is a change when its new top differs from its old top by `EqualityComparer<TMode>.Default`. Only a change discards, re-runs and lowers. "A mode change at *i*" in ADR-0030 point 3 and in § Failure positions means such a write. An undo in MODE-03 discards and lowers when it changes the top back.
2. **A change, where the writing layer reads the channel's reader directly.**
   1. The reader's buffered items at *i* or later are discarded.
   2. If item *i* was buffered, the reader returns to its slot's `stepStart` *s* and `stateBefore`. Its `groupIndex` is 0 (Context, question 2), so nothing is dropped.
   3. The reader withdraws the diagnostics raised by steps that started at or after *s*, with `FailureLog.WithdrawFromStep`. It lowers its own furthest failure at *s* (ADR-0030 points 3 and 4).
   4. The writer lowers its own furthest failure at *i*.
   5. Later pulls lex under the new mode.
3. **Saves.** A save held by `Lookahead`, `Not`, `Longest`, a filter or `Adjacent` keeps the position of the first change made after it, from the moment that change is made. When restored, it is lowered there (ADR-0030 point 3, rule 2): until MODE-03 adds undo, every change is still in effect. Writes that keep the top do not count. Before a stream buffer releases a segment, it copies the item and span at each open save's position in it, and a failure lowered there takes the copy. This refines ADR-0028 point 1.
4. **`ParseMany`.** At each item's start, a channel whose top differs from its initial mode changes at the item's first index. That discards what the last item's lookahead lexed under another mode. The log starts the item empty (ADR-0041 point 9).
5. **The log's length (C6).** When the log is full, before it grows, the writer drops the entries below the lower of its cursor and its floor, over any input. Each writer makes its log at its first write. A spare writer given back with room for more than 1,024 entries lets its log go. MODE-03's marks will keep absolute lengths, so the log will count the entries it drops.
6. **CRL0008 (B1).** MODE-04 owns the re-run that drops outputs, the check, how CRL0008 is reported, its docs page, and ADR-0040 point 7. MODE-02 asserts that a slot it re-runs has `groupIndex` 0.
7. **What MODE-02 builds alone.** A write whose layer reads the reader through another layer discards nothing yet: the change affects only the steps the reader runs after it, as under MODE-01 (ADR-0041 point 10). MODE-04 adds the cascade. Marks neither save the log's length nor undo entries until MODE-03.

Deciding reasons:

- **A1:** a write that keeps the top leaves every buffered item correct, and the selector cache already treats equal modes as one. Discarding then costs work and error detail and buys nothing.
- **B1:** a single boundary cannot drop an output, so the check belongs with the first card whose grammars can.
- **C6:** it is the only option that bounds the log for every grammar shape without adding work to marks, and that gives every restore an item to report.

What would change this:

- A mode type whose equality is reference identity makes A1 treat equal-looking modes as changes. That is still correct, only slower, and the selector cache already caches such modes once per instance (MODE-05).
- A measurement showing that dropping entries costs time would argue for a cheaper drop in MODE-03.
- Nothing in MODE-02 reads the log after a write, so a drop that removed too much would not show until MODE-03's undo reads it. MODE-03's tests cover it.
- If MODE-03 finds a rewind that reaches below the horizon, the argument under Fork C fails and the record returns to it.

## Consequences

- **Easier.** Grammars like Example C re-lex only when the mode actually changes. Errors keep the failures past a write that kept the top. A modal grammar over streamed input keeps bounded memory.
- **Harder.** A save carries one more position and the copy of the item there, a change visits the saves taken since the last change, and the release path reaches the writer's mode log.
- **For MODE-03.** An undo discards and lowers only when it changes the top back, and clears the position of every open save whose first change it undoes. Marks keep absolute log lengths. A rewind never reaches a dropped entry, by the horizon argument under Fork C, and MODE-03's property tests should check that.
- **For MODE-04.** It gains CRL0008 and ADR-0040 point 7, and lifts point 7's limit by adding cascades.
- **For GEN-06.** Generated code follows the same rules. ADR-0031 lets an engine skip pins over input held whole in memory, but the log drops entries below the floor that those pins lower. An engine that skips them must still lower the floor for the log, or drop nothing.
- **A token the old mode cannot read.** Found in MODE-02's build. A discard replaces only a token the old mode read. If the lexer fails to read the token in the old mode, its failure ends the parse before the parser can change the mode. Example C's `Primary`, which the whitepaper elides, must therefore not look past an operand for a postfix call before the operand sets operator mode: in `a / b / c;`, operand mode reads `/ c;` as an unterminated regex. MODE-02's Example C puts calls after the switch. Lexer recovery (ERR-07) would turn such a failure into an error token, which a discard drops along with its diagnostic.
- **Tests must force a discard.** `if (ok) /x/.test(s)` lexes the regex without one: the parser reads `)` and sets Operand mode before it pulls the next item. Criterion 1 therefore needs a case where the parser reads the item first. One such case is a statement after `;`, whose first item a choice reads in Operator mode before the operand sets Operand mode.

## Changes made

- Whitepaper:
  - [§ 3. Mode channels](../../../whitepaper.md#3-mode-channels): the invalidation rule says that a write changes the mode only when it leaves a different mode on top, and that a write that keeps it discards and lowers nothing. Step 3 logs every write, and says that a full log drops its entries below the lower of the cursor and the floor. The rollback rule applies steps 1 and 2 only when an undo changes the mode on top. A discard replaces only a token the old mode could read. The rule links this record.
  - [§ Failure positions](../../../whitepaper.md#failure-positions): a mode change is a write that changes the mode on top. A save keeps the position of the first change after it and is lowered there on restore, and a buffer copies the item there before release.
  - [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention): the mode log drops its entries below the lower of the cursor and the floor whenever it fills.
  - [§ C. A parser that steers its lexer](../../../whitepaper.md#c-a-parser-that-steers-its-lexer): re-lexing needs the old mode to read the token, so `Primary` ends with the operand, and a call is parsed after `Operand` sets Operator mode.
  - Checked and unchanged: § Guarantees point 1 ("unless a mode change invalidates it"), § Running pipelines, and the "Across several layers" paragraph, which MODE-04 builds.
- Cards:
  - [MODE-02](../../active/07-mode-channels/MODE-02-invalidation-rule.md): the CRL0008 and ADR-0040 point 7 scope lines moved to MODE-04 (B1), and the summary dropped "including inside multi-output steps". Scope lines give A1 and C6, and the record joined its artifacts.
  - [MODE-04](../../active/07-mode-channels/MODE-04-multi-layer-invalidation-cascade.md): gained the CRL0008 and ADR-0040 point 7 scope lines.
  - [MODE-03](../../active/07-mode-channels/MODE-03-rollback-rule.md): gained four scope lines:
    - an undo discards and lowers only when it changes the mode on top back;
    - marks keep absolute log lengths, so the log counts the entries it drops;
    - undoing a save's first change clears the position the save keeps;
    - property tests check that no rewind reaches a dropped entry and that a drop removes nothing a rewind needs.
  - [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md): gained a scope line: generated code that skips pins over input in memory still lowers the floor for the mode log.
- Index: a note that this record refines ADR-0030 points 1 and 3 and ADR-0028 point 1, and moves ADR-0040 point 7 to MODE-04.
