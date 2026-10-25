# ADR-0008: Control nodes: `Cut` holds up to the nearest predicate, and `Named` labels any parse that consumes nothing

| | |
| --- | --- |
| Status | Superseded by 0010 |
| Date | 2026-10-19 |
| Cards | [CORE-08](../../active/02-core-combinators/CORE-08-control-nodes.md), [INT-03](../../complete/03-interpreter/INT-03-execute-the-core-combinators.md), [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md), [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md) |
| Whitepaper | [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking), [§ The error model](../../../whitepaper.md#the-error-model) |
| Research | — (prior art read for this record, cited inline) |
| Supersedes | — (refines the `Named` rule that [ADR-0004](0004-expression-build-operator-table-and-semantics.md) point 5 restates; see Consequences) |

## Context

CORE-08 adds `Atomic`, `Lookahead`, `Not`, `Cut`, `Named` and `Context`. Its one criterion is that their semantics are precise enough for the interpreter (INT-03) and the generator (GEN-02) to implement the same way, including `Cut` inside `Atomic`. [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking) gives one line per combinator. It leaves open:

- how far a cut reaches: the innermost `Atomic`, every enclosing one, or something in between;
- what happens to a cut in an alternative that `Or` abandons, or inside `Lookahead`, `Not` or `Longest`;
- what `Lookahead` does when its parser fails after consuming input;
- whether `Not` ever consumes;
- whether `Named` applies when its parser succeeds without consuming;
- whether these are static members of `Grammar<TIn>` or methods called on a parser, and whether `Cut` is a parser or a combinator.

The whitepaper gives `Cut` two purposes: better messages ("a failure is reported here instead of being retried elsewhere") and earlier buffer release. [§ Results](../../../whitepaper.md#results) fixes the representation: a step returns `bool`, and a failure is committed exactly when the cursor moved.

Prior art, read for this record:

| Library | Backtrack | Lookahead on a failure that consumed | Negative lookahead | Label applies on | Cut |
| --- | --- | --- | --- | --- | --- |
| Parsec (`Text/Parsec/Prim.hs`, `Combinator.hs`) | `try` turns a consumed error into an empty one | Kept: "If @p@ fails and consumes some input, so does @lookAhead@" | `notFollowedBy` is wrapped in `try`; consumes nothing | Empty error always; empty success unless the error is unknown (`labels`, `eok'` and `eerr'`) | None |
| Megaparsec (`Text/Megaparsec/Internal.hs`, `Class.hs`) | `try` rewinds and keeps the error's offset | Kept: "it will fail consuming input if @p@ fails consuming input" | "never consumes any input and never modifies parser state" | Empty error; empty success only when it has hints (`pLabel`, `refreshHints`) | None |
| Pidgin (`Parser.Try.cs`, `Parser.Lookahead.cs`, `Parser.Not.cs`, `Parser.Labelled.cs`) | `Try` rewinds on failure | Kept: "No backtracking is performed upon failure" | `Not` "consumes the same amount of input as the supplied parser" | Any failure, consumed or not; never on success | None |
| FastParse ([docs](https://com-lihaoyi.github.io/fastparse/)) | Every `|`, `.rep` and `.?` backtracks by default | — | `!(...)` consumes no input | — | `~/`: "Once the parse has crossed a cut, it can no longer backtrack past the point at which the cut occured"; cuts "prevent backtracking throughout the entire parser"; `NoCut` isolates a branch; cuts let streaming parsers "drop earlier parts of the input" |

In FastParse a cut also stops `|` from trying its next alternative, because `|` backtracks by default. In Creole, `Or` already commits once input is consumed, so a cut only matters where something rewinds: `Atomic`, `Lookahead`, `Not` and `Longest`.

Forces:

- **Commit semantics and the representation** ([§ Results](../../../whitepaper.md#results)). Committed means the cursor moved. A rule that needs a second notion of committed costs every `Or` a check.
- **Bounded buffers** ([§ Guarantees](../../../whitepaper.md#guarantees), LAY-02). A pinning bookmark can be released only when nothing can rewind to it.
- **Equivalence** (G7). Both engines must agree on every case, so the rule must be short enough to state exactly and test from a table.
- **Simplicity:** we prefer the simple standard approach.

## Options

The main fork is how far a cut reaches.

### A. A cut holds up to the nearest predicate (recommended)

Once a `Cut` succeeds, no enclosing `Atomic` rewinds, up to the nearest enclosing `Lookahead`, `Not` or `Longest` alternative. The cut stays in force even if the alternative or repetition step that contains it later fails without consuming and the parse moves on. A cut never turns an empty failure into a committed one.

- Pros: says "from this point" literally. Engines can release an `Atomic`'s bookmark when a cut is passed, because nothing inside the predicate boundary can rewind past it, which is the whitepaper's buffer argument. One flag on the parse state, saved and cleared by `Atomic` and the three boundaries; `Or` and repetition never touch it. Close to FastParse, the main library with cuts.
- Cons: coarse in one case. In `Atomic(Seq(Cut, Fail).Or(Seq(a, b)))`, the cut in the abandoned first alternative still stops `Atomic` rewinding when `b` fails after `a`. FastParse behaves the same way and offers `NoCut` for it; Creole would need the author to restructure.

### B. A cut belongs to the attempt that is kept

The cut is part of the outcome. `Atomic` keeps a failure only if a cut on the kept path was passed. `Or`, `Optional` and repetition drop the cut of an attempt they move on from after an empty failure. `Lookahead`, `Not` and `Longest` drop the cuts of attempts they rewind.

- Pros: compositional and precise; a cut in an abandoned alternative has no effect.
- Cons: a bookmark cannot be released at the cut, because a later abandoned attempt may clear the flag and let `Atomic` rewind after all. This loses the buffer purpose. `Or`, `Optional` and every repetition must save and restore the flag, which touches the hottest nodes.

### C. A cut affects only the innermost `Atomic`

- Pros: simplest to state.
- Cons: in `Atomic(Seq(x, Atomic(Seq(If, Cut, Body))))` the outer `Atomic` still rewinds, so the error is retried elsewhere and the buffer is held. Neither stated purpose is met.

### D. FastParse's global cut

A cut also stops `Or` from trying the next alternative, even when nothing was consumed, with a `NoCut` combinator to isolate a branch.

- Pros: one rule across `Or` and `Atomic`.
- Cons: committed would no longer mean "the cursor moved". Every `Or` would read a second flag, against [§ Results](../../../whitepaper.md#results). It adds a combinator (`NoCut`).

### E. Defer `Cut`

Build `Atomic`, `Lookahead`, `Not`, `Named` and `Context` now and leave `Cut` for later.

- Pros: less to specify now.
- Cons: the card names `Cut` inside `Atomic` in its criterion, ANA-06 allows `Cut` in deterministic mode, and LAY-02's bookmark rules depend on what a cut releases.

### Sub-choices

| Fork | Recommended | Alternative not taken |
| --- | --- | --- |
| `Atomic`, `Lookahead`, `Not` | Static members of `Grammar<TIn>`, called `Atomic(p)`, as the whitepaper writes them | Extension members called `p.Atomic()` |
| `Cut` | A shared leaf parser, `Cut : Parser<TIn, Unit>`, on `Grammar<TIn>`, like `Any` and `End`; used as `If.Before(Cut)` or `Seq(If, Cut, LParen, …)` | A combinator `p.Cut()` meaning "`p`, then cut", which keeps `Unit` out of `Seq` tuples; can be added later as sugar |
| `Named`, `Context` | Extension members, called `p.Named("value")`, so they can reject a null receiver with the caller's expression (CORE-13) | Instance methods, which throw `NullReferenceException` before they run |
| Their class | `ErrorReporting` | `Labelling`, `Diagnosis` (too close to `Diagnostics`), `ErrorContext` (does not fit `Named`) |
| `Lookahead` on a failure that consumed | The failure is kept, committed (Parsec, Megaparsec, Pidgin) | Rewind on failure too |
| `Not` | Never consumes (Parsec, Megaparsec, and the whitepaper's "succeed without consuming") | Pidgin's: consume as much as `p` did |
| `Named` on an empty success | Applies (Parsec, Megaparsec). Chosen by the maintainer on 2026-10-19 | Only on an empty failure, as § The error model now says |
| Empty label or context text | `GrammarException` at construction | Accept it and render an empty name |

## Decision

Option A, with the recommended sub-choices. Accepted by the maintainer on 2026-10-19, with the class name `ErrorReporting`.

**1. API.**

```csharp
public abstract partial class Grammar<TIn>
{
    public static Parser<TIn, TOut> Atomic<TOut>(Parser<TIn, TOut> parser, [CallerArgumentExpression(nameof(parser))] string parserExpression = "");
    public static Parser<TIn, TOut> Lookahead<TOut>(Parser<TIn, TOut> parser, [CallerArgumentExpression(nameof(parser))] string parserExpression = "");
    public static Parser<TIn, Unit> Not<TOut>(Parser<TIn, TOut> parser, [CallerArgumentExpression(nameof(parser))] string parserExpression = "");
    public static Parser<TIn, Unit> Cut { get; }
}

public static class ErrorReporting
{
    extension<TIn, TOut>(Parser<TIn, TOut> parser)
    {
        public Parser<TIn, TOut> Named(string label, [CallerArgumentExpression(nameof(parser))] string parserExpression = "");
        public Parser<TIn, TOut> Context(string text, [CallerArgumentExpression(nameof(parser))] string parserExpression = "");
    }
}
```

Each parser argument and each receiver goes through `Guard.NotNull` with its expression. A null or empty `label` or `text` throws `GrammarException`. Each combinator builds one node: `NodeKind.Atomic`, `Lookahead`, `Not`, `Cut`, `Named` and `Context`, with the wrapped parser as the only child (`Cut` has none).

**2. Semantics.** Outcomes are those of [§ Results](../../../whitepaper.md#results). "Rewind" means move the cursor back to where the combinator started, so the outcome counts as empty.

| Combinator | `p` succeeds | `p` fails without consuming | `p` fails after consuming |
| --- | --- | --- | --- |
| `Atomic(p)` | `p`'s success | Empty failure | Rewind, empty failure; **unless a cut is in force**, then `p`'s committed failure |
| `Lookahead(p)` | Rewind; empty success with `p`'s value | Empty failure | `p`'s committed failure |
| `Not(p)` | Rewind; empty failure | Empty success, `Unit` | Rewind; empty success, `Unit` |
| `Cut` | — (always succeeds without consuming, and puts a cut in force) | — | — |
| `p.Named(l)`, `p.Context(t)` | `p`'s outcome | `p`'s outcome | `p`'s outcome |

**3. Cut scope.** A cut is in force from the moment `Cut` succeeds until the parse leaves the nearest enclosing *cut boundary*. The boundaries are `Lookahead`, `Not` and each alternative of `Longest`, because each of them must rewind by definition. While a cut is in force:

- no `Atomic` between the `Cut` and the boundary rewinds, even if the step that contained the cut has since failed without consuming and an `Or`, `Optional` or repetition has moved on;
- nothing else changes. `Or`, `Optional` and repetition still decide by whether the cursor moved, so a cut never makes an empty failure committed.

A cut with no enclosing boundary lasts until the parse ends. `Recover` (ERR-05) is not a boundary: it moves forward, never back.

One way to implement this, which both engines may use: a `cut` flag on the parse state. `Atomic` saves the flag, clears it, runs `p`, and on failure rewinds only if the flag is still clear; on exit it sets the flag to `saved || flag`. A boundary saves and clears it, runs its parser, and restores the saved value. `Or` and repetition do not touch it.

**4. Labels.** `p.Named(l)` acts only when `p` consumes nothing, whether it succeeds or fails. Then the expectations `p` recorded at its start position are replaced by the single `Label(l)`. If `p` recorded none there, none are added. Expectations at later positions, such as a deeper failure kept by an `Atomic` that rewound, are unchanged, so the furthest failure still wins. When `p` consumes input, `Named` changes nothing.

**5. Context.** `p.Context(t)` pushes `t` onto the context stack while `p` runs. A failure recorded while `p` runs carries the stack, innermost first, as `ParseError.Context`. Whether an engine appends the start position to the line (the whitepaper's rendered example shows "in object starting at 2:1") is left to ERR-02, which builds the stack.

**6. Starved.** `Atomic`, `Lookahead` and `Not` treat Starved as an abort that propagates unchanged, as [§ The restart protocol](../../../whitepaper.md#the-restart-protocol) says. None of them rewinds or inverts a starved attempt.

**7. Card scope.** CORE-08 builds the nodes, the entry points, XML docs and a `docs/` page stating points 2 to 6, and a shared table of cases (grammar, input, expected outcome) covering every row of point 2 and the cut rules of point 3. Running Creole against the table belongs to INT-03, the first card that executes these nodes. The table is not run against Pidgin: matching Pidgin is not a goal (maintainer, 2026-10-19).

Deciding reason: option A is the only one that meets both purposes the whitepaper gives `Cut`, better messages and earlier buffer release, while keeping "committed means the cursor moved". The coarse case it accepts is the one FastParse accepts too.

What would change this:

- Real grammars where a cut in an abandoned alternative blocks a rewind the author needs. Then a `NoCut`-style boundary combinator would be added, not option B.
- LAY-02 finding that releasing a bookmark at the cut buys nothing measurable. Then option B's precision would cost nothing, and would be worth reconsidering.

## Consequences

- **Easier:**
  - Engines implement one flag with save and restore in four combinators. `Or` and repetition are unchanged.
  - LAY-02 can release an `Atomic`'s pinning bookmark as soon as a cut is passed inside it.
- **Harder:**
  - Authors must learn where a cut stops (the three boundaries) and the coarse case below.
- **Differences from Pidgin**, recorded as facts rather than costs, since matching Pidgin is not a goal (maintainer, 2026-10-19):
  - Pidgin's `Not` consumes as much as its parser did; Creole's never consumes.
  - Pidgin's `Labelled` replaces expectations after any failure, consumed or not, and never on success; Creole's `Named` acts only when nothing was consumed, including on success.
- **Ruled out:**
  - A cut that changes `Or`'s choice without consumption (option D).
  - A cut that escapes `Lookahead`, `Not` or a `Longest` alternative.
- **Risks and how they are watched:**
  - The coarse case of option A. The case table includes it as a row, so both engines agree on it, and docs/ describes it.
  - [ADR-0004](0004-expression-build-operator-table-and-semantics.md) point 5 says `Named` "replaces its expected set only for empty failures, as for any node". That bullet restates the general rule rather than deciding an expression-specific one, so point 4 here applies to `Expression.Build` as to any node. ADR-0004 is not superseded (maintainer, 2026-10-19).
  - Another record numbered 0008 may be drafted in a parallel worktree (CORE-06, CORE-09, CORE-13). Renumber whichever merges second.

## Changes made

- **Whitepaper:**
  - [§ Authoring ergonomics](../../../whitepaper.md#authoring-ergonomics): `Grammar<TIn>` lists `Atomic`, `Lookahead`, `Not` and `Cut`.
  - [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking): `Lookahead` and `Not` on failures that consumed; the cut boundary rule and the flag implementation; links this record.
  - [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention): an `Atomic` may release its pinning bookmark once a cut is in force inside it.
  - [§ The error model](../../../whitepaper.md#the-error-model): `Named` applies whenever `p` consumes nothing, at `p`'s start position only; links this record.
- **Cards:**
  - [CORE-08](../../active/02-core-combinators/CORE-08-control-nodes.md): scope names the API, the class name and the case table; moved to Building.
  - [INT-03](../../complete/03-interpreter/INT-03-execute-the-core-combinators.md): scope names the cut boundaries, including each `Longest` alternative; gains a criterion to pass every row of the CORE-08 control table.
  - [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md): `Named` per point 4; deciding whether `Context` lines carry a start position.
  - [LAY-02](../../active/06-layers-and-pipelines/LAY-02-marks-and-retention.md): release an `Atomic`'s bookmark once a cut is in force inside it.
