using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// Reads one union token whose case is <typeparamref name="TCase"/>, and produces the case value.
/// Otherwise, or at the end of the input, fails without consuming.
/// </summary>
/// <remarks>
/// <see cref="CaseNode{TToken, TCase}"/> has already checked that <typeparamref name="TToken"/> is a union.
/// A struct token is boxed to read its case.
/// </remarks>
internal sealed class CasePlan<TToken, TCase> : Plan<TToken, TCase>
{
    public override bool TryParse(ref Cursor<TToken> cursor, ref TCase result)
    {
        if (!cursor.TryPeek(out var token) || token is not IUnion { Value: TCase value })
        {
            return false;
        }

        cursor.Advance(1);
        result = value;
        return true;
    }
}
