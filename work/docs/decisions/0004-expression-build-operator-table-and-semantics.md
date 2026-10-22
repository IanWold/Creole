# ADR-0004: `Expression.Build` is one node over a homogeneous operator table, parsed by precedence climbing

| | |
| --- | --- |
| Status | Accepted |
| Date | 2026-10-18 |
| Cards | [CORE-11](../../active/02-core-combinators/CORE-11-expressionbuild.md), [INT-04](../../active/03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md), [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md) |
| Whitepaper | [§ Expressions](../../../whitepaper.md#expressions) |
| Research | — (prior art read for this record, cited inline) |
| Supersedes | — |

## Context

[§ Expressions](../../../whitepaper.md#expressions) says `Expression.Build(atom, table)` takes prefix, postfix, left-, right- and non-associative infix operators per level, "compiles to precedence climbing", and is one node for analysis. It cites Willis and Wu (Haskell Symposium 2021). It does not fix the table's API, how prefix operators interact with levels, what happens for `a < b < c`, or how errors behave. CORE-11 needs all of these before it can build.

Prior art, read for this record:

| Library | Table | Operator shape | Prefix / postfix | Non-associative | Structure |
| --- | --- | --- | --- | --- | --- |
| Pidgin (`Pidgin/Expression/ExpressionParser.cs`, `Operator.cs`) | Rows, tightest first; a row mixes fixities | `Parser<TToken, Func<T, T, T>>` | Once per row; `PrefixChainable` / `PostfixChainable` opt in to repeats | `InfixN` parses one operator and stops; `< c` is left unconsumed | A fold of ordinary combinators, one row at a time |
| Parlot (`Fluent/AssociativeWithOperator.cs`, `Unary.cs`) | None; the author nests one combinator per level | Operator parser of any type plus a factory `Func<T, T, TOperator, T>` | Prefix repeats | Not supported | One node per level |
| Willis and Wu 2021, § 2.3 (Pattern 1c) | `Prec` list of levels, one fixity per level | `Op :: Fixity a b sig → (a → b) → Parser sig → Op a b` | `prefix` and `postfix` repeat | `InfixN` "can appear at most once"; § 5 reports chaining with a separate `notFollowedBy` pattern | A fold over the table into chain combinators |

Willis and Wu let each level change the result type (`Level :: Prec a → Op a b → Prec b`). None of the three is precedence climbing: they all build the expression parser level by level.

That level-by-level fold has a known gap. If `^` binds tighter than prefix `-` (so `-x ^ 2` is `-(x ^ 2)`), the operand to the right of `^` is parsed at the `^` level, which cannot start with `-`. So `2 ^ -x` fails. This follows from Pidgin's code, where each row's operands are built from the tighter rows only. Precedence climbing accepts a prefix operator wherever an operand is expected and parses its operand at the prefix operator's own level, so `2 ^ -x` is `2 ^ (-x)`.

Forces:

- **One analyzable node** (§ Expressions, G7): analysis and EBNF export must see a table, not a chain of anonymous combinators.
- **Parsec commit semantics** ([§ Combinator semantics](../../../whitepaper.md#combinator-semantics)).
- **ADR-0001:** a node's hidden types are reached only through `internal abstract` operations it overrides.
- **Simplicity:** we prefer the simple standard approach.

## Options

### A. One node, precedence climbing (the whitepaper's position)

A single `ExpressionNode` holding the atom and a table of levels. The engine runs precedence climbing over the table.

- Pros: one node with an explicit table, which is what analysis, export and the generator want. Prefix operators work in any operand position (`2 ^ -x`). Non-associative chains can be detected and reported, because the engine knows which level the last operator came from.
- Cons: a new engine operation for INT-04 and the generator, instead of reusing existing nodes.

### B. Desugar to existing nodes (Pidgin, Willis and Wu)

`Expression.Build` folds the table into `Seq`, `Many` and `Or` nodes at construction.

- Pros: nothing new for engines to implement.
- Cons: the structure is lost, so analysis sees chains rather than a table, which contradicts § Expressions. `2 ^ -x` fails when `-` is looser than `^`. A non-associative chain can only be reported with an extra `Not` lookahead per level.

### C. Per-level combinators (Parlot)

No table: `LeftAssociative(p, ops)`, `RightAssociative`, `Unary`, nested by hand.

- Pros: small, explicit nodes.
- Cons: precedence is implicit in how the nodes are nested, so there is no table for analysis. Authors write the level structure by hand, which is the boilerplate § Expressions exists to remove.

### Sub-choices taken by the maintainer (2026-10-18)

| Fork | Chosen | Alternative not taken |
| --- | --- | --- |
| Operator shape | Operator parser of any type `TOp` plus a combine function, as in Parlot | `Parser<TIn, Func<T, T, T>>`, as in Pidgin |
| Repeated prefix and postfix | Repeat by default, as in Willis and Wu and Parlot | Once, with opt-in chaining, as in Pidgin |
| `a < b < c` | Committed error naming the operator | Stop and leave `< c` unconsumed, as in Pidgin and Willis and Wu |
| Result types | One `T` for every level | Per-level types, as in Willis and Wu |

## Decision

Option A, with the sub-choices above. Accepted by the maintainer on 2026-10-18.

**1. API.** `Expression` is a static class nested in `Grammar<TIn>`, so `TIn` is fixed and grammar classes call it unqualified. The table is returned by a lambda that receives an `OperatorFactory<TIn, T>`. The factory fixes `T` (inferred from the atom), so lambdas need no type annotations. Levels are listed tightest first, as in Pidgin and C#'s operator precedence table.

```csharp
public sealed class Calc : CharGrammar
{
    static readonly Parser<char, double> Expr = Expression.Build(Atom, op =>
    [
        [op.Postfix(Char('!'), (x, _) => Factorial(x))],
        [op.InfixR(Char('^'), (l, _, r) => Math.Pow(l, r))],
        [op.Prefix(Char('-'), (_, x) => -x)],
        [op.InfixL(Char('*'), (l, _, r) => l * r), op.InfixL(Char('/'), (l, _, r) => l / r)],
        [op.InfixL(Char('+'), (l, _, r) => l + r), op.InfixL(Char('-'), (l, _, r) => l - r)],
        [op.InfixN(Char('<'), (l, _, r) => l < r ? 1 : 0)],
    ]);
}
```

| Factory method | Combine function |
| --- | --- |
| `Prefix<TOp>(Parser<TIn, TOp> op, Func<TOp, T, T> combine)` | operator value, operand |
| `Postfix<TOp>(Parser<TIn, TOp> op, Func<T, TOp, T> combine)` | operand, operator value |
| `InfixL<TOp>`, `InfixR<TOp>`, `InfixN<TOp>(Parser<TIn, TOp> op, Func<T, TOp, T, T> combine)` | left, operator value, right |

`Build` and every factory method null-check their parser and function arguments with their own `[CallerArgumentExpression]` parameters and `Guard.NotNull`, as CORE-02 requires. The table lambda runs once, inside `Build`. A spike on .NET 11 RC 2 (SDK 11.0.100-rc.2.26504.105) confirmed that `T` and `TOp` are inferred in this shape, and that the nested `Expression` does not stop `Expression<Func<…>>` from `System.Linq.Expressions` resolving in the same grammar class.

**2. Construction-time checks.** `Build` throws a `GrammarException` if the table is empty, a level is empty, or a level mixes infix associativities (L with R, or N with either). Mixed associativities at one level make `a + b ^ c` ambiguous. Prefix and postfix operators may share a level with each other and with infix operators of one associativity.

**3. Parsing semantics (precedence climbing).** Level *k* in the list has binding power *n − k*, so the first level binds tightest.

- **Operand position.** Try every prefix operator in table order. If one matches, parse an operand at that operator's level (so a looser operator cannot be its operand), then apply its combine function. Prefix operators therefore repeat (`- -x`) and appear in any operand position (`2 ^ -x`). If none matches, parse the atom.
- **Operator position**, after an operand, with a minimum level: try the postfix and infix operators whose level is at least that minimum. Tighter levels go first, and within a level operators are tried in declaration order. A postfix operator applies and loops, so postfix operators repeat (`f()()`). An infix operator parses its right operand with minimum level *k* + 1 for InfixL and InfixN, or *k* for InfixR, then loops. If no operator matches, the expression ends and succeeds.
- **Same level, prefix and postfix:** prefix applies first, so `-x!` is `(-x)!` when both are at one level, as in Pidgin. At different levels, the levels decide.
- **One token as prefix and infix.** Prefix operators are tried only in operand position, and postfix and infix only in operator position. `-` can be both prefix and infix with no conflict (`a - -b`). One token used as both postfix and infix is not resolved by position: the first match in the order above wins. This is a grammar ambiguity for analysis to report.

**4. Non-associative chains.** After an InfixN operator at level *k* and its right operand, if the next operator in operator position would also come from level *k*, the parse fails committed at that operator's span with the message "'<' cannot be chained; add parentheses", built from the operator's display. Operators from other levels continue normally, so `a < b + 1` and `a < b && c < d` still parse.

**5. Errors and commit.** These follow § Combinator semantics with no special cases.

- An operator that fails without consuming ends the operator loop. Its expectations merge with others at the same position under "furthest failure wins".
- An operator that consumed input commits. If its operand then fails, the failure is committed and the expression does not fall back to the shorter parse. An author who wants `a -` to stop before the `-` makes the operator look ahead for an operand without consuming it, as in `Atomic(Char('-').Before(Lookahead(Atom)))`. Wrapping the operator alone in `Atomic` does not help: the operator succeeds, and the failure of its operand comes after the `Atomic`.
- `Named` on the whole expression replaces its expected set only for empty failures, as for any node.
- Operand recursion, and the right recursion of InfixR, count against `ParseOptions.MaxDepth` ([§ Limits for untrusted input](../../../whitepaper.md#limits-for-untrusted-input)).
- An operator that succeeds without consuming input would loop forever. Engines treat it as `Many` treats a body that does. That rule is not decided yet: CORE-07 left it to [INT-03](../../active/03-interpreter/INT-03-execute-the-core-combinators.md), whose criterion covers the rows where Pidgin throws. The expression node follows whatever INT-03 decides, and ANA-01 reports such operators statically.

**6. Node and structural view.**

- One `ExpressionNode<TIn, T>` with `NodeKind.Expression`.
- `Children` is the atom, then every operator parser in table order (tightest level first, declaration order within a level).
- Analysis and export read an internal table view: for each level, its operators with their fixity (`Prefix`, `Postfix`, `InfixL`, `InfixR`, `InfixN`) and operator node.
- Each operator is an internal typed entry, such as `InfixOperator<TIn, TOp, T>`, behind a non-generic-in-`TOp` base. Per ADR-0001, engine operations that need `TOp` are `internal abstract` members on that base. No visitor is added.
- `ExpressionNode` overrides `Lower(PlanBuilder)` (INT-02) by calling `PlanBuilder.NotYetExecutable` until INT-04 implements it, as the repetition nodes do until INT-03.

**7. Card scope.** The arithmetic parsing tests named in CORE-11's acceptance criterion need an engine, and execution belongs to INT-04 (which CORE-11 blocks). So CORE-11's criterion covers construction: the table, the structural view, the construction-time checks and null checks. The parsing tests move to INT-04: associativity, mixed prefix and infix `-`, `2 ^ -x`, repeated prefix and postfix, and the non-associative error.

Deciding reason: only option A keeps the table visible to analysis, as § Expressions promises, and parses `2 ^ -x` correctly. The sub-choices take the more permissive and better-diagnosed behaviour in each case, at no extra cost to analysis.

What would change this:

- Measurements showing precedence climbing in the interpreter is slower than the desugared fold, by more than the gate allows (FND-06).
- Demand for per-level result types in real .NET grammars. That would be a new, additive overload, not a change to this one.

## Consequences

- **Easier:**
  - Authors get C-style tables without workarounds: `- -x`, `f()()`, `2 ^ -x`, and a clear error for `a < b < c`.
  - Analysis, EBNF export and the generator read a table, not a desugared chain.
  - Operator values (`TOp`), such as a token with its span, reach the combine function, so AST nodes can record operator positions.
- **Harder:**
  - INT-04 and the generator each implement precedence climbing, and the GEN-08 differential tests must cover it.
  - Each operator entry carries a hidden `TOp`, so it needs its own abstract engine operations (ADR-0001).
- **Ruled out:**
  - Per-level result types in this API.
  - Pidgin's once-only prefix and postfix as the default. An author who needs exactly one wraps the operand in their own rule.
  - Stopping silently on a non-associative chain.
- **Risks and how they are watched:**
  - A token used as both postfix and infix is resolved by order only. ANA-01 computes the operators' FIRST sets, and a later diagnostic can report the overlap. No CRL ID is assigned here.
  - Native AOT. CORE-07 found that generic instance methods on parsers failed the AOT smoke publish with IL3054 (generic recursion), and moved its combinators to C# 14 extension members. `OperatorFactory<TIn, T>.InfixL<TOp>` and its siblings build `InfixOperator<TIn, TOp, T>` and never a wider parser type, so they should not form a cycle. The spike did not test AOT, so the Build stage must confirm it with the AOT smoke publish. If it fails, the factory methods become extension members, as in CORE-07.
  - Behaviour differs from Pidgin's `ExpressionParser` in three places (repeats, `2 ^ -x`, non-associative chains). docs/ must say so for users coming from Pidgin.

## Changes made

- **Whitepaper:** [§ Expressions](../../../whitepaper.md#expressions) rewritten to describe the factory API, the table rules, precedence climbing, non-associative chains and commit behaviour, linking this record. No other section changed: the code-fix row in § Developer experience, the general-parsing spike and Example C stay correct as written.
- **Cards:**
  - [CORE-11](../../active/02-core-combinators/CORE-11-expressionbuild.md): scope and criterion rewritten to construction, the table view and the checks; moved to Building.
  - [INT-04](../../active/03-interpreter/INT-04-execute-bind-expressionbuild-and-custom.md): scope names the semantics in points 3 to 5; gains the arithmetic parsing criterion from point 7.
  - [ANA-01](../../active/09-static-analysis/ANA-01-grammar-property-analysis.md): reads the table view; reports operators that succeed without consuming input.
