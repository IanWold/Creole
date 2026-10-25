# ADR-0010: Cuts under predictive dispatch and across layers

| | |
| --- | --- |
| Status | Superseded by 0011 |
| Date | 2026-10-19 |
| Cards | [CORE-08](../../active/02-core-combinators/CORE-08-control-nodes.md), [INT-03](../../complete/03-interpreter/INT-03-execute-the-core-combinators.md), [GEN-02](../../active/11-source-generator/GEN-02-emit-the-core-combinators.md), [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md), [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md) |
| Whitepaper | [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking), [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention), [§ Source-generated engine](../../../whitepaper.md#source-generated-engine) |
| Research | — (follows an independent review of CORE-08) |
| Supersedes | [ADR-0008](0008-control-node-semantics.md); its points other than 3 stand (see Decision) |

## Context

[ADR-0008](0008-control-node-semantics.md) point 3 says a cut, once passed, stays in force up to the nearest `Lookahead`, `Not` or `Longest` alternative. It stays in force even if the alternative, `Optional` or repetition step that held it then fails without consuming and the parse moves on. ADR-0008 accepted this as a coarse case. The reason was that it let an `Atomic` release its pinning bookmark at the moment the cut is passed.

A review of CORE-08 found that this rule conflicts with predictive dispatch. The whitepaper plans dispatch in two places: the generator emits `Or` as "a `switch`" on the next item ([§ Source-generated engine](../../../whitepaper.md#source-generated-engine), GEN-02), and deterministic mode compiles "every `Or` to a jump table" ([§ Deterministic mode](../../../whitepaper.md#deterministic-mode)). Dispatch never runs an alternative whose FIRST set excludes the next item. Under ADR-0008, running it can matter, because a cut that it reaches before consuming anything stays in force.

Concrete case, row 25 of the CORE-08 table:

```csharp
Atomic(Seq(Cut, Char('x')).Or(Seq(Char('a'), Char('b'))))   // on "ac"
```

| Engine | What happens | Outcome |
| --- | --- | --- |
| Tries alternatives in order (the interpreter) | The first alternative passes the cut, then fails on `a` without consuming. The second reads `a` and fails on `c`. The cut is in force, so `Atomic` keeps the failure. | Committed failure |
| Dispatches on the next item (the planned generator) | `a` selects the second alternative. The cut never runs, so `Atomic` rewinds. | Empty failure |

G7 and [§ Equivalence](../../../whitepaper.md#equivalence) require the two engines to agree, so one of these must change. The same applies to `Optional`, repetition, `SepBy`'s separator, `Until`'s terminator and `Expression.Build`'s operators, whenever an engine predicts that they fail.

When the problem arises:

- **Only with a cut that can be reached without consuming**, from the start of an attempt the parse can abandon. A cut after consumed input is safe: that attempt can no longer fail without consuming, so dispatch would never skip it.
- **Only with an enclosing `Atomic`.** A cut affects nothing else. Deterministic mode forbids `Atomic`, so the jump tables it promises are never affected.
- **Rare in practice.** The intended idiom puts the cut after the tokens that identify the construct: `Seq(If, LParen, Cut, …)`. The risky form is a cut guarded only by a lookahead: `Seq(Lookahead(If), Cut, …)`.

Precedent in the design: [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention) already gives `Or` and repetition a light mark that "only undoes zero-width effects, such as a `SetMode`, when an alternative fails without consuming". A mode change in a skipped alternative is therefore harmless under dispatch, because a failed attempt's mode change is undone anyway. A cut is also a zero-width effect, but ADR-0008 does not undo it.

A second, smaller gap: ADR-0008 places the cut flag "on the parse state" without saying whether that state is shared across layers. A layer's step may pull items from the layer below, and fusion inlines those steps ([§ Layer fusion](../../../whitepaper.md#layer-fusion)). If the flag were shared, a cut inside a lexer rule could stop a parser-layer `Atomic` from rewinding, and whether it did would depend on whether the token was already buffered.

Forces:

- **G7, equivalence:** every engine gives the same outcome for every grammar and input.
- **Fast dispatch:** jump tables are central to the speed plan and to deterministic mode.
- **Buffer release:** this was the reason ADR-0008 accepted its coarse case.
- **Simplicity:** we prefer the simple standard approach.

## Options for dispatch (the main fork)

### A. Keep ADR-0008; engines must not skip an attempt that can reach a cut without consuming

Analysis computes a new property for each node: *can reach a `Cut` without consuming*. It is like nullability, with `Lookahead`, `Not` and `Longest` as boundaries that hide the cut. A dispatching engine must still run every alternative with this property that comes before the chosen one, in order. The same holds for predicted `Optional`, repetition and operator attempts.

- **Pros:**
  - No change to accepted semantics. Rows 25 to 27 stand.
  - Dispatch stays in place for every choice without such an alternative, which is nearly all of them.
- **Cons:**
  - A new analysis property, and a more complex jump table: an arm runs a list of prefixes before its target.
  - Running a skipped prefix is only an approximation of running the alternative. The prefix can contain a `Lookahead` or `Not` that decides whether the cut is reached, so the engine must run the alternative up to the point where it would fail. In practice that means running the whole alternative.
  - The coarse case remains: a cut in an abandoned alternative still blocks a later rewind.
- **Effect on buffers:** none; bookmarks are released at the cut, as in ADR-0008.

### B. Keep ADR-0008; turn dispatch off for any choice with such an alternative

The same analysis property as A. A choice, `Optional` or repetition with any attempt that can reach a cut without consuming is run in order, never dispatched. Deterministic mode is unaffected, because it has no `Atomic`. Even so, its "every `Or` to a jump table" promise would need the same exception, or it would have to reject such cuts.

- **Pros:**
  - Simple for engines: one property and a fallback that already exists.
  - Semantics unchanged.
- **Cons:**
  - The whole choice loses its jump table, not just the affected arm. That is a silent slowdown that only analysis output reveals.
  - Analysis must run before the generator can choose. The generator reads syntax (ADR-0001), so it needs the property from the shared analysis engine (RSLN-02).
  - The coarse case remains.
- **Effect on buffers:** none.

### C. A cut is a zero-width effect, undone with the light mark (recommended)

When `Or`, `Optional`, repetition, `SepBy`, `Until` or an `Expression.Build` operator moves on after an attempt fails without consuming, it drops any cut passed during that attempt. Its light mark already undoes `SetMode` in the same situation. Everything else in ADR-0008 stands: `Atomic` keeps a failure while a cut is in force, `Lookahead`, `Not` and `Longest` alternatives are boundaries, and a cut never commits an empty failure.

A skipped attempt would have failed without consuming, so its cut would have been dropped anyway. Dispatch is therefore exact with no extra analysis.

- **Pros:**
  - Dispatch, jump tables and deterministic mode need no new rule or analysis property.
  - It removes ADR-0008's coarse case. In `Atomic(Seq(Cut, Char('x')).Or(Seq(Char('a'), Char('b'))))`, the abandoned cut no longer stops the rewind.
  - It is consistent with how the design already treats `SetMode`.
  - It is the "kept attempt" rule of ADR-0008 option B, applied only to attempts that fail without consuming.
- **Cons:**
  - Supersedes an accepted decision two hours old. Rows 25 to 27 change from committed to empty failures.
  - `Or`, `Optional` and repetition carry one more bit in their light mark, saved before each attempt and restored after an empty failure. Those nodes already save a mark, so the cost is one field, not a new mechanism.
  - **Bookmark release becomes conditional.** Most cuts follow input that the same attempt has already consumed outside any inner `Atomic`: after `if (`, every attempt between the cut and the `Atomic` has consumed input it cannot give back, so none can fail without consuming. Such a cut is permanent as soon as it is passed, and the bookmark can be released then, as under ADR-0008. A cut at the start of an attempt becomes permanent only when that attempt consumes input it cannot give back, or completes. Until then the bookmark stays. Engines may always release later than this; releasing early is the only error.
  - It departs from FastParse, where a cut in an abandoned alternative still prevents backtracking.
- **Effect on buffers:** the same in the common idiom; later release for cuts at the start of an attempt.

ADR-0008 rejected its option B partly because "a bookmark cannot be released at the cut". That overstated the cost. Release must wait only when the cut is at an attempt's start, and the hot-path cost rides on light marks that already exist.

### D. Reject grammars with a cut that can be reached without consuming

Analysis or construction reports an error for a `Cut` reachable without consuming from the start of an attempt the parse can abandon.

- **Pros:**
  - The semantics stay as in ADR-0008.
  - The troublesome case cannot be written.
- **Cons:**
  - It rejects a reasonable idiom, committing once a lookahead confirms the construct: `Seq(Lookahead(If), Cut, …)`.
  - It needs analysis before a grammar can be used, and analysis cannot be complete at construction while `Rec` cycles are unresolved.
  - It adds a diagnostic ID.

### E. Accept the divergence

Dispatch may differ from in-order execution for such grammars, and the equivalence tests exclude them.

- **Pros:** no work.
- **Cons:** breaks G7, the reason the generator can be trusted. Listed for completeness only.

## Options for layers

### L1. One cut flag per layer (recommended)

Each layer's steps have their own flag. A cut in an upstream layer's rule affects only `Atomic`s in that layer.

- **Pros:**
  - Matches [§ Guarantees](../../../whitepaper.md#guarantees) point 4: "Upstream backtracking is private."
  - Outcomes do not depend on buffering or fusion.
- **Cons:** fused code must keep the layers' flags apart. This costs one boolean per fused layer.

### L2. One flag for the whole pipeline

- **Pros:** one field.
- **Cons:** a lexer rule's cut would leak into the parser's `Atomic`s, and only when the token was produced during that `Atomic`, not when it was already buffered. Fused and unfused pipelines could then disagree, which breaks G7.

## Decision

Options **C** and **L1**. Accepted by the maintainer on 2026-10-19.

This record supersedes ADR-0008 as a whole, because a record can only be superseded whole. ADR-0008's points 1 (API), 2 (the outcome table), 4 (labels), 5 (context), 6 (Starved) and 7 (card scope) stand unchanged and are not repeated here. Its point 3 is replaced by the following.

1. **The cut rule.**
   - `Cut` succeeds without consuming input and puts a cut in force.
   - `Atomic(p)` rewinds a failure of `p` unless a cut passed inside `p` is still in force. A cut passed before the `Atomic` was entered has no effect on it, because each `Atomic` starts with no cut in force. A cut passed inside an inner `Atomic` that has since succeeded is still in force.
   - A cut stops being in force when any of these happens:
     - the parse leaves the nearest enclosing `Lookahead`, `Not` or `Longest` alternative, whichever way that boundary ends;
     - the attempt that passed it fails without consuming, and a combinator holding a light mark acts on that empty failure: `Or` and `OneOf` trying their next alternative, `Optional` producing nothing, a repetition (including `SepBy`, `SepEndBy` and `Until`) stopping or trying its item, an `Expression.Build` operator attempt ending, or `Recover`;
     - the parse ends.
   - A cut never turns a failure that consumed nothing into a committed one.
   - Engines keep the cut as one flag per layer. `Atomic` and the three boundaries save and clear it on entry. On exit, `Atomic` merges its saved value with the current one, and a boundary restores its saved value. Each light mark saves the flag and restores it when its holder acts on an empty failure.
2. **Bookmarks.** An `Atomic` may release its pinning bookmark once a cut inside it is *permanent*. That is when no attempt between the cut and the `Atomic` can still fail without consuming, and no `Lookahead`, `Not` or `Longest` alternative lies between them. For a cut after input the same attempt has consumed outside any inner `Atomic`, that is the moment the cut is passed. Engines may release later, never earlier.
3. **Layers.** The cut flag belongs to one layer's parse. A step of one layer that pulls items from the layer below neither sees nor changes the upstream layer's flag, whether or not the layers are fused.

Deciding reason: C makes dispatch exact using a mechanism the design already has, and removes ADR-0008's coarse case. It costs one bit per light mark and later release only for a cut at an attempt's start, which is the case that caused the problem.

What would change this:

- LAY-02 measurements showing that cuts at an attempt's start are common in real grammars and that the later release costs real memory. A would then be the fallback.
- A dispatch technique that cannot restore one bit cheaply.

## Consequences

- **Easier:**
  - The generator, the interpreter's `Or` rewrite, DFA compilation and deterministic mode dispatch freely.
  - A cut affects only attempts that are kept. That is easier to explain than ADR-0008's abandoned-cut case.
- **Harder:**
  - Each light mark gains the cut bit.
  - LAY-02 must track which open attempts have consumed input they cannot give back, so it can release a bookmark at a permanent cut. A conservative engine can release at the `Atomic`'s exit instead and lose nothing but memory.
- **Ruled out:**
  - A cut in an abandoned attempt affecting anything.
  - A cut crossing layers.
- **Risks and how they are watched:**
  - Rows 25 to 27 of the CORE-08 table change to empty failures. New rows pin a cut inside a `Not` whose parser succeeds, and a cut in a failed `Longest` alternative.
  - Incremental reuse (INC-04): a reused rule from which a cut escaped must replay that cut, or a re-parse differs from a fresh parse. This holds under any option here.

## Changes made

- **Decision records:** [ADR-0008](0008-control-node-semantics.md) set to `Superseded by 0010`.
- **Whitepaper:**
  - [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking): the cut paragraph is rewritten with the three ways a cut ends, the per-layer flag, and why dispatch stays exact. It now follows the list instead of splitting it.
  - [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention): light marks are held by `Optional` and `Expression.Build` operators as well, and save and restore the cut; an `Atomic` releases its bookmark at a permanent cut.
  - [§ Guarantees](../../../whitepaper.md#guarantees) point 4: a layer's cuts are private too.
- **Cards:**
  - [CORE-08](../../active/02-core-combinators/CORE-08-control-nodes.md): back to Building. Table rows 25 to 27 change to empty failures, new rows are added, and the docs and XML docs follow.
  - [INT-03](../../complete/03-interpreter/INT-03-execute-the-core-combinators.md): the cut rule, and the cut bit in light marks.
  - [GEN-02](../../active/11-source-generator/GEN-02-emit-the-core-combinators.md): `Or` as a `switch` must carry the cut bit; dispatch is exact.
  - [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md): the permanent-cut release rule.
  - [INC-04](../../active/15-incremental-reparsing/INC-04-reusable-parser-rules.md): a reused rule replays an escaped cut.
  - [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md): unchanged; option C needs no new analysis property.
