# The cursor

`Cursor<T>` is how Creole reads input. It marks a position in the input and reads items from there without allocating. Why it is shaped this way is explained in the whitepaper's [§ Interpreted engine](../../whitepaper.md#interpreted-engine) and [§ Results](../../whitepaper.md#results).

Only Creole creates cursors; there is no public constructor. Code that reads input takes the cursor as `ref Cursor<T>`. Always pass it by `ref`: a copy is an independent position, so moving a copy does not move the original.

## Positions

`Position` is a `long` that counts the items before the cursor, starting from the beginning of the input. Positions are absolute even when the input arrives in pieces, so you can compare two positions or subtract one from another to get a length.

A step consumed input if and only if it moved the cursor. This is how Creole tells an empty failure from a committed one ([§ Results](../../whitepaper.md#results)), so reading code that fails must leave the cursor where it found it.

## Reading

| Member | Does |
| --- | --- |
| `TryPeek(out T item)` | Reads the item at the cursor without moving. Returns `false` at the end of the input. |
| `TryRead(out T item)` | Reads the item at the cursor and moves past it. Returns `false` at the end of the input, and then does not move. |
| `Advance(int count)` | Moves past `count` items. Throws `ArgumentOutOfRangeException` if `count` is negative or fewer than `count` items remain. |
| `Available` | The items from the cursor to the end of the current segment, as a `ReadOnlySpan<T>`. |
| `IsAtEnd` | Whether the cursor is at the end of the input. |

None of these allocate.

## Bulk scans with `Available`

Input may be held in several segments. `Available` holds only the rest of the current segment, so more input may follow it. It is empty only at the end of the input. To scan a run of items, search `Available`, advance, and repeat until the run ends.

This method reads an identifier with a vectorized `SearchValues` scan, and fails without moving if the input does not start with one. The caller reads the identifier's extent from `Position` before and after:

```csharp
private static readonly SearchValues<char> _identifierChars = SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_");

private static bool ScanIdentifier(ref Cursor<char> c)
{
    if (!c.TryPeek(out var first) || !(char.IsAsciiLetter(first) || first == '_'))
    {
        return false;
    }

    c.Advance(1);

    while (c.Available is { IsEmpty: false } available)
    {
        var length = available.IndexOfAnyExcept(_identifierChars);

        if (length >= 0)
        {
            c.Advance(length);
            return true;
        }

        c.Advance(available.Length);
    }

    return true;
}
```

The sample is tested in [CursorTests.cs](../../tests/Creole.Tests/CursorTests.cs).
