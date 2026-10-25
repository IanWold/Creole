# ADR-0021: The first failure at a position keeps its context, predicates drop their parser's failures, labels keep messages, and dispatch records what it skips

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-24 |
| Cards | [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md), [ERR-01](../../active/04-errors-and-recovery/ERR-01-error-and-result-types.md), [ERR-03](../../active/04-errors-and-recovery/ERR-03-diagnostic-rendering.md), [INT-05](../../active/03-interpreter/INT-05-interpreter-lowering-rewrites.md), [GEN-02](../../active/11-source-generator/GEN-02-emit-the-core-combinators.md) |
| Whitepaper | [§ The error model](../../../whitepaper.md#the-error-model), [§ Failure positions](../../../whitepaper.md#failure-positions), [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking), [§ Custom primitives](../../../whitepaper.md#custom-primitives) |
| Research | — (prior art read for this record by a research subagent on 2026-10-24, cited inline) |
| Supersedes | — |

## Context

[§ The error model](../../../whitepaper.md#the-error-model) says the furthest failure wins and that expected sets merge at equal positions. G7 requires both engines to agree on "every diagnostic's span and every expected set" ([§ Equivalence](../../../whitepaper.md#equivalence)). Several earlier records leave the detailed rules to ERR-02:

| Record | What it leaves open |
| --- | --- |
| [ADR-0008](0008-control-node-semantics.md) point 4 (stands under ADR-0011) | How `Named` treats expectations recorded at its start position before it ran |
| ADR-0008 point 5 | Whether a context line shows where its construct started |
| [ADR-0011](0011-a-cut-is-dropped-with-every-failed-attempt.md) point 6, [ADR-0016](0016-or-dispatches-on-each-alternatives-leading-item.md) point 4 | What an alternative that dispatch skips contributes to the expected set |
| [ADR-0012](0012-custom-scanner-metadata.md) point 4 | What `Satisfy` and a FIRST set of ⊤ record. A `Custom` with no `Name` and no `First` follows the same rule. |
| [ADR-0014](0014-a-rejected-filter-reports-at-the-start-of-the-value.md) point 5 | How `Named` treats a custom message from `Fail` or a filter |

ERR-02's scope adds three more questions: what `Not` and a successful `Lookahead` do with the failures recorded inside them, what `Not` records when its parser succeeds, and which context stack a merged failure keeps.

Constraints:

- **Failure paths allocate nothing** (ERR-02's criterion, [§ Failure positions](../../../whitepaper.md#failure-positions)). Each layer keeps one furthest failure as a buffer index and a handle, and builds a `ParseError` only for the failure it reports.
- **G2, bounded memory.** No new pinning bookmarks. A failure that is reported behind the cursor captures what its error needs by value (ADR-0014 point 6).
- **G7.** Every rule must be one the generator can emit, and dispatch must not change what is recorded.
- **Simplicity.** Choose the standard approach, and keep later additions non-breaking.

Prior art, read from source by the research subagent: Megaparsec at tag 9.8.3 ([`Internal.hs`](https://github.com/mrkkrp/megaparsec/blob/9.8.3/Text/Megaparsec/Internal.hs), with [`Error.hs`](https://github.com/mrkkrp/megaparsec/blob/master/Text/Megaparsec/Error.hs) on master), chumsky at tag 0.12 ([`error.rs`](https://github.com/zesterer/chumsky/blob/0.12/src/error.rs), [`label.rs`](https://github.com/zesterer/chumsky/blob/0.12/src/label.rs), [`combinator.rs`](https://github.com/zesterer/chumsky/blob/0.12/src/combinator.rs), [`primitive.rs`](https://github.com/zesterer/chumsky/blob/0.12/src/primitive.rs)) and FastParse at tag 3.1.1 ([`package.scala`](https://github.com/com-lihaoyi/fastparse/blob/3.1.1/fastparse/src-3/fastparse/package.scala), [`MacroInlineImpls.scala`](https://github.com/com-lihaoyi/fastparse/blob/3.1.1/fastparse/src-3/fastparse/internal/MacroInlineImpls.scala), [`SharedPackageDefs.scala`](https://github.com/com-lihaoyi/fastparse/blob/3.1.1/fastparse/src/fastparse/SharedPackageDefs.scala), [`Parsed.scala`](https://github.com/com-lihaoyi/fastparse/blob/3.1.1/fastparse/src/fastparse/Parsed.scala)).

| Question | Megaparsec | chumsky | FastParse |
| --- | --- | --- | --- |
| Successful lookahead: inner failures | Dropped (`eok a s mempty`) | `rewind` keeps those at or before its start and drops deeper ones | Kept |
| Negative lookahead: inner failures | Dropped | Dropped (the earlier error is restored) | Dropped (`terminalMsgs = startTerminals`) |
| Negative lookahead whose parser succeeds | Next item unexpected, nothing expected | Expects "something else" at the start | A failure at the start with no messages |
| Label over a custom message | Unchanged (`_ -> err` for a FancyError) | The label replaces the message | No special case |
| Label and failures recorded before it ran | Untouched | Untouched | Not determined |
| Two failures at one position | A FancyError beats a TrivialError, whose expected set is lost | A custom reason wins, and the first one wins if both are custom. Contexts come from the earlier error ("TODO: Merge contexts"). | No merge; failures are re-collected by tracing |
| Where a context applies | No contexts | Only to errors past the context's start; an error at the start is relabelled instead | Named rules push `name -> startIndex` |
| Context shows its start | — | Yes, as a span ("in X at …") | Yes, as an index |
| Predicate parser | `satisfy`, `oneOf`, `noneOf`: nothing expected | `filter` and `select!`: "something else"; `any`: "any" at end of input | `char-pred(…)`, `any-char` |

All three libraries keep a negative lookahead's inner failures out. chumsky's handling of contexts is the closest precedent for this record's choice.

One finding cuts the other way, and this record does not reopen it. Megaparsec 9.8.x combines alternatives so that an error at the alternation's start beats a deeper one reached through `try` (issue [#412](https://github.com/mrkkrp/megaparsec/issues/412), "(<|>) is not associative", closed by PR #599). Creole's furthest failure still wins past an `Atomic` (ADR-0008 point 4). A layer-wide maximum is associative by construction, so the issue that led Megaparsec to change does not arise. But Creole now has no direct precedent in Megaparsec for that rule.

## Options

### Fork 1. `Lookahead` and `Not`

#### 1A. Restore the furthest failure (chosen)

- A `Lookahead` whose parser succeeds restores the furthest failure to what it was when it started.
- A `Not` restores it in both outcomes.
- A `Not` whose parser succeeds then records a failure at its start position, with no expectations.

**Pros:**

- A negative lookahead's expectations describe what must not come next, so "expected keyword" from `Not(Keyword)` would mislead.
- A successful lookahead rewinds, but the failures it recorded past its start would outrank the real error at the start. `Lookahead(Digit.Many())` records "expected digit" where the digits end.
- Matches Megaparsec for both combinators.
- The restore is a few fields saved and put back, with no allocation.

**Cons:**

- Each `Lookahead` and `Not` saves the furthest failure on entry.
- A failure that `Not` records with no expectations reads only "unexpected 'if'". `Named` turns it into an expectation.

#### 1B. Keep everything

`Lookahead` and `Not` leave what their parser recorded.

**Pros:**

- The simplest option: no save and no restore.

**Cons:**

- Produces both misleading cases above.
- A `Not` whose parser succeeds still needs some recorded failure, or a parse could fail with nothing to report.

#### 1C. Keep only failures at or before the start (chumsky's `rewind`)

**Pros:**

- Keeps the alternatives a successful lookahead tried at its start.

**Cons:**

- Those alternatives did not fail the parse. In `Lookahead(Or(A, B)).Then(C)` on `B`, reporting "expected A or C" suggests that `A` would have helped, which it would not.
- `Not` still needs 1A.

### Fork 2. `Named`

#### 2A. The label replaces only what `p` recorded, counting failures rather than expectations (chosen)

When `p` consumed nothing:

- The expectations `p` recorded at its start position are replaced by the label.
- Expectations recorded there before `p` ran stay, and the label follows them.
- The label is added if `p` recorded any failure at its start that is still part of the furthest failure. A failure with no expectations counts, as ADR-0014 point 5 says for a rejection.

**Pros:**

- `Or(Kind(Comma), Value.Named("value"))` keeps `','`.
- `Satisfy(isHex).Named("hex digit")` reads "expected hex digit", although `Satisfy` records no expectation (fork 6).
- `Or(Char('a'), Char('a').Named("A"))` reads "expected 'a' or A", because the second alternative recorded a failure even though its expectation was already present.
- Megaparsec and chumsky also leave earlier failures untouched.

**Cons:**

- The engine must know whether `p` recorded a failure, not just whether the set changed. That takes a counter or a saved handle.

#### 2B. The label is added only when the expected set changed

**Pros:**

- Needs only a comparison of handles.

**Cons:**

- `Satisfy(...).Named(...)` and `Not(...).Named(...)` would never be labelled.
- An alternative that repeats an expectation already present loses its label.

#### 2C. The label replaces everything at the position

**Cons:**

- `Or(Kind(Comma), Value.Named("value"))` would drop `','`. No surveyed library does this.

#### Messages under `Named`

- **2D. The message stays (chosen).** `Named` works on expectations only. A message `p` recorded at its start stays as what was found. So `Atomic(Identifier.Where(isNotKeyword, "keyword is reserved")).Named("identifier")` on `if` reads "expected identifier", with the carets labelled "keyword is reserved". This matches Megaparsec. The author wrote the message deliberately, and a label at a higher level, such as `Value.Named("value")`, should not erase it.
- **2E. The label replaces the message.** This matches chumsky. It loses the author's text whenever any enclosing rule is named.

### Fork 3. Failures at one position

Every failure at the furthest position merges into one record. Its expectations are always the union, in the order recorded, with duplicates dropped. The open question is which context stack the merged record keeps.

#### 3A. The first failure keeps its stack, and a context line applies only past its start (chosen)

- The first failure recorded at a position supplies the context stack. Later failures at that position add expectations only.
- `p.Context(text)` gives a line only to failures recorded past the position where `p` started.

**Pros:**

- A construct that never began adds no line. `OneOf(ObjectValue.Context("in object"), …)` failing at its first token does not report "in object".
- Both rules are chumsky's.
- O(1) per record.
- An alternative that dispatch skips fails at its own start, so its inner contexts never apply. Dispatch needs to model none (fork 5).

**Cons:**

- When two constructs that both began earlier fail at the same position, only the first one's lines are shown. This needs `Atomic` backtracking to the same position, as in `Or(Atomic(Seq(A, B)).Context("in X"), Seq(A, C).Context("in Y"))`.
- Refines ADR-0008 point 5, which gives a failure "the stack" whatever its position.

#### 3B. The first failure keeps its stack, and lines apply at their start too

**Pros:**

- ADR-0008 point 5 as written.

**Cons:**

- Reports "in object starting at" the very token that failed to start an object.
- Order-dependent: which alternative comes first decides whether the line appears.

#### 3C. Only the lines shared by every merged failure

**Pros:**

- The most precise: every line holds for every expectation shown.

**Cons:**

- A walk up two stacks on each merge where the stacks differ.
- Two entries of one `Context` must compare by text and start, not identity.
- Dispatch must model the inner contexts of skipped alternatives.
- No surveyed library does this, and chumsky leaves it as a TODO.

#### Messages and spans at one position (no real fork)

- A custom message takes the place of the unexpected item, and the first message recorded at a position wins.
- Expectations still merge alongside a message. `ParseError` holds both, and [ERR-03](../../active/04-errors-and-recovery/ERR-03-diagnostic-rendering.md) shows both: the expectations in the headline and the message at the carets. This departs from Megaparsec and chumsky, where a custom error drops the expected set. Their error types hold one or the other; Creole's holds both.
- A failure reported behind the cursor, such as a rejected filter or a chained operator, carries a span over the input it names. The merged record's span covers the longest of these. Otherwise it covers the item at the position, or is zero-width at the end of input. This keeps a rejection's span over the value whenever the rejection is part of the report.

### Fork 4. Where a context line's start comes from

#### 4A. Structured: the line holds its text and where its construct started (chosen)

`ParseError.Context` becomes `IReadOnlyList<ContextLine>`, where `ContextLine(string Text, SourceSpan Start)` holds the span of the first item the construct read. The renderer prints "in object starting at 2:1". `Render()`, which has no source text, prints the character offset.

**Pros:**

- Engines need no line map. A streamed parse that had to append "L:C" text would have to build a `LineMap` as it reads, on the success path. The renderer already has the source text.
- A language server gets a related location for each line.
- chumsky and FastParse both keep a context's start as data.

**Cons:**

- Changes ERR-01's public `ParseError`, its tests and docs, and the whitepaper's sketch.
- Each `Context` entry captures its first item's span by value, because that item may be released before the error is built. This is the same capture that ADR-0014 point 6 needs.

#### 4B. The engine appends " starting at L:C"

**Pros:**

- `ParseError` keeps `IReadOnlyList<string>`, and the whitepaper is unchanged.

**Cons:**

- Engines need line and column when they build the error. For streamed input that means building a `LineMap` while reading.
- The position is frozen into text, so a language server cannot use it.

#### 4C. No position

**Pros:**

- The simplest option. The whitepaper's example becomes "= note: in object", and 4A can be added later.

**Cons:**

- The note loses the most useful thing it says: where the unclosed construct began.

### Fork 5. Alternatives that dispatch skips

An alternative with a key set fails at its leading primitive, without consuming, when the next item is outside that set or the input has ended (ADR-0016 point 1). What it would record there is therefore fixed by the grammar.

#### 5A. Precomputed per gap between candidates (chosen)

- Lowering computes, for each keyed alternative, what it would record there: its leading primitive's record (fork 6). If any `Named` lies on the walk from the alternative to that primitive, the outermost one's label replaces that record. `Context` adds nothing (fork 3A), and the other wrappers on the walk pass the record through.
- For each key, the dispatch plan keeps one merged record per gap: the skipped alternatives between one candidate and the next, and those after the last.
- At run time, before it runs each candidate, the plan records the gap before it. If every candidate fails without consuming, or there are none, it records the final gap.

**Pros:**

- Exact. Running in order records the same expectations, in the same order, and stops at the same success or committed failure. The skipped alternatives after a successful candidate would never have run, and none is recorded.
- One record operation per gap, not per skipped alternative, with nothing allocated.
- No pinning.

**Cons:**

- Lowering must compute what a primitive records, so that rule lives in one place that both the plan and the primitive use.

#### 5B. Ask each skipped alternative to record at run time

**Cons:**

- Exact, but the cost per skipped alternative takes back much of what dispatch saves.

#### 5C. Re-run the skipped alternatives when the error is built

**Cons:**

- Needs the input at the dispatch position when the error is built, which pins it or captures it. Both go against G2.

#### 5D. Record nothing for skipped alternatives

**Cons:**

- The error would depend on whether lowering chose to dispatch, which is a benchmark threshold (ADR-0016 point 2). This breaks the exactness ADR-0016 point 3 promises.

### Fork 6. `Satisfy` and ⊤

#### 6A. A failure with no expectation (chosen)

`Satisfy`, `Any` and a `Custom` with neither `Name` nor `First` record a failure with no expectation. Their FIRST set is ⊤, so no finite description is honest.

**Pros:**

- Counts as a failure for `Named` (fork 2A), so a label gives it a name.
- Matches Megaparsec's `satisfy`.

**Cons:**

- Unnamed, it reads only "unexpected 'q'".

#### 6B. A placeholder such as "something else"

**Pros:**

- chumsky's `SomethingElse` shows that an expectation existed.

**Cons:**

- Text that says nothing, which the author must override anyway.

#### 6C. An optional name parameter on `Satisfy`

**Cons:**

- New API that duplicates `Named`. It can be added later without breaking anything.

## Decision

Options **1A, 2A with 2D, 3A, 4A, 5A and 6A**. The maintainer chose 2D, 3A, 4A and 6A in conversation on 2026-10-24, and accepted the record, with 1A and 5A as recommended, on 2026-10-25.

**1. One record per layer.** Each layer keeps one furthest failure. It holds:

- a position, which is a buffer index;
- its expectations, in the order recorded, without duplicates;
- at most one custom message;
- a context stack;
- a span end.

Recording a failure works by comparing positions:

| New failure's position | Effect |
| --- | --- |
| Before the record's | Ignored |
| After the record's | Replaces the record |
| The same | Merged into the record |

Merging at one position:

- Expectations are added in order, skipping any already present.
- The first message recorded wins.
- The first failure recorded keeps its context stack.
- The span covers the longest span named by a failure recorded behind the cursor. Otherwise it covers the item at the position, or is zero-width at the end of input.

The unexpected part is the message if there is one. Otherwise it is the item at the position, or end of input. It is determined when the error is built.

**2. Lookahead and Not.**

- `Lookahead(p)`:
  - If `p` succeeds, the furthest failure is restored to what it was when `Lookahead` started.
  - If `p` fails, its failure stands.
- `Not(p)`:
  - If `p` fails, consuming or not, the furthest failure is restored.
  - If `p` succeeds, the furthest failure is restored. Then `Not` records a failure at its start position with no expectations.

**3. Labels.** This refines ADR-0008 point 4. When `p.Named(l)` ends and `p` consumed nothing:

- If the furthest failure is at `Named`'s start position and includes a failure `p` recorded, the expectations `p` recorded there are replaced by `Label(l)`. Expectations recorded at that position before `p` started are kept, ahead of the label.
- A failure with no expectations counts as one `p` recorded.
- A failure that a restore inside `p` removed does not count.
- Messages are never removed or replaced by `Named`.
- In every other case, `Named` changes nothing.

**4. Context.** This refines ADR-0008 point 5.

- `p.Context(text)` pushes a line while `p` runs, with the span of the item at `p`'s start, captured by value.
- A failure carries the lines whose construct started before the failure's position, innermost first.
- `ParseError.Context` is `IReadOnlyList<ContextLine>`, where `public sealed record ContextLine(string Text, SourceSpan Start)`. `Text` is not null or empty.
- [ERR-03](../../active/04-errors-and-recovery/ERR-03-diagnostic-rendering.md)'s renderer prints `= note: {Text} starting at {line}:{column}` from `Start`. `Render()` without sources prints the character offset instead.

**5. Dispatch.** This settles ADR-0011 point 6 and ADR-0016 point 4 for errors.

- Lowering computes what each keyed alternative records when it is skipped. That is its leading primitive's record (point 6), unless a `Named` lies on the walk from the alternative to that primitive. Then the outermost such `Named`'s label replaces it.
- Per key, the dispatch plan keeps one merged record for each gap between candidates, and one for the skipped alternatives after the last.
- At run time it records each gap before running the candidate that follows it. If every candidate fails without consuming, or there are none, it records the final gap.
- Lowering and the primitives share the code that says what a primitive records.

**6. What primitives record.** Each primitive records at its start position, when it fails:

| Primitive | Records |
| --- | --- |
| `Char(c)`, `Token(x)` | The item |
| `Kind(k)` | The kind. Its display follows the `Describe` decision (ERR-02, separate record) |
| `String(s)`, `CIString(s)` | `s`, as one expectation |
| A named class, such as `Digit` | `Label` with the class's name |
| `Range`, `NoneOf` and other unnamed classes of more than one character | One expectation that describes the set |
| `End` | `EndOfInput` |
| `Custom` | `Label(Name)`; otherwise its `First` as one expectation describing the set, like an unnamed class; otherwise as `Satisfy` |
| `Satisfy`, `Any` | A failure with no expectation |
| `Fail(message)` | The message, with no expectation |

The `Custom` row refines ADR-0012 point 4, which said "each item of `First`". That reading would list every character of a large `First`. With one character, the two readings show the same. Display strings, including how an unnamed class describes its set, are formatted when the error is built. They are a build detail, pinned by snapshot tests.

Deciding reasons, by fork:

- **1A:** the only option under which neither lookahead can make an error point at the wrong thing.
- **2A and 2D:** labels describe what was expected and messages say what was found, so neither erases the other.
- **3A:** O(1) per record, and context lines appear only for constructs that began.
- **4A:** keeps line maps out of engines and keeps the position usable by tools.
- **5A:** the only exact option that costs no more than dispatch saves.
- **6A:** honest about ⊤, and `Named` covers the rest.

What would change this:

- Users finding context lines missing because of 3A, for constructs that both began before the failure. Then 3C would replace it, without an API change.
- Benchmarks showing that capturing each `Context` start costs measurable time on success paths. Then the capture could move to the first failure recorded past the start, or 4A's position could become optional.
- Demand for a named `Satisfy`. Then 6C would be added.

## Consequences

**Easier:**

- Every combinator's contribution to an error is fixed, so the GEN-08 differential tests can compare whole reports.
- Dispatch changes no error message, so INT-05's keep-or-drop rule for rewrites stays a question of speed alone.
- Tools get a context's start as a span.

**Harder:**

- ERR-02 changes a public type from a Done card: `ParseError.Context`, with ERR-01's tests and docs/concepts/errors.md.
- ERR-03's renderer and its snapshot test change too.
- `Lookahead`, `Not`, filters and `Named` each save part of the furthest failure on entry.
- `Context` captures a span on entry.

**Implementation sketch (not decided here).** None of this needs allocation in steady state:

- An expectation set can be an interned handle with a merge cache, or a reusable per-layer log that a saved length truncates.
- Context frames can be a parent-pointer arena reused across parses, so a record keeps a frame index.

The build chooses, measured against ERR-02's allocation criterion and [BENCH-01](../../active/13-benchmarks/BENCH-01-phase-0-speed-gate.md).

**Ruled out:**

- A label that erases a message.
- Context lines for constructs that never began.
- An error that depends on whether dispatch ran.
- Placeholder text for ⊤.

**Risks and how they are watched:**

- **Dispatch drifting from running in order.** INT-05's dispatch exactness tests gain the recorded failure, and the CORE-08 control table gains expected sets.
- **A compiled subtree recording differently from the interpreter.** `Named` and `Context` may sit inside a DFA-compiled subtree (ADR-0017 point 1). The group B record on compiled subtrees must follow points 3 and 4.
- **Megaparsec 9.8's opposite choice for deeper failures past a `try`.** It is not reopened here. If users report errors that point past where they expected, it should get its own record.

## Changes made

- **Whitepaper:**
  - [§ The error model](../../../whitepaper.md#the-error-model): the `ParseError` sketch holds `ContextLine`s. **Furthest failure wins** gives the merge rule (point 1). **Labels** keeps earlier expectations, counts failures and never removes a message (point 3). New **Lookahead and Not** and **Primitives** bullets (points 2 and 6). **Context** applies a line only past its start and shows where it began (point 4). Each links this record.
  - [§ Failure positions](../../../whitepaper.md#failure-positions): `Lookahead`, `Not`, filters and `Named` save the record on entry, and `Context` captures its first item's span by value.
  - [§ Choice and backtracking](../../../whitepaper.md#choice-and-backtracking) and [§ Interpreted engine](../../../whitepaper.md#interpreted-engine): dispatch records what the attempts it skips would have expected (point 5).
  - [§ Custom primitives](../../../whitepaper.md#custom-primitives): `First` is one expectation, and a scanner with neither `Name` nor `First` records as `Satisfy` does.
- **Cards:**
  - [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md): the scope items this record settles cite it. The `ContextLine` change and the renderer change are in scope, and a new criterion requires tests for every point, including dispatch against running in order. The card stays in Design for groups B and C.
  - [GEN-02](../../active/11-source-generator/GEN-02-emit-the-core-combinators.md): its expectation-tracking line cites this record and names dispatch's records per gap.
- **Decisions index:** a note that this record refines ADR-0008 points 4 and 5 and ADR-0012 point 4.
