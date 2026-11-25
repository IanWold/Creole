namespace Creole;

/// <summary>
/// The mode channels one layer's grammar writes in one run: each channel's stack, found by its channel.
/// </summary>
/// <remarks>
/// A parser run on its own has no layer below it to read a channel.
/// Its writer keeps a stack for each channel it writes, which nothing else reads, so its grammar still runs as written.
/// It makes the writer when its grammar first changes a mode, so a parse that changes none allocates nothing for modes.
/// </remarks>
/// <param name="layer">The name of the layer whose grammar writes, which errors give.</param>
internal sealed class ModeWriter(string layer)
{
    private readonly string _layer = layer;
    private readonly List<ModeLink> _links = [];

    /// <summary>
    /// The link through which this writer changes <paramref name="channel"/>'s stack, made the first time it is asked for.
    /// </summary>
    public ModeLink<TMode> Link<TMode>(ModeChannel<TMode> channel)
    {
        foreach (var link in _links)
        {
            if (ReferenceEquals(link.Channel, channel))
            {
                return (ModeLink<TMode>)link;
            }
        }

        var made = new ModeLink<TMode>(channel);
        _links.Add(made);

        return made;
    }

    /// <summary>
    /// The writer on <paramref name="cursor"/>, which a parser run on its own makes the first time its grammar changes a mode.
    /// </summary>
    public static ModeWriter Of<T>(ref Cursor<T> cursor) =>
        cursor.Modes ??= new ModeWriter(Parsing.Layer);

    /// <summary>
    /// The exception for a <c>PopMode</c> of <paramref name="channel"/> when its stack holds only one mode.
    /// </summary>
    public GrammarException PoppedLast(object channel) =>
        new($"The {_layer} layer pops the last mode of a {channel}. Each PopMode needs a PushMode on the same channel before it.");
}
