# ADR-0014: A rejected filter reports at the start of the rejected value, stays committed, and may carry a message

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-22 |
| Cards | [INT-04](../../complete/03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md), [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md), [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md), [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md) |
| Whitepaper | [§ LINQ query syntax](../../../whitepaper.md#linq-query-syntax), [§ The error model](../../../whitepaper.md#the-error-model) |
| Research | — (prior art read for this record by a research subagent on 2026-10-22, cited inline) |
| Supersedes | — |

## Context

[§ LINQ query syntax](../../../whitepaper.md#linq-query-syntax) says "`where` becomes a filter that fails committed if it rejects after consuming input". It does not say what the failure reports or where. [CORE-10](../../active/02-core-combinators/CORE-10-bind-and-linq-query-syntax.md) built `FilterNode` and `p.Where(predicate)`, and left the report to INT-04.

Three facts shape the choice:

- **Furthest failure wins** ([§ The error model](../../../whitepaper.md#the-error-model)). A layer keeps the furthest failure recorded while it runs, including failures inside a parser that then succeeds. A `Number` built from a digit loop records "expected digit" where the loop stops. A rejection recorded at the start of that `Number` loses to it.
- **Memory is bounded** ([§ Guarantees](../../../whitepaper.md#guarantees), G2). `Or` and repetition pin nothing, so a streamed layer may release items as soon as the cursor passes them. Until now a reported failure is never behind the cursor. A failure reported at the start of a value the parser has already consumed would be the first.
- **The interpreter records no expectations yet.** [INT-03](../../complete/03-interpreter/INT-03-execute-the-core-combinators.md) lowered `Named`, `Context` and `Fail` to plans that only succeed or fail, and left recording to ERR-02. INT-04's scope names four things that only recording can show: the filter's report, the non-associative chain message ([ADR-0004](0004-expression-build-operator-table-and-semantics.md) point 4), a failed `Custom`'s expectations ([ADR-0012](0012-custom-scanner-metadata.md) point 4, already in ERR-02's scope) and `Named` on an expression ([ADR-0008](0008-control-node-semantics.md) point 4, which ERR-02 implements for every node).

Prior art, read for this record. Files were read at the branch heads named:

| Library | Combinator | Reports | Position | After a rejection |
| --- | --- | --- | --- | --- |
| Pidgin ([`Parser.Assert.cs`](https://github.com/benjamin-hodgson/Pidgin/blob/103dfc6d6a594310a6681def03bb7445f9a0cefc/Pidgin/Parser.Assert.cs), [`Parser.Where.cs`](https://github.com/benjamin-hodgson/Pidgin/blob/103dfc6d6a594310a6681def03bb7445f9a0cefc/Pidgin/Parser.Where.cs)) | `Assert(pred)`, `Assert(pred, string)`, `Assert(pred, Func<T, string>)`; `Where` is a synonym | Default "Assertion failed", and the expected item "result satisfying assertion" | End of `p` (`state.Location`); its test expects `Char('a').Assert('b'.Equals)` on "a" to fail at offset 1 | Consumed, so `OneOf` tries nothing else |
| Sprache ([`Parse.cs`](https://github.com/sprache/Sprache/blob/9d1721bb0dea638e35b9bbb2334fea6f99bf778e/src/Sprache/Parse.cs)) | `Where(pred)` | `"Unexpected {value}."`, no expectations | Start of `p` | Backtracks |
| Superpower ([`Combinators.cs`](https://github.com/datalust/superpower/blob/bac1b4b94b4f04729bf9ef2a7118e373b80a099c/src/Superpower/Combinators.cs)) | `Where(pred, string message = "unsatisfied condition")` | The message alone | Start of `p` | Backtracks |
| Parlot ([`When.cs`](https://github.com/sebastienros/parlot/blob/3b4f00038da427211650d83a2150d3cd457d8be6/src/Parlot/Fluent/When.cs)) | `When(pred)` | Nothing; `.ElseError(message)` adds text | Start of `p` (it calls `ResetPosition(start)`) | Backtracks ("and backtracks if not") |
| FParsec ([`Primitives.fs`](https://github.com/stephan-tolksdorf/fparsec/blob/156cbd751fac67ca7cc04cf661710f1f90a216a5/FParsec/Primitives.fs), [User's Guide § 5.7.2](https://www.quanttec.com/fparsec/users-guide/looking-ahead-and-backtracking.html)) | None built in. `>>=` then `fail`; the guide writes a `resultSatisfies` | `fail`: the message. `resultSatisfies`: the message | `fail`: end. `resultSatisfies`: start (`BacktrackTo`) | `fail`: consumed. `resultSatisfies`: backtracks |
| Megaparsec ([`Internal.hs`](https://github.com/mrkkrp/megaparsec/blob/1ccb703fbc227f5bdbd3d4575a576af07ddf3a7e/Text/Megaparsec/Internal.hs), [tutorial](https://markkarpov.com/tutorial/megaparsec.html), § "Controlling location of parse errors") | None built in. `guard` or `fail` after a bind; the tutorial's `withPredicate4` | `guard`: nothing. `fail` and `withPredicate4`: the message | `guard` and `fail`: end. `withPredicate4`: start, using an offset saved before `p` | Consumed in all three (inferred from `pBind` and `pParseError`) |
| FastParse ([`MacroInlineImpls.scala`](https://github.com/com-lihaoyi/fastparse/blob/475d042bc267c53e3ac7d383dc01271cdb4862a6/fastparse/src-3/fastparse/internal/MacroInlineImpls.scala)) | `filter(f)` | Nothing; "filter" in verbose traces | End of `p` | No commit on consumption; only a cut commits |

The Megaparsec tutorial presents the end position as a defect to work around: it moves the error to an offset saved before `p`. It rejects resetting the offset (its `withPredicate2`) because the parser state then "will no longer reflect reality". Superpower's fixed default text drew a request for a custom message (issue [#117](https://github.com/datalust/superpower/issues/117)). Its `message` parameter followed (PR #131; the research saw only a search snippet).

The three other .NET libraries backtrack, and so does FParsec's documented pattern. Only Pidgin, and the bind-then-fail idiom in Megaparsec and FParsec, commit as § LINQ query syntax does. No library surveyed drops the failures recorded inside `p` when it reports a rejection. Pidgin's expected set keeps them.

Forces:

- **Commit by default** ([§ Combinator semantics](../../../whitepaper.md#combinator-semantics)). Committed means the cursor moved (ADR-0008).
- **G9, diagnostics.** The error should point at what is wrong. A rejected value is wrong as a whole, not at the item after it.
- **G2, bounded memory.** No new pinning bookmarks.
- **Failure paths allocate nothing** ([§ Failure positions](../../../whitepaper.md#failure-positions), ERR-02's criterion).
- **G7, two engines.** The rule must be one the generator can emit too.
- **Simplicity.** Prefer the standard approach, and keep later additions non-breaking.

## Options for the position (fork 1)

### 1A. At the end of `p`, committed (the whitepaper as written)

A rejection behaves exactly like `p.Bind(x => pred(x) ? Return(x) : Fail(...))`. It is recorded at the cursor and merges with whatever `p` recorded there.

- **Pros:**
  - No new rule. ERR-02 records it as it records `Fail`.
  - Never behind the cursor, so nothing new for retention.
  - Matches Pidgin, and the `Bind` + `Fail` pattern docs/concepts/custom-scanners.md already teaches.
- **Cons:**
  - The caret points past the value. `Number.Where(n => n <= 255)` on `300;` reports at `;`, merged with `Number`'s own "expected digit", which misleads.
  - `Named` cannot label it, even under `Atomic`: `Named` replaces only expectations at its start position, and this failure is further on.

### 1B. At the start of `p`, committed, dropping `p`'s own failures (chosen)

The rejection is recorded at the position where `p` started, and its span covers what `p` consumed. The cursor stays where `p` left it, so the failure is committed as before. When the predicate rejects, the furthest failure goes back to what it was before `p` ran, so `p`'s trailing failures cannot bury the rejection.

- **Pros:**
  - The caret marks the rejected value: `300` in the example above.
  - Under `Atomic`, `Named` labels a rejection like any other failure at its start, so keyword exclusion reads "expected identifier".
  - Keeps commit by default. This is the Megaparsec tutorial's recommended placement.
- **Cons:**
  - Two new rules for ERR-02: restoring the furthest failure when the predicate rejects, and a failure reported behind the cursor.
  - A streamed layer may have released the rejected items by the time the error is built. The rejection must capture what it needs by value, with no pinning bookmark (point 6).
  - Recovery (ERR-05) can no longer assume a committed failure is reported at the cursor.
  - `Where` and `Bind` + `Fail` now report at different places, and the docs must say when to use which.

### 1C. At the start of `p`, backtracking

A rejection rewinds to `p`'s start and fails without consuming, as Sprache, Superpower, Parlot and FParsec's `resultSatisfies` do.

- **Pros:**
  - The most common behaviour among .NET libraries.
  - Never behind the cursor.
- **Cons:**
  - Contradicts § LINQ query syntax and commit by default. The filter becomes a hidden `Atomic` that must pin `p`'s start, which goes against G2 and ADR-0008.
  - `Byte.Many()` stops quietly before `300`, and an enclosing `Or` tries its next alternative, so the real error is lost.
  - An author who wants this behaviour can already write `Atomic(p.Where(...))`.

## Options for the content (fork 2)

### 2A. Nothing beyond the rejected input

There is no new API. The error shows the rejected input as unexpected. Authors label it with `Atomic(...).Named(...)` or report a message with `Bind` + `Fail`.

- **Pros:**
  - Simplest, and either option below can be added later without breaking anything.
- **Cons:**
  - A committed rejection cannot carry any text.

### 2B. An optional label

`Where(predicate, expected)` records `Label(expected)`, so the error reads "expected …, unexpected '300'".

- **Pros:**
  - The same shape as every other error.
  - The only way to label a committed rejection.
- **Cons:**
  - No surveyed library does this.
  - Authors tend to write messages, not expectations ("number out of range" would read "expected number out of range").

### 2C. An optional message (chosen)

`Where(predicate, message)` records what `Fail(message)` records: a `CustomMessage`, at the rejection's position.

- **Pros:**
  - What Pidgin and Superpower offer, and what Superpower's users asked for.
  - No new error-model concept: a `CustomMessage` exists for `Fail`.
- **Cons:**
  - One more parameter on `Where`.
  - The message replaces the "unexpected …" text, as it does for `Fail`.

### 2D. A fixed default text

Every rejection says the same thing, like Pidgin's "Assertion failed" or Superpower's "unsatisfied condition".

- **Pros:**
  - No API.
- **Cons:**
  - Says nothing useful.
  - Superpower's users complained about exactly this (#117).

## Options for the split with ERR-02 (fork 3)

### 3A. INT-04 runs the nodes, ERR-02 records what they report (chosen)

This follows INT-03's precedent for `Named`, `Context` and `Fail`. INT-04 executes the nodes and tests outcomes and cursor positions. ERR-02 records what this record, ADR-0004 point 4 and ADR-0012 point 4 say.

- **Pros:**
  - INT-04 is not blocked, and the same mechanism records every node's report.
- **Cons:**
  - INT-04's "non-associative chain error" test checks only the committed outcome until ERR-02 exists.

### 3B. INT-04 waits for ERR-02

ERR-02 becomes a blocker, and INT-04 records its nodes' reports itself.

- **Pros:**
  - INT-04's tests can check messages.
- **Cons:**
  - INT-04 waits on ERR-02's own open decisions: `Not`, `Lookahead`, dispatch and `Describe`.

## Decision

Options **1B**, **2C** and **3A**, chosen by the maintainer in conversation and accepted on 2026-10-22.

1. **Outcome** (unchanged from § LINQ query syntax). `p.Where(predicate)` runs `p`.
   - If `p` fails, the filter fails the same way.
   - If the predicate accepts, the filter succeeds with `p`'s result.
   - If it rejects, the filter fails and the cursor stays where `p` left it. The failure is committed if `p` consumed input, and empty otherwise.
   - A query's `where` clause is `Where(predicate)`.
2. **Position.** A rejection is reported at the position where `p` started, with a span covering the input `p` consumed. When `p` consumed nothing, it is reported like any other failure at that position.
3. **`p`'s own failures are dropped.**
   - When the predicate rejects, the layer's furthest failure is restored to what it was when `p` started, and then the rejection is recorded.
   - Failures recorded before `p` started are kept. A failure further on, such as one an earlier `Atomic` rewound past, still wins.
   - Expectations at the start position merge as usual: in `Or(Kind(Keyword), Identifier.Where(isNotKeyword))`, the rejection is reported together with the first alternative's expectation.
   - When the predicate accepts, nothing is dropped.
4. **Message.** `Where` gains an optional parameter, `Where(Func<TOut, bool> predicate, string? message = null)`, before its `[CallerArgumentExpression]` parameter.
   - It is not a second overload. C# prefers a candidate that needs no default arguments, so with an overload `p.Where(pred, "msg")` would bind `"msg"` to the existing overload's caller-expression parameter and lose the message. A compile check on the .NET 11 RC 2 SDK during INT-04's build confirmed this.
   - With a message, a rejection records what `Fail(message)` records, at the position in point 2.
   - Without one (null, the default), it records no expectation, and its unexpected item is the first item of the rejected input, as for any failure at that position.
   - `Where` throws `GrammarException` for a null predicate (naming the caller's expression, as now) and for a message that is empty or white space.
5. **Labels.** For ADR-0008 point 4, a rejection counts as a failure recorded at `p`'s start position even when it has no expectations. So `Atomic(Identifier.Where(isNotKeyword)).Named("identifier")` reports "expected identifier" at the keyword. ERR-02 decides how `Named` treats a custom message, and that rule applies to `Fail` and to a filter's message alike.
6. **Captured by value.** A streamed layer may release `p`'s items before the error is built, so the engine must be able to build the rejection's error without them. It must do so without allocating on the failure path and without a pinning bookmark. The expected way is for the filter to copy its start item and that item's source position before `p` runs. ERR-02 designs the record. The non-associative chain error of ADR-0004 point 4 is reported at the span of an operator already consumed, so it is behind the cursor too and follows the same rule.
7. **Split.**
   - **INT-04** executes `Bind`, the filter (with its optional message), `Expression.Build` and `Custom`. Its tests check outcomes and cursor positions. The arithmetic criterion's "non-associative chain error" is tested as a committed failure at the second operator.
   - **ERR-02** records the filter's report (points 2 to 6), the chain message (ADR-0004 point 4), `Custom`'s expectations (ADR-0012 point 4) and `Named` on any node, expressions included (ADR-0008 point 4).
   - **GEN-06** emits the same behaviour for the filter, and the GEN-08 differential tests compare reports (G7).

Deciding reason: what is wrong is the value `p` produced, so the error points at that value. The failure stays committed, which keeps commit by default. Dropping `p`'s own failures is what lets that error win under furthest failure. The message follows Pidgin and Superpower, and what Superpower's users asked for.

What would change this:

- ERR-02 finding that copying the start item on every filter run costs measurable time on success paths. Then the capture could move to the rejection plus a short-lived pin, or the position could fall back to 1A.
- Authors asking for a backtracking filter often enough. That would be a separate combinator, not a change to `Where`.
- Demand for messages built from the value. That would be an additive overload, `Where(predicate, Func<TOut, string> message)`, as Pidgin has.

## Consequences

- **Easier:**
  - Errors point at the rejected value, whether it is a range check (`300` is not a byte) or an identifier that turns out to be a keyword.
  - Keyword exclusion composes with `Atomic` and `Named`.
  - A message needs no `Bind` + `Fail`.
- **Harder:**
  - ERR-02 must save and restore the furthest failure around a filter, and keep a record that stays valid behind the cursor.
  - ERR-05 must resume recovery from the cursor, not from the reported position, since a committed failure may now be reported before the cursor.
  - The generator emits the same save, restore and capture (GEN-06).
- **Ruled out:**
  - A backtracking `Where`. `Atomic(p.Where(...))` covers it.
  - Fixed default text.
  - A failure reported past the rejected value.
- **Risks and how they are watched:**
  - Two report positions for similar code. `Where` reports at the start of the value, and `Bind` + `Fail` at the cursor, which is right for an unterminated raw string (docs/concepts/custom-scanners.md). docs/concepts/sequencing.md must say which to use.
  - The cost of the capture on every filter run. It is watched by ERR-02's allocation criterion and the Phase 0 benchmarks ([BENCH-01](../../active/13-benchmarks/BENCH-01-phase-0-speed-gate.md)).
  - A rejection reported behind the cursor in a streamed layer. ERR-02 needs a test where the rejected items have left the buffer before the error is built (with LAY-01).

## Changes made

- **Whitepaper:**
  - [§ LINQ query syntax](../../../whitepaper.md#linq-query-syntax): the `where` bullet says where a rejection is reported and names the optional message. It links this record.
  - [§ The error model](../../../whitepaper.md#the-error-model): a new **Filters** bullet gives points 2 to 5.
  - [§ Failure positions](../../../whitepaper.md#failure-positions): a failure reported behind the cursor captures what its error needs by value (point 6).
- **Cards:**
  - [INT-04](../../complete/03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md): moves to Building.
    - The filter line cites this record and adds the optional message.
    - The `Custom`, chain-message and `Named` lines say that ERR-02 records them (point 7).
    - The docs line adds the sequencing page.
    - The arithmetic criterion checks the chain error as a committed outcome.
    - § The error model joins its whitepaper references.
  - [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md): scope gains the filter's report and the chain message. A new criterion requires both to be reported as decided, building their errors without reading buffer items behind the cursor.
  - [ERR-05](../../active/04-errors-and-recovery/ERR-05-recover-combinator-and-strategies.md): recovery resumes from the cursor, not from the reported position.
  - [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md): scope names the filter node.
- **Docs:** docs/concepts/sequencing.md changes in INT-04's Build.
