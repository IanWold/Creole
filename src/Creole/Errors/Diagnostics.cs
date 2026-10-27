using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;

namespace Creole;

/// <summary>
/// The errors from one parse, ordered by span.
/// </summary>
/// <remarks>
/// <para>
/// Errors are ordered by source, then by start offset, then by end offset. Errors with equal spans keep the order they were given in.
/// Sources are ordered as their <see cref="SourceRegistry"/> issued them, with <see cref="SourceId.Unnamed"/> first.
/// </para>
/// <para>
/// The errors from a parse come with the text of the sources they point into, so <see cref="Render()"/> can show them against it.
/// A collection made with <see cref="Diagnostics(IEnumerable{ParseError})"/> holds no source texts.
/// </para>
/// <para>
/// A collection is immutable. Two collections are equal when they hold equal errors in the same order, whatever source texts they hold.
/// </para>
/// </remarks>
public sealed class Diagnostics : IReadOnlyList<ParseError>, IEquatable<Diagnostics>
{
    private readonly ParseError[] _errors;
    private readonly SourceText[] _sources;

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
    /// Creates a collection of <paramref name="errors"/>, ordered by span, that holds the text of <paramref name="sources"/> for <see cref="Render()"/>.
    /// </summary>
    /// <param name="errors">The errors.</param>
    /// <param name="sources">Texts with distinct ids, none null.</param>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="errors"/> contains a null error.</exception>
    internal Diagnostics(IEnumerable<ParseError> errors, params ReadOnlySpan<SourceText> sources)
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

        _sources = sources.ToArray();
    }

    /// <summary>
    /// Creates a collection of <paramref name="errors"/>, ordered by span.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="errors"/> contains a null error.</exception>
    public Diagnostics(IEnumerable<ParseError> errors) : this(errors, []) { }

    /// <summary>
    /// Indexes <paramref name="sources"/> by id.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="sources"/> contains a null or a repeated id.</exception>
    private static Dictionary<SourceId, SourceText> ById(ReadOnlySpan<SourceText> sources)
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

        return byId;
    }

    /// <summary>
    /// Writes each error against its source, which <paramref name="byId"/> covers.
    /// </summary>
    private string RenderAgainst(Dictionary<SourceId, SourceText> byId, RenderOptions options)
    {
        var builder = new StringBuilder();

        foreach (var error in _errors)
        {
            if (builder.Length > 0)
            {
                builder.Append("\n\n");
            }

            DiagnosticRenderer.Append(builder, error, byId[error.Span.Source], byId, options);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Describes the first source an error or its context points into that <paramref name="byId"/> has no text for, or whose text is too short; or returns null if there is none.
    /// </summary>
    private string? Uncovered(Dictionary<SourceId, SourceText> byId)
    {
        foreach (var error in _errors)
        {
            if (!byId.TryGetValue(error.Span.Source, out var source))
            {
                return $"No source text was given for {error.Span.Source}, which an error points into.";
            }

            if (error.Span.CharEnd > source.Text.Length)
            {
                return $"An error's span ends at character {error.Span.CharEnd}, past the end of the text for {error.Span.Source}, which has {source.Text.Length} characters.";
            }

            foreach (var line in error.Context)
            {
                if (!byId.TryGetValue(line.Start.Source, out var start))
                {
                    return $"No source text was given for {line.Start.Source}, where an error's context line starts.";
                }

                if (line.Start.CharStart > start.Text.Length)
                {
                    return $"An error's context line starts at character {line.Start.CharStart}, past the end of the text for {line.Start.Source}, which has {start.Text.Length} characters.";
                }
            }
        }

        return null;
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
    /// Writes the errors against the source texts this collection holds, as <see cref="Render(ReadOnlySpan{SourceText})"/> does, or one per line if it lacks a text they point into.
    /// </summary>
    /// <returns>The errors in span order, or an empty string when there are no errors.</returns>
    /// <remarks>
    /// <para>
    /// The errors from a parse hold the text of its input, so they are shown against it.
    /// Errors are written one per line when the collection holds no text for a source that an error or one of its context lines points into, or when a text is too short for a span.
    /// </para>
    /// <para>
    /// A line is the error's <see cref="ParseError.ToString()"/>, such as <c>error[parser] at 15..21: unexpected string "host"; expected ',' or '}'; in object starting at 2</c>, and lines are joined by <c>\n</c>.
    /// Offsets are character offsets (<see cref="SourceSpan.CharStart"/> to <see cref="SourceSpan.CharEnd"/>), and a context line shows the character offset where its construct started.
    /// </para>
    /// <para>
    /// What is shown is limited as <see cref="RenderOptions.Default"/> limits it.
    /// </para>
    /// </remarks>
    public string Render() =>
        Render(RenderOptions.Default);

    /// <summary>
    /// Writes the errors as <see cref="Render()"/> does, within the limits in <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The limits on what is shown, such as how many context lines an error shows.</param>
    /// <returns>The errors in span order, or an empty string when there are no errors.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    public string Render(RenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (_sources.Length > 0)
        {
            var byId = ById(_sources);

            if (Uncovered(byId) is null)
            {
                return RenderAgainst(byId, options);
            }
        }

        return string.Join('\n', _errors.Select(error => error.ToString(options)));
    }

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
    /// Only <paramref name="sources"/> are used, not the texts this collection holds.
    /// What is shown is limited as <see cref="RenderOptions.Default"/> limits it.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="sources"/> contains a null or a repeated id, lacks a source an error or its context points into, or has a text shorter than an error's span or than where a context line starts.
    /// </exception>
    public string Render(params ReadOnlySpan<SourceText> sources) =>
        Render(RenderOptions.Default, sources);

    /// <summary>
    /// Writes each error against the line of source text it points into, as <see cref="Render(ReadOnlySpan{SourceText})"/> does, within the limits in <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The limits on what is shown, such as how many context lines an error shows.</param>
    /// <param name="sources">The text of every source the errors point into, each with a different <see cref="SourceText.Id"/>.</param>
    /// <returns>The errors in span order, separated by blank lines, or an empty string when there are no errors.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="sources"/> contains a null or a repeated id, lacks a source an error or its context points into, or has a text shorter than an error's span or than where a context line starts.
    /// </exception>
    public string Render(RenderOptions options, params ReadOnlySpan<SourceText> sources)
    {
        ArgumentNullException.ThrowIfNull(options);

        var byId = ById(sources);

        return Uncovered(byId) is { } problem
            ? throw new ArgumentException(problem, nameof(sources))
            : RenderAgainst(byId, options);
    }
}
