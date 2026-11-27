using System.IO.Pipelines;
using System.Text;
using static Creole.CharGrammar;
using WordGrammar = Creole.Grammar<string>;

namespace Creole.Tests;

/// <summary>
/// A stream whose first read gives <paramref name="first"/>, and whose every later read never completes, whatever its token says.
/// </summary>
/// <param name="first">What the first read gives.</param>
file sealed class StuckStream(byte[] first) : Stream
{
    private byte[]? _first = first;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    /// <summary>
    /// Whether the token of a read that never completes was cancelled.
    /// </summary>
    public bool Cancelled { get; private set; }

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_first is { } first)
        {
            _first = null;
            first.CopyTo(buffer);

            return new ValueTask<int>(first.Length);
        }

        _ = cancellationToken.Register(() => Cancelled = true);

        return new ValueTask<int>(new TaskCompletionSource<int>().Task);
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

/// <summary>
/// <c>ParseManyAsync</c> over a <see cref="Stream"/>: what a starved item costs, how <see cref="ParseOptions.MaxBufferedItems"/> caps one record, that the reply to a record never waits for the next, and how an enumeration ends when it is cancelled, when its source fails, and when its caller stops early (ADR-0045).
/// </summary>
public class ParseManyAsyncTests
{
    private const int Limit = 1_000;

    private static readonly ParseOptions _limited = new() { MaxBufferedItems = Limit };

    private static readonly Layer<char, string> _words = Layer.Tokens(Letter.AtLeastOnce().Text().Or(Char(';').As(";")), skip: Whitespace.SkipMany());

    private static readonly Pipeline<byte, string> _records = Pipeline.From(Layer.Utf8().Then(_words)).Then(WordGrammar.Token("x").SkipMany().Then(WordGrammar.Token("end")));

    private static readonly Pipeline<byte, string> _requests = Pipeline.From(Layer.Utf8().Then(_words)).Then(WordGrammar.Any.Before(WordGrammar.Token(";")));

    private static async Task<List<ParseResult<T>>> All<T>(IAsyncEnumerable<ParseResult<T>> results)
    {
        var all = new List<ParseResult<T>>();

        await foreach (var result in results)
        {
            all.Add(result);
        }

        return all;
    }

    private static string Error<T>(ParseResult<T> result) =>
        Assert.Single(result.Diagnostics).ToString();

    private static byte[] Record(int length) =>
        Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("x ", (length - 3) / 2)) + "end");

    private static string Value<T>(ParseResult<T> result) =>
        $"{Assert.IsType<Parsed<T>>(result.Value).Value}";

    [Fact]
    public async Task AnErrorTheWriterCompletesWithEndsTheEnumeration()
    {
        var pipe = new Pipe();

        await pipe.Writer.WriteAsync("ping; he"u8.ToArray(), TestContext.Current.CancellationToken);
        await pipe.Writer.CompleteAsync(new IOException("The connection was reset."));

        Assert.Equal("The connection was reset.", (await Assert.ThrowsAsync<IOException>(() => All(_requests.ParseManyAsync(pipe.Reader.AsStream(), cancellationToken: TestContext.Current.CancellationToken)))).Message);
    }

    [Fact]
    public async Task AnItemFromAStreamThatFillsEveryReadRunsLogarithmicallyManyAttempts()
    {
        var record = Record(100_001);
        var receives = 0;

        await foreach (var result in Pipeline.ReceiveMany(_records, new MemoryStream(record), ParseOptions.Default, _ => receives++, TestContext.Current.CancellationToken))
        {
            Assert.Equal("end", Value(result));
            break;
        }

        Assert.InRange(receives + 1, 2, (int)Math.Ceiling(Math.Log2(record.Length)) + 2);
    }

    [Fact]
    public async Task AnItemFromAStreamWhoseReadsAreShortAndPendingRunsAtMostOneAttemptMoreThanItsReads()
    {
        var stream = new ChunkedStream(Record(201), static () => 1, pending: true);
        var receives = 0;

        await foreach (var result in Pipeline.ReceiveMany(_records, stream, ParseOptions.Default, _ => receives++, TestContext.Current.CancellationToken))
        {
            Assert.Equal("end", Value(result));
            Assert.InRange(receives + 1, 2, stream.Reads + 1);
            break;
        }
    }

    [Fact]
    public async Task ARecordLongerThanTheLimitEndsAfterBoundedAttemptsWithTheLimitsError()
    {
        var receives = 0;
        var held = 0L;

        var results = await All(Pipeline.ReceiveMany(_records, new MemoryStream(Record(5_001)), _limited, bytes => (receives, held) = (receives + 1, Math.Max(held, bytes)), TestContext.Current.CancellationToken));

        Assert.Equal("error[decoder] at 0..0: buffered input from here exceeds ParseOptions.MaxBufferedItems of 1000", Error(Assert.Single(results)));
        Assert.InRange(receives + 1, 2, (int)Math.Ceiling(Math.Log2(Limit + 1)) + 2);
        Assert.InRange(held, 1, Limit + 1);
    }

    [Fact]
    public async Task ARecordWithinTheLimitParses() =>
        Assert.Equal("end", Value(Assert.Single(await All(_records.ParseManyAsync(new MemoryStream(Record(899)), _limited, TestContext.Current.CancellationToken)))));

    [Fact(Timeout = 10_000)]
    public async Task AReplyToEachRecordNeverWaitsForTheNext()
    {
        var pipe = new Pipe();
        var replies = new SemaphoreSlim(0);
        string[] requests = ["ping;", "hello;", "goodbye;"];
        var received = new List<string>();

        var producer = Task.Run(
            async () =>
            {
                foreach (var request in requests)
                {
                    var bytes = Encoding.UTF8.GetBytes(request);

                    await pipe.Writer.WriteAsync(bytes.AsMemory(0, 2), TestContext.Current.CancellationToken);
                    await Task.Delay(20, TestContext.Current.CancellationToken);
                    await pipe.Writer.WriteAsync(bytes.AsMemory(2), TestContext.Current.CancellationToken);
                    await replies.WaitAsync(TestContext.Current.CancellationToken);
                }

                await pipe.Writer.CompleteAsync();
            },
            TestContext.Current.CancellationToken
        );

        await foreach (var result in _requests.ParseManyAsync(pipe.Reader.AsStream(), cancellationToken: TestContext.Current.CancellationToken))
        {
            received.Add(Value(result));
            replies.Release();
        }

        await producer;

        Assert.Equal(["ping", "hello", "goodbye"], received);
    }

    [Theory(Timeout = 10_000)]
    [InlineData(65_536)]
    [InlineData(131_072)]
    [InlineData(196_607)]
    [InlineData(196_608)]
    [InlineData(196_609)]
    public async Task AReplyToALargeRecordNeverWaitsForTheNext(int length)
    {
        var pipe = new Pipe();
        var replied = new SemaphoreSlim(0);
        var request = Encoding.UTF8.GetBytes(new string('a', length - 1) + ";");
        var received = new List<int>();

        var producer = Task.Run(
            async () =>
            {
                await pipe.Writer.WriteAsync(request, TestContext.Current.CancellationToken);
                await replied.WaitAsync(TestContext.Current.CancellationToken);
                await pipe.Writer.CompleteAsync();
            },
            TestContext.Current.CancellationToken
        );

        await foreach (var result in _requests.ParseManyAsync(pipe.Reader.AsStream(), cancellationToken: TestContext.Current.CancellationToken))
        {
            received.Add(Value(result).Length);
            replied.Release();
        }

        await producer;

        Assert.Equal([length - 1], received);
    }

    [Fact]
    public async Task CancellingTheTokenEndsTheEnumerationBeforeTheNextItemEvenWithItsBytesReceived()
    {
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var items = 0;

        async Task Enumerate()
        {
            await foreach (var _ in _requests.ParseManyAsync(new MemoryStream("a; b; c; d;"u8.ToArray()), cancellationToken: cancel.Token))
            {
                items++;
                await cancel.CancelAsync();
            }
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(Enumerate);
        Assert.Equal(1, items);
    }

    [Fact(Timeout = 10_000)]
    public async Task CancellingTheTokenWhileAReadIsPendingEndsTheEnumeration()
    {
        var pipe = new Pipe();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var enumeration = All(_requests.ParseManyAsync(pipe.Reader.AsStream(), cancellationToken: cancel.Token));

        await Task.Delay(50, TestContext.Current.CancellationToken);
        await cancel.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => enumeration);
    }

    [Fact(Timeout = 10_000)]
    public async Task CancellingTheTokenGivenToWithCancellationEndsTheEnumeration()
    {
        var pipe = new Pipe();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);

        async Task Enumerate()
        {
            await foreach (var _ in _requests.ParseManyAsync(pipe.Reader.AsStream(), cancellationToken: TestContext.Current.CancellationToken).WithCancellation(cancel.Token))
            {
            }
        }

        var enumeration = Enumerate();

        await Task.Delay(50, TestContext.Current.CancellationToken);
        await cancel.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => enumeration);
    }

    [Fact]
    public async Task ManyShortRecordsParseUnderALimitSmallerThanEveryRead() =>
        Assert.Equal(300, (await All(_requests.ParseManyAsync(new MemoryStream(Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("hello; ", 300)))), new ParseOptions { MaxBufferedItems = 10 }, TestContext.Current.CancellationToken))).Count(static result => result.Value is Parsed<string>));

    [Fact(Timeout = 10_000)]
    public async Task StoppingEarlyWithAReadPendingCancelsItWithoutWaitingForIt()
    {
        var stream = new StuckStream("ping;"u8.ToArray());

        await foreach (var result in _requests.ParseManyAsync(stream, cancellationToken: TestContext.Current.CancellationToken))
        {
            Assert.Equal("ping", Value(result));
            break;
        }

        Assert.True(stream.Cancelled);
    }

    [Fact]
    public async Task TheStreamIsLeftOpen()
    {
        var stream = new MemoryStream("ping; pong;"u8.ToArray());

        Assert.Equal(["ping", "pong"], (await All(_requests.ParseManyAsync(stream, cancellationToken: TestContext.Current.CancellationToken))).Select(Value));
        Assert.True(stream.CanRead);
    }
}
