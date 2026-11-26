namespace Creole;

/// <summary>
/// The mode channels one layer's grammar writes in one run: each channel's stack, found by its channel.
/// </summary>
/// <remarks>
/// <para>
/// In a pipeline, a writer holds the links of the channels read below its layer, and changes only those.
/// A change to any other channel throws, and so does a change to a channel another layer has already changed in the run.
/// </para>
/// <para>
/// A parser run on its own has no layer below it to read a channel.
/// Its writer keeps a stack for each channel it writes, which nothing else reads, so its grammar still runs as written.
/// It makes the writer when its grammar first changes a mode, so a parse that changes none allocates nothing for modes.
/// </para>
/// </remarks>
/// <param name="layer">The name of the layer whose grammar writes, which errors give.</param>
/// <param name="below">In a pipeline, the links of the channels read below the layer; null for a parser run on its own.</param>
internal sealed class ModeWriter(string layer, ModeLink[]? below)
{
    private readonly ModeLink[]? _below = below;
    private readonly string _layer = layer;
    private List<ModeLink>? _own;

    private ModeLink<TMode> Own<TMode>(ModeChannel<TMode> channel)
    {
        _own ??= [];

        foreach (var link in _own)
        {
            if (ReferenceEquals(link.Channel, channel))
            {
                return (ModeLink<TMode>)link;
            }
        }

        var made = new ModeLink<TMode>(channel);
        _own.Add(made);

        return made;
    }

    /// <summary>
    /// The link through which this writer changes <paramref name="channel"/>'s stack.
    /// </summary>
    /// <exception cref="GrammarException">In a pipeline: no layer below reads <paramref name="channel"/>, or another layer already writes it.</exception>
    public ModeLink<TMode> Link<TMode>(ModeChannel<TMode> channel)
    {
        if (_below is null)
        {
            return Own(channel);
        }

        foreach (var link in _below)
        {
            if (!ReferenceEquals(link.Channel, channel))
            {
                continue;
            }

            link.Writer ??= this;

            return ReferenceEquals(link.Writer, this)
                ? (ModeLink<TMode>)link
                : throw new GrammarException($"The {_layer} layer changes the mode of a {channel}, which the {link.Writer._layer} layer already changes. A channel has one writing layer.");
        }

        throw new GrammarException($"The {_layer} layer changes the mode of a {channel}, but no layer below it reads that channel. Read it with Layer.Modal in a layer below, using the same ModeChannel instance.");
    }

    /// <summary>
    /// Returns every channel this writer has changed to its initial mode alone, as a <c>ParseMany</c> item starts.
    /// </summary>
    public void Reset()
    {
        foreach (var link in _below ?? [])
        {
            if (ReferenceEquals(link.Writer, this))
            {
                link.Reset();
            }
        }

        _own?.Clear();
    }

    /// <summary>
    /// The writer on <paramref name="cursor"/>, which a parser run on its own makes the first time its grammar changes a mode.
    /// </summary>
    public static ModeWriter Of<T>(ref Cursor<T> cursor) =>
        cursor.Modes ??= new ModeWriter(Parsing.Layer, below: null);

    /// <summary>
    /// The exception for a <c>PopMode</c> of <paramref name="channel"/> when its stack holds only one mode.
    /// </summary>
    public GrammarException PoppedLast(object channel) =>
        new($"The {_layer} layer pops the last mode of a {channel}. Each PopMode needs a PushMode on the same channel before it.");
}
