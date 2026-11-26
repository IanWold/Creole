using System.IO.Pipelines;
using System.Text;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;
using WordGrammar = Creole.Grammar<string>;

namespace Creole.Tests;

/// <summary>
/// A pipeline over UTF-8 bytes parsing a document from a <see cref="Pipe"/> on a thread of its own, as docs/concepts/pipelines.md shows under huge documents.
/// </summary>
public class PipeTests
{
    private static readonly Parser<char, char> _nested = OneOf(Letter, Char('(').Then(Rec(() => _nested)).Before(Char(')')));

    private static readonly Pipeline<byte, IReadOnlyList<string>> _words =
        Pipeline.From(Layer.Utf8().Then(Layer.Tokens(Letter.AtLeastOnce().Text(), skip: Whitespace.SkipMany()))).Then(WordGrammar.Any.Many().Before(WordGrammar.End));

    private static T OnAThreadWithStack<T>(int maxStackSize, Func<T> parse)
    {
        var result = default(T);
        var thread = new Thread(() => result = parse(), maxStackSize);

        thread.Start();
        thread.Join();

        return result!;
    }

    private static Task<T> OnItsOwnThread<T>(Func<T> parse) =>
        Task.Factory.StartNew(parse, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    [Fact]
    public async Task ADocumentThatArrivesInPartsParsesOnAThreadOfItsOwn()
    {
        var pipe = new Pipe();
        var onThePool = true;
        var parse = OnItsOwnThread(() =>
        {
            onThePool = Thread.CurrentThread.IsThreadPoolThread;
            return _words.Parse(pipe.Reader.AsStream());
        });
        var bytes = "héllo wörld"u8.ToArray();

        foreach (var part in new[] { bytes[..2], bytes[2..9], bytes[9..] })
        {
            await pipe.Writer.WriteAsync(part, TestContext.Current.CancellationToken);
        }

        await pipe.Writer.CompleteAsync();

        Assert.Equal(["héllo", "wörld"], Assert.IsType<Parsed<IReadOnlyList<string>>>((await parse).Value).Value);
        Assert.False(onThePool);
    }

    [Fact]
    public async Task AnErrorTheWriterCompletesWithEndsTheParse()
    {
        var pipe = new Pipe();
        var parse = OnItsOwnThread(() => _words.Parse(pipe.Reader.AsStream()));

        await pipe.Writer.WriteAsync("héllo "u8.ToArray(), TestContext.Current.CancellationToken);
        await pipe.Writer.CompleteAsync(new IOException("connection reset"));

        Assert.Equal("connection reset", (await Assert.ThrowsAsync<IOException>(() => parse)).Message);
    }

    [Fact]
    public void AThreadWithALargerStackParsesDeeperNesting()
    {
        var pipeline = Pipeline.From(Layer.Utf8()).Then(_nested);
        var bytes = Encoding.UTF8.GetBytes(new string('(', 10_000) + "a" + new string(')', 10_000));
        var options = new ParseOptions { MaxDepth = int.MaxValue };

        var small = OnAThreadWithStack(512 * 1024, () => pipeline.Parse(new MemoryStream(bytes), options));
        var large = OnAThreadWithStack(256 * 1024 * 1024, () => pipeline.Parse(new MemoryStream(bytes), options));

        Assert.Contains("nesting is too deep for the thread's stack", Assert.Single(small.Diagnostics).ToString(), StringComparison.Ordinal);
        Assert.Equal('a', Assert.IsType<Parsed<char>>(large.Value).Value);
    }

    [Fact]
    public async Task CancellingThePendingReadEndsTheParse()
    {
        var pipe = new Pipe();
        using var cancellation = new CancellationTokenSource();
        using var registration = cancellation.Token.Register(pipe.Reader.CancelPendingRead);

        await pipe.Writer.WriteAsync("héllo "u8.ToArray(), TestContext.Current.CancellationToken);

        var parse = OnItsOwnThread(() => _words.Parse(pipe.Reader.AsStream()));

        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => parse);
    }
}
