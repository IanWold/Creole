# ADR-0035: `Adjacent` commits like a sequence, `Position` is a zero-width span, and span combinators hold their spans without the failure log

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-11-24 |
| Cards | [SPAN-04](../../active/05-source-spans/SPAN-04-span-combinators.md), [INC-05](../../active/15-incremental-reparsing/INC-05-session-span-table.md), [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md), [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md) |
| Whitepaper | [§ Using spans in grammars](../../../whitepaper.md#using-spans-in-grammars), [§ 1. Split and merge](../../../whitepaper.md#1-split-and-merge), [§ Functions in grammars](../../../whitepaper.md#functions-in-grammars), [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention), [§ Failure positions](../../../whitepaper.md#failure-positions) |
| Research | — |
| Supersedes | — |

## Context

§ Using spans in grammars names four combinators:

- `p.Spanned()` returns `Parser<TIn, Spanned<TOut>>`, and `p.WithSpan((value, span) => node)` builds AST nodes that keep their location.
- `Adjacent(p, q)` succeeds only if `q` starts exactly where `p` ended.
- `Position` is a zero-width parser returning the current offset.

§ 1. Split and merge uses `Adjacent`: "The lexer emits single `>` tokens, and the parser combines them with `Adjacent(Kind(Gt), Kind(Gt))`, which succeeds only when the two spans touch with no trivia between." SPAN-04's criterion is that this example works.

Five things are not settled.

1. **How `Adjacent` fails.** In an expression grammar, shift binds tighter than `>`, so the shift level tries `Adjacent(Kind(Gt), Kind(Gt))` first. On `a > b` it consumes the `>`, and the second `Kind(Gt)` fails at `b`. Under commit semantics (§ Combinator semantics) that failure is committed, and the parse never reaches the relational level. On `a > > b`, both `>` are consumed before the spans are found not to touch. The whitepaper says neither whether `Adjacent` rewinds nor where its failure is reported.
2. **What `Position` returns.** "The current offset" could be the layer's item index or a source offset. At a token layer, a source offset could be the end of the last token consumed or the start of the next, and the two differ by the whitespace between them. Over a byte root (SPAN-06), a `long` offset is a byte offset with no character offset beside it, so `LineMap` cannot turn it into a line and column.
3. **`Spanned<T>`'s shape, and the span of a parser that consumed nothing.**
4. **Keeping spans behind the cursor.** `Spanned` needs the span of `p`'s first item after `p` has run. Over a streamed layer, the buffer may have released it by then.
   - [ADR-0031](0031-constructs-that-read-their-input-back-pin-their-start.md) point 1 says a construct that "needs only certain positions … holds them under ADR-0028, as … `Spanned` will (SPAN-04)", and § Bookmarks and retention repeats it.
   - [ADR-0028](0028-an-errors-items-are-copied-when-the-buffer-releases-them.md) keeps its register of held positions in the layer's failure log, and point 2 says "a layer without a failure log registers nothing". That is right for errors, which need no copies when nothing is recorded. `Spanned` produces a value on the success path, so it needs its span whether or not a log exists.
   - Runs without a log are common. LAY-05's scope has a pipeline over in-memory text run first without failure logs ([ADR-0027](0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md)). Its token buffer is still a `StreamBuffer` that releases segments.
5. **Spans over in-memory input.** A `Cursor<T>` has no way to ask for a span today. Over in-memory input, `Parse`'s errors already use item offsets in `SourceId.Unnamed` (INT-08, INT-11). Over a stream buffer, `StreamBuffer<T>.SpanOf(start, end)` gives source spans.

## Options

### Fork 1. How `Adjacent` fails

#### 1A. Like a sequence with a filter (recommended)

`Adjacent(p, q)` runs `p`, then `q`, then checks the spans. If `p` or `q` fails, `Adjacent` fails the same way. If the spans do not touch, it fails as a rejected `Where` over the pair ([ADR-0014](0014-a-rejected-filter-reports-at-the-start-of-the-value.md)). The cursor stays after `q`, so the failure is committed if anything was consumed. The `>>` example is written `Atomic(Adjacent(Kind(Gt), Kind(Gt)))`.

- Pros:
  - It is how `Seq` and `Where` already behave, so it adds no rule.
  - It takes no pin, so it adds nothing to retention (§ Guarantees point 2).
  - A failure deep inside a large `q` stays committed where it happened, so its error stays precise.
  - Authors already wrap multi-token operators in `Atomic`. [ADR-0004](0004-expression-build-operator-table-and-semantics.md) gives that advice for an operator that should not commit, so the pattern is familiar.
- Cons:
  - The whitepaper's example needs `Atomic`. An author who leaves it out gets a committed failure on `a > b`. ANA-04's choice checks or a CRL hint could catch the common case later.

#### 1B. Atomic, like a literal

Any failure rewinds to the start, so `Adjacent` stands in for the `>>` token the lexer did not produce, as `String(">>")` would.

- Pros:
  - The whitepaper's example works as written.
- Cons:
  - It must pin its start while it runs (ADR-0031 point 1), which is a hidden cost.
  - Used around a large `q`, such as `Adjacent(Name, ArgumentList)`, it turns a deep failure into an empty one at `Name`, unless a `Cut` is in force. That gives an `Atomic`'s coarser errors.
  - It makes `Adjacent` the only combinator of two parsers that is atomic. Literal primitives are atomic because each is a single match.
  - The whitepaper's list of constructs that pin would grow.

#### 1C. Rewind only when the spans do not touch

If `p` and `q` succeed but do not touch, `Adjacent` rewinds and fails empty. Other failures stay committed.

- Cons:
  - It does not fix the example. On `a > b`, `q` fails after `p` consumed, and that is still committed.
  - It still needs a pin, so it has 1B's cost without its benefit.

### Fork 2. When `Adjacent` checks

#### 2A. After `q` (recommended)

The spans are compared once `q` has succeeded.

- Pros:
  - If `q` starts with `SetMode`, the item at its start is lexed again under the new mode (§ 3. Mode channels). Checking afterwards compares against the item `q` actually consumed.
  - It is exactly a filter over the pair, so it reuses ADR-0014's reporting.
- Cons:
  - `q` runs even when the spans already do not touch.
  - `q`'s first item may be released while `q` runs, so its position must be held (Fork 4).

#### 2B. Before `q`

After `p`, the check peeks at the next item and compares spans, then runs `q` only if they touch.

- Pros:
  - It is cheaper on a mismatch, and needs no hold.
- Cons:
  - It reads an unconsumed item under the old mode. When `q` starts with `SetMode`, the check compares against an item that `q` never reads. [ADR-0028](0028-an-errors-items-are-copied-when-the-buffer-releases-them.md) rejected copies taken on entry for the same reason.

### Fork 3. What `Position` returns

#### 3A. A zero-width `SourceSpan` at the start of the next item (recommended)

`Parser<TIn, SourceSpan>`, with `Start == End`.

- Pros:
  - It carries the source id and both units, so over a byte root `CharStart` still feeds `LineMap`.
  - It is the type `Spanned` gives.
  - The spans docs already say "a zero-width span marks a position" ([docs/concepts/spans.md](../../../docs/concepts/spans.md)).
  - The start of the next item matches span rule 3, which places a synthetic item there, and `StreamBuffer<T>.SpanOf` for an empty range. A `Position` before a node gives the node's start, after any whitespace.
- Cons:
  - It changes the whitepaper's "returning the current offset".
  - Reading the next item's span means peeking, which pulls one item. Any peek does the same.

#### 3B. A `long` offset in root units

What the whitepaper literally says.

- Pros:
  - Simplest to use over text.
- Cons:
  - Over a byte root, it is a byte offset with no character offset.
  - It drops the source id, which embedded sources (SPAN-07) need.

#### 3C. The layer's item index

The cursor's `Position`.

- Pros:
  - Free.
- Cons:
  - It is not a location in the source at any layer above the root, so it is no use for diagnostics or AST positions.

For 3A and 3B, the alternative to "the start of the next item" is "the end of the last item consumed". That needs no peek and never reads an unconsumed item, but a `Position` before a node would then point before the whitespace that precedes it.

### Fork 4. How span combinators keep spans behind the cursor

#### 4A. Pin the start

Use a pinning bookmark from `p`'s start, as `Slice` does.

- Cons:
  - `WithSpan` on a large node, such as a whole compilation unit, would hold the whole file in the last layer. This breaks guarantee 2, which ADR-0028 point 2 and ADR-0031 point 1 already keep position-only constructs from doing.

#### 4B. Copy the span on entry

Peek at the item at `p`'s start and copy its span into a local before `p` runs.

- Pros:
  - Cheap, and needs no register.
- Cons:
  - If `p` starts with `SetMode`, the copy is of the item lexed under the old mode. ADR-0028 rejected entry copies for this reason (its review finding 16).

#### 4C. Hold through the failure log

Register the position with the failure log, as filters do, and require a log in any layer that uses a span combinator.

- Cons:
  - Every run of such a grammar would pay for failure recording. That undoes [ADR-0027](0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md)'s first run without a log, which BENCH-01's speed gate relied on.
  - It ties a success-path value to the error machinery.

#### 4D. Hold in a register of its own, copied at release (recommended)

The layer reading a buffer keeps a register of span holds beside its failure log, present whether or not the log is. Before the buffer releases a segment, it copies the span at each held position in that segment, as ADR-0028 point 3 does for errors.

- Pros:
  - It is ADR-0028's mechanism, which ADR-0031 point 1 and § Bookmarks and retention already name for `Spanned`.
  - It takes no pin.
  - It copies only consumed items, which a mode change never discards, so a copy is never stale (ADR-0028 point 5).
  - Over in-memory input nothing is held, because nothing is released.
- Cons:
  - It is a second register, with a push and a pop per span combinator in a streamed layer, and one more check per released segment.

## Decision

Options **1A, 2A, 3A and 4D**. The maintainer chose 1A and 3A in conversation on 2026-11-24. 2A and 4D follow from ADR-0028 and ADR-0031. Accepted by the maintainer on 2026-11-24, as written.

1. **`Spanned`.** `p.Spanned()` returns `Parser<TIn, Spanned<TOut>>`. `Spanned<T>` is a `readonly record struct Spanned<T>(T Value, SourceSpan Span)`.
   - The span runs from the start of the first item `p` consumed to the end of the last, as span rule 2 gives a derived item's.
   - If `p` consumed nothing, the span is zero-width where `Position` would be (point 4).
   - If `p` fails, `Spanned` fails the same way.
2. **`WithSpan`.** `p.WithSpan((value, span) => node)` gives its function `p`'s value and the span point 1 defines, and produces the function's result.
   - The function is a value function (§ Functions in grammars).
   - `WithSpan` is its own node, not `Spanned().Select(…)`, so that INC-05 can find the nodes whose results it records.
3. **`Adjacent`.** `Adjacent(p, q)` runs `p`, then `q`, and produces `(p's value, q's value)`, as `Seq(p, q)` does.
   - If `p` or `q` fails, it fails the same way.
   - Once both have succeeded, it checks that the end of `p`'s span equals the start of `q`'s, in the same source. `p`'s span and `q`'s are those point 1 defines, so an empty `p` or `q` touches whatever follows it.
   - If they do not touch, it fails as a rejected filter over the pair (ADR-0014 points 1 to 3). The cursor stays after `q`, so the failure is committed if anything was consumed. It is reported at `p`'s start, over the input both consumed. The failures recorded while `p` and `q` ran are dropped. It has a fixed message that names both parsers; the build chooses the wording.
   - A grammar that must fall back to a shorter form writes `Atomic(Adjacent(p, q))`. The `>>` example becomes `Atomic(Adjacent(Kind(Gt), Kind(Gt)))`.
   - It takes no message parameter for now. One can be added later, as `Where`'s was.
4. **`Position`.** `Position` is a `Parser<TIn, SourceSpan>` that never fails and consumes nothing. Its span is zero-width:
   - at the start of the next item's span;
   - at the end of the last item's span, at the end of the input;
   - at offset 0 of the source, in an empty input.

   It reads the next item under the mode in force, as `Lookahead` does. A later `SetMode` at the same index does not change a value already produced.
5. **Spans over in-memory input** are item offsets in `SourceId.Unnamed`, as `Parse`'s errors already use: character offsets over text, item indexes over a token array.
   - Over a token array, consecutive tokens always touch, so `Adjacent`'s check always passes there. Split and merge needs a lexer layer, which gives tokens real spans.
   - The cursor gets spans from its source, so a stream buffer gives source spans.
6. **Holding spans.** Span combinators hold positions; they do not pin.
   - `Spanned` and `WithSpan` hold `p`'s start while `p` runs.
   - `Adjacent` reads the end of `p`'s span as soon as `p` succeeds. That item is the one just below the cursor, which the buffer keeps (ADR-0028 point 3). It then holds `q`'s start while `q` runs.
   - The layer reading a buffer keeps these holds in a register of their own, whether or not it has a failure log. Before the buffer releases a segment, it copies the span at each held position in it.
   - Over input held whole in memory, nothing is held.
   - The register is scoped parse state, like ADR-0028's held starts in [ADR-0030](0030-rewinds-recovery-restarts-and-reuse-keep-all-parse-state.md)'s table, so a restart resets it.
   - This refines ADR-0028 point 2. "A layer without a failure log registers nothing" holds for the positions errors need, not for span holds.
7. **Analysis and engines.**
   - `Spanned` and `WithSpan` have `p`'s properties.
   - `Adjacent` has `Seq(p, q)`'s, and like a filter it can fail after both succeed.
   - `Position` is nullable, never fails and consumes nothing.
   - For the `Longest` automaton, `Adjacent` is not regular, because its outcome depends on spans the automaton does not see. `Spanned` and `WithSpan` are regular when `p` is, and `Position` always is. Their values come from running the winner again, like any value that does not depend on the matched characters alone.
   - Generated code behaves the same and holds the same positions (GEN-06, checked by GEN-08's differential tests).

Deciding reasons:

- **1A** keeps `Adjacent` inside the existing commit rules, without a hidden pin or coarser errors. The cost is one `Atomic` in the example.
- **2A and 4D** are the only choices correct under mode changes without a pin, and they are the mechanism ADR-0031 already named for `Spanned`.
- **3A** keeps a location usable for line and column over every root, and matches the type `Spanned` gives.

What would change this:

- Authors forgetting `Atomic` around `Adjacent` often enough to matter. The remedy would be an analyzer hint, or a new record making `Adjacent` atomic, not a silent change.
- A measurement in LAY-05 or BENCH-02 showing the span register costs measurable time in a streamed last layer.

## Consequences

**Easier:**

- AST nodes get locations without position fields in tokens.
- `Adjacent` needs no new failure rule; it is a sequence plus a filter.
- `Position` and `Spanned` give the same type, which `LineMap` and `Diagnostics` already take.

**Harder:**

- The split-and-merge example needs `Atomic`, and the whitepaper must say so.
- A streamed layer keeps a second register, and `Release` copies from it.
- A user who lexes outside Creole and calls `Parse` over a token array cannot use `Adjacent` meaningfully, because those tokens have no source spans. A source that supplies tokens with spans would be LAY-05's or a later card's to add.

**Ruled out:**

- `Adjacent` rewinding by itself.
- Span combinators pinning, or copying on entry.
- `Position` as a bare `long` or an item index.

**Risks and how they are watched:**

- **A span read after release.** SPAN-04 tests `Spanned` and `Adjacent` over a buffer with a segment size of 1, so a long `p` or `q` crosses segment boundaries and is released while it runs. It does so with and without a failure log, and against the same parse over in-memory input.
- **A forgotten `Atomic`.** SPAN-04's criterion test shows both `a >> b` and `a > b`, so the docs example cannot drift into the committed form.

## Changes made

- Whitepaper:
  - [§ Using spans in grammars](../../../whitepaper.md#using-spans-in-grammars): `Spanned<T>`'s shape and the span of an empty `p`; `WithSpan` as its own node; `Adjacent`'s result, check after `q`, commit and report; `Position` as a zero-width `SourceSpan`. A new paragraph covers in-memory spans and span holds, and links this record.
  - [§ 1. Split and merge](../../../whitepaper.md#1-split-and-merge): the example is `Atomic(Adjacent(Kind(Gt), Kind(Gt)))`, and says why.
  - [§ Functions in grammars](../../../whitepaper.md#functions-in-grammars): `WithSpan`'s function joins the value functions.
  - [§ Bookmarks and retention](../../../whitepaper.md#bookmarks-and-retention): span combinators hold positions whether or not the layer records failures.
  - [§ Failure positions](../../../whitepaper.md#failure-positions): copy at release serves span combinators, whose holds are apart from the failure log. Links this record.
  - [§ Properties](../../../whitepaper.md#properties): `Spanned`, `WithSpan` and `Position` join the regular nodes (point 7).
- Cards:
  - [SPAN-04](../../active/05-source-spans/SPAN-04-span-combinators.md): scope follows points 1 to 6. The criterion names `Atomic(Adjacent(…))` and both `a >> b` and `a > b`. A second criterion covers spans read after release, with and without a failure log. Moved to Building.
  - [INC-05](../../active/15-incremental-reparsing/INC-05-session-span-table.md): `WithSpan` is its own node, and its function may run more than once.
  - [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md): the span combinators' properties (point 7).
  - [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md): span holds and `Adjacent`'s report, as the interpreter does them.
- Decision index: a note that this record refines ADR-0028 point 2.
