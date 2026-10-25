# ADR-0011: A cut is dropped with every attempt that fails without consuming

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-19 |
| Cards | [CORE-08](../../active/02-core-combinators/CORE-08-control-nodes.md), [INT-03](../../complete/03-interpreter/INT-03-execute-the-core-combinators.md), [INT-04](../../complete/03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md), [GEN-02](../../active/11-source-generator/GEN-02-emit-the-core-combinators.md), [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md) |
| Whitepaper | [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking), [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention), [§ Source-generated engine](../../../whitepaper.md#source-generated-engine) |
| Research | — (measured with a model built for this record; see Context) |
| Supersedes | [ADR-0010](0010-cuts-under-dispatch-and-across-layers.md); its points 2 and 3 stand (see Decision) |

## Context

[ADR-0010](0010-cuts-under-dispatch-and-across-layers.md) drops a cut "when the attempt that passed it fails without consuming". It lists the cases as "`Or` and `OneOf` trying their next alternative, `Optional` producing nothing, a repetition … stopping or trying its item". The aim was to make dispatch exact: an attempt that dispatch skips would only have failed without consuming, so its cut would have been dropped anyway.

The list leaves out attempts where the combinator does not move on afterwards:

- the last alternative of an `Or` or `OneOf`;
- a mandatory item: the first item of `AtLeastOnce` or `SepBy1`, an item of `Repeat` below its minimum, or an `Until` item;
- the atom of an `Expression.Build` operand.

Dispatch skips these attempts too, but the list does not say to drop their cuts. The sources also disagree: [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention) has the light mark undo a cut "when an alternative fails without consuming", which includes the last one. [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking) says the cut is dropped when the combinator "moves on". The docs, the XML docs and INT-03 say "an attempt" without saying which nodes count as one.

Concrete case: `Atomic(Seq(a, Or(b, Seq(Cut, c))))` on `ad`.

| Engine | What happens | Outcome |
| --- | --- | --- |
| In order, ADR-0010 as written | `a` is consumed and `b` fails without consuming. The last alternative passes the cut, then `c` fails without consuming. `Or` has no next alternative, so the cut stays, and `Atomic` keeps the committed failure. | Committed failure |
| Dispatch on the next item | `d` selects no alternative. The cut never runs, so `Atomic` rewinds. | Empty failure |

### Measurements

To compare candidate rules, a throwaway model of the control nodes was written and run. It was not checked in; CORE-08 checks in a model of the chosen rule only.

- **Grammars:** every grammar up to depth 3 over the leaves `a`, `b`, `ab` and `Cut`, using `Seq`, `Or` and `Longest` (binary) and `Atomic`, `Lookahead`, `Not`, `Optional`, `Many` and `AtLeastOnce`. Repetitions of a nullable item were left out. That gives 16,812 grammars.
- **Inputs:** all 15 strings over `a` and `b` up to length 3, so 252,180 cases per comparison.
- **What was compared:** each case's outcome and the cut flag left at the end. A difference in the flag shows up when the grammar is wrapped in `Atomic(Seq(prefix, …))`, so it counts as a difference in behaviour.
- **Dispatch modes:**
  - *In order:* run every attempt.
  - *Choice dispatch:* skip an `Or` alternative, an `Optional` item or an optional repetition item whose FIRST set excludes the next item and which is not nullable.
  - *Fail fast anywhere:* skip any such node.
- **Nesting:** each rule was also run on every grammar with nested sequences flattened, so `Seq(a, Seq(b, c))` became `Seq(a, b, c)`.

Cases that differ, out of 252,180:

| Rule | Choice dispatch vs in order | Fail fast anywhere vs in order | Nested vs flat sequences |
| --- | --- | --- | --- |
| ADR-0008: never drop | 2,132 | 7,280 | 0 |
| ADR-0010 as written | 640 | 5,788 | 0 |
| **A.** Drop with every attempt of `Or`, `Optional` or a repetition that fails without consuming | **0** | 5,124 | **0** |
| **B.** Drop with every node that fails without consuming | 0 | 0 | 556 |

The smallest case found for each nonzero cell:

- **ADR-0010 as written, choice dispatch:** `Or(a, Seq(Cut, a))` on the empty input. The last alternative keeps its cut.
- **Rule A, fail fast anywhere:** `Atomic(Seq(Cut, a))` on the empty input. Skipping the `Atomic` skips its cut.
- **Rule B, nesting:** `Seq(a, Seq(Cut, a))` on `a`. The inner sequence fails without consuming and drops the cut, but `Seq(a, Cut, a)` keeps it.

Every rule except ADR-0008 also gives all 45 rows of the CORE-08 table their expected outcomes. ADR-0008 differs on the three rows ADR-0010 changed on purpose.

These rows are evidence for the choice, not a proof. The model was written by the author of this record. Its cross-check is the table, which an independent review traced by hand.

### What cannot be had together

No rule makes every skip exact and also keeps nesting irrelevant. Take `Seq(a, Cut, c)`:

- If nesting does not matter, `Seq(a, Seq(Cut, c))` must behave the same.
- So when `c` fails without consuming, the inner `Seq(Cut, c)` must keep its cut.
- But an engine that fails fast skips `Seq(Cut, c)` without running it, so the cut is never set.

One of the two properties has to go.

Forces:

- **G7, equivalence:** both engines give the same outcome for every grammar and input.
- **Fast dispatch:** jump tables for `Or`, as § Source-generated engine plans.
- **Predictable authoring:** `Then` and `Before` build nested sequences. Whether a cut works should not depend on how a sequence was grouped.
- **Simplicity:** we prefer the simple standard approach.

## Options

### A. Every light-mark attempt drops its cut (recommended)

Each attempt made by a combinator that takes a light mark restores the cut flag if it fails without consuming. That covers every alternative of `Or` and `OneOf` including the last, the item of `Optional`, every item of a repetition whether mandatory or not, and the attempts of separators (`SepBy`, `SepBy1`, `SepEndBy`), `Until` terminators, `Expression.Build` operators and atoms, and `Recover`. It doesn't matter whether the combinator then moves on, stops or fails.

- **Pros:**
  - Choice dispatch is exact. The measurements found 0 differences, and that holds for the last alternative and mandatory items too.
  - Nested and flat sequences behave the same (0 differences), so how an author groups a sequence never changes what a cut does.
  - It matches the whitepaper's light-mark sentence and states it uniformly. "Attempt" has one definition: an attempt held by a light mark.
  - Each holder already saves a light mark, so the rule costs nothing beyond what ADR-0010 already does.
- **Cons:**
  - An engine may not fail fast on a node that can reach a `Cut` without consuming when an `Atomic` encloses it. Skipping that node would skip the cut. Fail-fast elsewhere, and all dispatch at choices, stay allowed. Nothing in the whitepaper plans this optimisation.
  - A cut can still escape a node that fails without consuming, if no light mark holds it. For example, `Atomic(Seq(a, Seq(Cut, c)))` on `ad` keeps the committed failure, as `Atomic(Seq(a, Cut, c))` does.

### B. Every node drops its cut when it fails without consuming

Any node that fails without consuming restores the cut flag to its value when the node started.

- **Pros:**
  - Every skip is exact (0 differences in both modes), so engines may fail fast anywhere.
  - Short to state.
- **Cons:**
  - How a sequence is nested changes behaviour (556 differences). `Seq(If, LParen, Cut.Then(Cond))` drops the cut when `Cond` fails without consuming, but `Seq(If, LParen, Cut, Cond)` keeps it. `Then` and `Before` build exactly this nesting, so authors would hit it in ordinary code.
  - Every node pays to save and restore the flag, not just the light-mark holders.

### C. Keep ADR-0010's wording; restrict dispatch instead

Drop a cut only when the combinator moves on, as ADR-0010 says. Engines must then not skip a last alternative or mandatory item that can reach a cut without consuming. Analysis computes which can.

- **Pros:** no change to ADR-0010's wording.
- **Cons:**
  - Brings back the analysis property that ADR-0010 chose C to avoid.
  - Jump tables need exceptions for last alternatives and mandatory items.
  - The last alternative is treated differently from the others for no reason an author would guess.

### D. Accept the divergence

- **Pros:** no work.
- **Cons:** breaks G7. Listed for completeness only.

## Decision

Option **A**. Accepted by the maintainer on 2026-10-19.

This record supersedes ADR-0010 as a whole, because a record can only be superseded whole. ADR-0010's point 2 (bookmark release at a permanent cut) and point 3 (one cut flag per layer) stand unchanged. ADR-0008's points 1, 2 and 4 to 7 stand, as ADR-0010 said. The cut rule is now:

1. `Cut` succeeds without consuming input and puts a cut in force.
2. `Atomic(p)` rewinds a failure of `p` unless a cut passed inside `p` is still in force. Each `Atomic` starts with no cut in force. A cut passed inside an inner `Atomic` is still in force after it, whether that `Atomic` succeeded or failed (point 7: it merges its cut outward on exit).
3. **Attempts.** The following combinators take a light mark before each attempt they make:
   - `Or` and `OneOf`: each alternative;
   - `Optional`: its item;
   - every repetition: each item, mandatory or not, each separator of `SepBy`, `SepBy1` and `SepEndBy`, and each `Until` terminator;
   - `Expression.Build`: each operator and each atom;
   - `Recover`: its parser.
   
   When an attempt fails without consuming, its light mark restores the cut flag to its value when the attempt started. It does so whether the combinator then moves on, stops or fails. Only the failed attempt's cut is dropped: when a separator matches without consuming and the item after it then fails without consuming, the list ends, and a cut the separator passed stays in force, because the separator's own attempt succeeded. A `SetMode` in that separator stays in effect for the same reason (confirmed by the maintainer, 2026-10-19).
4. **Boundaries.** A cut passed inside a `Lookahead`, a `Not` or a `Longest` alternative does not survive the parse leaving it, whichever way that boundary ends.
5. A cut never turns a failure that consumed nothing into a committed one.
6. **Dispatch.** An engine may skip an attempt listed in point 3 when the next item shows it would fail without consuming. It must not skip any other node that is enclosed by an `Atomic` and can reach a `Cut` without consuming, outside any `Lookahead`, `Not` or `Longest` alternative. A cut inside one of those boundaries cannot leave it, so skipping the boundary is exact.
7. **Implementation.** As ADR-0010 described: one flag per layer. `Atomic` and the three boundaries save and clear it on entry. On exit, `Atomic` merges its saved value with the current one, and a boundary restores its saved value. Each light mark saves the flag, and restores it on its attempt's empty failure.

Deciding reason: A is the only rule measured that keeps both exact choice dispatch and grouping-independent sequences. The only optimisation it rules out is one the design does not plan.

What would change this:

- A generator design that needs to fail fast inside sequences over nodes that can reach a cut. B would then trade grouping independence for it.
- A case outside the measured space where A's choice dispatch is not exact. The checked-in model's tests cover every grammar to depth 3; an independent review of CORE-08 also found none in about 17 million random cases to depth 6.

## Consequences

- **Easier:**
  - One definition of "attempt" and one drop rule, the same in the whitepaper, the XML docs, the docs and the engine cards.
  - `Then`, `Before` and `Seq` can be mixed freely around a cut.
- **Harder:**
  - Generators must not apply fail-fast to a node that can reach a cut without consuming under an `Atomic`. GEN-02 records this.
- **Ruled out:**
  - B's every-node restore.
  - The last-alternative exception implied by ADR-0010's wording.
- **Risks and how they are watched:**
  - The measurements came from a model that is not checked in. CORE-08 checks in a model of rule A with three tests over every grammar to depth 3, a larger space than the measurements because it adds `SepBy` and `Until`:
    - it gives every table row its expected outcome;
    - choice dispatch matches running in order;
    - flattening sequences changes no outcome.
  - INT-03 and GEN-02 can compare their engines against that model.

## Changes made

- **Decision records:** [ADR-0010](0010-cuts-under-dispatch-and-across-layers.md) set to `Superseded by 0011`.
- **Whitepaper:**
  - [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking): the cut paragraph states the drop rule of point 3, the dispatch rule of point 6, and links this record.
  - [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention): light marks are taken before each attempt, as listed in point 3, and restore the cut on any empty failure of that attempt.
  - [§ Source-generated engine](../../../whitepaper.md#source-generated-engine): the dispatch rule of point 6.
- **Cards:**
  - [CORE-08](../../active/02-core-combinators/CORE-08-control-nodes.md): back to Building. The checked-in model, its tests, the new table rows and the alignment of the docs, XML docs and stale references follow as the next change.
  - [INT-03](../../complete/03-interpreter/INT-03-execute-the-core-combinators.md): the drop rule for every light-mark attempt.
  - [INT-04](../../complete/03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md): operator and atom attempts.
  - [GEN-02](../../active/11-source-generator/GEN-02-emit-the-core-combinators.md): the dispatch rule.
  - [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md): `Recover` restores the cut on an empty failure of its parser.
