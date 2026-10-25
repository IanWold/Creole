# Backtracking and labels

Control combinators decide when a parse may go back and try again, and what a failure reports. `Atomic`, `Lookahead`, `Not` and `Cut` are members of `Grammar<TIn>`, so a grammar class calls them unqualified. `p.Named(label)` and `p.Context(text)` are extension members of `Parser<TIn, TOut>` in the `ErrorReporting` class. The design and its reasons are in the [whitepaper's § Choice and backtracking](../../whitepaper.md#choice-and-backtracking) and [ADR-0008](../../work/docs/decisions/0008-control-node-semantics.md), and for `Cut`, [ADR-0011](../../work/docs/decisions/0011-a-cut-is-dropped-with-every-failed-attempt.md).

> The interpreter runs these combinators as this page describes. `ControlTable` in the test project lists the behaviour as cases: a grammar, an input, the expected outcome and the failure it reports. The interpreter passes every case. It also agrees with a reference model of the cut rules on every grammar the model generates up to three levels deep, over every input up to three characters long. Run a parser with `Parse` ([Running a parser](parsing.md)).

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

## What failures report

`Atomic` keeps the failures its parser recorded, even ones it rewound past, so the furthest failure still wins. `Lookahead` and `Not` do not:

- `Lookahead(p)`, when `p` succeeds, forgets every failure `p` recorded. So `Lookahead(Digit.SkipMany()).Then(Char('x'))` on `12y` reports "expected 'x'" at the `1`, not "expected digit" after the digits it only looked at. When `p` fails, its failure is reported as usual.
- `Not(p)` always forgets the failures `p` recorded, because they describe what must not come next. When `p` succeeds, `Not` fails and reports what it found there with nothing expected: `Not(String("if"))` on `if` reads "unexpected 'i'". Give it a name with `Named` to say more.

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

`Cut` is a parser that always succeeds without consuming input. While it stays in force, the `Atomic`s around it do not rewind a failure. Put it where the grammar knows which construct it is reading:

```csharp
// After "if" and "(", a failure is reported here instead of being retried elsewhere.
public static readonly Parser<char, char> IfHead =
    Seq(If, Char('('), Cut, Condition, Char(')')).Select((_, _, _, condition, _) => condition);
```

The failure is then reported where it happened, rather than an earlier alternative being retried and failing somewhere less helpful. It also lets engines release buffered input they no longer need to rewind to.

How far a cut reaches:

- **It reaches every enclosing `Atomic`**, not just the nearest one, and it stays in force after the `Atomic` around it has succeeded or failed.
- **It stops at the nearest `Lookahead`, `Not` or `Longest` alternative.** These must rewind, so a cut inside one only affects `Atomic`s inside it. With no such boundary around it, a cut lasts until the parse ends.
- **It is dropped with an attempt that fails without consuming.** An attempt is each alternative of `Or` or `OneOf`, including the last; the item of `Optional`; each item of a repetition, mandatory or not; each separator and each terminator; and each operator and atom of `Expression.Build`. In `Atomic(Cut.Then(Char('x')).Or(Char('a').Then(Char('b'))))` on input `ac`, the first alternative passes the cut and fails without consuming, so the cut is dropped and `Or` tries the second. That reads `a` and fails at `c`, and `Atomic` rewinds. The cut is dropped whether the combinator then moves on, stops or fails, so a cut in the last alternative behaves like one in the first. A cut in an attempt that succeeded, or failed after consuming, stays in force. So a separator that matches without consuming keeps its cut even when the item after it fails and the list ends there.
- **Nesting does not matter.** `Seq(a, Seq(Cut, c))` behaves like `Seq(a, Cut, c)`, so building a sequence with `Then` and `Before` gives the same result as listing it in one `Seq`.
- **So dispatch is exact.** An engine may pick an alternative, or skip an item, by the next item of input instead of trying it: an attempt it skips would only have failed without consuming, and its cut would have been dropped.
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
- It replaces the expectations `p` recorded at the position where it started, and adds none if `p` recorded no failure there.
- Expectations recorded at that position before `p` ran stay, ahead of the label: `Char(',').Or(Letter.Named("name"))` on `1` reports "expected ',' or name".
- A failure that expected nothing in particular, such as that of `Satisfy` or `Not`, still counts, so `Satisfy(char.IsAsciiHexDigit).Named("hex digit")` reports "expected hex digit".
- The label never removes a message from `Fail`: the message still says what was found, and the label says what was expected.
- If `p` consumes input, the label changes nothing. A failure after consuming keeps its precise expectations, and so does a deeper failure that an `Atomic` rewound past.
- Of two labels on one parser, the outer one wins.

## `Context`

`p.Context(text)` adds `text` to the context of failures recorded while `p` runs, past the position where `p` started. Context lines appear in `ParseError.Context`, innermost first, each with where its construct started, and as `= note:` lines such as `in object starting at 2:1` when the error is rendered (see [Errors](errors.md)).

- A construct that never began adds no line. `Char('{').Then(Char('}')).Context("in object")` on `{x` reports "in object", but on `x` it does not.
- When several failures merge at one position, the first one recorded there keeps its lines.
- A label from `Named` keeps the lines of the context around `p`.

```csharp
public static readonly Parser<char, string> Name = Letter.AtLeastOnce().Text().Context("in name");
```

## Arguments

- A null parser argument or receiver throws a `GrammarException` naming the expression you passed and suggesting `Rec` (see [Grammars as data](grammars-as-data.md)).
- A null or empty `label` or `text` throws a `GrammarException`.
- `Cut` is one shared parser for each input type.
