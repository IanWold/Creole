# ADR-0016: The interpreter's `Or` dispatches on each alternative's leading item

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-22 |
| Cards | [INT-05](../../active/03-interpreter/INT-05-interpreter-lowering-rewrites.md), [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md), [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md) |
| Whitepaper | [§ Interpreted engine](../../../whitepaper.md#interpreted-engine), [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking) |
| Research | — |
| Supersedes | — |

## Context

[§ Interpreted engine](../../../whitepaper.md#interpreted-engine) lists the rewrites lowering performs. One of them reads: "An `Or` over primitives with disjoint FIRST sets becomes a `switch` on token kind or character."

Read literally, this rarely applies. Grammars wrap their primitives before choosing between them. Every alternative of Worked Example A's `Value` is a wrapper:

| Alternative | Node shape | Leading primitive |
| --- | --- | --- |
| `Kind(J.String).Select(...)`, `Kind(J.Number).Select(...)` | `Map` over `Kind` | `Kind(String)`, `Kind(Number)` |
| `Kind(J.True).As(...)` and the other two literals | `Map` over `Kind` | `Kind(True)`, `Kind(False)`, `Kind(Null)` |
| `ArrayValue` | `Map` over `Between`, a `Seq` | `Kind(LBracket)` |
| `ObjectValue` | `Map` over `Between`, a `Seq` | `Kind(LBrace)` |

None is a bare primitive, so the rewrite as written never fires on the whitepaper's own example. A character-level JSON value is the same: `Char('{')` and `Char('[')` start sequences, and `String("true")` sits under `As`.

The semantics are already settled. [ADR-0011](0011-a-cut-is-dropped-with-every-failed-attempt.md) point 6 lets an engine skip an `Or` alternative "when the next item shows it would fail without consuming", and [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking) says that dispatching on FIRST sets gives the same result as trying in order. The model behind ADR-0011 measured 0 differences for this choice dispatch over 252,180 cases. What is open is how the interpreter finds which alternatives the next item rules out.

Constraints:

- **Exactness (G7).** Dispatch must not change any outcome, value, position, cut flag or, once [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md) exists, any expected set.
- **No analysis yet.** [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md), the fixpoint that computes nullability and FIRST per node, has not started and does not block INT-05. Primitives already report their FIRST set, nullability and opacity (`IPrimitiveNode`).
- **Measured.** INT-05 keeps a rewrite only if its benchmark shows it helps. In INT-05's build plan the maintainer chose option B below over A and C.

## Options

### A. As written: an `Or` whose alternatives are all primitives with disjoint FIRST sets

The `Or` becomes a `switch` only when every alternative is itself a primitive and no two FIRST sets overlap. Otherwise it runs in order.

- Pros: no design change, and the simplest code.
- Cons: misses Worked Example A and most real choices, as the table shows. One overlapping pair, such as a keyword and an identifier both starting with `i`, sends the whole `Or` back to running in order. INT-05's keep rule would likely drop it for lack of any effect.

### B. Leading item, through a fixed set of wrappers, with a candidate list per key

Lowering walks each alternative down its leading position, through nodes that cannot consume, cut or succeed before their first item does: `Map`, `Named`, `Context`, `Slice`, `Text`, `Atomic`, a filter, the source of `Bind`, and the first item of `Seq`. If the walk reaches a keyed primitive, the alternative takes that primitive's keys. Otherwise it has none. For each key, the dispatch plan holds the alternatives that can start with it, plus every alternative with no keys, in the original order. FIRST sets may overlap.

- Pros:
  - Covers all seven alternatives of Example A's `Value`, and the bracketed, quoted and literal alternatives of a character-level JSON value.
  - A partly keyed `Or` still dispatches. Alternatives with no keys, or overlapping ones, run in order among the candidates.
  - Local and conservative. Any node it does not know ends the walk, and that alternative is simply always tried.
  - No dependency on ANA-01.
- Cons:
  - A second, partial FIRST computation beside ANA-01's. The two could disagree; the differential tests check this computation against running in order.
  - Misses alternatives behind a nullable prefix, such as a number that starts with `Char('-').Optional()`, and alternatives behind `Rec`. These are tried for every key, so they are still correct, just slower.

### C. FIRST and nullable from ANA-01

Lowering takes each alternative's FIRST set and nullability from ANA-01's fixpoint.

- Pros: exact, complete through nullable prefixes and `Rec`, and one source of truth shared with analysis and the Roslyn component (RSLN-02).
- Cons: ANA-01 has not started. Dispatch would wait for it or split off into a card it blocks. Running a fixpoint on first use adds to startup for grammars built at run time.

### D. Jump tables only in deterministic mode

Dispatch only grammars that `Grammar.Deterministic` has checked to be LL(1) ([§ Deterministic mode](../../../whitepaper.md#deterministic-mode), ANA-06). Everything else runs in order.

- Pros: no partial dispatch. It is exact because of the LL(1) check.
- Cons: ordinary grammars get no speedup. It waits for ANA-06, which needs ANA-01, ANA-02, LAY-02 and MODE-01.

## Decision

Option B.

1. **Keys.** An alternative has a key set when, whenever the next item is outside that set or the input has ended, the alternative fails without consuming input and without passing a `Cut`. Lowering finds key sets by walking the alternative's leading position:
   - A primitive has the keys of its FIRST set when that set holds only keys (characters or enum values, with no case types, literal values, ⊤ or end of input), and the primitive is neither nullable nor opaque. This covers `Char`, character classes, `String` and `CIString` (their first character, in every case variant for `CIString`), `Kind`, `Token` over characters or enum values, and a `Custom` that declares `First` and is not nullable. Dispatch trusts a scanner's declaration, as analysis does ([ADR-0012](0012-custom-scanner-metadata.md)).
   - `Select`, `As` and `Ignore`, `Named`, `Context`, `Slice`, `Text`, `Atomic`, a filter (`Where`) and the source of a `Bind` have the key set of the parser they wrap. None of them runs user code, or passes a cut, before that parser succeeds.
   - A sequence (`Seq`, `Then`, `Before`, `Between`, and query syntax that becomes a sequence) has the key set of its first item.
   - Every other node has no key set, including `Rec`, `Optional`, repetitions, `Or`, `Longest`, `Lookahead`, `Not`, `Cut`, `Return`, `Fail`, `End`, `Satisfy`, `Case`, `Switch`, `Match`, `Expression.Build` and an opaque `Custom`.
   - All keys in one `Or` must be read from the next item in the same way: as a character, as the item itself when the item is an enum, or as `Kind` of an `IKinded<TKind>` token for one `TKind`. If the keys are read in more than one way, the `Or` runs in order.
2. **Dispatch.** The `Or` (and `OneOf`, the same node) peeks one item and reads its key. Its candidates are, in the original order, the alternatives whose key set contains that key and the alternatives with no key set. At the end of the input, the candidates are the alternatives with no key set. It runs the candidates exactly as `Or` runs its alternatives: each with its light mark, stopping at the first success or committed failure. If every candidate fails without consuming, or there are none, it fails without consuming. Lowering leaves an `Or` in order when dispatch would not pay, for example when no alternative has keys; INT-05's benchmarks set that threshold.
3. **Exactness.** Every skipped alternative would have failed without consuming input and without passing a cut, so this is ADR-0011 point 6's choice dispatch. INT-05 tests that the outcome, value, position and cut flag match running in order.
4. **Errors.** A skipped alternative would have recorded expectations at the current position. For each key, the dispatch plan keeps the alternatives it skips, so that ERR-02 can record the same expected set as running in order (G7). ERR-02 decides what is recorded.
5. **Later.** When ANA-01 computes nullability and FIRST for every node, lowering may take key sets from it instead of from the walk. The condition in point 1 still applies. The dispatch plan does not change. This extends dispatch to alternatives behind nullable prefixes and `Rec`.

Deciding reason: B makes the rewrite apply to the grammars the whitepaper and the benchmarks use, now, without waiting for analysis. ADR-0011 already shows that dispatch of this kind is exact. C gives the most coverage, but it can replace B's walk later without changing the plan.

What would change this: INT-05's benchmarks showing that a candidate list per key costs more than it saves on realistic `Or`s. The rewrite would then be dropped, as INT-05's criterion requires. A case where the walk's key set lets a skipped alternative do something other than fail without consuming would be a defect in the walk, and the differential tests are there to catch it.

## Consequences

- **Easier:**
  - Choices over wrapped tokens and characters skip alternatives that cannot start, without the author writing a `Switch`.
  - ANA-06's jump tables can build on the dispatch plan.
- **Harder:**
  - Two FIRST computations exist until ANA-01 replaces the walk.
  - ERR-02 must record expectations for skipped alternatives (point 4). This is already in its scope.
  - A trace shows only the candidates tried. TOOL-02 can lower with dispatch off when every attempt should appear.
- **Ruled out:** a `switch` that falls back to running the whole `Or` in order as soon as two FIRST sets overlap.
- **Risks and how they are watched:**
  - A wrong key set would skip an alternative that could have succeeded. INT-05's differential tests compare dispatch with running in order, on the CORE-06, CORE-07 and CORE-08 tables, Worked Example A, and generated `Or` shapes.
  - Dispatch might not pay. INT-05's keep rule measures it on its own benchmark category, and drops it if it fails.

## Changes made

- **Whitepaper:** [§ Interpreted engine](../../../whitepaper.md#interpreted-engine): the `Or` bullet describes dispatch on each alternative's leading item, with candidates per key, and links this record. [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking) already states that dispatch on FIRST sets is exact and is unchanged.
- **Cards:**
  - [INT-05](../../active/03-interpreter/INT-05-interpreter-lowering-rewrites.md): the scope bullet for `Or` follows point 2, and the card returns to Building.
  - [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md): the scope notes that lowering may take key sets from its properties (point 5).
  - [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md): unchanged; its scope already covers what a skipped attempt records (point 4).
