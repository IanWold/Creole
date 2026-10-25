using System.Globalization;
using System.Text;

namespace Creole;

/// <summary>
/// How errors show characters, strings, character classes and tokens.
/// </summary>
/// <remarks>
/// Quote marks, backslashes and characters that print nothing or move the cursor, such as controls, format characters and lone surrogates, are escaped as in C#.
/// So a message can never carry a terminal escape sequence.
/// </remarks>
internal static class Display
{
    private static void Append(StringBuilder builder, char c, char quote)
    {
        switch (c)
        {
            case '\\':
                builder.Append(@"\\");
                break;
            case '\0':
                builder.Append(@"\0");
                break;
            case '\n':
                builder.Append(@"\n");
                break;
            case '\r':
                builder.Append(@"\r");
                break;
            case '\t':
                builder.Append(@"\t");
                break;
            default:
                if (c == quote)
                {
                    builder.Append('\\').Append(c);
                }
                else if (IsHidden(c))
                {
                    builder.Append(CultureInfo.InvariantCulture, $"\\u{(int)c:X4}");
                }
                else
                {
                    builder.Append(c);
                }

                break;
        }
    }

    private static bool IsHidden(char c) =>
        char.GetUnicodeCategory(c) is UnicodeCategory.Control
            or UnicodeCategory.Format
            or UnicodeCategory.Surrogate
            or UnicodeCategory.PrivateUse
            or UnicodeCategory.OtherNotAssigned
            or UnicodeCategory.LineSeparator
            or UnicodeCategory.ParagraphSeparator;

    private static string Part(CharSet set) =>
        Single(set) is { } c
        ? Character(c)
        : set.ToString();

    private static char? Single(CharSet set) =>
        set.Keys.Ranges is [var (first, last)] && first == last
        ? (char)first
        : null;

    private static long Size(CharSet set)
    {
        var size = 0L;

        foreach (var (first, last) in set.Keys.Ranges)
        {
            size += last - first + 1;
        }

        return size;
    }

    /// <summary>
    /// A character in single quotes, such as <c>'a'</c> or <c>'\n'</c>.
    /// </summary>
    public static string Character(char c)
    {
        var builder = new StringBuilder("'");

        Append(builder, c, '\'');

        return builder.Append('\'').ToString();
    }

    /// <summary>
    /// A character class as one phrase: <c>'a'</c> for one character, <c>any character</c> for all of them,
    /// <c>any character except '"'</c> when the complement is smaller, and otherwise class notation such as <c>[0-9A-Fa-f]</c>.
    /// </summary>
    public static string CharacterSet(CharSet set)
    {
        if (Single(set) is { } c)
        {
            return Character(c);
        }

        var complement = set.Complement();

        return complement.IsEmpty
            ? "any character"
            : Size(complement) < Size(set)
            ? $"any character except {Part(complement)}"
            : set.ToString();
    }

    /// <summary>
    /// What a token type calls <paramref name="kind"/>, or the kind's enum name if it names it nothing.
    /// </summary>
    public static string Kind<TToken, TKind>(TKind kind)
        where TToken : IKinded<TKind>
        where TKind : struct, Enum =>
        TToken.Describe(kind) is { Length: > 0 } name
        ? name
        : kind.ToString();

    /// <summary>
    /// A string in double quotes, such as <c>"let"</c>.
    /// </summary>
    public static string Text(string text)
    {
        var builder = new StringBuilder("\"");

        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                builder.Append(text[i]).Append(text[++i]);
            }
            else
            {
                Append(builder, text[i], '"');
            }
        }

        return builder.Append('"').ToString();
    }

    /// <summary>
    /// A type's name as written in source, such as <c>Ident</c>: without the arity of a generic type or the prefix the compiler gives a <see langword="file"/> type.
    /// </summary>
    public static string TypeName(Type type)
    {
        var name = type.Name;
        var file = name.LastIndexOf("__", StringComparison.Ordinal);

        if (name.StartsWith('<') && file >= 0)
        {
            name = name[(file + 2)..];
        }

        var arity = name.IndexOf('`', StringComparison.Ordinal);

        return arity < 0
            ? name
            : name[..arity];
    }

    /// <summary>
    /// A token as an error shows it when it is unexpected: its kind's name, then a space and its detail if it has one.
    /// </summary>
    public static string Token<TToken, TKind>(TToken token)
        where TToken : IKinded<TKind>
        where TKind : struct, Enum
    {
        var name = Kind<TToken, TKind>(token.Kind);

        return token.Detail is { Length: > 0 } detail
            ? $"{name} {detail}"
            : name;
    }
}
