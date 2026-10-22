# The interpreter

The interpreter runs any grammar, including one built at run time. On first use it lowers the grammar to an execution plan and keeps the plan for as long as the grammar is alive, so later parses reuse it. Plans are immutable and shared between threads; everything a parse changes lives in its cursor. The design is in the whitepaper's [§ Interpreted engine](../../whitepaper.md#interpreted-engine).

> The public method that runs a parser on your input arrives with INT-08.

## What lowering speeds up

Lowering rewrites three shapes of grammar. A rewrite never changes a parse's result, the input it consumes, or whether a failure is committed, so you can ignore them and still get correct parses. Writing grammars in these shapes makes them faster.

| Shape | Runs as | To get it |
| --- | --- | --- |
| A skipped run of a character class: `Whitespace.SkipMany()`, `Digit.SkipAtLeastOnce()`, `NoneOf('"', '\\').SkipMany()` | One vectorized search per segment of input, not one step per character | Use a character class (`Char`, `Range`, `NoneOf`, `Digit`, `Letter`, `LetterOrDigit`, `Whitespace`), not `Satisfy`, whose predicate the interpreter cannot see into. Skip the run with `SkipMany` or `SkipAtLeastOnce`, and take its text with `Slice()`, not by collecting characters with `Many()`. |
| A `Select` over a `Select`, including `As` and `Ignore` after a `Select` | One step that applies both functions in turn | Nothing to do. |
| `Or` and `OneOf` | Only the alternatives that can start with the next character or token kind are tried | Start each alternative with a literal: see below. |

### How a choice picks its alternatives

For each alternative, lowering looks for the first thing it reads. It looks through `Select`, `As`, `Ignore`, `Named`, `Context`, `Slice`, `Text`, `Atomic`, `Where`, the first parser of `Seq`, `Then`, `Before` and `Between`, and the first `from` of a query. If it finds one of these, the alternative is tried only when the next item can start it:

- `Char`, `Range`, `NoneOf` and the other character classes;
- `String`, by its first character, and `CIString`, by its first character in any case;
- `Kind(k)`, by token kind, and `Token(x)` when the items are characters or an enum;
- a `Custom` scanner that declares `First` and is not `Nullable`.

Any other alternative is tried for every item. That includes a choice inside a choice, so `OneOf(a, b, c)` can skip more alternatives than `a.Or(b).Or(c)`. The alternatives that are tried keep their order, so an alternative that overlaps a later one still wins: `OneOf(String("if"), identifier)` tries `String("if")` first on `i`. A choice works this way when at least two of its alternatives start with a literal; otherwise it tries every alternative in order.

| Alternative | Tried when the next character is |
| --- | --- |
| `Char('{').Then(members).Before(Char('}'))` | `{` |
| `String("null").As(...)` | `n` |
| `Digit.Then(Digit.SkipMany()).Slice()` | a digit |
| `Digit.SkipAtLeastOnce().Slice()` | anything: lowering does not look into repetitions |
| `Char('-').Optional().Then(digits)` | anything: `Optional` can match nothing, so the first character could be a digit or something else |
| `Satisfy(char.IsDigit)` | anything: the predicate is opaque |

## What allocates

Results that are value types flow through the plan without boxing, so a parse whose results are value types or literal strings allocates nothing. These allocate by design:

| Combinator | Allocates |
| --- | --- |
| `Many`, `AtLeastOnce`, `Repeat`, `SepBy`, `SepBy1`, `SepEndBy`, `Until` | the list of items |
| `Text()` | the string |
| `CIString` | the matched text, whose case may differ from the literal |
| `Slice()` over streamed input | a copy of the text ([ADR-0013](../../work/docs/decisions/0013-slices-view-in-memory-input-and-copy-streamed-input.md)); over in-memory input it is a view and allocates nothing |
| A query whose later `from` builds a new parser from an earlier value | that parser, and its plan, on every match; return an existing parser, such as a rule field, to avoid it |
| `Case<T>()` over union tokens that are structs, and `Match` over a union that is a struct, such as `Maybe<T>` | the case value, boxed to read it |

Struct tokens that implement `IKinded<TKind>`, matched with `Kind(k)`, never box.
