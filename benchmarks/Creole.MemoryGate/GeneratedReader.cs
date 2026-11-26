using System.Text;

namespace Creole.MemoryGate;

/// <summary>
/// A reader of an input generated one record at a time as it is read, so that no more than one record is ever held.
/// </summary>
/// <remarks>
/// The reader ends after the first record that brings the characters written to at least its length.
/// Before writing record 0 and every <c>recordsPerSample</c>th record after it, it calls <c>sample</c>.
/// </remarks>
/// <param name="input">The input to generate.</param>
/// <param name="length">The fewest characters to write.</param>
/// <param name="recordsPerSample">How many records to write between calls to <paramref name="sample"/>.</param>
/// <param name="sample">What to call between records.</param>
public sealed class GeneratedReader(GateInput input, long length, int recordsPerSample, Action sample) : TextReader
{
    private int _offset;
    private readonly Random _random = new(input.Seed);
    private readonly StringBuilder _record = new();

    /// <summary>
    /// The number of characters written so far.
    /// </summary>
    public long Characters { get; private set; }

    /// <summary>
    /// The number of records written so far.
    /// </summary>
    public long Records { get; private set; }

    /// <summary>
    /// Reads the text of <paramref name="input"/> up to <paramref name="length"/> characters, calling nothing between records.
    /// </summary>
    /// <param name="input">The input to generate.</param>
    /// <param name="length">The fewest characters to write.</param>
    /// <returns>The text.</returns>
    public static string Text(GateInput input, long length)
    {
        using var reader = new GeneratedReader(input, length, int.MaxValue, static () => { });

        return reader.ReadToEnd();
    }

    private bool HasNext()
    {
        if (_offset < _record.Length)
        {
            return true;
        }

        if (Characters >= length)
        {
            return false;
        }

        if (Records % recordsPerSample == 0)
        {
            sample();
        }

        _record.Clear();
        input.WriteRecord(_random, _record);
        _offset = 0;
        Characters += _record.Length;
        Records++;

        return true;
    }

    /// <inheritdoc/>
    public override int Peek() =>
        HasNext()
        ? _record[_offset]
        : -1;

    /// <inheritdoc/>
    public override int Read() =>
        HasNext()
        ? _record[_offset++]
        : -1;

    /// <inheritdoc/>
    public override int Read(char[] buffer, int index, int count) =>
        Read(buffer.AsSpan(index, count));

    /// <inheritdoc/>
    public override int Read(Span<char> buffer)
    {
        var count = 0;

        while (count < buffer.Length && HasNext())
        {
            var take = Math.Min(buffer.Length - count, _record.Length - _offset);

            _record.CopyTo(_offset, buffer.Slice(count, take), take);
            _offset += take;
            count += take;
        }

        return count;
    }
}
