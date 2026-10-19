# Errors

When a parse fails, Creole describes the failure as a `ParseError`: where it happened, which layer raised it, what was found and what was expected. The design reasons are in the [whitepaper's § Errors, diagnostics and recovery](../../whitepaper.md#errors-diagnostics-and-recovery).

## `ParseError`

```csharp
public sealed record ParseError(
    SourceSpan Span,
    string Layer,
    Unexpected Unexpected,
    IReadOnlyList<Expectation> Expected,
    IReadOnlyList<string> Context);
```

| Property | Meaning |
| --- | --- |
| `Span` | Where the error is, in the original source (see [Source spans](spans.md)). |
| `Layer` | The layer that raised it, such as `"lexer"` or `"parser"`. |
| `Unexpected` | What the parse found there. |
| `Expected` | What it would have accepted there. May be empty. |
| `Context` | Lines such as `"in object starting at 2:3"`, innermost first. May be empty. |

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
- `Recovered<T>` deconstructs: `var (value, diagnostics) = recovered;`.

### `default` results

Creole never returns `default(ParseResult<T>)`, but your own code can create one, for example as an element of a new array. A `default` result holds no case:

- A switch over it throws `SwitchExpressionException`.
- Its `Diagnostics` is empty.
- If your code might hold one, add a `null` arm. The compiler still reports missing cases.

The reasons are in [ADR-0002](../../work/docs/decisions/0002-parseresult-as-a-plain-union.md).

## `Diagnostics`

`Diagnostics` is an immutable, ordered collection of `ParseError`. It implements `IReadOnlyList<ParseError>`.

- Errors are ordered by source, then start offset, then end offset. Errors with equal spans keep the order they were given in. Sources are ordered as their `SourceRegistry` issued them, with `SourceId.Unnamed` first.
- `Diagnostics.Empty` has no errors.
- `new Diagnostics(errors)` sorts `errors`. It throws `ArgumentException` if one is null.
- Two collections are equal when they hold equal errors in the same order.

### Rendering

`Render()` writes one line per error, in order, joined by `\n`. `ParseError.ToString()` gives the same line for one error.

```text
error[lexer] at 4..7: number too large
error[parser] at 15..21: unexpected string "host"; expected ',' or '}'; in object starting at 2:1
error[parser] at 30..30: unexpected end of input; expected value, '[' or end of input; in array; in object starting at 1:1
```

- The offsets are character offsets, `CharStart..CharEnd`.
- The expected part is left out when nothing was expected. Context lines follow, innermost first.
- `Render()` shows no file names, lines or columns.
