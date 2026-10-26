# ADR-0028: The items an error needs behind the cursor are copied when the buffer releases them, not when a construct starts

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-26 |
| Cards | [LAY-01](../../active/06-layers-and-pipelines/LAY-01-streambuffer-and-pulling-cursor.md), [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md), [MODE-02](../../active/07-mode-channels/MODE-02-invalidation-rule.md), [LAY-08](../../active/06-layers-and-pipelines/LAY-08-per-layer-parse-state.md), [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md) |
| Whitepaper | [§ Failure positions](../../../whitepaper.md#failure-positions), [§ The stream buffer](../../../whitepaper.md#the-stream-buffer), [§ Mode channels](../../../whitepaper.md#3-mode-channels) |
| Research | [Phase 0 review](../research/2026-10-25-phase-0-review.md), finding 16 |
| Supersedes | — |

## Context

A streamed layer's buffer releases segments below its horizon and refills them (§ The stream buffer). An error built later may need items behind the cursor. Three decisions say how it gets them:

- **Filters and chained operators** copy their first item before they run ([ADR-0014](0014-a-rejected-filter-reports-at-the-start-of-the-value.md) point 6). ERR-02 built this in [`FilterPlan.cs`](../../../src/Creole/Plans/FilterPlan.cs) and [`ExpressionPlan.cs`](../../../src/Creole/Plans/ExpressionPlan.cs), for every input.
- **`Context`** records the position of its first item. A streamed layer is to copy that item's span "when it is recorded", that is, on entry ([ADR-0021](0021-how-failures-are-recorded-and-merged.md) point 4). This was deferred to LAY-01.
- **The furthest failure** is a position. Its span is to be copied when it is recorded on a streamed layer (ADR-0021 point 4). Its unexpected item is read when the error is built (ADR-0021 point 1).

**Copies taken on entry go stale** (review finding 16). Mode invalidation discards buffered items at or past the cursor and lexes them again (§ Mode channels). A construct that starts with `SetMode` copies the item that invalidation then discards. Example C's `Operand` starts this way. A filter around it would report the token lexed under the old mode, and a `Context` around it would keep that token's span. Spans can differ between modes as well as items: under an operand mode, the item at the cursor may be `/x/` rather than `/`.

**The furthest failure's item has no rule on streamed input.** This was found while drafting this record. Take `Atomic(p).Or(q.Where(pred))`:

1. `p` consumes from s and fails at f. `Atomic` rewinds to s and releases its pin. The failure at f stands (§ The error model, Labels).
2. `q` consumes from s to well past f, and `pred` rejects.
3. The filter restores the failure it saved on entry, the one at f. Its own rejection at s is earlier, so it is ignored.
4. The error reads the item at f with `TryPeekAt` ([`FailureLog.cs:394`](../../../src/Creole/Errors/FailureLog.cs#L394)). Once the buffer has released f, that throws ([`Cursor.cs:300`](../../../src/Creole/Cursor.cs#L300)).

**Two invariants frame the choice:**

- **Release.** The buffer releases only items below the cursor and every pin. Those are consumed items, and no rewind can return to them: a rewind goes to a pinned position, and a light mark rewinds only when nothing was consumed.
- **Invalidation.** A mode change at index *i* happens with the cursor at *i*. It discards only items at *i* or later, so it never touches a consumed item. A rollback applies invalidation again at a mode-log entry's index, which is at or past the pinned position it rewinds to. Across layers, each layer pins its input at the `stepStart` of the oldest item it still holds, so a cascade never truncates below a lower buffer's horizon (§ Bookmarks and retention).

So an item the buffer has released is never one that invalidation can discard. An item invalidation can discard is still in the buffer.

Constraints:

- **Failure paths allocate nothing** (ERR-02's criterion).
- **No new pinning bookmarks.** A pin held for an error would break guarantee 2: a filter around a large construct would hold the whole construct.
- **G7.** The interpreter and generated code must report the same errors.
- **Simplicity:** the maintainer's standing preference.
- **Speed.** In-memory parses must not slow down. The Phase 0 gate measures them.

## Options

### A. Copy on entry, and take the copy again on a mode change

Constructs copy on entry, as now, and `Context` and the furthest failure copy too. A mode change at *i* takes again every open copy whose position is *i*.

**Pros:**

- Close to the current code and to ADR-0014 point 6's wording.
- The buffer knows nothing about errors.

**Cons:**

- The mode-change path must find every open copy. That needs a register of open constructs per layer, which is the main cost of option B as well.
- It copies on every entry, and on every new furthest position, whether or not the copy is used.
- Over in-memory input the copies are never needed, so they would have to be turned off there.

### B. Record positions only, and copy at release (recommended)

Constructs and the failure log record positions only. Just before the buffer releases a segment, the layer reading it copies the item and span at each position it still refers to in that segment.

**Pros:**

- Correct by construction. It copies only released items, which are consumed, so invalidation can never have discarded them. Every other item is read from the buffer, under the current mode.
- One rule for filters, chained operators, `Context` and the furthest failure. It also closes the gap in the furthest failure's item.
- It copies only when a referenced item is actually released. Over in-memory input, which releases nothing, it copies nothing. The filter's peek on entry goes away.

**Cons:**

- Filters and chained operators must register their start while they are open, as a `Context` frame does now.
- The buffer's release path must call into the layer that reads it. That layer already supplies the horizon (§ Bookmarks and retention), so this adds a step to an existing exchange, not a new dependency.
- One check per released segment, over a list bounded by nesting depth.

### C. Copy on first consumption

The construct records its position on entry. The copy is taken the first time the cursor moves past that position.

**Pros:**

- Correct for the same reason as B: once consumed, an item is never invalidated.

**Cons:**

- Every cursor advance must check whether a copy is due. That is the hottest path in the engine.

### D. Pin the start

A construct an error may report keeps its start in the buffer until it exits.

**Pros:**

- No copies.

**Cons:**

- Breaks guarantee 2. `Many(record).Where(…)` over a large file would hold the whole file.
- Contradicts ADR-0014 point 6, which rules out a pinning bookmark.

### E. Keep copying on entry (the current design)

Keep ADR-0014 point 6 and ADR-0021 point 4 as written. Document that a construct starting with a mode change reports the item lexed under the old mode.

**Pros:**

- No change to ERR-02's code.

**Cons:**

- Errors are wrong for a pattern the whitepaper's own Example C uses.
- Leaves the furthest failure's item unhandled. That case throws rather than misreports.

## Decision

Option **B**, as recommended. Accepted by the maintainer on 2026-10-26.

1. **What an error may read behind the cursor.** Each layer's failure log knows every position an error built later may read:
   - the start of each open filter, and of a chained operator while it is matched: item and span;
   - the start of each `Context` frame the log holds, whether open or kept for the furthest failure: span;
   - the furthest failure's position: item and span;
   - the end of the input that a rejection widened the furthest failure to: span.
2. **Positions on entry.** No construct copies an item or a span when it starts. A filter and a chained operator register their start with the layer's failure log while they are open, as `Context` does now. A layer without a failure log registers nothing.
3. **Copy at release.** Before a buffer releases a segment, the layer reading it copies the item and span at each position in point 1 that falls in that segment. The copies go into storage the log reuses, so they allocate nothing once it is large enough. When a rejection is recorded at a start that was already copied, the record takes that copy.
4. **Reads.** An error uses the copy if one was taken. Otherwise it reads the buffer, which still holds the item.
5. **Mode changes.** Point 3 copies only consumed items, and invalidation discards only unconsumed ones, so a copy is never stale. A construct that starts with `SetMode` reports the item lexed under the new mode.
6. **In-memory input** keeps every item until the parse ends. Nothing is copied, and errors read the input, as ADR-0021 point 4 says.
7. **Engines.** The rule binds every engine. Generated code registers the same starts (GEN-06).
8. **Refinements.**
   - This refines ADR-0014 point 6. The filter's start item is copied when the buffer releases it, not before `p` runs. The constraints there stand: no allocation on the failure path, and no pinning bookmark.
   - It refines ADR-0021 point 4. A streamed layer copies a span when its item is released, not when the span is recorded.
   - It fills ADR-0021 point 1 for streamed input. The furthest failure's item is copied the same way.
9. **Left to other cards.**
   - **[LAY-08](../../active/06-layers-and-pipelines/LAY-08-per-layer-parse-state.md):** whether the furthest failure survives a mode change at or before its position, or is reset or restored (review finding 15). Under this record, a failure kept across such a change reads the item lexed under the new mode. The register of open starts in point 2 is per-layer parse state, so LAY-08 lists it.
   - **[LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md):** constructs that read a whole range back, such as `Text` and `Slice` (review finding 17). They need every item from their start to their end, so copying a few positions does not serve them.

Deciding reason: B is the only option that is correct under mode changes without extra work on a hot path. It also copies least, and it gives one rule where there were three.

What would change this:

- A measurement showing the check at release costs measurable time. That is unlikely, because it runs once per segment.
- Any design that releases items the parse has not consumed, for example a future release ahead of the cursor. The invariant this rests on would then fail, and this record would need replacing.

## Consequences

**Easier:**

- One mechanism for every item and span an error needs behind the cursor, in every layer.
- In-memory parses lose the filter's peek on entry. Their errors do not change.
- MODE-02 needs no special handling for copies: invalidation and rollback leave them correct.

**Harder:**

- ERR-02's code changes, on a Done card. `FilterPlan` and `ExpressionPlan` register their start instead of peeking. `FailureLog` and `FailureMark` keep their item fields as the copy taken at release. A `Context` frame gains room for a copied span. Behaviour over in-memory input does not change, and the existing tests must pass unchanged.
- The buffer's release step gains a call into the layer that reads it, made before each segment goes back to the pool.
- Compacting context frames must keep the spans already copied into them.

**Ruled out:**

- Copying an item or span when a construct starts.
- Pinning the buffer for an error's sake.

**Risks and how they are watched:**

- **A copy missed before release.** LAY-01 tests a buffer that has released the items involved. A rejected filter, a chained operator, a `Context` line and the furthest failure must each report the same error as over in-memory input. The cases include the `Atomic(p).Or(q.Where(pred))` example above.
- **A stale item after a mode change.** MODE-02 tests a filter and a `Context` around Example C's `Operand`, which starts with `SetMode`, and checks that each reports the item lexed under the new mode.
- **Generated code disagreeing.** The GEN-08 differential tests compare errors over streamed input.

## Changes made

- **Whitepaper:**
  - [§ Failure positions](../../../whitepaper.md#failure-positions): recording keeps positions, not items. A filter or chained operator registers its start while it runs. A streamed layer copies the item and span at each position its errors still refer to before its buffer releases them, the furthest failure's own included. This replaces "captures the item its error needs by value" and "captures their spans by value when they are recorded". Links this record.
  - [§ The stream buffer](../../../whitepaper.md#the-stream-buffer): before a segment is released, the layer reading the buffer copies out the items and spans its errors may still need.
  - [§ Mode channels](../../../whitepaper.md#3-mode-channels): the invalidation rule notes that errors copy only consumed items, so invalidation never leaves an error holding a discarded one.
- **Cards:**
  - [LAY-01](../../active/06-layers-and-pipelines/LAY-01-streambuffer-and-pulling-cursor.md): the two scope items on capture become the rule in this record, including the change to ERR-02's code. A criterion is added: a buffer that has released the items involved reports the same errors as in-memory input.
  - [MODE-02](../../active/07-mode-channels/MODE-02-invalidation-rule.md): a criterion for a filter and a `Context` around Example C's `Operand`.
  - [LAY-08](../../active/06-layers-and-pipelines/LAY-08-per-layer-parse-state.md): the register of open starts and the copies taken at release are listed as per-layer state.
  - [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md): generated code registers the same starts.
- **Decisions index:** a note that this record refines ADR-0014 point 6 and ADR-0021 point 4, and fills ADR-0021 point 1 for streamed input.
