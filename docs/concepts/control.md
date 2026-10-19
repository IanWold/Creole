# Backtracking and labels

Control combinators decide when a parse may go back and try again, and what a failure reports. `Atomic`, `Lookahead`, `Not` and `Cut` are members of `Grammar<TIn>`, so a grammar class calls them unqualified. `p.Named(label)` and `p.Context(text)` are extension members of `Parser<TIn, TOut>` in the `ErrorReporting` class. The design and its reasons are in the [whitepaper's § Choice and backtracking](../../whitepaper.md#choice-and-backtracking) and [ADR-0008](../../work/docs/decisions/0008-control-node-semantics.md), refined by [ADR-0010](../../work/docs/decisions/0010-cuts-under-dispatch-and-across-layers.md).

> Nothing runs these combinators yet. They build grammar nodes, and the interpreter that executes them arrives with INT-03. Each method's XML documentation and this page state the behaviour engines must implement. `ControlTable` in the test project lists it as cases: a grammar, an input and the expected outcome.

## Commit semantics, briefly

A parser that fails **without consuming input** lets an enclosing choice try its next alternative. A parser that fails **after consuming input** commits: the failure passes up and no other alternative is tried (see [Choice](choice.md)). The combinators on this page change that, case by case.

## At a glance

| Combinator | Parser succeeds | Parser fails without consuming | Parser fails after consuming |
| --- | --- | --- | --- |
| `Atomic(p)` | succeeds as `p` did | fails without consuming | rewinds and fails without consuming, unless a cut passed inside `p` is still in force |
| `Lookahead(p)` | rewinds; succeeds with `p`'s value, consuming nothing | fails without consuming | fails after consuming |
| `Not(p)` | rewinds; fails without consuming | succeeds with `Unit`, consuming nothing | rewinds; succeeds with `Unit`, consuming nothing |
| `Cut` | always succeeds, consuming nothing | — | — |
| `p.Named(label)`, `p.Context(text)` | as `p` | as `p` | as `p` |

When input is streamed, an attempt can also run out of buffered data. That is not a failure: `Atomic`, `Lookahead` and `Not` pass it on unchanged, without rewinding or inverting it ([§ The restart protocol](../../whitepaper.md#the-restart-protocol)).

## `Atomic`

`Atomic(p)` runs `p`. If `p` fails after consuming input, `Atomic` rewinds to where it started, so the failure counts as having consumed nothing. It is Parsec's `try`.

```csharp
// Without Atomic, "x=1" would fail inside the first alternative after reading "x".
public static readonly Parser<char, char> Separator =
    Atomic(Seq(Letter, Char(':')).Select((name, colon) => colon)).Or(Seq(Letter, Char('=')).Select((name, equals) => equals));
```

Success, and failure without consuming, pass through unchanged.

## `Lookahead`

`Lookahead(p)` runs `p` and, if it succeeds, rewinds and produces `p`'s value without consuming input. The next parser starts where `Lookahead` did.

If `p` fails after consuming input, the failure stays committed, as in Parsec. Write `Atomic(Lookahead(p))` to make it a failure without consuming.

## `Not`

`Not(p)` succeeds with `Unit` if `p` fails, and fails if `p` succeeds. It never consumes input: it rewinds in every case, including when `p` fails after consuming.

```csharp
// "if" as a keyword, but not the start of "iffy".
public static readonly Parser<char, string> If = String("if").Before(Not(LetterOrDigit));
```

## `Cut`

`Cut` is a parser that always succeeds without consuming input. Once it has succeeded, no enclosing `Atomic` rewinds a failure. Put it where the grammar knows which construct it is reading:

```csharp
// After "if" and "(", a failure is reported here instead of being retried elsewhere.
public static readonly Parser<char, char> IfHead =
    Seq(If, Char('('), Cut, Condition, Char(')')).Select((_, _, _, condition, _) => condition);
```

The failure is then reported where it happened, rather than an earlier alternative being retried and failing somewhere less helpful. It also lets engines release buffered input they no longer need to rewind to.

How far a cut reaches:

- **It reaches every enclosing `Atomic`**, not just the nearest one, and it stays in force after the `Atomic` around it has succeeded.
- **It stops at the nearest `Lookahead`, `Not` or `Longest` alternative.** These must rewind, so a cut inside one only affects `Atomic`s inside it. With no such boundary around it, a cut lasts until the parse ends.
- **It is dropped with an attempt that fails without consuming.** In `Atomic(Cut.Then(Char('x')).Or(Char('a').Then(Char('b'))))` on input `ac`, the first alternative passes the cut and fails without consuming, so the cut is dropped and `Or` tries the second. That reads `a` and fails at `c`, and `Atomic` rewinds. The same holds for a cut in a repetition step or an `Optional` that failed without consuming. A cut in a step that succeeded, or failed after consuming, stays in force.
- **So dispatch is exact.** An engine may pick an alternative by the next item instead of trying each in order: an alternative it skips would only have failed without consuming, and its cut would have been dropped.
- **A cut after a lookahead commits once the lookahead matches.** In `Atomic(Lookahead(Char('a')).Before(Cut).Then(Char('a').Then(Char('b'))))`, the cut runs only on input starting with `a`, and then a failure at the `b` stays committed.
- **It never makes a failure without consuming committed.** `Or`, `Optional` and repetition still look only at whether input was consumed, so `Atomic(Cut.Then(Char('a'))).Or(Char('b'))` matches `b`.
- **A cut before an `Atomic` has no effect on it.** Only a cut passed inside an `Atomic` stops it rewinding.

`Cut` produces `Unit`. To keep it out of a result, attach it with `Before` or `Then`: `Char('(').Before(Cut)`.

## `Named`

`p.Named(label)` describes `p` by one name in error messages, in place of the items it tried:

```csharp
public static readonly Parser<char, char> Sign = Char('+').Or(Char('-')).Named("sign");
// On "x", expected: sign. Without the label, expected: '+' or '-'.
```

- The label applies when `p` consumes nothing, whether `p` fails or succeeds. So in `Digit.Many().Named("digits")` followed by `Char(';')`, input `x` reports "expected digits or ';'".
- It replaces the expectations `p` recorded at the position where it started, and adds none if there were none.
- If `p` consumes input, the label changes nothing. A failure after consuming keeps its precise expectations, and so does a deeper failure that an `Atomic` rewound past.

## `Context`

`p.Context(text)` adds `text` to the context of every failure recorded while `p` runs. Context lines appear in `ParseError.Context`, innermost first, and as `= note:` lines when the error is rendered (see [Errors](errors.md)).

```csharp
public static readonly Parser<char, string> Name = Letter.AtLeastOnce().Text().Context("in name");
```

## Arguments

- A null parser argument or receiver throws a `GrammarException` naming the expression you passed and suggesting `Rec` (see [Grammars as data](grammars-as-data.md)).
- A null or empty `label` or `text` throws a `GrammarException`.
- `Cut` is one shared parser for each input type.
