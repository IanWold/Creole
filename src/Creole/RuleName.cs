namespace Creole;

/// <summary>
/// Derives a rule's name from the caller information <c>Rec</c> captures (ADR-0009).
/// </summary>
internal static class RuleName
{
    /// <summary>
    /// Returns the rule name for a <c>Rec</c>, or null if neither argument gives one.
    /// </summary>
    /// <param name="referenceExpression">
    /// The caller's expression for the reference.
    /// If it is a parameterless lambda whose body is a member reference, such as <c>() =&gt; Value</c> or <c>static () =&gt; JsonGrammar.Value</c>, the body is the name.
    /// </param>
    /// <param name="memberName">
    /// The member that called <c>Rec</c>, used when the expression gives no name.
    /// In a field initializer this is the field.
    /// </param>
    public static string? From(string referenceExpression, string memberName) =>
        ReferencedMember(referenceExpression) ?? (IsMemberReference(memberName) ? memberName : null);

    private static bool IsIdentifier(ReadOnlySpan<char> text)
    {
        if (text.StartsWith('@'))
        {
            text = text[1..];
        }

        if (text.IsEmpty || !(char.IsLetter(text[0]) || text[0] == '_'))
        {
            return false;
        }

        foreach (var c in text)
        {
            if (!(char.IsLetterOrDigit(c) || c == '_'))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsMemberReference(ReadOnlySpan<char> text)
    {
        foreach (var range in text.Split('.'))
        {
            if (!IsIdentifier(text[range]))
            {
                return false;
            }
        }

        return !text.IsEmpty;
    }

    private static string? ReferencedMember(string expression)
    {
        var text = expression.AsSpan().Trim();

        if (text.StartsWith("static") && text.Length > 6 && char.IsWhiteSpace(text[6]))
        {
            text = text[6..].TrimStart();
        }

        if (!text.StartsWith("()"))
        {
            return null;
        }

        text = text[2..].TrimStart();

        if (!text.StartsWith("=>"))
        {
            return null;
        }

        text = text[2..].Trim();

        return IsMemberReference(text) ? text.ToString() : null;
    }
}
