namespace Creole;

/// <summary>
/// The mode channels one layer's grammar writes in one run: each channel's stack, found by its channel, and the log of the writes.
/// </summary>
/// <remarks>
/// <para>
/// Each write appends an entry to the log: where the layer's cursor was, the channel, and its stack before.
/// A write changes the mode when the top of the stack after it differs from the top before it.
/// Only a change makes the layer forget its failures past the cursor, because only a change can make the layer below read the next item differently.
/// When the log is full, the writer first drops the entries below the lower of the layer's cursor and its floor, which no rewind can reach, so the log stays as small as the window the layer can still go back over, over any input.
/// A change also discards the items the <c>Layer.Modal</c> reading the channel produced from the cursor on, if the layer reads them directly, so they are read again in the new mode.
/// </para>
/// <para>
/// In a pipeline, a writer holds the links of the channels read below its layer, and changes only those.
/// A change to any other channel throws, and so does a change to a channel another layer has already changed in the run.
/// </para>
/// <para>
/// A parser run on its own has no layer below it to read a channel.
/// Its writer keeps a stack for each channel it writes, which nothing else reads, so its grammar still runs as written.
/// It takes the writer when its grammar first changes a mode, so a parse that changes none uses none.
/// Any writer makes its log at its first write, so a layer whose grammar changes no mode allocates none.
/// Each thread keeps a spare writer for parsers run on their own, which a parse takes and gives back when it ends, so the log keeps its storage from one parse to the next, up to room for <see cref="SpareLimit"/> entries.
/// </para>
/// </remarks>
/// <param name="layer">The name of the layer whose grammar writes, which errors give.</param>
/// <param name="below">In a pipeline, the links of the channels read below the layer; null for a parser run on its own.</param>
internal sealed class ModeWriter(string layer, ModeLink[]? below)
{
    /// <summary>
    /// One write, as the log keeps it.
    /// </summary>
    /// <param name="Index">The position of the writing layer's cursor in its input.</param>
    /// <param name="Link">The channel whose stack the write replaced.</param>
    /// <param name="Previous">The stack before the write.</param>
    /// <param name="Changed">Whether the write changed the mode: whether the top after it differs from the top before it.</param>
    private readonly record struct Entry(long Index, ModeLink Link, object Previous, bool Changed);

    /// <summary>
    /// The most entries a spare writer's log keeps room for. A larger log is let go when its writer is given back, so a thread does not hold on to the memory of its largest parse.
    /// </summary>
    private const int SpareLimit = 1024;

    private readonly ModeLink[]? _below = below;
    private readonly string _layer = layer;
    private List<Entry>? _log;
    private List<ModeLink>? _own;

    [ThreadStatic]
    private static ModeWriter? _spare;

    /// <summary>
    /// The number of entries the log has room for, for tests of its memory bound.
    /// </summary>
    internal int Capacity => _log?.Capacity ?? 0;

    /// <summary>
    /// The number of entries the log holds, for tests of its memory bound.
    /// </summary>
    internal int Count => _log?.Count ?? 0;

    /// <summary>
    /// Drops the entries of <paramref name="log"/> below <paramref name="horizon"/>, the lowest position the layer can still go back to.
    /// </summary>
    /// <remarks>
    /// Entries are in the order of their positions, so those below the horizon are the oldest.
    /// </remarks>
    private static void Drop(List<Entry> log, long horizon)
    {
        var count = 0;

        while (count < log.Count && log[count].Index < horizon)
        {
            count++;
        }

        log.RemoveRange(0, count);
    }

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
    /// The writer on <paramref name="cursor"/>, which a parser run on its own takes the first time its grammar changes a mode: the thread's spare, or a new one.
    /// </summary>
    public static ModeWriter Of<T>(ref Cursor<T> cursor)
    {
        if (cursor.Modes is { } modes)
        {
            return modes;
        }

        var writer = _spare ?? new ModeWriter(Parsing.Layer, below: null);

        _spare = null;
        cursor.Modes = writer;

        return writer;
    }

    /// <summary>
    /// The exception for a <c>PopMode</c> of <paramref name="channel"/> when its stack holds only one mode.
    /// </summary>
    public GrammarException PoppedLast(object channel) =>
        new($"The {_layer} layer pops the last mode of a {channel}. Each PopMode needs a PushMode on the same channel before it.");

    /// <summary>
    /// Returns every channel this writer has changed to its initial mode alone, with an empty log, as a <c>ParseMany</c> item starts at <paramref name="position"/> of <paramref name="input"/>.
    /// </summary>
    /// <remarks>
    /// A channel whose mode that changes discards the items read ahead from <paramref name="position"/> on in the mode the last item left.
    /// </remarks>
    public void Reset<T>(ICursorSource<T> input, long position)
    {
        foreach (var link in _below ?? [])
        {
            if (ReferenceEquals(link.Writer, this) && link.Reset())
            {
                input.Discard(position, link);
            }
        }

        _own?.Clear();
        _log?.Clear();
    }

    /// <summary>
    /// Gives back the writer a parser run on its own took, if it took one, as the thread's spare, with no channels and an empty log.
    /// </summary>
    public static void Return(ModeWriter? writer)
    {
        if (writer is null)
        {
            return;
        }

        writer._own?.Clear();

        if (writer._log is { Capacity: > SpareLimit })
        {
            writer._log = null;
        }

        writer._log?.Clear();
        _spare = writer;
    }

    /// <summary>
    /// Replaces the stack of <paramref name="link"/> with <paramref name="next"/>, logging the write at <paramref name="cursor"/>'s position.
    /// </summary>
    /// <remarks>
    /// If the log is full, it first drops the entries no rewind can reach.
    /// If the write changes the mode, the layer forgets its failures past the cursor, and discards the items read ahead from the cursor on.
    /// </remarks>
    public void Write<T, TMode>(ref Cursor<T> cursor, ModeLink<TMode> link, ModeStack<TMode> next)
    {
        var previous = link.Stack;
        var changed = !EqualityComparer<TMode>.Default.Equals(previous.Top, next.Top);
        var log = _log ??= [];

        if (log.Count == log.Capacity)
        {
            Drop(log, Math.Min(cursor.Position, cursor.Floor));
        }

        log.Add(new Entry(cursor.Position, link, previous, changed));
        link.Stack = next;

        if (changed)
        {
            cursor.Failures?.ChangeAt(cursor.Position);
            cursor.Discard(link);
        }
    }
}
