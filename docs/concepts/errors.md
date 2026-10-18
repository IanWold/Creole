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
