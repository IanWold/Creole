# ADR-0022: A successful `Longest` restores the furthest failure, so a compiled `Longest` records nothing when it matches

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-25 |
| Cards | [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md), [INT-06](../../active/03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md), [GEN-05](../../active/11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md) |
| Whitepaper | [§ Longest match](../../../whitepaper.md#longest-match), [§ The error model](../../../whitepaper.md#the-error-model), [§ Interpreted engine](../../../whitepaper.md#interpreted-engine) |
| Research | [DFA compilation under commit semantics](../research/2026-10-23-dfa-compilation-under-commit-semantics.md), finding 31 |
| Supersedes | — |

## Context

[ADR-0017](0017-regular-subtrees-compile-to-an-eager-commit-aware-automaton.md) point 6 says that a compiled subtree that succeeds must contribute the same expectations as the interpreter (G7). It leaves to ERR-02 how to do that, and how far each scan read. It names two candidates: re-running the subtree lazily, and labelling automaton states with expectations (research note, finding 31). A compiled subtree that fails already re-runs on the interpreter (ADR-0017 point 5), so its errors are exact.

Only `Longest` compiles today. [INT-06](../../active/03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md) measured the one other candidate, a character-level JSON token choice, at about 0.6× the speed of dispatch and scans. The compiled `Longest` tokenized 3.31× faster with a 12-rule JSON lexer and 13.58× faster with a 138-rule C#-like lexer.

**What the interpreter records today.** `Longest` runs every alternative atomically from the same position. Under [ADR-0021](0021-how-failures-are-recorded-and-merged.md), every failure those runs record stands, whichever alternative wins:

- **The winner's loop exits.** `Number` on `12x` records "digit" and "'.'" at position 2.
- **Losers that failed at the start.** These are superseded as soon as anything consumes past them.
- **Losers that read past the winner before failing.** `Longest(Char('-'), Seq(Char('-'), Char('-'), Char('>')))` on `--x` records "'>'" at position 2, beyond the winner's end at 1.

**Why the scan cannot supply that cheaply.** The scan stops where the last thread dies. That is only an upper bound on the interpreter's furthest record, for two reasons:

- **Strings.** [`RegularTerm`](../../../src/Creole/Analysis/RegularTerm.cs) models `String("<=")` as an `Atomic` sequence of character classes, so its thread dies at the first character that does not match. ADR-0021 point 6 records a failed `String` at its start. On `<x`, the scan stops at position 1, but the interpreter's furthest record from `Longest(String("<="), String("<"))` is at position 0.
- **Speculative threads.** The automaton runs lower-priority threads that the interpreter runs only if a higher-priority thread fails without consuming (finding 31). A speculative thread that dies under an `Atomic` that later succeeds is not a real failure.

Constraints:

- **Failure paths allocate nothing** (ERR-02's criterion).
- **G2:** no new pinning bookmarks.
- **G7:** the compiled and interpreted plans, and the generator's DFAs ([GEN-05](../../active/11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md)), must record the same thing.
- **INT-06's speed must survive.**
- **Simplicity:** the maintainer's standing preference.

## Options

### A. A successful `Longest` restores the furthest failure (chosen)

When some alternative succeeds, `Longest` restores the furthest failure to what it was when it started, as a successful `Lookahead` does (ADR-0021 point 2). When every alternative fails, their failures stand, as now. A compiled `Longest` that matches then records nothing, and one that fails still re-runs on the interpreter.

**Pros:**

- Exact for free: neither plan records anything on success.
- No pin and no deferred state. There is no scan extent to expose and no change to the scan loop.
- The generator's DFAs and fused lexers ([§ Layer fusion](../../../whitepaper.md#layer-fusion)) inherit the rule unchanged.
- Consistent with ADR-0011 point 4, which makes each `Longest` alternative a boundary like `Lookahead` and `Not`. Every alternative is tried and rewound, and only the winner's input is kept.
- Drops losers' deep failures. These can point past the real error: a loser that reads several characters ahead outranks a failure at the next token's start.

**Cons:**

- A semantic change. It drops the hints a token leaves at its end. `Seq(Longest(Number, Identifier), Char(';'))` on `12x` reads "expected ';'" instead of "expected digit, '.' or ';'".
- In a lexer layer this matters only when the lexer itself fails, because a layer's furthest failure is reported only when that layer fails. In a single-layer grammar it removes those hints from the parser's own errors.
- `Longest(a, b)` where `b` never matches no longer reports exactly as `Atomic(a)` does.

### B. Lazy re-run, exact under today's semantics

A compiled match leaves a pending record holding the plan, the start, the scan's stop, and marks into the expected list. If the pending record could be part of the reported failure, the interpreter re-runs the `Longest` with recording when the error is built.

**Pros:**

- Keeps the interpreter's richer records, and costs O(1) for each match.

**Cons:**

- The record's position is unknown until the re-run, so failures recorded later at positions up to the stop must still splice in recording order (ADR-0021 point 1). That needs marks into the list and resolution in time order when several records are pending.
- `Named` must resolve a pending record created inside it before relabelling, and a restore must drop pending records created inside it.
- A streamed layer must pin the token's input while its record is pending.
- Both engines carry all of this.

### C. Expectations on automaton states

Each state carries the ordered expectations of its threads, as a Moore machine's output.

**Pros:**

- No re-run, and no pin.

**Cons:**

- Exactness needs registers in the scan for each thread:
  - the start of each string being read, since a failed string records at its start;
  - whether a death is real, which depends on whether an enclosing `Atomic` later succeeds;
  - whether `Named` has consumed anything since it was entered;
  - the start position of each `Context` frame, for ADR-0021 point 4.
- That is a tagged-DFA extension. It splits states, slows construction (which INT-09 is already trying to cut), and adds work per character to the scan loop.

### D. Re-run the interpreter after every compiled match

**Pros:**

- Exact, and simple.

**Cons:**

- Throws away INT-06's measured gain, since the interpreter runs every alternative anyway.

## Decision

Option **A**, chosen by the maintainer in conversation and accepted on 2026-10-25.

1. **Semantics.** `Longest(p1, …, pn)` runs every alternative atomically from its start position, as now.
   - If any alternative succeeds, the furthest failure is restored to what it was when `Longest` started. Then the winner's input is consumed.
   - If every alternative fails, the failures they recorded stand, in alternative order.
   - This extends ADR-0021 point 2's restoring combinators, `Lookahead` and `Not`, to `Longest`.
2. **Compiled `Longest`.**
   - A match records nothing.
   - When nothing matches, the interpreter re-runs, as ADR-0017 point 5 says, so the failure is exact.
   - This settles ADR-0017 point 6 for `Longest`. No scan extent is exposed, and the scan loop does not change.
3. **Labels.** A `Longest` that succeeds without consuming leaves nothing that an enclosing `Named` could relabel. This is ADR-0021 point 3's rule for failures removed by a restore.
4. **Other compiled subtrees.** None compile today. A proposal to compile another kind of Regular subtree must say how it records what the interpreter records, in its own decision record. The context above lists the facts it must handle: string starts, speculative threads, and the bookkeeping that option B needs.

Deciding reason: A makes the compiled path exact at no cost and with no new machinery. What it gives up, hints from inside a token that has already matched, is the part of today's record that is as likely to mislead as to help.

What would change this:

- Users of single-layer grammars missing token-continuation hints, such as "digit or '.'" after a number. Then option B for `Longest` would be designed in a new record.
- A non-`Longest` subtree that measures worth compiling. Its record faces the same choice.

## Consequences

**Easier:**

- A compiled `Longest` and its interpreter plan agree on errors without extra state.
- GEN-05 and fusion emit nothing for a match's errors.
- The scan keeps its current inner loop.

**Harder:**

- The interpreter's `LongestPlan` saves the furthest failure on entry and restores it after a success. That is a few fields, with no allocation.
- docs/concepts/choice.md must say that a successful `Longest` contributes nothing to error messages.

**Ruled out:**

- Hints from a matched `Longest`'s winner or losers.
- Exposing the scan extent for errors. It may still be wanted for retention or for Reps memoization (ANA-04, ANA-07), on their own terms.

**Risks and how they are watched:**

- **The DFA path recording something on a match.** ERR-02's tests compare the recorded failure after a compiled and an interpreted `Longest` over `RegularShapes` and the lexer samples, for matches and non-matches.
- **Poorer messages in single-layer character grammars.** Watched through user reports. The remedy is option B, in a new record.

## Changes made

- **Whitepaper:**
  - [§ Longest match](../../../whitepaper.md#longest-match): a new paragraph. A `Longest` that matches contributes nothing to error messages, and one that fails reports every alternative's failures. The DFA records nothing when it matches.
  - [§ The error model](../../../whitepaper.md#the-error-model): the **Lookahead and Not** bullet becomes **Lookahead, Not and Longest**.
  - [§ Interpreted engine](../../../whitepaper.md#interpreted-engine): the DFA bullet says a match records no failure and a non-match re-runs on the interpreter.
- **Cards:**
  - [ERR-02](../../active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md): the compiled-subtrees scope item cites this record, and the scan-extent work is dropped. A new criterion compares the failures a compiled and an interpreted `Longest` record.
  - [GEN-05](../../active/11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md): generated DFAs record nothing on a match and re-run on a non-match.
- **Decisions index:** a note that this record settles ADR-0017 point 6 for `Longest` and extends ADR-0021 point 2.
