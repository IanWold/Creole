# Custom scanners

Some tokens need hand-written code. C# raw string literals and Rust's `r#"…"#` strings count their delimiters, which no combinator can do. `Custom(scanner)` turns an `IScanner<TIn, TOut>` into a parser. The design is in the [whitepaper's § Custom primitives](../../whitepaper.md#custom-primitives), and the choices behind `ScannerInfo` are in [ADR-0012](../../work/docs/decisions/0012-custom-scanner-metadata.md).

> The interpreter does not run `Custom` yet; that arrives with INT-04. `Custom` builds the grammar node, and a scanner can be called directly on a cursor.

## Writing a scanner

A scanner has two members:

| Member | Does |
| --- | --- |
| `ScannerInfo Info` | Declares whether the scanner can match empty input, which characters it can start with, and what error messages call it. |
| `bool Scan(ref Cursor<TIn> cursor, out TOut value)` | Reads one token. On success it moves the cursor past the token and returns `true`. On failure it returns `false` and leaves the cursor where it was. |

This scanner reads Rust-style raw strings: `r`, any number of `#`, a quote, the text, then a quote followed by the same number of `#`. It is taken from the tests ([RawStringSampleTests.cs](../../tests/Creole.Tests/Samples/RawStringSampleTests.cs)).

```csharp
readonly record struct RawStringLiteral(string Text, bool IsTerminated);

sealed class RawStringScanner : IScanner<char, RawStringLiteral>
{
    public ScannerInfo Info { get; } = new(Nullable: false, First: CharSet.Of('r'), Name: "raw string");

    // Moves past hashes '#' characters if they all follow, and otherwise leaves the cursor where it was.
    private static bool TryReadHashes(ref Cursor<char> cursor, int hashes)
    {
        var c = cursor;

        for (var i = 0; i < hashes; i++)
        {
            if (!c.TryRead(out var hash) || hash != '#')
            {
                return false;
            }
        }

        cursor = c;
        return true;
    }

    // Scans a copy of the cursor and moves the real one only on success, so a failure consumes nothing.
    public bool Scan(ref Cursor<char> cursor, out RawStringLiteral value)
    {
        var c = cursor;
        value = default;

        if (!c.TryRead(out var r) || r != 'r')
        {
            return false;
        }

        var hashes = 0;

        while (c.TryPeek(out var hash) && hash == '#')
        {
            c.Advance(1);
            hashes++;
        }

        if (!c.TryRead(out var quote) || quote != '"')
        {
            return false;
        }

        // The text is copied: a token can cross a segment boundary, and the cursor only exposes spans.
        var text = new StringBuilder();

        while (c.TryRead(out var next))
        {
            if (next == '"' && TryReadHashes(ref c, hashes))
            {
                cursor = c;
                value = new(text.ToString(), IsTerminated: true);
                return true;
            }

            text.Append(next);
        }

        // Unterminated: succeed with the text so far, so the grammar can report the error where the input ends.
        cursor = c;
        value = new(text.ToString(), IsTerminated: false);
        return true;
    }
}
```

The rules a scanner must follow:

- **Fail without consuming input.** Creole tells an empty failure from a committed one by whether the cursor moved ([The cursor](../engines/cursor.md)). Scan a copy of the cursor, as above, and assign it back only on success.
- **Copy text that may cross segments.** Input can arrive in several segments, and the cursor exposes only spans, so a token's text cannot be returned as a slice of the input. Copy it, as the `StringBuilder` does above.
- **Keep the declaration true.** Analysis trusts `Info`. A scanner that matches empty input after declaring `Nullable: false`, or starts with a character outside `First`, makes analysis wrong.

## `ScannerInfo`

`ScannerInfo(bool Nullable, CharSet? First = null, string? Name = null)` is what the scanner declares.

| Field | Meaning | If null |
| --- | --- | --- |
| `Nullable` | Whether the scanner can succeed without consuming input. | — |
| `First` | The characters the scanner can start with, as a [`CharSet`](primitives.md#character-sets). Only for a scanner over `char`. | The scanner can start with any item. Analysis treats it like `Satisfy`: it matches anything, and analysis can say little about rules that use it. |
| `Name` | What error messages call the token, such as `"raw string"`. | A failed scanner expects the characters of `First`, such as `'r'`. With no `First` either, it adds no expectation. |

Give a character scanner a `First` whenever you can: it keeps analysis precise for every rule that uses the scanner. A scanner over tokens leaves `First` null. Give a `Name` too, or wrap the parser in `Named`; "expected 'r'" is rarely the message you want.

`Custom` reads `Info` once, when it builds the node. Changing what `Info` returns afterwards has no effect on that parser. It throws a `GrammarException` when:

- the scanner is null. That is usually a scanner field declared after the rule that uses it. Declare the scanner first; `Rec` defers parsers, not scanners.
- `Info` is null;
- `First` is given but the input type is not `char`;
- `Name` is empty or white space.

## Using a scanner in a grammar

`Custom` is a member of `Grammar<TIn>`, so a grammar class calls it unqualified. The parser it returns works with every other combinator, `Longest` included:

```csharp
sealed class RawStringLexer : CharGrammar
{
    public static readonly Parser<char, RawStringLiteral> RawString = Custom(new RawStringScanner());

    public static readonly Parser<char, Token> Identifier = LetterOrDigit.AtLeastOnce().Text().Select(text => new Token(TokenKind.Identifier, text));

    public static readonly Parser<char, Token> RawStringToken = RawString.Select(s => new Token(s.IsTerminated ? TokenKind.RawString : TokenKind.UnterminatedRawString, s.Text));

    // On "r#abc", the scanner fails without consuming and Identifier reads "r".
    public static readonly Parser<char, Token> AnyToken = Longest(RawStringToken, Identifier);

    // Longest gives back the input of an alternative that fails, so the check goes after it.
    // The failure follows consumed input, so it is committed and reported where the string ends.
    public static readonly Parser<char, Token> CheckedToken = AnyToken.Bind(t => t.Kind == TokenKind.UnterminatedRawString ? Fail<Token>("unterminated raw string") : Return(t));
}
```

## Reporting errors from a scanner

`Scan` only says yes or no. A failure is reported at the position where the scanner started, as "expected raw string". It cannot say why.

To report something more specific, such as an unterminated string, the scanner succeeds instead. It consumes the malformed text and returns a value that marks it, as `IsTerminated: false` does above. The grammar then checks the value with [`Bind`](sequencing.md#query-syntax) and calls `Fail(message)`. The scanner consumed input, so the failure is committed and points at the end of the malformed text.

Put the check after any `Longest` that holds the scanner, as `CheckedToken` does. `Longest` gives back the input of an alternative that fails, even a committed failure. A check inside the alternative would lose its message to the next-longest match, here `Identifier` reading `r`.
