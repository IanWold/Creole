# The interpreter

The interpreter runs any grammar, including one built at run time. On first use it lowers the grammar to an execution plan and keeps the plan for as long as the grammar is alive, so later parses reuse it. Plans are immutable and shared between threads; everything a parse changes lives in its cursor. The design is in the whitepaper's [§ Interpreted engine](../../whitepaper.md#interpreted-engine).

## What lowering speeds up

Lowering rewrites four shapes of grammar. A rewrite never changes a parse's result, the input it consumes, whether a failure is committed, or what a failure reports, so you can ignore them and still get correct parses and the same error messages. Writing grammars in these shapes makes them faster.

| Shape | Runs as | To get it |
| --- | --- | --- |
| A skipped run of a character class: `Whitespace.SkipMany()`, `Digit.SkipAtLeastOnce()`, `NoneOf('"', '\\').SkipMany()` | One vectorized search per segment of input, not one step per character | Use a character class (`Char`, `Range`, `NoneOf`, `Digit`, `Letter`, `LetterOrDigit`, `Whitespace`), not `Satisfy`, whose predicate the interpreter cannot see into. Skip the run with `SkipMany` or `SkipAtLeastOnce`, and take its text with `Slice()`, not by collecting characters with `Many()`. |
| A `Select` over a `Select`, including `As` and `Ignore` after a `Select` | One step that applies both functions in turn | Nothing to do. |
| `Or` and `OneOf` | Only the alternatives that can start with the next character or token kind are tried | Start each alternative with a literal: see below. |
| `Longest` over characters, when every alternative is regular | One automaton that reads each character once, however many alternatives there are | Build each alternative from the combinators listed in [When a Longest compiles](#when-a-longest-compiles). For more than 16 alternatives, nest `Longest` directly, `Longest(a, …, Longest(p, …))`: a `Longest` that is itself an alternative joins the outer one. |

A single test of a character class, such as `Char(',')`, `Range('a', 'f')` or `Digit`, looks an ASCII character up in a table and searches the class's ranges for any other. `Satisfy` calls its predicate for every character instead, so prefer a class wherever one fits.

### How a choice picks its alternatives

For each alternative, lowering looks for the first thing it reads. It looks through `Select`, `As`, `Ignore`, `Named`, `Context`, `Slice`, `Text`, `Atomic`, `Where`, the first parser of `Seq`, `Then`, `Before` and `Between`, and the first `from` of a query. If it finds one of these, the alternative is tried only when the next item can start it:

- `Char`, `Range`, `NoneOf` and the other character classes;
- `String`, by its first character, and `CIString`, by its first character in any case;
- `Kind(k)`, by token kind, and `Token(x)` when the items are characters or an enum;
- a `Custom` scanner that declares `First` and is not `Nullable`.

Any other alternative is tried for every item. That includes a choice inside a choice, so `OneOf(a, b, c, d)` can skip alternatives where `a.Or(b).Or(c).Or(d)` cannot. The alternatives that are tried keep their order, so an alternative that overlaps a later one still wins: `OneOf(String("if"), identifier)` tries `String("if")` first on `i`. An alternative that is skipped still counts toward the error: the choice records what it would have expected, in its place in the order, so a message lists the same expectations as trying every alternative would. A choice works this way when at least four of its alternatives start with a literal, because with fewer, checking the next item costs more than skipping saves; otherwise it tries every alternative in order. For the same reason, put the alternatives that start with a literal before those that do not: when they come last, the choice pays for the check and skips nothing that would have been tried.

| Alternative | Tried when the next character is |
| --- | --- |
| `Char('{').Then(members).Before(Char('}'))` | `{` |
| `String("null").As(...)` | `n` |
| `Digit.Then(Digit.SkipMany()).Slice()` | a digit |
| `Digit.SkipAtLeastOnce().Slice()` | anything: lowering does not look into repetitions |
| `Char('-').Optional().Then(digits)` | anything: `Optional` can match nothing, so the first character could be a digit or something else |
| `Satisfy(char.IsDigit)` | anything: the predicate is opaque |

### When a Longest compiles

A `Longest` compiles to an automaton when its input is characters and every alternative is regular:

- **What an alternative can use.** Character classes (including `Any`), `String` and `CIString`, `Seq`, `Then`, `Before` and `Between`, `Or` and `OneOf`, `Optional`, `Atomic`, and `Many`, `AtLeastOnce`, `SkipMany` and `SkipAtLeastOnce` over a parser that cannot match nothing. `Select`, `As`, `Ignore`, `Slice`, `Text`, `Named` and `Context` can wrap any of these.
- **What sends it to the interpreter.** Anything else in any alternative: `Satisfy`, `Custom`, `Where`, `Lookahead`, `Not`, `Cut`, `Rec`, a query, `Repeat`, `SepBy`, `Until`, `Return`, `End`, or a `Longest` anywhere but directly as an alternative.
- **Choices inside `Atomic`.** A choice there must be one the automaton can settle early. Call the rest what follows the choice up to the end of the outermost `Atomic` around it. Either the first branch followed by the rest cannot fail once it has consumed input, or the second branch followed by the rest cannot match nothing and starts with different characters from the first branch followed by the rest. `Atomic(Char('.').Then(digits))`, the usual fraction of a number, qualifies. `Atomic(String("ab").Or(String("a")).Then(Char('c')))` does not: both branches start with `a`, and the `Atomic` can still fail after `ab`.
- **Size.** The automaton may have at most 2,000 states, and building it may take at most 500,000 steps, where trying a character class from a state takes a step for each way through the alternatives still open there. Lexers are far smaller: a 12-rule JSON lexer has 39 states and takes 1,767 steps, and a 138-rule C#-like lexer has 513 states and takes 90,816. A `Longest` over either limit runs on the interpreter, with the same results ([ADR-0025](../../work/docs/decisions/0025-automaton-construction-stops-at-a-state-cap-or-a-work-budget.md)). A `Longest` nested directly as one of its alternatives is then tried on its own, so the alternatives it holds may still compile.

A `Longest` that does not compile still works: it runs each alternative in turn, as before.

The automaton follows the interpreter's commit semantics, so a compiled `Longest` gives exactly the results it would give otherwise. That is not always what an alternative's regular-expression reading suggests. `digits.Then(Char('.').Then(digits).Optional())` fails on `1.x`, because the `Optional` has consumed the `.`; write the fraction as `Atomic(Char('.').Then(digits)).Optional()` to match `1`.

**Values.** The automaton finds the winning alternative and where its match ends. The plan produces the winner's value directly when it depends only on the text matched: a constant from `As` or `Ignore`, `Slice()`, `Text()`, a single character, a `String`, or a `Select` over one of these. Any other value, such as a `Seq`'s tuple or a `Select` over `Optional`, comes from running the winning alternative once more. Only the winner's functions run: a `Select` on an alternative that loses does not, though it does when the interpreter's own `Longest` runs (see [How often your functions run](../concepts/sequencing.md#how-often-your-functions-run)). When no alternative matches, the interpreter's own `Longest` runs and reports the failure.

**Cost.** The automaton is built once, when the grammar is first lowered. In a new process that takes about 30 ms for the JSON lexer and about 85 ms for the C#-like lexer, mostly the runtime compiling the construction code. Once that code is compiled, building takes about 0.24 ms and 10 ms. The C#-like lexer allocates about 16 MB of short-lived memory while it builds. Tokenizing 100 KB then takes 3.4 times less time with the JSON lexer and 14.4 times less with the C#-like one (medians of three runs). So for a grammar used once on a small input, the interpreter alone is cheaper: with the construction code compiled, the automaton pays for itself after about 21 KB of JSON or 57 KB of C#-like text.

**Only `Longest` compiles.** A choice over regular alternatives that is not a `Longest` stays on the interpreter. As one automaton, a character-level JSON token `OneOf` ran at 0.6 times the speed it reaches with dispatch and vectorized scans. JSON strings are long runs, which a scan handles faster than an automaton reading one character at a time.

## What allocates

Results that are value types flow through the plan without boxing, so a parse whose results are value types or literal strings allocates only its result: `Parse` returns a `Parsed<T>`, one object. These allocate by design:

| Combinator | Allocates |
| --- | --- |
| `Many`, `AtLeastOnce`, `Repeat`, `SepBy`, `SepBy1`, `SepEndBy`, `Until` | the list of items |
| `Text()` | the string |
| `CIString` | the matched text, whose case may differ from the literal |
| `Slice()` over streamed input | a copy of the text ([ADR-0013](../../work/docs/decisions/0013-slices-view-in-memory-input-and-copy-streamed-input.md)); over in-memory input it is a view and allocates nothing |
| A query whose later `from` builds a new parser from an earlier value | that parser, and its plan, on every match; return an existing parser, such as a rule field, to avoid it |
| `Case<T>()` over union tokens that are structs, and `Match` over a union that is a struct, such as `Maybe<T>` | the case value, boxed to read it |

Struct tokens that implement `IKinded<TKind>`, matched with `Kind(k)`, never box.

A successful parse records no failures at all. `Parse` runs the plan without a failure log, and only a parse that fails runs again from the start with one, to build its error ([ADR-0027](../../work/docs/decisions/0027-in-memory-parses-run-without-a-failure-log-and-again-with-one-on-failure.md)). Recording failures allocates nothing either. A parse keeps its furthest failure in buffers that each thread keeps, one set for each type of input item, and reuses from one parse to the next, holding references to descriptions each grammar node creates once. A parse that runs inside another, such as one called from a `Select` function, gets buffers of its own. Only the failure that is finally reported becomes a `ParseError`, and that is when token names are looked up and messages are formed (see [Errors](../concepts/errors.md#what-a-failure-reports)). Recording does take time, because every alternative that fails and every loop that stops records where it did, which is why a successful parse skips it. A failed parse therefore costs up to about two and a half times a successful one: it runs the grammar twice, the second time recording, so it builds the grammar's values twice, and then the error and its `Failed` result. A medium JSON document broken at its end took 2.3 times as long as the valid one. It copies none of its input, which the result refers to.

## Deep input

A parse counts how deep it recurses, so deeply nested input cannot overflow the stack and crash the process. One level is counted while each of these runs:

- a `Rec`;
- the parser a `Bind` continuation returned, including a query whose later `from` uses an earlier value;
- a prefix operator's operand in `Expression.Build`;
- an `InfixR` right operand.

Nothing else counts, so how deep a parse goes depends only on the grammar and the input. With the JSON grammar of [Worked Example A](../../whitepaper.md#a-json-in-two-layers), whose `Value` is a `Rec`, each nested array or object is one level deeper.

Deeper than 20 levels, each counted level also checks that the thread has enough stack left. A parse that goes too deep for its limit, or for the thread's stack, ends there. No combinator inside the parse catches that, including `Or`, `Optional`, repetitions, `Not`, `Lookahead` and `Atomic`, so the limit never changes what a grammar accepts. On a thread with a small stack, the stack check can end a parse before the limit does. Recursion in your own code is not counted: `Select` and `Where` functions, custom scanners, and code that builds grammars. The reasons are in [ADR-0020](../../work/docs/decisions/0020-depth-counts-rec-bind-and-operands-and-ends-the-parse.md).

The limit is `ParseOptions.MaxDepth`, 1,000 levels by default. A parse that goes too deep returns `Failed` with one error where the level would have started, whose message names the cause. [Running a parser § Options](../concepts/parsing.md#options) shows how to set the limit and what the error holds.
