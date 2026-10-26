using System.Diagnostics.CodeAnalysis;

namespace Creole;

/// <summary>
/// Hand-written code that reads one token, for tokens no combinator can describe, such as raw string literals that count their delimiters.
/// Turn it into a parser with <see cref="Grammar{TIn}.Custom{TOut}(IScanner{TIn, TOut}, string)"/>.
/// </summary>
/// <typeparam name="TIn">The input item type, such as <see cref="char"/> or a token type.</typeparam>
/// <typeparam name="TOut">The type of value the scanner produces.</typeparam>
/// <remarks>
/// <para>
/// A scanner must fail without consuming input: when <see cref="Scan"/> returns <see langword="false"/>, the cursor must be where it was.
/// The simplest way is to scan a copy of the cursor and assign the copy back only on success.
/// </para>
/// <para>
/// Analysis and engines trust <see cref="Info"/>. A scanner that breaks its declaration makes analysis wrong.
/// </para>
/// <para>
/// <see cref="Scan"/> must be deterministic: at the same position in the same input it gives the same answer every time, because an engine may call it more than once there.
/// It should keep no state between calls; state a parse needs belongs in the grammar.
/// </para>
/// </remarks>
public interface IScanner<TIn, TOut>
{
    /// <summary>
    /// What the scanner declares about itself: whether it can match empty input, which characters it can start with, and its name in error messages.
    /// </summary>
    /// <remarks>
    /// <c>Custom</c> reads it once, when the grammar node is built. Changing it later has no effect on that node.
    /// </remarks>
    ScannerInfo Info { get; }

    /// <summary>
    /// Reads one token at the cursor.
    /// </summary>
    /// <param name="cursor">The input. On success, move it past the token. On failure, leave it where it was.</param>
    /// <param name="value">The token's value, when the scan succeeds.</param>
    /// <returns><see langword="true"/> if a token was read.</returns>
    bool Scan(ref Cursor<TIn> cursor, [MaybeNullWhen(false)] out TOut value);
}
