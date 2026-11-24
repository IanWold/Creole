using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// Starts pipelines and runs them: <c>Pipeline.From(lexer).Then(parser).Parse(text)</c>.
/// </summary>
/// <remarks>
/// <para>
/// A parse over input in memory runs every layer and the parser without recording failures.
/// Only if it fails does it run the whole pipeline again from the start, with new buffers and every layer recording, to build its error.
/// So a successful parse pays nothing for errors, a failed parse costs up to about two and a half times as much, and the grammar's functions run again when a parse fails.
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

            return Run(checkedPipeline, input.AsMemory(), source, options ?? ParseOptions.Default);
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
            Run(Guard.NotNull(pipeline, pipelineExpression), input, source, options ?? ParseOptions.Default);
    }

    private static ParseResult<TOut> Run<TOut>(Pipeline<char, TOut> pipeline, ReadOnlyMemory<char> input, string? source, ParseOptions options)
    {
        var root = new MemorySource<char>(input);

        if (pipeline.TryParse(root, options, recordFailures: false, out var value, out _))
        {
            return new Parsed<TOut>(value);
        }

        return pipeline.TryParse(root, options, recordFailures: true, out value, out var error)
            ? new Parsed<TOut>(value)
            : new Failed(new Diagnostics([error], new SourceText(input, source)));
    }

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
