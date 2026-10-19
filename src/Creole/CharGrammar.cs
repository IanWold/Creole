using System.Diagnostics.CodeAnalysis;

namespace Creole;

/// <summary>
/// The base class for grammars over characters, such as lexers.
/// </summary>
/// <remarks>
/// <para>
/// It adds character classes and string literals to the combinators of <see cref="Grammar{TIn}"/>.
/// Every character class consumes one character and produces it, and fails without consuming input on any other character or at the end of input.
/// </para>
/// <para>
/// Characters are UTF-16 code units. A character outside the Basic Multilingual Plane is two code units (a surrogate pair), and no named class matches either half.
/// </para>
/// <para>
/// Inside a class derived from this one, <c>Char</c> and <c>String</c> name these methods, not <see cref="char"/> and <see cref="string"/>.
/// Use the keywords <c>char</c> and <c>string</c> for the types.
/// </para>
/// </remarks>
public abstract class CharGrammar : Grammar<char>
{
    /// <summary>
    /// A parser that consumes one decimal digit, as <see cref="char.IsDigit(char)"/> defines it.
    /// </summary>
    /// <remarks>
    /// This is every Unicode decimal digit, such as the Arabic-Indic digit three (U+0663), not only <c>0</c> to <c>9</c>.
    /// For ASCII digits only, use <c>Range('0', '9')</c>.
    /// </remarks>
    public static Parser<char, char> Digit { get; } = new CharClassNode(CharSet.Digit, "digit");

    /// <summary>
    /// A parser that consumes one letter, as <see cref="char.IsLetter(char)"/> defines it.
    /// </summary>
    public static Parser<char, char> Letter { get; } = new CharClassNode(CharSet.Letter, "letter");

    /// <summary>
    /// A parser that consumes one letter or decimal digit, as <see cref="char.IsLetterOrDigit(char)"/> defines it.
    /// </summary>
    public static Parser<char, char> LetterOrDigit { get; } = new CharClassNode(CharSet.LetterOrDigit, "letter or digit");

    /// <summary>
    /// A parser that consumes one whitespace character, as <see cref="char.IsWhiteSpace(char)"/> defines it.
    /// </summary>
    /// <remarks>
    /// This includes line breaks.
    /// </remarks>
    public static Parser<char, char> Whitespace { get; } = new CharClassNode(CharSet.Whitespace, "whitespace");

    /// <summary>
    /// Creates a grammar. Grammar classes hold static rules and are not usually instantiated.
    /// </summary>
    protected CharGrammar() { }

    /// <summary>
    /// A parser that consumes the character <paramref name="c"/> and produces it.
    /// </summary>
    /// <param name="c">The character to match.</param>
    /// <returns>A parser that fails without consuming input on any other character.</returns>
    [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "The whitepaper's combinator names, shared with Pidgin and Parsec; grammar authors read Char('a') and String(\"if\") as grammar notation.")]
    public static Parser<char, char> Char(char c) => new CharClassNode(CharSet.Of(c), null);

    /// <summary>
    /// A parser that consumes the characters of <paramref name="text"/>, ignoring case, and produces the input as written.
    /// </summary>
    /// <param name="text">The characters to match. Must not be empty.</param>
    /// <returns>A parser that fails without consuming input if any character does not match.</returns>
    /// <exception cref="GrammarException"><paramref name="text"/> is null or empty.</exception>
    /// <remarks>
    /// Characters are compared one at a time by <see cref="char.ToLowerInvariant(char)"/>, so the result does not depend on the current culture.
    /// The match always has the same length as <paramref name="text"/>.
    /// The result is the input's own characters: <c>CIString("select")</c> reading <c>SeLeCt</c> produces <c>"SeLeCt"</c>.
    /// </remarks>
    public static Parser<char, string> CIString(string text) => new StringNode(text, ignoreCase: true, nameof(CIString));

    /// <summary>
    /// A parser that consumes one character that is not in <paramref name="chars"/>, and produces it.
    /// </summary>
    /// <param name="chars">The characters to reject. With none, the parser matches any character.</param>
    /// <returns>A parser that fails without consuming input on a character in <paramref name="chars"/>, or at the end of input.</returns>
    public static Parser<char, char> NoneOf(params ReadOnlySpan<char> chars) => new CharClassNode(CharSet.Of(chars).Complement(), null);

    /// <summary>
    /// A parser that consumes one character from <paramref name="first"/> to <paramref name="last"/>, inclusive, and produces it.
    /// </summary>
    /// <param name="first">The lowest character to match.</param>
    /// <param name="last">The highest character to match.</param>
    /// <returns>A parser that fails without consuming input on a character outside the range.</returns>
    /// <exception cref="GrammarException"><paramref name="last"/> comes before <paramref name="first"/>.</exception>
    public static Parser<char, char> Range(char first, char last) => new CharClassNode(CharSet.Range(first, last), null);

    /// <summary>
    /// A parser that consumes the characters of <paramref name="text"/>, in order, and produces <paramref name="text"/>.
    /// </summary>
    /// <param name="text">The characters to match. Must not be empty.</param>
    /// <returns>A parser that fails without consuming input if any character does not match.</returns>
    /// <exception cref="GrammarException"><paramref name="text"/> is null or empty.</exception>
    /// <remarks>
    /// The parser is atomic: on <c>abd</c>, <c>String("abc")</c> fails without consuming input, so an enclosing choice can try its next alternative.
    /// Characters are compared by code unit. The result is <paramref name="text"/> itself, so a match allocates nothing.
    /// </remarks>
    [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "The whitepaper's combinator names, shared with Pidgin and Parsec; grammar authors read Char('a') and String(\"if\") as grammar notation.")]
    public static Parser<char, string> String(string text) => new StringNode(text, ignoreCase: false, nameof(String));
}
