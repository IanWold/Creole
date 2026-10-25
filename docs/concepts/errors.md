# Errors

When a parse fails, Creole describes the failure as a `ParseError`: where it happened, which layer raised it, what was found and what was expected. The design reasons are in the [whitepaper's § Errors, diagnostics and recovery](../../whitepaper.md#errors-diagnostics-and-recovery).

## `ParseError`

```csharp
public sealed record ParseError(
    SourceSpan Span,
    string Layer,
    Unexpected Unexpected,
    IReadOnlyList<Expectation> Expected,
    IReadOnlyList<ContextLine> Context);

public sealed record ContextLine(string Text, SourceSpan Start);
```

| Property | Meaning |
| --- | --- |
| `Span` | Where the error is, in the original source (see [Source spans](spans.md)). |
| `Layer` | The layer that raised it, such as `"lexer"` or `"parser"`. |
| `Unexpected` | What the parse found there. |
| `Expected` | What it would have accepted there. May be empty. A `p.Named(label)` rule appears here as its label (see [Backtracking and labels](control.md)). |
| `Context` | The constructs the parse was inside, innermost first, from `p.Context(text)` rules. May be empty. See [Context lines](#context-lines). |

An error is immutable. It copies `Expected` and `Context`, so changing the lists you passed in afterwards does not change the error. A `with` expression, such as `error with { Expected = merged }`, makes a new error and checks and copies the values it sets in the same way.

The constructor throws `ArgumentNullException` if `Layer`, `Expected` or `Context` is null. It throws `ArgumentException` if `Unexpected` is `default`, or if a list contains a null or a `default` expectation.

### Context lines

A `ContextLine` names a construct the parse was inside when it failed, such as `in object`, and holds `Start`, the span of the first item that construct read. Renderers show where it started: `in object starting at 2:1` against the source text, or `in object starting at 2` (a character offset) on one line.

- `Text` is the text given to `p.Context(text)`. A null `Text` throws `ArgumentNullException`, and an empty one throws `ArgumentException`, in the constructor and in a `with` expression.
- Two lines are equal when their text and start are equal.

Two errors are equal when all their parts are equal. `Expected` and `Context` are compared element by element, in order, so two errors built from separate lists with the same contents are equal.

## What was found: `Unexpected`

`Unexpected` is a union of three cases:

| Case | Meaning |
| --- | --- |
| `UnexpectedItem(string Display)` | An input item, shown as `Display`, such as `string "host"`. |
| `EndOfInput` | The input ended. |
| `CustomMessage(string Text)` | A message from the grammar in place of an item. |

## What was expected: `Expectation`

`Expectation` is a union of three cases:

| Case | Meaning |
| --- | --- |
| `Label(string Name)` | A name for everything a rule accepts, such as `value`. |
| `ExpectedItem(string Display)` | One input item, such as `','`. |
| `EndOfInput` | The end of the input. |

`EndOfInput` is a case of both unions. It carries no data, so all instances are equal.

## Switching over the cases

A switch expression that covers every case needs no default arm. If you leave a case out, the compiler reports CS8509.

```csharp
string Describe(Expectation expectation) => expectation switch
{
    Label label       => label.Name,
    ExpectedItem item => item.Display,
    EndOfInput        => "end of input",
};
```

Case values convert to the union implicitly, so `Expectation e = new Label("value");` works. Because `EndOfInput` converts to either union, a call such as `M(new EndOfInput())` is ambiguous when `M` has overloads for both `Unexpected` and `Expectation`. Give such methods different names, or cast the argument.

## What a failure reports

> The interpreter records failures as this section describes. The public method that runs a parser and returns its errors arrives with INT-08.

Each layer keeps the furthest failure of its parse, the one at the latest position, and reports that one. Where several parts of a grammar fail at that same position, their failures merge:

- A failure before the furthest one is ignored, and one past it replaces it.
- Expectations are listed in the order they were recorded. One that shows the same as an earlier one is listed once.
- A message, from `Fail(message)`, a filter's message or a chained operator, takes the place of what was found. The first message recorded at the position wins, and the expectations stay alongside it.
- The span covers the item at the position, or is zero-width at the end of the input. A rejected value or a chained operator reported there widens it to cover that input, the longest of them if there are several.
- What was found is the message if there is one, and otherwise the item at the position or the end of input.

A failure deeper than the cursor counts too. In `Atomic(Char('a').Then(Char('b'))).Or(Char('c'))` on `ax`, the report is "expected 'b'" at the `x`, even though the `Atomic` rewound before `Char('c')` ran.

`Named`, `Context`, `Lookahead`, `Not` and `Longest` change what is reported, as [Backtracking and labels](control.md#what-failures-report) and [Choice](choice.md#longest-match) describe. Context lines come from the first failure recorded at the position, innermost first, and leave out a construct that started at the position itself.

What each primitive expects when it fails:

| Parser | Expects |
| --- | --- |
| `Char(c)`, `Token(x)` | the item, such as `','` or `3` |
| `String(s)`, `CIString(s)` | the whole string, such as `"let"`, at the position where it started |
| `Digit`, `Letter`, `LetterOrDigit`, `Whitespace` | its name: `digit`, `letter`, `letter or digit`, `whitespace` (a `Label`) |
| `Range`, `NoneOf` and other classes | one description of the class, such as `[a-z]` or `any character except '"'` |
| `End` | the end of input |
| `Kind(k)` | the kind, as the token type describes it (see [How errors show tokens](primitives.md#how-errors-show-tokens)) |
| `Case<T>()` | the case type's name, such as `Ident` |
| `Custom(scanner)` | the scanner's `Name` (a `Label`), or else its `First` characters as one description, or else nothing in particular |
| `Any`, `Satisfy(predicate)` | nothing in particular: they can start with any item |
| `Fail(message)` | nothing; it reports `message` |

A `Match` whose value has no case expects nothing in particular, where its branch would have started.

Two failures are reported before the cursor, at the start of the input they name: a value that `Where` rejects ([Sequencing](sequencing.md)) and a chained non-associative operator ([Expressions](expressions.md)). Each keeps what its error needs, such as the first item of the rejected value, when it is recorded, so building the error never reads input the parse has already moved past.

How items show:

- A character shows in single quotes, such as `'a'`. Quotes, backslashes and characters that print nothing or move the cursor are escaped as in C#, such as `'\n'` or `'\u001B'`, so a message never carries a terminal escape sequence. A string shows in double quotes with the same escapes.
- A token from a `TokenGrammar` shows as its type describes it, such as `','` or `string "host"`.
- Any other item shows as its `ToString()`.

## Results: `ParseResult<T>`

A parse returns a `ParseResult<T>`, a union of three cases:

| Case | Meaning | `Diagnostics` |
| --- | --- | --- |
| `Parsed<T>(T Value)` | The parse succeeded with no errors. | Empty |
| `Recovered<T>(T Value, Diagnostics Diagnostics)` | The parse produced a value after recovering from errors. | At least one error |
| `Failed(Diagnostics Diagnostics)` | The parse produced no value. | At least one error |

Switch over the three cases. The switch needs no default arm and no `null` arm, and leaving a case out is CS8509:

```csharp
string summary = json.Parse(text) switch
{
    Parsed<JsonValue>         => "ok",
    Recovered<JsonValue> r    => $"parsed with {r.Diagnostics.Count} errors",
    Failed f                  => f.Diagnostics.Render(),
};
```

- `result.Diagnostics` gives the errors whichever case it holds: empty for `Parsed<T>`.
- `Recovered<T>` and `Failed` throw `ArgumentException` if given no errors. A parse without errors is `Parsed<T>`.
- Both `Recovered<T>` and `Failed` deconstruct, so positional patterns such as `Recovered<T>(var value, var diagnostics)` and `Failed(var diagnostics)` work.

### `default` results

Creole never returns `default(ParseResult<T>)`, but your own code can create one, for example as an element of a new array. Passing a null case to a `ParseResult<T>` constructor, such as `new ParseResult<int>((Failed)null!)`, creates the same value. A `default` result holds no case:

- A switch over it throws `SwitchExpressionException`.
- Reading its `Diagnostics` throws `SwitchExpressionException` too.
- If your code might hold one, add a `null` arm. The compiler still reports missing cases.

The reasons are in [ADR-0002](../../work/docs/decisions/0002-parseresult-as-a-plain-union.md).

## `Diagnostics`

`Diagnostics` is an immutable, ordered collection of `ParseError`. It implements `IReadOnlyList<ParseError>`.

- Errors are ordered by source, then start offset, then end offset. Errors with equal spans keep the order they were given in. Sources are ordered as their `SourceRegistry` issued them, with `SourceId.Unnamed` first.
- `Diagnostics.Empty` has no errors.
- `new Diagnostics(errors)` sorts `errors`. It throws `ArgumentException` if one is null.
- A collection can also hold the text of the sources its errors point into, so that `Render()` can show the errors against it. A collection made with `new Diagnostics(errors)` holds none.
- Two collections are equal when they hold equal errors in the same order, whatever source texts they hold.

### Rendering against the source

`Render(sources)` shows each error against the line it points into, in the format of the whitepaper's [§ The error model](../../whitepaper.md#the-error-model). Pass a `SourceText` for every source the errors point into. `Render(sources)` uses only the texts it is given, not any the collection holds:

```csharp
var sources = new SourceRegistry();
var config = sources.Add("config.json");

// diagnostics: the errors from parsing text, with spans that point into config.
string report = diagnostics.Render(new SourceText(config, sources.GetName(config), text));
```

```text
error[parser]: expected ',' or '}'
 --> config.json:3:16
  |
3 |   "port": 8080 "host": "x"
  |                ^^^^^^ unexpected string "host"
  = note: in object starting at 2:1
```

- **Headline.** `error[layer]:` and what was expected. The carets are labelled with what was found. When nothing was expected, the headline says what was found and the carets have no label. A `CustomMessage` is shown as its text.
- **Location.** ` --> name:line:column`, or ` --> line:column` for a source with no name. Lines and columns start at 1, and columns count Unicode scalar values, as [`LineMap`](spans.md) does.
- **Notes.** One `= note:` line per context line, innermost first, ending with where the construct started, as `starting at line:column`. The start may lie in another source, such as the file an embedded script sits in.
- **Order.** Errors come in span order, so they are grouped by source, in the order the `SourceRegistry` issued the ids, with `SourceId.Unnamed` first. A blank line separates errors, and lines are joined by `\n`.

`Render()`, with no arguments, writes the same as `Render(sources)` when the collection holds a text for every source its errors and their context lines point into, and each text is long enough for its spans. Otherwise it writes [one line per error](#one-line-per-error). It never throws.

#### `SourceText`

A `SourceText` holds a source's full text with the id and name to show it under. It builds the source's `LineMap` on first use and keeps it, so many errors in one source share it.

| Constructor | Use |
| --- | --- |
| `new SourceText(text, name)` | A `string` source with no registry id (`SourceId.Unnamed`). `name` may be omitted or null. |
| `new SourceText(id, name, text)` | A source with an id from a `SourceRegistry`, or `SourceId.Unnamed`. `text` is a `string` or a `ReadOnlyMemory<char>`. `name` is usually `registry.GetName(id)`. |

`Text` is a `ReadOnlyMemory<char>` over the text as given. Nothing is copied. If you change memory after wrapping it, rendering shows the new contents, so render before you reuse a buffer.

`Render(sources)` throws `ArgumentException` if an error or one of its context lines points into a source with no text, if two texts have the same id, if a text is null, or if an error's span ends, or a context line starts, past the end of its text. That last one usually means the wrong text was passed.

#### Caret alignment

Carets line up under the span in a terminal with a monospace font:

| Input | How it is shown |
| --- | --- |
| Tab | Spaces to the next stop. Stops are every 4 columns. |
| East Asian Wide or Fullwidth character, such as CJK ideographs and most emoji | Two columns. |
| Regional-indicator flag, or a cluster with U+FE0F (emoji presentation) | Two columns. |
| Combining mark, ZWJ and other format characters | No column of their own. A mark shares its base character's cell. |
| Line break | Never shown. CR, LF and CRLF all end a line. |
| Control character (C0 or DEL) | Its Control Picture, such as `␛` for ESC, one column. |
| C1 control character or lone surrogate | U+FFFD, one column. |
| Anything else, including East Asian Ambiguous characters | One column. |

Widths are measured per grapheme cluster, as .NET's `StringInfo` splits text (Unicode extended grapheme clusters). A span that starts or ends inside a cluster marks the whole cluster. The location still counts scalar values, so `e` followed by U+0301 is two columns in ` --> ` but one caret.

- An empty span, or a span over only a line break, gets one caret where it is, after the last character if it is at the end of the line.
- A span over several lines is shown on its first line, with carets to the end of that line.
- Control characters are replaced so that printing a diagnostic cannot send escape sequences to a terminal. The replacement applies to the source line only. Layer names, `Display` strings, messages and context lines are printed as given.

What this does not handle:

- **Ambiguous width.** Terminals set up for CJK show East Asian Ambiguous characters two columns wide. Creole counts them as one.
- **Font and terminal differences.** Some terminals draw some emoji, flags or ZWJ sequences at a width other than two.
- **Indic conjuncts.** .NET 11 does not join conjuncts such as क्ष into one cluster (Unicode 15.1's rule GB9c), so carets under them can be off by a column.
- **Right-to-left text.** Carets follow the order of the characters in memory, not their order on screen.

The width table comes from Unicode 16.0.0, the version .NET 11 uses. `scripts/gen-east-asian-width.py` regenerates it.

### One line per error

`Render()` writes one line per error, in order, joined by `\n`, when the collection lacks a text its errors point into, such as one made with `new Diagnostics(errors)`. `ParseError.ToString()` gives the same line for one error, so `string.Join('\n', diagnostics)` writes single lines for any collection.

```text
error[lexer] at 4..7: number too large
error[parser] at 15..21: unexpected string "host"; expected ',' or '}'; in object starting at 2
error[parser] at 30..30: unexpected end of input; expected value, '[' or end of input; in array starting at 25; in object starting at 0
```

- The offsets are character offsets, `CharStart..CharEnd`.
- The expected part is left out when nothing was expected. Context lines follow, innermost first, each with the character offset where its construct started.
- A line shows no file name, line or column. Use it when the source text is not at hand.
