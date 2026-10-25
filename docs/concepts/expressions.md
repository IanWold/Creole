# Expressions

`Expression.Build` parses operator expressions from an atom and a table of operators, so a grammar never needs left recursion for `a + b * c`. It builds one grammar node that holds the whole table. The design is in the [whitepaper's § Expressions](../../whitepaper.md#expressions) and [ADR-0004](../../work/docs/decisions/0004-expression-build-operator-table-and-semantics.md).

> The interpreter parses expressions as this page describes. The public method that runs a parser on your input arrives with INT-08.

## Writing a table

`Expression` is nested in `Grammar<TIn>`, so a grammar class calls it unqualified. The second argument is a lambda that receives an operator factory, `op`, and returns the table: a list of levels, **tightest first**, each a list of operators.

```csharp
public sealed class Calc : CharGrammar
{
    // Atom, Bang, Caret, Minus, Star, Slash, Plus and Less are parsers declared earlier in the class.
    public static readonly Parser<char, double> Expr = Expression.Build(Atom, op =>
    [
        [op.Postfix(Bang, (x, _) => x)],
        [op.InfixR(Caret, (l, _, r) => Math.Pow(l, r))],
        [op.Prefix(Minus, (_, x) => -x)],
        [op.InfixL(Star, (l, _, r) => l * r), op.InfixL(Slash, (l, _, r) => l / r)],
        [op.InfixL(Plus, (l, _, r) => l + r), op.InfixL(Minus, (l, _, r) => l - r)],
        [op.InfixN(Less, (l, _, r) => l < r ? 1 : 0)],
    ]);
}
```

This is the table in `ExpressionNodeTests`.

| Method | Example | Combine function receives |
| --- | --- | --- |
| `op.Prefix(op, combine)` | `-x` | operator value, operand |
| `op.Postfix(op, combine)` | `n!`, `f()` | operand, operator value |
| `op.InfixL(op, combine)` | `a - b - c` is `(a - b) - c` | left, operator value, right |
| `op.InfixR(op, combine)` | `a ^ b ^ c` is `a ^ (b ^ c)` | left, operator value, right |
| `op.InfixN(op, combine)` | `a < b`, which cannot be chained | left, operator value, right |

The operator parser can produce any type. If it produces a token with a span, the combine function can record where the operator was. Every level produces the atom's type, so an AST usually uses a common base type or a union. Because `op` fixes that type, the combine functions need no parameter types.

The table lambda runs once, inside `Build`, and the node keeps a copy of the table.

## How an expression is parsed

Where an operand is expected, every prefix operator is tried, in table order. If one matches, its operand is parsed with only the tighter levels, and the combine function is applied. If none matches, the atom is parsed. After an operand, the postfix and infix operators are tried, tighter levels first and in declaration order within a level, until none matches. Prefix operators are never tried after an operand, so one token can be both prefix and infix.

With the table above:

| Input | Groups as | Why |
| --- | --- | --- |
| `a - b - c` | `(a - b) - c` | `-` is `InfixL` |
| `a ^ b ^ c` | `a ^ (b ^ c)` | `^` is `InfixR` |
| `a - -b` | `a - (-b)` | `-` is prefix where an operand is expected, and infix after one |
| `- -x`, `x!!` | `-(-x)`, `(x!)!` | Prefix and postfix operators repeat |
| `-x ^ 2` | `-(x ^ 2)` | `^` is tighter than prefix `-` |
| `2 ^ -x` | `2 ^ (-x)` | A prefix operator can start any operand, even one after a tighter operator |
| `-x!` | `-(x!)` | `!` is tighter than `-`. When a prefix and a postfix operator share a level, the prefix applies first: `(-x)!` |
| `a < b + 1` | `a < (b + 1)` | `+` is tighter than `<` |
| `a < b < c` | An error at the second `<` | `<` is `InfixN`, so it cannot be chained; write `(a < b) < c` |

**Chained non-associative operators.** On `a < b < c`, the expression fails after consuming the second `<`. The error points at that operator, with the message `'<' cannot be chained; add parentheses`. The operator is named as an error would name what it expects: by its `Named` label if it has one, otherwise as its leading character, string or token kind (`"<="` for `String("<=")`). Anything else expected at that position, such as a tighter operator, is still listed.

**Commit.** Ordinary [commit semantics](choice.md#ordered-choice) apply. An operator that fails without consuming input ends the expression. An operator that consumed input commits, so on `a +` the expression fails at the end of the input instead of producing `a`. An expression that fails without consuming, as on `+`, lets an enclosing choice try its next alternative.

**Operators that consume nothing.** An infix operator may match without consuming input, as a juxtaposition operator for function application does (`f x`). If the operand after it then fails without consuming, the expression ends before it. A prefix or postfix operator that succeeds without consuming input, or an infix operator and operand that together consume nothing, would repeat forever, so the parse throws `GrammarException`.

## Checks when the grammar is built

`Build` throws a `GrammarException` when:

- the atom, the table lambda, the table, a level or an operator is null (null arguments are named, as for every combinator; see [Grammars as data](grammars-as-data.md));
- the table has no levels, or a level has no operators;
- a level mixes infix operators of different associativity, such as `InfixL` with `InfixR`. Without this check, `a + b ^ c` would be ambiguous. Messages count levels and operators from 0.

Prefix and postfix operators may share a level with each other and with infix operators of one associativity.

## Coming from Pidgin

The table follows Pidgin's `ExpressionParser.Build`. The construction differences are:

- Pidgin's operator parser produces the combining function (`Parser<TToken, Func<T, T, T>>`). Creole takes the operator parser and the function separately, and passes the operator's value to the function.
- Pidgin lists a level as an `OperatorTableRow` and accepts any mix of operators in a row. Creole rejects a level that mixes infix associativities.

Parsing differs in three ways (the rationale is in ADR-0004):

- **Repeats.** Pidgin applies a prefix or postfix operator once per level unless it is marked chainable (`PrefixChainable`, `PostfixChainable`). Creole repeats them: `- -x`, `f()()`.
- **Prefix after a tighter operator.** Pidgin parses the operand after an infix operator from the tighter levels only, so `2 ^ -x` fails when `-` is looser than `^`. Creole tries every prefix operator wherever an operand is expected.
- **Non-associative chains.** On `a < b < c`, Pidgin stops after `a < b` and leaves `< c` unparsed. Creole fails at the second `<`.
