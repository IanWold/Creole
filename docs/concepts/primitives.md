# Primitives

Primitives are the leaves of a grammar: the parsers that read input directly. Every other combinator is built from them. The design is in the [whitepaper's § Types](../../whitepaper.md#types) and [§ Choice and backtracking](../../whitepaper.md#choice-and-backtracking).

> The interpreter runs these parsers as this page describes. The public method that runs a parser on your input arrives with INT-08.

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
- `Satisfy` runs your code, so analysis cannot tell which items it accepts and treats it as matching any item. Prefer `Token`, a character class or `Kind` where one fits. The predicate must not have side effects: an engine may call it more than once for the same item. A null predicate throws a `GrammarException`.

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

### Character sets

`CharSet` is an immutable set of UTF-16 code units. Every character class above holds one, and analysis reads it to learn which characters a rule can start with.

| Member | Gives |
| --- | --- |
| `CharSet.Of(c1, c2, ...)` | the listed characters; order and repeats do not matter |
| `CharSet.Range(first, last)` | the characters from `first` to `last`, inclusive; throws a `GrammarException` if `last` comes before `first` |
| `CharSet.Digit`, `Letter`, `LetterOrDigit`, `Whitespace` | the same characters as the class of that name |
| `CharSet.All`, `CharSet.Empty` | every code unit, and none |
| `a.Union(b)`, `a.Complement()` | the characters in either set, and the characters not in `a` |
| `a.Contains(c)`, `a.IsEmpty` | whether `c` is in `a`, and whether `a` has no characters |

```csharp
var hexDigit = CharSet.Range('0', '9').Union(CharSet.Range('a', 'f')).Union(CharSet.Range('A', 'F'));

hexDigit.ToString();   // "[0-9A-Fa-f]"
```

Two sets are equal when they hold the same characters, however they were built: `CharSet.Of('b', 'c', 'a')` equals `CharSet.Range('a', 'c')`. `ToString` writes the set in character-class notation, with characters outside printable ASCII as `\u` escapes.

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

## Tokens

`TokenGrammar<TToken, TKind>` adds two ways to match one token by what kind it is.

| Parser | Matches | Produces |
| --- | --- | --- |
| `Kind(k)` | one token whose `Kind` is `k` | the token |
| `Case<T>()` | one token of a union token type whose case is `T` | the case value, typed `T` |

```csharp
public enum J { String, Number, Comma }

public readonly record struct JTok(J Kind, string Text) : IKinded<J>;

public sealed class JsonTokens : TokenGrammar<JTok, J>
{
    public static readonly Parser<JTok, JTok> Comma = Kind(J.Comma);
}
```

- Both fail without consuming input on a token of another kind or case, or at the end of input.
- Kinds make analysis precise: two `Kind` parsers with different kinds never match the same token.
- `Case<T>()` is for tokens declared as a C# union, and it throws a `GrammarException` if `TToken` is not a union. It does not check that `T` is one of the union's cases; a parser for a type that is not a case never matches.
- A union token must still implement `IKinded<TKind>` to be used with `TokenGrammar`:

  ```csharp
  public sealed record Ident(string Name);
  public sealed record Number(int Value);

  public union Tok(Ident, Number) : IKinded<J>
  {
      public J Kind => this switch { Ident => J.String, Number => J.Number };
  }

  public sealed class UnionTokens : TokenGrammar<Tok, J>
  {
      public static readonly Parser<Tok, Ident> Name = Case<Ident>();
  }
  ```

- Analysis treats each case type as a kind, but it cannot tell which `TKind` a case has. It assumes `Kind(k)` and `Case<T>()` may match the same token.
- A union stores its case as an `object`, so struct cases box. For the fastest lexers, prefer struct tokens matched with `Kind` ([whitepaper § Where unions are not used](../../whitepaper.md#where-unions-are-not-used)).

### How errors show tokens

A token type says how error messages show it, through two members of `IKinded<TKind>` that have defaults:

| Member | Default | Used for |
| --- | --- | --- |
| `static virtual string Describe(TKind kind)` | the kind's enum name | an expected kind, and the start of an unexpected token |
| `string? Detail { get; }` | `null` | what follows the kind's name when the token is unexpected, such as its text |

Override them on the token type with explicit implementations:

```csharp
public readonly record struct JTok(J Kind, string Text) : IKinded<J>
{
    static string IKinded<J>.Describe(J kind) => kind switch
    {
        J.Comma => "','",
        J.String => "string",
        _ => kind.ToString(),
    };

    string? IKinded<J>.Detail => Kind == J.String ? Text : null;
}
```

With these, a message reads `expected ','`, and an unexpected string token reads `unexpected string "host"`: the kind's name, a space, then the detail. A comma, with no detail, reads `unexpected ','`.

- Engines call both only when they build an error, never while parsing, so `Describe` may read the current UI culture.
- `Describe` must not throw. A null or empty result is replaced by the kind's enum name.
- A union token overrides them the same way, inside its declaration.
- A struct token that keeps the default `Detail` is boxed for the call. That happens only when an error is built.

The interpreter does not record failures yet, so nothing calls these members until the error model's failure tracking arrives (ERR-02). The reasons are in [ADR-0023](../../work/docs/decisions/0023-token-types-describe-their-kinds.md).
