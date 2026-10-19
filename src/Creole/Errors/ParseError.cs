using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace Creole;

/// <summary>
/// One error from a parse: where it happened, which layer raised it, what was found and what was expected.
/// </summary>
/// <param name="Span">Where the error is, in the original source.</param>
/// <param name="Layer">The name of the layer that raised the error, such as <c>lexer</c> or <c>parser</c>.</param>
/// <param name="Unexpected">What the parse found at <paramref name="Span"/>.</param>
/// <param name="Expected">What the parse would have accepted there, in the order it was recorded. May be empty.</param>
/// <param name="Context">The context lines for the error, innermost first, such as <c>in object starting at 2:3</c>. May be empty.</param>
/// <remarks>
/// <para>
/// An error is immutable: it copies <paramref name="Expected"/> and <paramref name="Context"/>, so later changes to the lists passed in do not affect it.
/// A <see langword="with"/> expression checks and copies the values it sets in the same way.
/// </para>
/// <para>
/// Two errors are equal when their span, layer and unexpected value are equal and their <paramref name="Expected"/> and <paramref name="Context"/> lists hold equal elements in the same order.
/// </para>
/// </remarks>
/// <exception cref="ArgumentNullException"><paramref name="Layer"/>, <paramref name="Expected"/> or <paramref name="Context"/> is null.</exception>
/// <exception cref="ArgumentException">
/// <paramref name="Unexpected"/> holds no case, or <paramref name="Expected"/> or <paramref name="Context"/> contains a null or a value that holds no case.
/// </exception>
public sealed record ParseError(
    SourceSpan Span,
    string Layer,
    Unexpected Unexpected,
    IReadOnlyList<Expectation> Expected,
    IReadOnlyList<string> Context
)
{
    /// <summary>
    /// The context lines for the error, innermost first. May be empty.
    /// </summary>
    public IReadOnlyList<string> Context { get; init => field = CopyContext(value); } = CopyContext(Context);

    /// <summary>
    /// What the parse would have accepted at <see cref="Span"/>, in the order it was recorded. May be empty.
    /// </summary>
    public IReadOnlyList<Expectation> Expected { get; init => field = CopyExpected(value); } = CopyExpected(Expected);

    /// <summary>
    /// The name of the layer that raised the error, such as <c>lexer</c> or <c>parser</c>.
    /// </summary>
    public string Layer { get; init => field = CheckLayer(value, nameof(Layer)); } = CheckLayer(Layer, nameof(Layer));

    /// <summary>
    /// What the parse found at <see cref="Span"/>.
    /// </summary>
    public Unexpected Unexpected { get; init => field = CheckUnexpected(value, nameof(Unexpected)); } = CheckUnexpected(Unexpected, nameof(Unexpected));

    private static string CheckLayer(string layer, string name) =>
        layer ?? throw new ArgumentNullException(name);

    private static Unexpected CheckUnexpected(Unexpected unexpected, string name) =>
        unexpected.Value is not null
            ? unexpected
            : throw new ArgumentException("The unexpected value must hold a case.", name);

    private static ReadOnlyCollection<T> Copy<T>(IReadOnlyList<T> items, string name, Func<T, bool> isValid)
    {
        ArgumentNullException.ThrowIfNull(items, name);

        T[] copy = [.. items];

        foreach (var item in copy)
        {
            if (!isValid(item))
            {
                throw new ArgumentException("The list must not contain a null or a value that holds no case.", name);
            }
        }

        return Array.AsReadOnly(copy);
    }

    private static ReadOnlyCollection<string> CopyContext(IReadOnlyList<string> context) =>
        Copy(context, nameof(Context), static line => line is not null);

    private static ReadOnlyCollection<Expectation> CopyExpected(IReadOnlyList<Expectation> expected) =>
        Copy(expected, nameof(Expected), static expectation => expectation.Value is not null);

    /// <summary>
    /// Describes <see cref="Expected"/> as a list, such as <c>value, '[' or end of input</c>. Empty when nothing was expected.
    /// </summary>
    internal string DescribeExpected()
    {
        var builder = new StringBuilder();

        for (var i = 0; i < Expected.Count; i++)
        {
            builder.Append(i == 0 ? "" : i == Expected.Count - 1 ? " or " : ", ");
            builder.Append(Expected[i] switch
            {
                Label label => label.Name,
                ExpectedItem item => item.Display,
                EndOfInput => "end of input",
            });
        }

        return builder.ToString();
    }

    /// <summary>
    /// Describes <see cref="Unexpected"/>, such as <c>unexpected string "host"</c>. A <see cref="CustomMessage"/> is its text alone.
    /// </summary>
    internal string DescribeUnexpected() => Unexpected switch
    {
        UnexpectedItem item => $"unexpected {item.Display}",
        EndOfInput => "unexpected end of input",
        CustomMessage message => message.Text,
    };

    /// <inheritdoc/>
    public bool Equals(ParseError? other) =>
        other is not null
        && Span == other.Span
        && Layer == other.Layer
        && Equals(Unexpected.Value, other.Unexpected.Value)
        && Expected.Select(e => e.Value).SequenceEqual(other.Expected.Select(e => e.Value))
        && Context.SequenceEqual(other.Context);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(Span);
        hash.Add(Layer);
        hash.Add(Unexpected.Value);

        foreach (var expectation in Expected)
        {
            hash.Add(expectation.Value);
        }

        foreach (var line in Context)
        {
            hash.Add(line);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var builder = new StringBuilder();

        builder.Append(CultureInfo.InvariantCulture, $"error[{Layer}] at {Span.CharStart}..{Span.CharEnd}: ");
        builder.Append(DescribeUnexpected());

        if (Expected.Count > 0)
        {
            builder.Append("; expected ").Append(DescribeExpected());
        }

        foreach (var line in Context)
        {
            builder.Append("; ").Append(line);
        }

        return builder.ToString();
    }
}
