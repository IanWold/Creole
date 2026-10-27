# ADR-0031: Constructs that read their input back pin their start, and a producing layer pins the step it is running

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-27 |
| Cards | [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md), [MODE-03](../../active/07-mode-channels/MODE-03-rollback-rule.md), [ERR-04](../../active/04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md), [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md) |
| Whitepaper | [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention), [§ Guarantees](../../../whitepaper.md#guarantees), [§ Text capture](../../../whitepaper.md#text-capture) |
| Research | [Phase 0 review](../research/2026-10-25-phase-0-review.md) finding 17, [Parlot 2.0 baselines and generator](../research/2026-10-24-parlot-2-0-baselines-and-generator.md) finding 23 |
| Supersedes | — |

## Context

§ Bookmarks and retention says four constructs pin the buffer: `Atomic`, `Lookahead`, `Not` and `Longest`, compiled or not. The buffer may release every segment below the minimum of the cursor and the oldest pin. Other constructs also go back to their start:

| Construct | What it does at its start | Window |
| --- | --- | --- |
| `Slice`, `Text` | Run `p`, then `Since(start)` copies the range ([SlicePlan.cs](../../../src/Creole/Plans/SlicePlan.cs), [TextPlan.cs](../../../src/Creole/Plans/TextPlan.cs)) | Whatever `p` consumed: no bound |
| `String` of more than one character | `Reset(start)` after a partial match ([StringPlan.cs](../../../src/Creole/Plans/StringPlan.cs)) | The literal's length less one |
| `CIString` | The same, and `Since(start)` on a match | The literal's length |
| Compiled `Longest` | Reads past the match, backs up, and may run the winner or the interpreter again from the start ([DfaLongestPlan.cs](../../../src/Creole/Plans/DfaLongestPlan.cs)) | The token plus its backup. Already pinned |
| `Custom` | Its scanner may scan a copy of the cursor and assign it back only on success, as [custom-scanners.md](../../../docs/concepts/custom-scanners.md) tells authors to. The original stays at the start while the copy reads ahead | The scanner's token |
| Filters, chained operators, `Context`, custom scanners | Read one position behind the cursor when an error is built | One position each. Covered by [ADR-0028](0028-an-errors-items-are-copied-when-the-buffer-releases-them.md) |

A custom scanner has no method that rewinds or slices. The public `Cursor<T>` offers only `Available`, `Advance`, `TryPeek`, `TryRead` and `IsAtEnd`. But `Cursor<T>` is a struct, and a copy is an independent position over the same input ([cursor.md](../../../docs/engines/cursor.md)). A scanner goes back by keeping the original while a copy reads ahead. A release computed from the copy's position can free the segment the original still holds, and the original then reads pooled memory without any error.

Over a stream buffer, `Since` and `Reset` throw when the start's segment has been released ([Cursor.cs](../../../src/Creole/Cursor.cs)). Nothing calls `Release` yet (LAY-01's outcome), so no test fails today. Once LAY-02 computes the horizon, a `Text` whose range crosses a released segment throws. The Phase 0 review found this with the gate's own `NoneOf(...).SkipAtLeastOnce().Text()` (finding 17). `StreamBuffer.Release` keeps the segment holding the item just below the horizon (ADR-0028 point 3). So a window of one item never fails, and a one-character `String` is safe. Longer windows fail when segments are small, and the tests use segments of one and two items.

Two earlier records expect this choice:

- [ADR-0030](0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 1 lists "pins held by `Atomic`, `Lookahead`, `Not`, `Longest` and constructs that read behind the cursor" as a scoped part. The whitepaper does not name those constructs.
- ADR-0028 point 9 leaves "constructs that read a whole range back, such as `Text` and `Slice`" to LAY-02, because copying a few positions does not serve them.

§ Guarantees point 2 says "a large document parsed without `Atomic`, `Lookahead` or `Not` keeps a constant number of items per boundary". That is false for a `Text` that spans the document, under any choice below, because the captured text has to be held somewhere.

Drafting this record exposed a second gap, in the layer's own pin. § Bookmarks and retention says "each layer in turn pins its own input at the `stepStart` of the oldest output slot it still holds". While a step runs, it has emitted nothing yet. If the layer downstream has released every earlier output, nothing pins the running step's start. A release during the step can then free that start. The output the step then emits has a `stepStart` the buffer no longer holds, so MODE-02 cannot produce it again. Whether this happens depends on when `Release` runs, which no record fixes.

Constraints:

- **Speed.** BENCH-01 passed at 1.37× Parlot against a bar of 1.5× ([ADR-0003](0003-numeric-gate-thresholds.md)). Its parses are in memory and release nothing. Finding 18 warns that Phase 1 changes the hot path it measured.
- **Slots.** A `StreamBuffer<T>` keeps a 32-byte packed slot beside each item (SPAN-02's outcome). A `char` is 2 bytes, so a window pinned in a `StreamBuffer<char>` holds about 17 times its characters' own size. Whether the character buffer under a lexer is a `StreamBuffer` depends on LAY-05 and SPAN-06: a decoding layer would produce one, while a `TextReader` source might not.
- **Prior art.** The FND-10 note records from Parlot 2.0's streaming guide that its compacting buffer "only retains the text a parser can still move back to", that "memory is bounded by the largest token or backtracking region", and that choices, options and repetitions pin their start (finding 23). Parlot backtracks by default, so it pins where Creole's commit semantics need nothing. How Parlot keeps a captured token's text across compaction is not in the note and is *unverified*.

## Options

### Fork 1. What keeps a range that a construct reads back

#### 1A. Nothing more (the current whitepaper)

Only the four constructs pin.

- Pros: no change; nothing on the hot path.
- Cons: `Slice`, `Text`, `CIString` and a longer `String` throw over streamed input once their range crosses a released segment. Every streamed lexer uses them. The only fix would be to forbid them over streams, which breaks G2 (§ Design goals and non-goals) for the pipelines it is meant for.

#### 1B. Each such construct pins its start while it runs (recommended)

A construct that reads its range back, or rewinds into it, pins its start until it exits, as `Atomic` does. That covers `Slice`, `Text`, and a `String` or `CIString` of more than one character. A construct that needs only some positions holds them under ADR-0028, as filters do now and as `Spanned` will (SPAN-04).

- Pros:
  - One mechanism, which ADR-0030 point 1 already lists.
  - Generated code does the same.
  - LAY-06's buffer limit covers long captures with no extra work.
  - It is Parlot's approach.
- Cons:
  - A pinned window keeps each item's slot as well as the item. That is about 17× a capture's characters in a `StreamBuffer<char>`.
  - Guarantee 2 changes.
  - Each `String` and `Text` takes a pin on the hot path, unless Fork 2 and point 4 below let engines skip it.

#### 1C. Copy at release, extending ADR-0028 to ranges

An open `Slice` or `Text` registers its start. Before a segment is released, the layer copies the open captures' items in it into a side buffer it reuses, and `Since` joins that buffer to what is still held. `String` and `CIString` still pin, because they move the cursor back, and a cursor cannot read a copy.

- Pros:
  - It holds 2 bytes per character rather than 34.
  - Segments go back to the pool during a long capture.
- Cons:
  - Two mechanisms.
  - `Since` gains a second path.
  - Nested captures share the outermost start.
  - Generated code must do the same.
  - LAY-06 must count the side buffer.
  - `Text` copies again to build its string, so the peak is about twice the characters.
  - It saves memory only where three things hold at once: a character buffer with slots, a long capture, and a layer that is not inside a step. Fork 2 shows a producing layer retains its step's input anyway.

### Fork 2. The producing layer's own pin

#### 2A. Pin at the oldest held output's `stepStart` (the current whitepaper)

- Pros: no change.
- Cons: the gap above, unless something else stops a release during a step.

#### 2B. Release a producing layer's input only between its steps

- Pros: closes the gap without a new pin.
- Cons:
  - It ties when `Release` runs to step ends.
  - The last layer of a pipeline does not run in steps, so it needs a different trigger. That gives two trigger rules.

#### 2C. Pin the running step's start from the moment the step begins (recommended)

The layer's pin moves to the start of each step as the step begins. It stays at the `stepStart` of the oldest output still held, which is the same pin, now taken earlier.

- Pros:
  - `Release` may run at any time.
  - No `Atomic` spans two steps (ADR-0030 point 1), so every pin a node takes inside a step lies at or above the step's start. In a producing layer, node pins add nothing to retention, and an engine may skip them.
- Cons:
  - A step that emits nothing, such as a trivia step, still holds its input until it ends. It would hold that under 2B too.

## Decision

Options **1B and 2C**. Accepted by the maintainer on 2026-10-27, as written, and edited in place before merge to pin `Custom` (see the note after Changes made).

**1. Constructs that pin.** These pin their start while they run:

| Construct | Why |
| --- | --- |
| `Atomic`, `Lookahead`, `Not`, every `Longest`, compiled or not | They may rewind (unchanged) |
| `Slice`, `Text` | They read their range back |
| `String` or `CIString` of more than one character | They rewind into their range after a partial match. `CIString` also reads the range back |
| `Custom` | Its scanner may keep the cursor at its start while a copy reads ahead |

A one-character `String` need not pin, because the buffer keeps the item below the horizon (ADR-0028 point 3). A construct added later follows the same rule:

- If it reads its range back or rewinds into it, it pins its start.
- If it needs only certain positions, it holds them under ADR-0028.

A copy of the cursor made inside a pinned construct carries the floor (point 3), so it never releases below the construct's start.

**2. The layer's own pin.** A producing layer pins its input at the start of the step it is running from the moment the step begins. When the step ends, the pin moves to the `stepStart` of the oldest output the layer still holds, or to the next step's start if it holds none. A restart keeps the pin (ADR-0030 point 1).

**3. Pins nest, so a layer tracks only the oldest.** Pins are scoped, and the cursor never moves below an open pin's start. So the oldest open pin is always the lowest one. A layer keeps one floor: each pin saves it, lowers it to its own start, and restores it on exit. No count is needed. A layer's horizon on its input is the minimum of four positions:

- its cursor;
- that floor;
- its own pin from point 2;
- the restart bookmark, when there is one.

**4. Where pins may be skipped.** Each case below leaves the horizon where it would be anyway, so skipping changes no result, no error and no buffer limit:

- Input held whole in memory releases nothing, so an engine may take no pins there.
- Inside a producing layer's step, point 2's pin lies at or below every node pin, so an engine may skip node pins in that layer.

So pins cost time only in the last layer of a pipeline over streamed input. That includes a single-layer parse.

**5. § Guarantees point 2 becomes:**

> **Bounded memory.** Memory at a boundary is proportional to the longest window held open by a pin in the layer above, plus the input of the step that layer is running and of the steps that produced the items it still holds. `Atomic`, `Lookahead`, `Not`, `Longest`, `Slice`, `Text`, `Custom` and a longer `String` or `CIString` pin (§ Bookmarks and retention). `Or` and repetition add nothing, so a large document keeps a constant number of items per boundary, however deeply its `Or`s nest, as long as no pin in the last layer spans it.

**6. What LAY-02 builds.** LAY-02 keeps:

- every pin in point 1;
- `Atomic`'s release at its exit;
- the layer's pin in point 2;
- the horizon, with calls to `Release` that pass the layer's failure log;
- the two-field light mark and its criteria.

The parts that ADR-0030 adds to marks go to the cards that create them. Each card takes point 9's rule for leaving its part out, and point 6's re-run of a `Longest` winner that can change its part:

| Part | Card | Change |
| --- | --- | --- |
| Mode log | MODE-03 | Gains point 9's rule for the mode log and point 6's re-run |
| Own diagnostics | ERR-04 | Gains point 9's rule for diagnostics and point 6's re-run. MODE-03 already holds the rewind order |
| Layer state | LAY-04 | None. Its scope already has both |

LAY-02's scope says pins are "reference-counted". That changes to point 3.

Deciding reasons:

- **1B** is one mechanism, and ADR-0030 already lists it. Under point 4, it costs only in a streamed last layer at character level. 1C saves memory only in that layer, and only over a buffer with slots, yet it needs a second mechanism in every engine.
- **2C** closes a real gap without constraining when `Release` runs. It also makes node pins redundant in producing layers, which is what lets 1B stay off the lexer's hot path.

What would change this:

- **1B's memory constant.** A measurement in LAY-05 or BENCH-02 of a streamed single-layer character parse might show pinned slots dominating memory for long captures, such as large JSON string values. 1C for `Slice` and `Text` would then be the remedy, and it changes no result. If LAY-05's character sources hold no slots, the case for 1C goes away.
- **2C's redundancy argument.** It rests on no `Atomic` spanning two steps. If that ever changes, node pins in producing layers matter again.

## Consequences

**Easier:**

- One rule says what pins, and it covers constructs added later.
- `Since` and `Reset` never meet released input.
- Lexers pay nothing for pins, and in-memory parses keep Phase 0's hot path.
- LAY-06's limit covers long captures.

**Harder:**

- A streamed last layer at character level holds about 17× a long capture's characters, if its buffer carries slots.
- The horizon has four inputs.
- Each engine must know whether a layer produces, to skip pins in it. Taking them anyway is correct, only slower.

**Ruled out:**

- `Since` or `Reset` reading released input.
- A producing layer emitting an item whose `stepStart` has been released.
- `Slice` and `Text` copying at release, until a measurement asks for it.

**Risks and how they are watched:**

- **A construct that reads back without pinning.** LAY-02 gains a criterion: over a stream buffer with segments of one and two items, `Slice`, `Text`, `String` and `CIString` give the in-memory result when their range crosses a released segment.
- **A release during a step.** LAY-02's tests check that a producing layer's horizon never passes the start of the step it is running.
- **Skipping pins wrongly.** GEN-08's differential tests compare buffer contents with skipping on and off, as ADR-0030 does for point 9.

## Changes made

- **Whitepaper:**
  - [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention): the pinning bookmarks list gains `Slice`, `Text`, a `String` or `CIString` of more than one character, and `Custom`, with the rule for constructs added later (point 1). The retention paragraph gives the horizon's four inputs and the single floor (point 3), the producing layer's pin from the start of each step (point 2), and where engines may skip pins (point 4).
  - [§ Guarantees](../../../whitepaper.md#guarantees) point 2, as in point 5.
  - [§ Text capture](../../../whitepaper.md#text-capture): while `Slice` or `Text` runs over streamed input, its start is pinned.
  - Checked and unchanged: § Guarantees point 3 and § Deterministic mode. They speak of token layers, which have no `Slice`, `Text`, `String` or `CIString`, and rule 4 of § Deterministic mode excludes opaque nodes such as `Custom`. Also § The restart protocol, which already keeps the layers' pins.
- **Cards:**
  - [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md): scope narrowed as point 6 says, "reference-counted" replaced by point 3, and a new criterion for read-back constructs across released segments and the step pin.
  - [MODE-03](../../active/07-mode-channels/MODE-03-rollback-rule.md) and [ERR-04](../../active/04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md): each gains ADR-0030 point 9's rule and point 6's re-run for its part.
  - [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md): generated code pins as the interpreter does, and may skip pins where point 4 allows.
  - No new cards.

This record was edited in place before its branch merged. LAY-02's build plan found that the Context's "a custom scanner cannot go back" was wrong: the documented way to write a scanner keeps the original cursor at its start while a copy reads ahead. `Custom` now pins its start (point 1), and the Context, point 5's guarantee text and Changes made say so.
