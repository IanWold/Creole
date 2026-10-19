namespace Creole;

/// <summary>
/// Issues <see cref="SourceId"/>s and records the name of each source, such as a file path.
/// </summary>
/// <remarks>
/// A registry belongs to one parse, or to a group of parses that share sources, such as a parse and the sub-pipelines it embeds.
/// It is safe to use from several threads.
/// </remarks>
public sealed class SourceRegistry
{
    private readonly Lock _lock = new();
    private readonly List<string> _names = [];

    /// <summary>
    /// The number of sources added so far, not counting <see cref="SourceId.Unnamed"/>.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _names.Count;
            }
        }
    }

    /// <summary>
    /// Adds a source and returns its new id. Adding the same name twice gives two different ids.
    /// </summary>
    /// <param name="name">The name to show in diagnostics, such as <c>config.json</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    public SourceId Add(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        lock (_lock)
        {
            _names.Add(name);
            return new SourceId(_names.Count);
        }
    }

    /// <summary>
    /// Returns the name a source was added with, or <see langword="null"/> for <see cref="SourceId.Unnamed"/>.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="id"/> was not issued by this registry.</exception>
    public string? GetName(SourceId id)
    {
        if (id.Value == 0)
        {
            return null;
        }

        lock (_lock)
        {
            if (id.Value > _names.Count)
            {
                throw new ArgumentException($"{id} was not issued by this registry.", nameof(id));
            }

            return _names[id.Value - 1];
        }
    }
}
