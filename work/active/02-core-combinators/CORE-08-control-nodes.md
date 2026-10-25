# CORE-08: Control nodes

| | |
| --- | --- |
| Epic | [Core combinators](../../README.md#core--core-combinators) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

Nodes that control backtracking and error labelling.

## Scope

- `Atomic(p)`, `Lookahead(p)`, `Not(p)` and the shared leaf `Cut`, as static members of `Grammar<TIn>` in `GrammarOfTIn.Control.cs` ([ADR-0008](../../docs/decisions/0008-control-node-semantics.md)).
- `p.Named(label)` and `p.Context(text)`, as extension members in `ErrorReporting`, each guarding its receiver with its own `[CallerArgumentExpression]` parameter. A null or empty label or text throws `GrammarException`.
- XML docs and a `docs/` page stating the semantics of ADR-0008 (points other than 3) and ADR-0011 (the cut rule), a shared table of cases (grammar, input, expected outcome), and a reference model of the semantics with tests that it gives every table row, that dispatching gives the same results as running in order, and that nesting sequences changes no result. INT-03 and GEN-02 run their engines against the table and the model.
- Each public combinator null-checks its parser arguments by taking its own `[CallerArgumentExpression]` parameter per parser argument and passing it to `Guard.NotNull`, so the `GrammarException` names the caller's expression (see `Grammar.Deterministic`, CORE-02).

## Acceptance criteria

- [x] Node semantics are specified precisely enough for both engines to implement (including `Cut` inside `Atomic`).

## Blocked by

- [CORE-02 Grammar base classes and authoring surface](../02-core-combinators/CORE-02-grammar-base-classes-and-authoring-surface.md)

## Unblocks

- [INT-03 Execute the core combinators](../03-interpreter/INT-03-execute-the-core-combinators.md)
- [ANA-01 Grammar property analysis](../09-static-analysis/ANA-01-grammar-property-analysis.md)
- [TOOL-05 EBNF and railroad diagram export](../12-grammar-author-tooling/TOOL-05-ebnf-and-railroad-diagram-export.md)

## Whitepaper references

- [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking)
- [§ The error model](../../../whitepaper.md#the-error-model)

## Artifacts

- Decision: [ADR-0008 Control node semantics](../../docs/decisions/0008-control-node-semantics.md) (superseded by ADR-0010; its points other than 3 stand)
- Decision: [ADR-0010 Cuts under predictive dispatch and across layers](../../docs/decisions/0010-cuts-under-dispatch-and-across-layers.md) (superseded by ADR-0011; its points 2 and 3 stand)
- Decision: [ADR-0011 A cut is dropped with every attempt that fails without consuming](../../docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md)
- Docs: [Backtracking and labels](../../../docs/concepts/control.md)

## Progress notes

- 2026-10-19 (Designing): Started at Design with the maintainer's agreement, because INT-03, ERR-02 and LAY-02 depend on the cut, lookahead and label rules. Drafted ADR-0008 after reading Parsec, Megaparsec, Pidgin and FastParse; the maintainer chose that `Named` applies to an empty success. Next: the maintainer accepts, rejects or revises ADR-0008 (cut scope option A, class name for `Named` and `Context`); on acceptance fold it into the whitepaper and cards, then Build.
- 2026-10-19 (Building): The maintainer accepted ADR-0008 (cut scope option A, class `ErrorReporting`, no Pidgin differential run); folded into the whitepaper, INT-03, ERR-02 and LAY-02. Next: build the nodes, entry points, case table, docs page and tests.
- 2026-10-19 (Building): Added the six nodes (`NodeKind` group `Atomic` to `Context`), `Atomic`/`Lookahead`/`Not`/`Cut` in `GrammarOfTIn.Control.cs`, `Named`/`Context` in `ErrorReporting`, `Guard.NotNullOrEmpty`, `ControlNodeTests`, the 38-row `ControlTable` with `ControlTableTests` (rows build; no parse is run), and docs/concepts/control.md. Build clean, 578 tests pass, AOT smoke publish clean. The criterion appears met; `Longest` boundary rows wait for CORE-06. Next: maintainer review, then `/complete`.
- 2026-10-19 (Building): Merged origin/main (CORE-06, CORE-13). Added the two `Longest` cut-boundary rows to `ControlTable` (40 rows) and the cut rule to `Longest`'s XML docs and docs/concepts/choice.md. CORE-13's `ExtensionReceiverTests` passes with `ErrorReporting`; 637 tests pass. Next: maintainer review, then `/complete`.
- 2026-10-19 (Building): `/complete` stopped: an independent critic review found no wrong table rows but gaps that would let two engines disagree. These are FIRST-set dispatch skipping a cut in an abandoned alternative's nullable prefix, the cut flag's scope across layers, and the expectations recorded inside `Not`, `Lookahead` and `Context`. It also found the whitepaper's and LAY-02's bookmark-release wording too loose (row 28), plus editorial and table gaps. Next: the maintainer decides on a follow-up decision (ADR-0010) and the fixes, then re-run `/complete`.
- 2026-10-19 (Designing): Back to Design for the review's dispatch and layer gaps. Drafted ADR-0010 with five dispatch options (recommend C: a cut is undone with the light mark like `SetMode`) and two layer options (recommend one flag per layer). Next: the maintainer accepts, rejects or revises ADR-0010; then apply it with the review's editorial fixes and table rows, and re-run `/complete`.
- 2026-10-19 (Building): The maintainer accepted ADR-0010 (options C and L1); folded into the whitepaper, ADR-0008's status, INT-03, GEN-02, LAY-02 and INC-04. Next: apply it in code, docs and the table (rows 25 to 27 become empty failures; add the review's rows), plus the review's other fixes (ERR-02 and INT-04 pointers, control.md wording), then re-run `/complete`.
- 2026-10-19 (Building): Applied ADR-0010. `Cut`, `Atomic` and `CutNode` XML docs and docs/concepts/control.md state the drop rule and exact dispatch. In `ControlTable` (45 rows), the three abandoned-cut rows are now empty failures, with new rows for a cut in a successful repetition step, a cut after a lookahead, `Not` whose parser succeeds, a failed `Longest` alternative, and `Context` without consuming. Also fixed control.md's `x=1` comment, added the review's expectation questions to ERR-02, and pointed INT-04 at ADR-0008 point 4 and ADR-0010. 642 tests pass. Next: re-run `/complete`.
- 2026-10-19 (Building): Second `/complete` stopped. The critic confirmed all 45 rows, but ADR-0010 leaves open whether a cut is dropped when the last `Or` alternative or a mandatory repetition item fails without consuming (`Atomic(Seq(a, Or(b, Seq(Cut, c))))` on `ad` differs between in-order and dispatch). Its wording also disagrees with whitepaper § Bookmarks and retention, and the docs, XML docs and INT-03 state the drop rule more broadly than the ADR. Next: the maintainer chooses the clarifying rule (ADR-0011), then align the texts, add rows, fix stale ADR-0008 references, and re-run `/complete`.
- 2026-10-19 (Building): Explored candidate cut rules with a throwaway model, not checked in. It compared every grammar to depth 3 (16,812) on every input to length 3, running in order and with dispatch. Exact choice dispatch needs the cut dropped when the last `Or` alternative or a mandatory item fails without consuming (smallest failing case `Or(a, Seq(Cut, a))` on ""). Restoring after every attempt of `Or`, `Optional` and repetitions keeps choice dispatch exact and nested sequences equal to flat ones. Restoring after every node's empty failure makes all skipping exact but makes `Seq(a, Seq(Cut, a))` differ from `Seq(a, Cut, a)`. Added `AtLeastOnce` to `ControlShape`. Next: agree the rule in ADR-0011, then check in a model of that rule only, with tests for table agreement, exact choice dispatch and nesting.
- 2026-10-19 (Designing): Drafted ADR-0011 from the exploration's measurements. It recommends restoring the cut on every light-mark attempt's empty failure, including the last alternative and mandatory items, over the every-node rule (which breaks grouping) and over keeping ADR-0010's wording (which needs dispatch exceptions). Next: the maintainer accepts, rejects or revises ADR-0011; then the CORE-08 change described in its Changes made, and `/complete`.
- 2026-10-19 (Building): The maintainer accepted ADR-0011 (option A). Folded it into the whitepaper (§ Choice and backtracking, § Bookmarks and retention, § Source-generated engine), ADR-0010's status, INT-03, INT-04, GEN-02 and ERR-05. Next: check in a reference model of the rule with tests for table agreement, exact choice dispatch and grouping; add table rows; align the docs, XML docs and stale ADR-0008 references; then `/complete`.
- 2026-10-19 (Building): Applied ADR-0011. Checked in `ControlModel`, a reference model of the semantics (no rule variants), with `ControlModelTests`: every table row matches in order and dispatching, and over 39,306 grammars (depth 3) by 15 inputs, dispatching matches running in order and nesting sequences changes no result. Removing the cut restore from the model makes both the dispatch test and the drop rows fail. Added `SepBy` and `Until` to `ControlShape`, and six table rows (51 total): last alternative, mandatory item, `Until` terminator and item, `SepBy` separator, nested sequence. Aligned the `Cut` and `Atomic` XML docs, `CutNode`, `Longest` and control.md with the attempt definition; added an ADR-0004 note to the decisions index. 702 tests pass. Next: `/complete`.
- 2026-10-19 (Building): Third `/complete` stopped. The critic confirmed rule A (0 dispatch and nesting differences over 200,000 random grammars to depth 6) but found two blocking defects. First, the model, the `Cut` XML and control.md treat a `SepBy` separator and the item after it as one attempt, while ADR-0011 and the whitepaper make each its own attempt (`Atomic(SepBy(Seq(Or(a, Cut), b), a))` on `aba` differs). Second, the model's FIRST for `SepBy` ignores the separator when the item is nullable, hidden by excluding nullable-item `SepBy`. Also: two boundary rules that no row pins (`Not` whose parser succeeds, unwrapped; `Longest` with the cut in a later alternative), expected sets under dispatch unowned, and stale ADR citations. Next: fix to the ADR's reading (no decision needed), add rows, re-run `/complete`.
- 2026-10-19 (Building): Fixed the third review's findings. The model's `SepBy` now treats each separator and each item as separate attempts, as ADR-0011 and the whitepaper say; the `Cut` XML and control.md now say the same. FIRST for `SepBy` now includes the separator after a nullable item, and the exhaustive space now keeps `SepBy` with a nullable item (only item and separator both nullable are excluded). Added rows for an unwrapped `Not` whose parser succeeds, a later `Longest` alternative, and an item after a separator (53 rows); mutants that leak the cut from `Not` or `Longest` now fail exactly those rows. control.md now cites ADR-0011, LAY-02 and INC-04 point to ADR-0011, and ERR-02 now owns expected sets under dispatch. 706 tests pass. Next: `/complete`.
- 2026-10-19 (Building): Fourth `/complete` review: criterion met, no blocking defects. An independent evaluator written from the ADR text agrees with the model on about 18 million cases to depth 6, with dispatch and nesting exact. Gaps before ticking: (S1) the depth-3 tests never check FIRST or nullable on composite children, so mutants such as the old `First(SepBy)` bug survive; (S2) no row pins that a zero-width separator's cut stays when the item after it fails without consuming; (M1) no row pins a cut escaping an inner `Atomic` that fails without consuming; plus wording, number and dependent-card fixes. Next: the maintainer confirms the S2 semantics; then fix and complete.
- 2026-10-19 (Building): Closed the fourth review's gaps. ADR-0011 was edited before it was pushed, with the maintainer's agreement: it now lists every separator, covers an inner `Atomic` that fails, excludes boundaries from point 6, states the zero-width separator case (confirmed by the maintainer), and describes test coverage accurately. Added `FirstAndNullableAreSound` and rows for the zero-width separator and a failed inner `Atomic` (55 rows; the exhaustive space is 43,965 grammars). The four FIRST mutants and the separator-revert and inner-`Atomic` mutants now each fail. Added notes to INT-06 (the regular property versus commit semantics), GEN-04 (the cut flag across fallback calls), LAY-02 and INC-04. 711 tests pass.

## Outcome

`Atomic`, `Lookahead`, `Not`, `Cut`, `Named` and `Context` exist as grammar nodes, with their semantics specified for INT-03 and GEN-02 in prose and as executable checks.

- **API:**
  - [GrammarOfTIn.Control.cs](../../../src/Creole/GrammarOfTIn.Control.cs): `Atomic`, `Lookahead` and `Not` as static members of `Grammar<TIn>`, and `Cut` as one shared `Parser<TIn, Unit>`.
  - [ErrorReporting.cs](../../../src/Creole/ErrorReporting.cs): `Named` and `Context` as extension members that guard their receiver.
  - Six sealed nodes in `src/Creole/Nodes/`, a `NodeKind` group from `Atomic` to `Context`, and `Guard.NotNullOrEmpty`. Each node lowers to `PlanBuilder.NotYetExecutable` until INT-03.
- **Decisions:**
  - [ADR-0008](../../docs/decisions/0008-control-node-semantics.md): the API, outcomes, labels and context.
  - [ADR-0010](../../docs/decisions/0010-cuts-under-dispatch-and-across-layers.md): bookmark release and one cut flag per layer.
  - [ADR-0011](../../docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md): the cut rule. A cut is dropped with every attempt that fails without consuming, so choice dispatch is exact and nesting does not matter.
  - All three are folded into the whitepaper: § Choice and backtracking, § Bookmarks and retention, § Guarantees, § The error model and § Source-generated engine.
- **Specification as tests:**
  - [ControlTable.cs](../../../tests/Creole.Tests/Utilities/ControlTable.cs): 55 cases of grammar, input and expected outcome.
  - [ControlModel.cs](../../../tests/Creole.Tests/Utilities/ControlModel.cs): a reference model with in-order and dispatching modes.
  - [ControlModelTests.cs](../../../tests/Creole.Tests/ControlModelTests.cs), over every grammar to depth 3 (43,965) by every input to length 3:
    - the model matches every row;
    - dispatching matches running in order;
    - FIRST and nullable are sound;
    - flattening sequences changes no result.
  - `ControlNodeTests` and `ControlTableTests` cover node shapes and argument checks.
  - No test runs a Creole parse; INT-03 and GEN-02 run their engines against the table and the model.
- **Evidence:**
  - Four independent critic reviews.
  - The fourth wrote its own evaluator from the ADR texts. It agreed with the model on about 18 million cases to depth 6, with dispatch and nesting exact.
  - Mutation tests on the model: removing the cut restore; leaks from `Not`, `Lookahead` or `Longest`; `Atomic` dropping or not merging its cut; unsound FIRST or nullable; and the per-node and separator-revert variants are each caught by the tests.
  - The build is clean, all 711 tests pass, and the AOT smoke publish is clean.
- **Docs:**
  - [Backtracking and labels](../../../docs/concepts/control.md).
  - Updates to [Choice](../../../docs/concepts/choice.md) and [Errors](../../../docs/concepts/errors.md).
- **Deviations:**
  - The whitepaper's one-line semantics were refined only through the three Accepted decisions above.
  - ADR-0008's rule that a cut survives an abandoned attempt was replaced, first by ADR-0010 and then by ADR-0011.
  - The Pidgin differential test planned at first was dropped: matching Pidgin is not a goal.
- **Follow-up, recorded on existing cards rather than as new cards:**
  - INT-03 runs the table and the model.
  - INT-04: `Named` per ADR-0008, and attempts for operators and atoms.
  - GEN-02: the dispatch rule.
  - GEN-04: the cut flag across fallback calls.
  - INT-06: the regular property must respect commit semantics and the control nodes.
  - LAY-02: bookmark release at a permanent cut.
  - INC-04: replaying an escaped cut.
  - ERR-02 decides the open expected-set questions: `Not`, `Lookahead`, merged context, `Named` over earlier expectations, skipped attempts, and context start positions.
  - ERR-05: `Recover` takes a light mark.
