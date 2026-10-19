# Primitives

Primitives are the leaves of a grammar: the parsers that read input directly. Every other combinator is built from them. The design is in the [whitepaper's § Types](../../whitepaper.md#types) and [§ Choice and backtracking](../../whitepaper.md#choice-and-backtracking).

> Nothing runs these parsers yet. They build grammar nodes, and the interpreter that executes them arrives with INT-03. Each member's XML documentation states the behaviour engines must implement; this page will describe it once it runs.

## Items

These are on `Grammar<TIn>`, so every grammar has them, whatever its input type.

| Parser | Matches | Produces |
| --- | --- | --- |
| `Any` | any one item | the item |
| `Token(x)` | one item equal to `x`, by `EqualityComparer<TIn>.Default` | the item read |
| `Satisfy(predicate)` | one item for which `predicate` returns `true` | the item |
| `End` | the end of input, without consuming anything | `Unit` |

```csharp
public sealed class Bytes : Grammar<byte>
{
    public static readonly Parser<byte, byte> Delete = Token(0x7F);
    public static readonly Parser<byte, byte> Positive = Satisfy(b => b > 0);
}
```

- Each one consumes at most one item. On anything else, or at the end of input, it fails without consuming input.
- `Any` and `End` are properties. Every use refers to the same parser.
- Put `End` last in a top-level rule to require that the rule reads the whole input.
- `Satisfy` runs your code, so analysis cannot tell which items it accepts and treats it as matching any item. Prefer `Token` or a character class where one fits. The predicate must not have side effects: an engine may call it more than once for the same item. A null predicate throws a `GrammarException`.

## Characters

`CharGrammar` adds character classes. Each consumes one character and produces it, and fails without consuming input on any other character or at the end of input.

| Parser | Matches |
| --- | --- |
| `Char(c)` | the character `c` |
| `Range(first, last)` | a character from `first` to `last`, inclusive |
| `NoneOf(c1, c2, ...)` | a character that is not one of those listed |
| `Digit` | a character for which `char.IsDigit` is true |
| `Letter` | a character for which `char.IsLetter` is true |
| `LetterOrDigit` | a character for which `char.IsLetterOrDigit` is true |
| `Whitespace` | a character for which `char.IsWhiteSpace` is true, including line breaks |

```csharp
public sealed class Lexer : CharGrammar
{
    public static readonly Parser<char, char> HexLetter = Range('a', 'f');
    public static readonly Parser<char, char> StringChar = NoneOf('"', '\\');
}
```

- The named classes are Unicode, as in .NET and Pidgin. `Digit` matches the Arabic-Indic digit three (U+0663), not only `0` to `9`. For ASCII digits only, use `Range('0', '9')`.
- Characters are UTF-16 code units. A character outside the Basic Multilingual Plane, such as most emoji, is a surrogate pair, and no named class matches either half.
- `Range` throws a `GrammarException` if `last` comes before `first`. `NoneOf()` with no characters matches any character.
- Inside a grammar class, `Char` and `String` name these combinators, not the `System.Char` and `System.String` types. Use the keywords `char` and `string` for the types.

## Strings

| Parser | Matches | Produces |
| --- | --- | --- |
| `String(text)` | the characters of `text`, in order | `text` itself, so a match allocates nothing |
| `CIString(text)` | the characters of `text`, ignoring case | the input as written: `CIString("select")` reading `SeLeCt` produces `"SeLeCt"` |

```csharp
public sealed class Operators : CharGrammar
{
    public static readonly Parser<char, string> LessOrEqual = String("<=");
    public static readonly Parser<char, string> Select = CIString("select");
}
```

- Both are atomic: if any character does not match, they fail without consuming input, so an enclosing choice tries its next alternative. On `abd`, `String("abc")` fails without consuming. Pidgin's `String` consumes `ab` in that case.
- `CIString` compares one character at a time by `char.ToLowerInvariant`, as Pidgin does, so the result does not depend on the current culture. The match is always as long as `text`. One consequence: the Kelvin sign (U+212A) matches `k`, but the long s (U+017F) does not match `s`.
- An empty or null `text` throws a `GrammarException`. To succeed without consuming input, use `Return("")`.
