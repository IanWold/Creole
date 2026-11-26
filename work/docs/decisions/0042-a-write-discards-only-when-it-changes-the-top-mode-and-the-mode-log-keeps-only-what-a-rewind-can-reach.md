# ADR-0042: A write discards only when it changes the top mode, the mode log keeps only what a rewind can still reach, and CRL0008 moves to cascades

| | |
| --- | --- |
| Status | Proposed |
| Date | 2026-11-26 |
| Cards | [MODE-02](../../active/07-mode-channels/MODE-02-invalidation-rule.md), [MODE-03](../../active/07-mode-channels/MODE-03-rollback-rule.md), [MODE-04](../../active/07-mode-channels/MODE-04-multi-layer-invalidation-cascade.md) |
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

## Criteria

Fixed before any option is tried:

1. **Items.** After any sequence of writes, the writing layer reads the items that a lex from scratch under the same mode history gives. Example C lexes `/` as division in `(a) / 2` and as a regex in `if (ok) /x/.test(s)`. One case must make the parser read an item under the old mode before the change, so that a discard happens (MODE-02's first criterion).
2. **Errors.** MODE-02's second and third criteria pass. A filter and a `Context` around an operand that starts with `SetMode` each report the item lexed under the new mode. ADR-0030 point 11's tests for MODE-02 pass.
3. **Bounded log.** A streamed parse of a modal grammar over a run of statements, in segments of 16 items, holds no more log entries at its peak over 100,000 statements than over 1,000.
4. **Speed.** Three default-job runs of `LayerBenchmarks` before and after, compared with `scripts/bench-compare.py`: the median ratio for `Layer.Tokens` is at most 1.05 on each input. A one-mode `Layer.Modal` stays within 1.05 of `Layer.Tokens`, as MODE-01 measured (1.02–1.03).
5. **Allocation.** Once the log has reached its working size, a write allocates nothing. MODE-01's test that `SetMode` over a one-mode stack allocates nothing still passes after a warm-up.
6. **Hot path.** Marks get no work that MODE-02 does not need. Layers whose grammars have no mode nodes get no work at all (ADR-0030 point 9).
7. **Simplicity**, the maintainer's standing preference.

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

#### C2. Drop entries below the writing layer's horizon as its input buffer releases; an open save keeps the one index it needs (recommended)

When the writing layer's cursor releases segments of its input buffer, the writer drops the log entries whose index is below the horizon it states. Marks keep absolute log lengths, and the log counts the entries it has dropped, so lengths still compare. Before entries are dropped, each open save whose length falls among them, and that has no index yet, keeps the index of the first dropped entry that changed the top. When restored, the save is lowered at that index if it has one, since indexes never decrease and a dropped entry is never undone. Otherwise it is lowered from the log as ADR-0030 says. In practice only a filter's save ever keeps an index. A run without a failure log has no saves and drops entries without checking.

Over input held whole in memory and read without a stream buffer, as by a parser run on its own over text or a list of tokens, nothing is released. The log then grows with the writes, which is no worse than holding the input itself.

- Pros:
  - The log holds only entries at or above the horizon. Its size is then bounded by the window the pins hold open, which § Guarantees point 2 already requires of the buffers.
  - The work runs once per released segment, as ADR-0028's copies do.
  - Restores stay exact.
- Cons:
  - Each open save carries one more index, and the log carries a count of the entries it has dropped.
  - Release now reaches the writer's mode log as well as the failure log.

**Result:** Open. Tried in the build against criteria 3 to 6.

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

**Result:** Open. The maintainer may prefer it. The recommendation is C2.

## Decision

Proposed: options **A1, B1 and C2**. The maintainer chose A1 and B1 on 2026-11-26. C2 is recommended and is tried in the build against criteria 3 to 6. Criteria 1 and 2 are tests of the invalidation rule under A1 and B1.

1. **What a change is.** Every `SetMode`, `PushMode` and `PopMode` appends (index *i*, previous stack) to the writing layer's mode log, where *i* is its cursor's position. A write is a change when its new top differs from its old top by `EqualityComparer<TMode>.Default`. Only a change discards, re-runs and lowers. "A mode change at *i*" in ADR-0030 point 3 and in § Failure positions means such a write. An undo in MODE-03 discards and lowers when it changes the top back.
2. **A change, where the writing layer reads the channel's reader directly.**
   1. The reader's buffered items at *i* or later are discarded.
   2. If item *i* was buffered, the reader returns to its slot's `stepStart` *s* and `stateBefore`. Its `groupIndex` is 0 (Context, question 2), so nothing is dropped.
   3. The reader withdraws the diagnostics raised by steps that started at or after *s*, with `FailureLog.WithdrawFromStep`. It lowers its own furthest failure at *s* (ADR-0030 points 3 and 4).
   4. The writer lowers its own furthest failure at *i*.
   5. Later pulls lex under the new mode.
3. **Saves.** A save held by `Lookahead`, `Not`, `Longest` or a filter notes the log's length. When restored, it is lowered at the lowest index among the changes made since it that are still in effect (ADR-0030 point 3, rule 2). Entries that kept the top do not count.
4. **`ParseMany`.** At each item's start, a channel whose top differs from its initial mode changes at the item's first index. That discards what the last item's lookahead lexed under another mode. The log starts the item empty (ADR-0041 point 9).
5. **The log's length (C2).** When the writing layer's cursor releases segments of its input, the writer drops the entries below the horizon it states. Marks keep absolute lengths. Before dropping, each open save that a dropped entry follows, and that has no index yet, keeps the index of the first dropped entry that changed the top, and its restore lowers at that index.
6. **CRL0008 (B1).** MODE-04 owns the re-run that drops outputs, the check, how CRL0008 is reported, its docs page, and ADR-0040 point 7. MODE-02 asserts that a slot it re-runs has `groupIndex` 0.
7. **What MODE-02 builds alone.** A write whose layer reads the reader through another layer discards nothing yet: the change affects only the steps the reader runs after it, as under MODE-01 (ADR-0041 point 10). MODE-04 adds the cascade. Marks neither save the log's length nor undo entries until MODE-03.

Deciding reasons, as proposed:

- **A1:** a write that keeps the top leaves every buffered item correct, and the selector cache already treats equal modes as one. Discarding then costs work and error detail and buys nothing.
- **B1:** a single boundary cannot drop an output, so the check belongs with the first card whose grammars can.
- **C2:** it is the only option that bounds the log for every grammar shape without adding work to marks.

What would change this:

- A mode type whose equality is reference identity makes A1 treat equal-looking modes as changes. That is still correct, only slower, and the selector cache already caches such modes once per instance (MODE-05).
- A measurement in the build showing that release-time dropping costs time would argue for C5, and for a cheaper drop in MODE-03.
- If MODE-03 finds a rewind that reaches below the horizon, C2's argument fails and the record returns to Fork C.

## Consequences

- **Easier.** Grammars like Example C re-lex only when the mode actually changes. Errors keep the failures past a write that kept the top. A modal grammar over streamed input keeps bounded memory.
- **Harder.** A save carries one more index, and the release path reaches the writer's mode log.
- **For MODE-03.** An undo discards and lowers only when it changes the top back. Marks keep absolute log lengths. A rewind never reaches a dropped entry, by the horizon argument under Fork C, and MODE-03's property tests should check that.
- **For MODE-04.** It gains CRL0008 and ADR-0040 point 7, and lifts point 7's limit by adding cascades.
- **For GEN-06.** Generated code follows the same rules.
- **Tests must force a discard.** `if (ok) /x/.test(s)` lexes the regex without one: the parser reads `)` and sets Operand mode before it pulls the next item. Criterion 1 therefore needs a case where the parser reads the item first. One such case is a statement after `;`, whose first item a choice reads in Operator mode before the operand sets Operand mode.

## Changes made

Made with this draft:

- Cards:
  - [MODE-02](../../active/07-mode-channels/MODE-02-invalidation-rule.md): the CRL0008 and ADR-0040 point 7 scope lines move to MODE-04 (B1), and the summary drops "including inside multi-output steps". A scope line gives A1. The record joins its artifacts.
  - [MODE-04](../../active/07-mode-channels/MODE-04-multi-layer-invalidation-cascade.md): gains the CRL0008 and ADR-0040 point 7 scope lines.

On acceptance:

- Whitepaper:
  - § 3. Mode channels: the invalidation rule says that a change is a write that changes the top, and that every write is logged. It says how long the log keeps an entry.
  - § Failure positions: "a mode change" becomes a change of the top.
  - § Bookmarks and retention: the writer's mode log is trimmed at release, alongside the buffer.
- Cards: MODE-03's scope gains the undo test of point 1 and the absolute lengths of point 5. MODE-02 gains a scope line for C2, or MODE-03 does if C5 is chosen.
- Index: a note that this record moves ADR-0040 point 7 to MODE-04 and refines what ADR-0030 point 3 calls a mode change.
