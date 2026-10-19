using System.Collections;
using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// The errors from one parse, ordered by span.
/// </summary>
/// <remarks>
/// <para>
/// Errors are ordered by source, then by start offset, then by end offset. Errors with equal spans keep the order  they were given in.
/// Sources are ordered as their <see cref="SourceRegistry"/> issued them, with <see cref="SourceId.Unnamed"/> first.
/// </para>
/// <para>
/// A collection is immutable. Two collections are equal when they hold equal errors in the same order.
/// </para>
/// </remarks>
public sealed class Diagnostics : IReadOnlyList<ParseError>, IEquatable<Diagnostics>
{
    private readonly ParseError[] _errors;

    /// <summary>
    /// The number of errors.
    /// </summary>
    public int Count =>
        _errors.Length;

    /// <summary>
    /// A collection with no errors.
    /// </summary>
    public static Diagnostics Empty { get; } = new([]);

    /// <summary>
    /// The error at <paramref name="index"/>, in span order.
    /// </summary>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is negative or not less than <see cref="Count"/>.</exception>
    public ParseError this[int index] =>
        _errors[index];

    /// <summary>
    /// Creates a collection of <paramref name="errors"/>, ordered by span.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="errors"/> contains a null error.</exception>
    public Diagnostics(IEnumerable<ParseError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        _errors = [.. errors
            .OrderBy(error => error?.Span.Source.Value)
            .ThenBy(error => error?.Span.Start)
            .ThenBy(error => error?.Span.End)
        ];

        if (Array.IndexOf(_errors, null) >= 0)
        {
            throw new ArgumentException("A collection of errors cannot contain null.", nameof(errors));
        }
    }

    /// <summary>
    /// Returns <paramref name="diagnostics"/>, or throws if it is null or empty.
    /// </summary>
    internal static Diagnostics RequireAny(Diagnostics diagnostics, [CallerArgumentExpression(nameof(diagnostics))] string? name = null)
    {
        ArgumentNullException.ThrowIfNull(diagnostics, name);

        return diagnostics.Count > 0
            ? diagnostics
            : throw new ArgumentException("A result that is not Parsed must carry at least one error.", name);
    }

    /// <inheritdoc/>
    public bool Equals(Diagnostics? other) =>
        other is not null && _errors.AsSpan().SequenceEqual(other._errors);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as Diagnostics);

    /// <inheritdoc/>
    public IEnumerator<ParseError> GetEnumerator() =>
        ((IEnumerable<ParseError>)_errors).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var error in _errors)
        {
            hash.Add(error);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Writes the errors one per line, in span order, such as <c>error[parser] at 15..21: unexpected string "host"; expected ',' or '}'</c>.
    /// </summary>
    /// <returns>The lines joined by <c>\n</c>, or an empty string when there are no errors.</returns>
    /// <remarks>
    /// Each line is the error's <see cref="ParseError.ToString"/>. Offsets are character offsets (<see cref="SourceSpan.CharStart"/> to <see cref="SourceSpan.CharEnd"/>).
    /// </remarks>
    public string Render() =>
        string.Join('\n', (IEnumerable<ParseError>)_errors);
}
