using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// How errors show an input item of type <typeparamref name="T"/>.
/// </summary>
/// <remarks>
/// A character shows quoted. A token type that a token grammar has been built over shows as <see cref="Display.Token{TToken, TKind}"/> does.
/// Any other item shows as its <see cref="object.ToString"/>.
/// </remarks>
internal static class Display<T>
{
    /// <summary>
    /// How a token grammar over <typeparamref name="T"/> shows its tokens, or null if none has been built.
    /// </summary>
    public static Func<T, string>? Tokens { get; set; }

    /// <summary>
    /// <paramref name="item"/> as an error shows it.
    /// </summary>
    public static string Item(T item)
    {
        if (typeof(T) == typeof(char))
        {
            return Display.Character(Unsafe.As<T, char>(ref item));
        }

        return Tokens is { } tokens
            ? tokens(item)
            : item?.ToString() ?? "null";
    }
}
