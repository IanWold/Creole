using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;

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
    /// Writes the errors one per line, in span order, such as <c>error[parser] at 15..21: unexpected string "host"; expected ',' or '}'; in object starting at 2</c>.
    /// </summary>
    /// <returns>The lines joined by <c>\n</c>, or an empty string when there are no errors.</returns>
    /// <remarks>
    /// Each line is the error's <see cref="ParseError.ToString"/>. Offsets are character offsets (<see cref="SourceSpan.CharStart"/> to <see cref="SourceSpan.CharEnd"/>), and a context line shows the character offset where its construct started.
    /// To show errors against the lines they point into, pass the source texts to <see cref="Render(ReadOnlySpan{SourceText})"/>.
    /// </remarks>
    public string Render() =>
        string.Join('\n', (IEnumerable<ParseError>)_errors);

    /// <summary>
    /// Writes each error against the line of source text it points into, such as:
    /// <code>
    /// error[parser]: expected ',' or '}'
    ///  --> config.json:3:16
    ///   |
    /// 3 |   "port": 8080 "host": "x"
    ///   |                ^^^^^^ unexpected string "host"
    ///   = note: in object starting at 2:1
    /// </code>
    /// </summary>
    /// <param name="sources">The text of every source the errors point into, each with a different <see cref="SourceText.Id"/>.</param>
    /// <returns>The errors in span order, separated by blank lines, or an empty string when there are no errors.</returns>
    /// <remarks>
    /// Columns count Unicode scalar values. Carets are aligned for a terminal: by grapheme cluster, with wide characters as two columns and tabs expanded to stops of four.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="sources"/> contains a null or a repeated id, lacks a source an error or its context points into, or has a text shorter than an error's span or than where a context line starts.
    /// </exception>
    public string Render(params ReadOnlySpan<SourceText> sources)
    {
        var byId = new Dictionary<SourceId, SourceText>(sources.Length);

        foreach (var source in sources)
        {
            if (source is null)
            {
                throw new ArgumentException("The source texts cannot contain null.", nameof(sources));
            }

            if (!byId.TryAdd(source.Id, source))
            {
                throw new ArgumentException($"Two source texts have the id {source.Id}.", nameof(sources));
            }
        }

        var builder = new StringBuilder();

        foreach (var error in _errors)
        {
            if (!byId.TryGetValue(error.Span.Source, out var source))
            {
                throw new ArgumentException($"No source text was given for {error.Span.Source}, which an error points into.", nameof(sources));
            }

            if (error.Span.CharEnd > source.Text.Length)
            {
                throw new ArgumentException($"An error's span ends at character {error.Span.CharEnd}, past the end of the text for {error.Span.Source}, which has {source.Text.Length} characters.", nameof(sources));
            }

            foreach (var line in error.Context)
            {
                if (!byId.TryGetValue(line.Start.Source, out var start))
                {
                    throw new ArgumentException($"No source text was given for {line.Start.Source}, where an error's context line starts.", nameof(sources));
                }

                if (line.Start.CharStart > start.Text.Length)
                {
                    throw new ArgumentException($"An error's context line starts at character {line.Start.CharStart}, past the end of the text for {line.Start.Source}, which has {start.Text.Length} characters.", nameof(sources));
                }
            }

            if (builder.Length > 0)
            {
                builder.Append("\n\n");
            }

            DiagnosticRenderer.Append(builder, error, source, byId);
        }

        return builder.ToString();
    }
}
