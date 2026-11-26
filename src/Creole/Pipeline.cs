using System.Runtime.CompilerServices;
using System.Text;

namespace Creole;

/// <summary>
/// Starts pipelines and runs them: <c>Pipeline.From(lexer).Then(parser).Parse(text)</c>.
/// </summary>
/// <remarks>
/// <para>
/// <c>Parse</c> over input in memory runs every layer and the parser without recording failures.
/// Only if it fails does it run the whole pipeline again from the start, with new buffers and every layer recording, to build its error.
/// So a successful parse pays nothing for errors, a failed parse costs up to about two and a half times as much, and the grammar's functions run again when a parse fails.
/// </para>
/// <para>
/// <c>Parse</c> over a <see cref="TextReader"/> or a <see cref="Stream"/>, which cannot be read again, and every <c>ParseMany</c>, record failures as they go.
/// </para>
/// <para>
/// When a parse ends, however it ends, its buffers return their memory to their pools.
/// </para>
/// </remarks>
public static class Pipeline
{
    extension<TOut>(Pipeline<char, TOut> pipeline)
    {
        /// <summary>
        /// Parses <paramref name="input"/> with this pipeline.
        /// </summary>
        /// <param name="input">The text to parse.</param>
        /// <param name="source">The name to show the input under in diagnostics, such as a file name, or <see langword="null"/> for none.</param>
        /// <param name="options">The settings for the parse, or <see langword="null"/> for <see cref="ParseOptions.Default"/>.</param>
        /// <param name="pipelineExpression">
        /// The caller's expression for this pipeline, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>
        /// <see cref="Parsed{T}"/> with the parser's value if it succeeded.
        /// Otherwise <see cref="Failed"/> with one error, which holds <paramref name="input"/> so that <see cref="Diagnostics.Render()"/> shows the error against it.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The error comes from the layer that failed, named as that layer is, or from the parser, named <c>parser</c>.
        /// Its spans are character offsets in <paramref name="input"/>, in the source <see cref="SourceId.Unnamed"/>.
        /// A layer or parser that goes deeper than <see cref="ParseOptions.MaxDepth"/>, or runs short of stack, fails with one error where the level would have started, whose message names the cause.
        /// </para>
        /// <para>
        /// The result and the slices in its value refer to <paramref name="input"/> and copy none of it.
        /// </para>
        /// </remarks>
        /// <exception cref="GrammarException">This pipeline is null, or a grammar in it is not valid.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
        public ParseResult<TOut> Parse(string input, string? source = null, ParseOptions? options = null, [CallerArgumentExpression(nameof(pipeline))] string pipelineExpression = "")
        {
            var checkedPipeline = Guard.NotNull(pipeline, pipelineExpression);

            ArgumentNullException.ThrowIfNull(input);

            return Run(checkedPipeline, input.AsMemory(), Text(input.AsMemory(), source), options ?? ParseOptions.Default);
        }

        /// <summary>
        /// Parses <paramref name="input"/> with this pipeline.
        /// </summary>
        /// <param name="input">The text to parse.</param>
        /// <param name="source">The name to show the input under in diagnostics, such as a file name, or <see langword="null"/> for none.</param>
        /// <param name="options">The settings for the parse, or <see langword="null"/> for <see cref="ParseOptions.Default"/>.</param>
        /// <param name="pipelineExpression">
        /// The caller's expression for this pipeline, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>
        /// <see cref="Parsed{T}"/> with the parser's value if it succeeded.
        /// Otherwise <see cref="Failed"/> with one error, which holds <paramref name="input"/> so that <see cref="Diagnostics.Render()"/> shows the error against it.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The error comes from the layer that failed, named as that layer is, or from the parser, named <c>parser</c>.
        /// Its spans are character offsets in <paramref name="input"/>, in the source <see cref="SourceId.Unnamed"/>.
        /// A layer or parser that goes deeper than <see cref="ParseOptions.MaxDepth"/>, or runs short of stack, fails with one error where the level would have started, whose message names the cause.
        /// </para>
        /// <para>
        /// The result and the slices in its value refer to <paramref name="input"/> and copy none of it.
        /// Render the result and finish with the slices before reusing the memory.
        /// </para>
        /// </remarks>
        /// <exception cref="GrammarException">This pipeline is null, or a grammar in it is not valid.</exception>
        public ParseResult<TOut> Parse(ReadOnlyMemory<char> input, string? source = null, ParseOptions? options = null, [CallerArgumentExpression(nameof(pipeline))] string pipelineExpression = "") =>
            Run(Guard.NotNull(pipeline, pipelineExpression), input, Text(input, source), options ?? ParseOptions.Default);

        /// <summary>
        /// Parses the text <paramref name="input"/> reads with this pipeline, reading as the layers pull.
        /// </summary>
        /// <param name="input">The reader to parse from, which blocks until it has characters or ends. The parse leaves it open.</param>
        /// <param name="options">The settings for the parse, or <see langword="null"/> for <see cref="ParseOptions.Default"/>.</param>
        /// <param name="pipelineExpression">
        /// The caller's expression for this pipeline, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>
        /// <see cref="Parsed{T}"/> with the parser's value if it succeeded.
        /// Otherwise <see cref="Failed"/> with one error, which holds no text, so <see cref="Diagnostics.Render()"/> writes it on one line.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The parse reads only as far as the layers need, and keeps only what they may still go back to.
        /// It records failures as it goes, since it cannot read the input again.
        /// </para>
        /// <para>
        /// The error comes from the layer that failed, named as that layer is, or from the parser, named <c>parser</c>.
        /// Its spans are offsets of the characters read, counted from 0, in the source <see cref="SourceId.Unnamed"/>.
        /// </para>
        /// <para>
        /// Slices in the value are copies, since the characters read are not kept.
        /// </para>
        /// </remarks>
        /// <exception cref="GrammarException">This pipeline is null, or a grammar in it is not valid.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
        public ParseResult<TOut> Parse(TextReader input, ParseOptions? options = null, [CallerArgumentExpression(nameof(pipeline))] string pipelineExpression = "")
        {
            var checkedPipeline = Guard.NotNull(pipeline, pipelineExpression);

            ArgumentNullException.ThrowIfNull(input);

            return checkedPipeline.TryParse(Read(input), options ?? ParseOptions.Default, recordFailures: true, out var value, out var error)
                ? new Parsed<TOut>(value)
                : new Failed(new Diagnostics([error]));
        }

        /// <summary>
        /// Parses <paramref name="input"/> as a sequence of items, running this pipeline's parser at the start and again after each item until no input remains.
        /// </summary>
        /// <param name="input">The text to parse.</param>
        /// <param name="source">The name to show the input under in diagnostics, such as a file name, or <see langword="null"/> for none.</param>
        /// <param name="options">The settings for the parse, or <see langword="null"/> for <see cref="ParseOptions.Default"/>.</param>
        /// <param name="pipelineExpression">
        /// The caller's expression for this pipeline, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>
        /// Each item's result, parsed as it is asked for.
        /// A <see cref="Failed"/> result is the last, and holds <paramref name="input"/> so that <see cref="Diagnostics.Render()"/> shows its error against it.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Each item starts with no furthest failure in any layer, and failures are recorded as the parse goes, so a failed item never runs the items before it again.
        /// A layer that fails while the next item is being looked for ends the sequence with that next item's <see cref="Failed"/> result.
        /// </para>
        /// <para>
        /// Nothing runs until the sequence is enumerated, and each enumeration parses from the start.
        /// Ending an enumeration early returns its buffers' memory to their pools.
        /// </para>
        /// </remarks>
        /// <exception cref="GrammarException">This pipeline is null. When enumerated, a grammar in it is not valid, or the parser succeeded without consuming input.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
        public IEnumerable<ParseResult<TOut>> ParseMany(string input, string? source = null, ParseOptions? options = null, [CallerArgumentExpression(nameof(pipeline))] string pipelineExpression = "")
        {
            var checkedPipeline = Guard.NotNull(pipeline, pipelineExpression);

            ArgumentNullException.ThrowIfNull(input);

            return Many(checkedPipeline, input.AsMemory(), Text(input.AsMemory(), source), options ?? ParseOptions.Default);
        }

        /// <summary>
        /// Parses <paramref name="input"/> as a sequence of items, running this pipeline's parser at the start and again after each item until no input remains.
        /// </summary>
        /// <param name="input">The text to parse.</param>
        /// <param name="source">The name to show the input under in diagnostics, such as a file name, or <see langword="null"/> for none.</param>
        /// <param name="options">The settings for the parse, or <see langword="null"/> for <see cref="ParseOptions.Default"/>.</param>
        /// <param name="pipelineExpression">
        /// The caller's expression for this pipeline, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>
        /// Each item's result, parsed as it is asked for.
        /// A <see cref="Failed"/> result is the last, and holds <paramref name="input"/> so that <see cref="Diagnostics.Render()"/> shows its error against it.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Each item starts with no furthest failure in any layer, and failures are recorded as the parse goes, so a failed item never runs the items before it again.
        /// A layer that fails while the next item is being looked for ends the sequence with that next item's <see cref="Failed"/> result.
        /// </para>
        /// <para>
        /// Nothing runs until the sequence is enumerated, and each enumeration parses from the start.
        /// Ending an enumeration early returns its buffers' memory to their pools.
        /// The results and the slices in their values refer to <paramref name="input"/> and copy none of it.
        /// </para>
        /// </remarks>
        /// <exception cref="GrammarException">This pipeline is null. When enumerated, a grammar in it is not valid, or the parser succeeded without consuming input.</exception>
        public IEnumerable<ParseResult<TOut>> ParseMany(ReadOnlyMemory<char> input, string? source = null, ParseOptions? options = null, [CallerArgumentExpression(nameof(pipeline))] string pipelineExpression = "") =>
            Many(Guard.NotNull(pipeline, pipelineExpression), input, Text(input, source), options ?? ParseOptions.Default);

        /// <summary>
        /// Parses the text <paramref name="input"/> reads as a sequence of items, running this pipeline's parser at the start and again after each item until the reader ends.
        /// </summary>
        /// <param name="input">The reader to parse from, which blocks until it has characters or ends. The parse leaves it open.</param>
        /// <param name="options">The settings for the parse, or <see langword="null"/> for <see cref="ParseOptions.Default"/>.</param>
        /// <param name="pipelineExpression">
        /// The caller's expression for this pipeline, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>
        /// Each item's result, parsed as it is asked for, reading only as far as that item needs.
        /// A <see cref="Failed"/> result is the last, and holds no text, so <see cref="Diagnostics.Render()"/> writes its error on one line.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Each item starts with no furthest failure in any layer, and failures are recorded as the parse goes.
        /// A layer that fails while the next item is being looked for ends the sequence with that next item's <see cref="Failed"/> result.
        /// </para>
        /// <para>
        /// Nothing is read until the sequence is enumerated. Enumerate it once: a second enumeration reads on from wherever the reader is, counting offsets from 0 again.
        /// Ending an enumeration early returns its buffers' memory to their pools and leaves the reader open.
        /// Slices in the values are copies, since the characters read are not kept.
        /// </para>
        /// </remarks>
        /// <exception cref="GrammarException">This pipeline is null. When enumerated, a grammar in it is not valid, or the parser succeeded without consuming input.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
        public IEnumerable<ParseResult<TOut>> ParseMany(TextReader input, ParseOptions? options = null, [CallerArgumentExpression(nameof(pipeline))] string pipelineExpression = "")
        {
            var checkedPipeline = Guard.NotNull(pipeline, pipelineExpression);

            ArgumentNullException.ThrowIfNull(input);

            return checkedPipeline.ParseMany(() => Read(input), options ?? ParseOptions.Default, static () => null);
        }
    }

    extension<TOut>(Pipeline<byte, TOut> pipeline)
    {
        /// <summary>
        /// Parses the bytes <paramref name="input"/> with this pipeline, whose first layer decodes them, such as <see cref="Layer.Utf8"/>.
        /// </summary>
        /// <param name="input">The bytes to parse. A <see langword="byte"/> array converts to this type.</param>
        /// <param name="source">The name to show the input under in diagnostics, such as a file name, or <see langword="null"/> for none.</param>
        /// <param name="options">The settings for the parse, or <see langword="null"/> for <see cref="ParseOptions.Default"/>.</param>
        /// <param name="pipelineExpression">
        /// The caller's expression for this pipeline, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>
        /// <see cref="Parsed{T}"/> with the parser's value if it succeeded.
        /// Otherwise <see cref="Failed"/> with one error.
        /// If the first layer is <see cref="Layer.Utf8"/>, the error holds the decoded text, so that <see cref="Diagnostics.Render()"/> shows it against its line; otherwise it holds none, and renders on one line.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The error comes from the layer that failed, named as that layer is, or from the parser, named <c>parser</c>.
        /// Its spans hold byte offsets in <paramref name="input"/>, in the source <see cref="SourceId.Unnamed"/>, and, above <see cref="Layer.Utf8"/>, character offsets in the decoded text.
        /// </para>
        /// <para>
        /// The text a failed result holds is what <see cref="Encoding.UTF8"/> decodes from the bytes after any byte-order mark, decoded when the parse fails.
        /// Its character offsets match the decoder's up to the first invalid sequence, where the decoder ends the parse, so every error renders at its line and column.
        /// </para>
        /// <para>
        /// The parse runs without recording failures, and again from the start with every layer recording only if it fails.
        /// Slices in the value are copies, since the decoded characters are not the caller's memory.
        /// </para>
        /// </remarks>
        /// <exception cref="GrammarException">This pipeline is null, or a grammar in it is not valid.</exception>
        public ParseResult<TOut> Parse(ReadOnlyMemory<byte> input, string? source = null, ParseOptions? options = null, [CallerArgumentExpression(nameof(pipeline))] string pipelineExpression = "")
        {
            var checkedPipeline = Guard.NotNull(pipeline, pipelineExpression);

            return Run(checkedPipeline, input, Decoded(checkedPipeline, input, source), options ?? ParseOptions.Default);
        }

        /// <summary>
        /// Parses the bytes <paramref name="input"/> reads with this pipeline, whose first layer decodes them, such as <see cref="Layer.Utf8"/>, reading as the layers pull.
        /// </summary>
        /// <param name="input">The stream to parse from, whose reads block until it has bytes or ends, such as a file, a socket, or a <c>PipeReader</c>'s <c>AsStream()</c>. The parse leaves it open.</param>
        /// <param name="options">The settings for the parse, or <see langword="null"/> for <see cref="ParseOptions.Default"/>.</param>
        /// <param name="pipelineExpression">
        /// The caller's expression for this pipeline, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>
        /// <see cref="Parsed{T}"/> with the parser's value if it succeeded.
        /// Otherwise <see cref="Failed"/> with one error, which holds no text, so <see cref="Diagnostics.Render()"/> writes it on one line.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The parse reads only as far as the layers need, and keeps only what they may still go back to.
        /// It records failures as it goes, since it cannot read the input again.
        /// It blocks the calling thread while the stream waits for bytes; to parse a document that arrives slowly, run it on a thread of its own.
        /// </para>
        /// <para>
        /// The error comes from the layer that failed, named as that layer is, or from the parser, named <c>parser</c>.
        /// Its spans hold offsets of the bytes read, counted from 0, in the source <see cref="SourceId.Unnamed"/>, and, above <see cref="Layer.Utf8"/>, character offsets in the decoded text.
        /// So it is the error that <c>Parse</c> over the same bytes in memory reports.
        /// </para>
        /// <para>
        /// Slices in the value are copies, since the bytes read are not kept.
        /// An exception the stream throws, such as <see cref="OperationCanceledException"/> from a cancelled read, ends the parse and is not caught.
        /// </para>
        /// </remarks>
        /// <exception cref="GrammarException">This pipeline is null, or a grammar in it is not valid.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
        public ParseResult<TOut> Parse(Stream input, ParseOptions? options = null, [CallerArgumentExpression(nameof(pipeline))] string pipelineExpression = "")
        {
            var checkedPipeline = Guard.NotNull(pipeline, pipelineExpression);

            ArgumentNullException.ThrowIfNull(input);

            return checkedPipeline.TryParse(Read(input), options ?? ParseOptions.Default, recordFailures: true, out var value, out var error)
                ? new Parsed<TOut>(value)
                : new Failed(new Diagnostics([error]));
        }

        /// <summary>
        /// Parses the bytes <paramref name="input"/> as a sequence of items, running this pipeline's parser at the start and again after each item until no input remains.
        /// </summary>
        /// <param name="input">The bytes to parse. A <see langword="byte"/> array converts to this type.</param>
        /// <param name="source">The name to show the input under in diagnostics, such as a file name, or <see langword="null"/> for none.</param>
        /// <param name="options">The settings for the parse, or <see langword="null"/> for <see cref="ParseOptions.Default"/>.</param>
        /// <param name="pipelineExpression">
        /// The caller's expression for this pipeline, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>
        /// Each item's result, parsed as it is asked for. A <see cref="Failed"/> result is the last.
        /// If the first layer is <see cref="Layer.Utf8"/>, its error holds the decoded text, so that <see cref="Diagnostics.Render()"/> shows it against its line; otherwise it holds none, and renders on one line.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Each item starts with no furthest failure in any layer, and failures are recorded as the parse goes.
        /// A layer that fails while the next item is being looked for ends the sequence with that next item's <see cref="Failed"/> result.
        /// The text a failed result holds is what <see cref="Encoding.UTF8"/> decodes from the bytes after any byte-order mark, decoded when that result is built.
        /// </para>
        /// <para>
        /// Nothing runs until the sequence is enumerated, and each enumeration parses from the start.
        /// Ending an enumeration early returns its buffers' memory to their pools.
        /// </para>
        /// </remarks>
        /// <exception cref="GrammarException">This pipeline is null. When enumerated, a grammar in it is not valid, or the parser succeeded without consuming input.</exception>
        public IEnumerable<ParseResult<TOut>> ParseMany(ReadOnlyMemory<byte> input, string? source = null, ParseOptions? options = null, [CallerArgumentExpression(nameof(pipeline))] string pipelineExpression = "")
        {
            var checkedPipeline = Guard.NotNull(pipeline, pipelineExpression);

            return Many(checkedPipeline, input, Decoded(checkedPipeline, input, source), options ?? ParseOptions.Default);
        }

        /// <summary>
        /// Parses the bytes <paramref name="input"/> reads as a sequence of items, running this pipeline's parser at the start and again after each item until the stream ends.
        /// </summary>
        /// <param name="input">The stream to parse from, whose reads block until it has bytes or ends, such as a file, a socket, or a <c>PipeReader</c>'s <c>AsStream()</c>. The parse leaves it open.</param>
        /// <param name="options">The settings for the parse, or <see langword="null"/> for <see cref="ParseOptions.Default"/>.</param>
        /// <param name="pipelineExpression">
        /// The caller's expression for this pipeline, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>
        /// Each item's result, parsed as it is asked for, reading only as far as that item needs.
        /// A <see cref="Failed"/> result is the last, and holds no text, so <see cref="Diagnostics.Render()"/> writes its error on one line.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Each item starts with no furthest failure in any layer, and failures are recorded as the parse goes.
        /// A layer that fails while the next item is being looked for ends the sequence with that next item's <see cref="Failed"/> result.
        /// Spans hold offsets of the bytes read and, above <see cref="Layer.Utf8"/>, character offsets in the decoded text, as over the same bytes in memory.
        /// </para>
        /// <para>
        /// Nothing is read until the sequence is enumerated. Enumerate it once: a second enumeration reads on from wherever the stream is, counting offsets from 0 again.
        /// Ending an enumeration early returns its buffers' memory to their pools and leaves the stream open.
        /// Slices in the values are copies, since the bytes read are not kept.
        /// </para>
        /// </remarks>
        /// <exception cref="GrammarException">This pipeline is null. When enumerated, a grammar in it is not valid, or the parser succeeded without consuming input.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
        public IEnumerable<ParseResult<TOut>> ParseMany(Stream input, ParseOptions? options = null, [CallerArgumentExpression(nameof(pipeline))] string pipelineExpression = "")
        {
            var checkedPipeline = Guard.NotNull(pipeline, pipelineExpression);

            ArgumentNullException.ThrowIfNull(input);

            return checkedPipeline.ParseMany(() => Read(input), options ?? ParseOptions.Default, static () => null);
        }
    }

    /// <summary>
    /// Makes the text a failed result over <paramref name="input"/> holds: what <see cref="Encoding.UTF8"/> decodes after any byte-order mark, if the pipeline's first layer is <see cref="Layer.Utf8"/>, and otherwise none.
    /// </summary>
    private static Func<SourceText?> Decoded<TOut>(Pipeline<byte, TOut> pipeline, ReadOnlyMemory<byte> input, string? source) =>
        pipeline.DecodesUtf8
        ? () => new SourceText(Encoding.UTF8.GetString(WithoutByteOrderMark(input.Span)), source)
        : static () => null;

    private static IEnumerable<ParseResult<TOut>> Many<TIn, TOut>(Pipeline<TIn, TOut> pipeline, ReadOnlyMemory<TIn> input, Func<SourceText?> text, ParseOptions options) =>
        pipeline.ParseMany(() => new MemorySource<TIn>(input), options, text);

    private static StreamBuffer<char> Read(TextReader reader) =>
        new(new ReaderUpstream(reader));

    private static StreamBuffer<byte> Read(Stream stream) =>
        new(new StreamUpstream(stream));

    private static ParseResult<TOut> Run<TIn, TOut>(Pipeline<TIn, TOut> pipeline, ReadOnlyMemory<TIn> input, Func<SourceText?> text, ParseOptions options)
    {
        var root = new MemorySource<TIn>(input);

        if (pipeline.TryParse(root, options, recordFailures: false, out var value, out _))
        {
            return new Parsed<TOut>(value);
        }

        return pipeline.TryParse(root, options, recordFailures: true, out value, out var error)
            ? new Parsed<TOut>(value)
            : new Failed(Diagnostics.Of(error, text()));
    }

    private static Func<SourceText?> Text(ReadOnlyMemory<char> input, string? source) =>
        () => new SourceText(input, source);

    private static ReadOnlySpan<byte> WithoutByteOrderMark(ReadOnlySpan<byte> bytes) =>
        bytes.StartsWith(Encoding.UTF8.Preamble)
        ? bytes[Encoding.UTF8.Preamble.Length..]
        : bytes;

    /// <summary>
    /// Starts a pipeline with <paramref name="layer"/>, which reads the input. End it in a parser with <c>Then</c>.
    /// </summary>
    /// <typeparam name="TIn">The type of item the pipeline reads, such as <see cref="char"/>.</typeparam>
    /// <typeparam name="TOut">The type of item the layer produces, such as a token type.</typeparam>
    /// <param name="layer">The first layer, such as a lexer.</param>
    /// <param name="layerExpression">Filled in by the compiler with the expression for <paramref name="layer"/>.</param>
    /// <returns><paramref name="layer"/> itself: add layers to it with <see cref="Layer{TIn, TOut}.Then{TNext}(Layer{TOut, TNext}, string)"/>, and end it with <see cref="Layer{TIn, TOut}.Then{TValue}(Parser{TOut, TValue}, string)"/>.</returns>
    /// <exception cref="GrammarException"><paramref name="layer"/> is null.</exception>
    public static Layer<TIn, TOut> From<TIn, TOut>(Layer<TIn, TOut> layer, [CallerArgumentExpression(nameof(layer))] string layerExpression = "") =>
        Guard.NotNull(layer, layerExpression);
}
