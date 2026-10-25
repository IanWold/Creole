# ADR-0015: A prefix operator's operand takes only tighter levels, and an operator looks ahead to avoid committing on a missing operand

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-22 |
| Cards | [INT-04](../../complete/03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md), [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md) |
| Whitepaper | [§ Expressions](../../../whitepaper.md#expressions) |
| Research | — (found while building INT-04) |
| Supersedes | — (refines [ADR-0004](0004-expression-build-operator-table-and-semantics.md) points 3 and 5, which otherwise stand) |

## Context

INT-04 built `Expression.Build` by precedence climbing, as [ADR-0004](0004-expression-build-operator-table-and-semantics.md) describes. Two sentences in that record cannot be implemented as written. ADR-0004 is on `main` and is not edited, so this record corrects them.

**The prefix operand.** ADR-0004 point 3 has two rules that conflict when a prefix operator shares a level with other operators:

- **Operand position:** "parse an operand at that operator's level (so a looser operator cannot be its operand)". The same point gives an infix operator's right operand "minimum level *k* + 1" for strictly tighter levels, so "at that operator's level" admits operators from the prefix's own level.
- **Same level, prefix and postfix:** "prefix applies first, so `-x!` is `(-x)!` when both are at one level, as in Pidgin". Under the first rule, a postfix `!` at the prefix's level would be part of the operand, giving `-(x!)`.

[§ Expressions](../../../whitepaper.md#expressions) repeats both: "a prefix operator's operand is parsed at its own level" and "At one level, prefix applies before postfix." INT-04's plan, agreed with the maintainer, built the reading that keeps the example: the operand takes strictly tighter levels. The readings differ only when a prefix operator shares a level with infix or postfix operators. ADR-0004's own example table puts prefix `-` alone on its level, where every reading agrees.

**The missing operand.** ADR-0004 point 5 says: "An author who wants `a -` to stop before the `-` wraps that operator in `Atomic`." Under commit semantics this does not work. `Atomic(Char('-'))` succeeds, having consumed the `-`. The operand that then fails comes after the `Atomic`, so the failure is committed. The whitepaper does not repeat the claim.

Forces:

- **One rule per level**, easy to state in docs and to emit in the generator (G7).
- **The whitepaper's stated examples** (`2 ^ -x`, `-x ^ 2`, prefix before postfix at one level) should all hold.
- **Simplicity:** prefer the reading already built and tested, unless another is clearly better.

## Options for the prefix operand (fork 1)

### 1A. Only tighter levels (chosen; as built)

A prefix operator at level *k* parses its operand with levels 0 to *k* − 1. Neither an operator from level *k* nor a looser one can be part of the operand without parentheses.

- **Pros:**
  - One rule, which keeps every example in the whitepaper and ADR-0004: `-x!` is `(-x)!` at one level, `-x ^ 2` is `-(x ^ 2)`, `2 ^ -x` is `2 ^ (-x)`.
  - At one level, a prefix operator then applies before both postfix and infix operators: `-a * b` is `(-a) * b`. This matches Pidgin, which applies a row's prefix operators to each term before the row's infix operators. That is inferred from ADR-0004's reading of Pidgin's row-by-row fold, not re-read for this record.
  - Built, tested and mutation-checked in INT-04. No code change.
- **Cons:**
  - The whitepaper sentence "parsed at its own level" changes.
  - An author who wants `-(a * b)` puts the prefix on a looser level than `*`.

### 1B. Own level, except its postfix operators

The literal combination of ADR-0004's two rules. Infix operators from the prefix's own level may extend the operand (`-a * b` is `-(a * b)`), but postfix operators from that level may not (`-x!` is `(-x)!`).

- **Pros:**
  - Keeps both sentences of ADR-0004 and the whitepaper word for word.
- **Cons:**
  - Two rules for one level. They are hard to state and to remember, and both engines must special-case them.
  - Needs a code change and new tests.

### 1C. Own level, all its operators

The operand includes every operator of the prefix's own level: `-x!` is `-(x!)` and `-a * b` is `-(a * b)`.

- **Pros:**
  - The usual "prefix binding power" reading of precedence climbing.
- **Cons:**
  - Drops "at one level, prefix applies before postfix" from the whitepaper and ADR-0004, and differs from Pidgin at one level.
  - Needs a code change.

## Options for a missing operand (fork 2)

### 2A. The operator looks ahead for an operand (chosen)

An operator that should not commit when no operand follows checks for one without consuming it: `Atomic(Char('-').Before(Lookahead(Atom)))`. If no operand follows, the operator fails without consuming, so the expression ends before it.

- **Pros:**
  - Uses existing combinators, with no new API.
  - Pinned by INT-04's `AnOperatorThatLooksAheadForAnOperandStopsBeforeAMissingOne`: `a-` gives `a`, and `a-b` gives `(a-b)`.
- **Cons:**
  - The lookahead must recognise the start of any operand, including a prefix operator, so the author repeats the operand's start.
  - One extra lookahead for each operator that matches.

### 2B. A table option that makes an infix attempt atomic

For example, a flag on `op.InfixL` that rewinds the operator when its operand fails.

- **Pros:**
  - Says what the author means, in one place.
- **Cons:**
  - New public API and a second commit rule inside expressions.
  - No current grammar needs it. It can be added later without breaking anything.

## Decision

Options **1A** and **2A**, chosen by the maintainer in conversation and accepted on 2026-10-22.

1. **Prefix operand.** A prefix operator at level *k* (0 is the tightest) parses its operand with levels 0 to *k* − 1 only. An operator from level *k* or a looser level is not part of the operand unless the operand is parenthesized. At one level, a prefix operator therefore applies before the level's postfix and infix operators:
   - `-x!` is `(-x)!`;
   - `-a * b` is `(-a) * b`.

   Where an operand is expected, every prefix operator is still tried, whatever its level, so `2 ^ -x` is `2 ^ (-x)`. This replaces ADR-0004 point 3's "parse an operand at that operator's level (so a looser operator cannot be its operand)".
2. **Missing operand.** An operator that consumed input commits, as ADR-0004 point 5 says. To stop `a -` before the `-`, the operator looks ahead for an operand without consuming it, as in `Atomic(Char('-').Before(Lookahead(Atom)))`. Wrapping the operator alone in `Atomic` does not help. This replaces the last sentence of ADR-0004's bullet on operators that consumed input.
3. **Everything else in ADR-0004 stands.** ADR-0004 stays Accepted. The decisions index carries a note pointing its readers here, as it does for the `Named` rule in its point 5.

Deciding reason: 1A is the only reading that states the operand rule once and keeps every example the whitepaper and ADR-0004 give. It is also what INT-04 built and tested. 2A corrects a claim that does not hold under commit semantics, using combinators that exist.

What would change this:

- Real grammars that want a prefix operator to take same-level infix operators as its operand. The author can put the prefix on a looser level; if that proves awkward, 1C could be revisited.
- Repeated use of the lookahead pattern in real grammars. Then 2B could be added as an additive option.

## Consequences

- **Easier:**
  - One sentence states the prefix rule in the whitepaper, docs and generator.
  - INT-04 closes with no code change.
- **Harder:**
  - Authors who expect `-a * b` to be `-(a * b)` at one level must put the prefix on its own looser level.
- **Ruled out:**
  - Mixed per-fixity rules within a level (1B).
- **Risks and how they are watched:**
  - The generator (GEN-06) must use the same operand limit. GEN-08's differential tests cover expressions.
  - INT-04 gains a test for a prefix sharing a level with an infix operator (`-a*b` is `((-a)*b)`). Nothing tests that case yet.

## Changes made

- **Whitepaper:** [§ Expressions](../../../whitepaper.md#expressions): the Operands bullet says a prefix operator's operand is parsed with only the tighter levels, so at one level a prefix applies before postfix and infix operators. It links this record.
- **Decisions index:** a note that this record refines ADR-0004 points 3 and 5.
- **Cards:**
  - [INT-04](../../complete/03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md): links this record. `ExpressionExecutionTests.APrefixAtTheSameLevelAsAnInfixOperatorAppliesFirst` pins `-a*b` as `((-a)*b)`.
  - [GEN-06](../../active/11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md): the scope's node line cites this record for `Expression.Build`.
- **Docs:** none. docs/concepts/expressions.md already states the rule, and does not give the `Atomic` advice.
