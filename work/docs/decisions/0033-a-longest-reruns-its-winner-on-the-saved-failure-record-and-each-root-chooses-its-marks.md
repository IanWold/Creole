# ADR-0033: A `Longest` runs its winner again on the failure record it started with, and each grammar lowered on its own chooses its marks

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-11-23 |
| Cards | [ERR-04](../../active/04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md), [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md), [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md) |
| Whitepaper | [§ Longest match](../../../whitepaper.md#longest-match), [§ Properties](../../../whitepaper.md#properties) |
| Research | — |
| Supersedes | — |

## Context

[ADR-0030](0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 6 promises that after a `Longest` matches, the layer's diagnostics are what the winner leaves when it runs alone from the start. Its mechanism is to run the winner again when another alternative ran after it. ERR-04 built that, and its completion check found two problems.

**1. The re-run does not keep the promise.** Point 2's table keeps the failure record between alternatives, so that a `Longest` that fails reports every alternative's failures. A winner therefore runs, and runs again, on a record that holds the failures of the alternatives tried before it. A diagnostic built from the furthest failure ([ADR-0030](0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 8) reports those failures.

Measured on ERR-04's build, with full marks, on input `ab`, using a test node that reports the furthest failure as `Recover` will:

| Grammar | Diagnostic |
| --- | --- |
| `W` alone, where `W` is `Report(Char('q')).Then(String("ab"))` | `'q'` expected at 0 |
| `Longest(W, Char('a').Then(Char('z')))` | `'z'` expected at 1 |
| `Longest(Char('a').Then(Char('z')), W)` | `'z'` expected at 1 |

In the second row the winner runs again because the loser ran after it, but on a record that holds the loser's failure at 1. In the third row the winner is last, so point 6 does not run it again, and its only run came after the loser's.

Fork 6 of ADR-0030 compared what state a `Longest` leaves, not what the winner sees while it runs, so it did not weigh this. Nothing raises such a diagnostic before ERR-05 builds `Recover`.

**2. Marks are chosen per grammar, not per layer.** Point 9 lets an engine leave a part out of every mark "in a layer whose grammar cannot change it", and says "a layer whose grammar contains a `Bind` saves every part, because a continuation can return any parser". [§ Properties](../../../whitepaper.md#properties) puts the choice in per-layer analysis.

ERR-04's lowering makes the choice for each grammar it lowers on its own. A `Bind` continuation's parser is one of those: [`BindPlan`](../../../src/Creole/Plans/BindPlan.cs) gets its plan from [`PlanCache`](../../../src/Creole/Plans/PlanCache.cs), which lowers each parser once, by itself, whichever grammar returned it. So a continuation that returns a parser that cannot raise diagnostics gets light marks, inside a layer whose grammar contains a `Bind`. Behaviour is the same as point 2's table, but the wording of point 9 is not met.

Constraints:

- **G7.** Every engine must give the same diagnostics. A compiled `Longest` holds no node that can raise one ([ADR-0017](0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md)), so only interpreted and generated `Longest` nodes are affected.
- **Speed.** The Phase 0 gate grammars contain no `Longest` and no `Bind`. Token `Longest` nodes in grammars without `Bind`, `Recover`, `OrInsert` or `Embed` take light marks and never run a winner again.
- **[ADR-0026](0026-engines-may-skip-unused-values-and-repeat-user-functions.md).** Engines may run user functions again, so a re-run is allowed.
- **[ADR-0027](0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md) point 3.** A run without a failure log raises no diagnostics: `Recover` ends it at its first catch.

## Options

### Fork 1. What a `Longest` winner sees

#### 1A. Keep point 6's rule and narrow the promise

The whitepaper would say that a diagnostic built from the furthest failure inside a winner may report failures of the other alternatives.

**Pros:** no change to code and no extra runs.

**Cons:**

- A `Recover` inside a winner reports another alternative's expectations, at another alternative's position.
- Which ones depends on the order of the alternatives, which `Longest` otherwise only uses to break ties.

#### 1B. Restore the saved failure record before running the winner again, and run it again whenever another alternative ran (recommended)

Whenever a successful `Longest` runs its winner again, it first puts back the failure record it saved at its start. For diagnostics, it runs the winner again whenever it has more than one alternative, whichever one won.

**Pros:**

- Exact: the winner sees what it sees alone.
- Restoring is the same operation that the `Longest` already does at its end.
- It applies only where it can matter: a grammar that can raise diagnostics, in a run with a failure log.

**Cons:**

- A winner that is last, which point 6 ran once, now runs twice.
- A winner's user functions run again (allowed by ADR-0026).

#### 1C. Start every alternative from the saved failure record

Each alternative starts from the record the `Longest` saved, and when every alternative fails, the `Longest` runs them all again in order to build the record point 2 requires.

**Pros:** a last winner needs no re-run.

**Cons:**

- A winner that is not last still needs one, because the rewind before the next alternative withdraws its diagnostics.
- Every failure of the `Longest` costs two runs of each alternative. A `Longest` inside an `Or` fails on every attempt the `Or` rejects.

#### 1D. Refuse `Recover` and `OrInsert` inside a `Longest` alternative

**Cons:**

- A `Bind` can return either one at run time, so the refusal would also need a run-time error.
- It refuses grammars that work under 1B. ADR-0030 rejected the wider refusal, 6B, for the same reason.

### Fork 2. What chooses the marks

#### 2A. Each layer, as point 9 words it

A parser that a `Bind` continuation returns takes the marks of the layer that runs it.

**Cons:**

- The plan cache must key plans by parser and mark type, and `BindPlan` becomes generic in the mark.
- A parser used in two layers is lowered twice.
- Behaviour is the same as 2B.

#### 2B. Each grammar lowered on its own (recommended)

A layer's grammar, and each parser that a `Bind` continuation returns, chooses its marks from the nodes it reaches, following `Rec`.

**Pros:**

- It fits the plan cache, which already lowers each such grammar on its own.
- A continuation that returns parsers that cannot raise diagnostics keeps light marks.

**Behaviour is that of point 2's table.** A mark is taken and rewound inside one plan, and that plan runs only nodes reachable from its root. A grammar that reaches no node able to raise diagnostics raises none between a mark and its rewind.

#### 2C. Each subtree

Each attempt chooses its mark from its own item. Point 9's "What would change this" names this step, if the per-layer flag's cost shows in measurements.

**Cons:** it needs a property per node, and one grammar's plans then mix mark types. Nothing measured asks for it.

## Decision

Options **1B and 2B**. Accepted by the maintainer on 2026-11-23.

**1. A `Longest` runs its winner again on the failure record it started with.** This refines [ADR-0030](0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 6.

- Whenever a successful `Longest` runs its winner again, it rewinds as between alternatives: it withdraws diagnostics, undoes mode changes and restores layer state to its start. It then restores the failure record it saved at its start, last, as point 2 orders a rewind.
- In a run with a failure log, in a grammar that can raise diagnostics (point 2 below), a successful `Longest` with more than one alternative runs its winner again, whichever alternative won.
- An engine may skip that re-run when the winner itself cannot raise diagnostics.
- Point 6's rule for the mode log and layer state stays: the winner runs again when another alternative ran after it and it can change them. When both rules apply, one re-run serves both.
- After the re-run, the `Longest` ends as point 6 says: the failure record is restored, lowered by any mode change the winner made.

**2. Each grammar lowered on its own chooses its marks.** This refines [ADR-0030](0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md) point 9.

- In point 9, "a layer whose grammar" reads "a grammar lowered on its own". That is a layer's grammar, or a parser that a `Bind` continuation returns.
- Each one decides from the nodes it reaches, following `Rec`.
- A grammar that contains a `Bind` saves every part. The parser its continuation returns decides for itself when it is lowered.
- Until [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md) records these properties, lowering finds them with its own walk.

Deciding reasons:

- **1B** is the only option that keeps the whitepaper's promise without refusing grammars. It costs nothing in grammars that cannot raise diagnostics, or in a first in-memory run, which has no failure log.
- **2B** behaves exactly as 2A does and fits how plans are already cached.

What would change this:

- If a measured grammar with a `Bind` or `Recover` shows the re-run's cost, an engine would use the permission to skip winners that cannot raise diagnostics.
- If ERR-05 finds that `Recover` needs a different failure record inside a `Longest`, its record refines point 1.

## Consequences

**Easier:**

- A `Recover` inside a `Longest` winner reports what it reports alone, whatever the order of the alternatives.
- A `Bind` continuation that returns plain parsers keeps light marks.

**Harder:**

- In a grammar that can raise diagnostics, a run with a failure log runs every winner of a `Longest` with several alternatives twice.
- ANA-01's property must be available for each grammar lowered on its own, not only for each layer, if lowering is to read it.

**Ruled out:**

- A diagnostic in a `Longest` winner that names another alternative's failure.

**Left to ERR-05:** a successful `Longest` restores its saved failure record at the end. If a `Recover` in the winner took failures from that saved record, the record and the diagnostic then both hold them, so a later error at that position can report what the diagnostic already reported. Point 2's table causes this whether or not this record is accepted.

**Risks and how they are watched:**

- ERR-04 adds a test of the three rows above: the winner reports `'q'` at 0 in both orders.
- GEN-08's differential tests run with point 9 off as well as on (ADR-0030), which covers 2B.

## Changes made

- **Whitepaper.**
  - [§ Longest match](../../../whitepaper.md#longest-match): the paragraph on what a match leaves now says that a winner runs again from the failure record the `Longest` started with, and, where the grammar can raise diagnostics, in a run with a failure log, whenever there are several alternatives. It links this record.
  - [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention): "a layer whose grammar cannot …" becomes "a grammar that cannot …", and each grammar lowered on its own decides for itself.
  - [§ Properties](../../../whitepaper.md#properties): the properties are recorded for each grammar lowered on its own, a `Bind` continuation's parser decides for itself, and the paragraph links this record.
  - Checked and unchanged: [§ Recovery](../../../whitepaper.md#recovery) and its withdrawal paragraph, which say nothing about `Longest` or how marks are chosen.
- **Decision index.** A note says this record refines ADR-0030 points 6 and 9. ADR-0030 is not superseded.
- **[ERR-04](../../active/04-errors-and-recovery/ERR-04-diagnostic-withdrawal.md).** Scope bullet 3 follows this record. It returns to Building for the change to `LongestPlan` and its test.
- **[ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md).** Scope gains the end-restore question under Consequences.
- **[ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md).** Scope records the properties for each grammar lowered on its own, and lowering may read them instead of its own walk.
