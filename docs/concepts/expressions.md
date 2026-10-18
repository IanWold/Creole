# Expressions

`Expression.Build` parses operator expressions from an atom and a table of operators, so a grammar never needs left recursion for `a + b * c`. It builds one grammar node that holds the whole table. The design is in the [whitepaper's § Expressions](../../whitepaper.md#expressions) and [ADR-0004](../../work/docs/decisions/0004-expression-build-operator-table-and-semantics.md).

> Nothing runs expression nodes yet. `Expression.Build` builds the node and checks the table, and the interpreter that parses with it arrives with INT-04. The XML documentation of `Expression.Build` and of each operator method states the parsing rules engines must implement; this page will describe them once they run.

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

Parsing also differs, in repeated prefix and postfix operators, prefix operators after a tighter infix operator, and non-associative chains. This page describes those once INT-04 runs expressions; the rationale is in ADR-0004.
