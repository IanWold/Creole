using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// Text capture at character layers, called as methods on a parser: <c>identifier.Slice()</c>, <c>number.Text()</c>.
/// </summary>
/// <remarks>
/// Both discard the parser's own result and produce the characters it consumed.
/// Tokens should normally carry slices, which do not allocate; call <see cref="Text"/> where a <see cref="string"/> is needed.
/// </remarks>
public static class TextCapture
{
    extension<TOut>(Parser<char, TOut> parser)
    {
        /// <summary>
        /// A parser that runs this parser and produces a view of the characters it consumed, without copying them.
        /// </summary>
        /// <param name="parserExpression">Filled in by the compiler with the expression this method is called on.</param>
        /// <returns>A parser that matches the same input as this parser.</returns>
        /// <exception cref="GrammarException">This parser is null.</exception>
        public Parser<char, ReadOnlyMemory<char>> Slice([CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
            new SliceNode<TOut>(Guard.NotNull(parser, parserExpression));

        /// <summary>
        /// A parser that runs this parser and produces the characters it consumed as a new <see cref="string"/>.
        /// </summary>
        /// <param name="parserExpression">Filled in by the compiler with the expression this method is called on.</param>
        /// <returns>A parser that matches the same input as this parser.</returns>
        /// <exception cref="GrammarException">This parser is null.</exception>
        public Parser<char, string> Text([CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
            new TextNode<TOut>(Guard.NotNull(parser, parserExpression));
    }
}
