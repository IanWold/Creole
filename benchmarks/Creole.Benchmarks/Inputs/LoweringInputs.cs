using System.Text;

namespace Creole.Benchmarks;

/// <summary>
/// Inputs for <see cref="LoweringBenchmarks"/>, each shaped to exercise one lowering rewrite.
/// </summary>
public static class LoweringInputs
{
    private static readonly Lazy<string> _lazyStrings = new(() => GenerateStrings(seed: 6, targetLength: 100_000));
    private static readonly Lazy<string> _lazyWords = new(() => GenerateWords(seed: 5, targetLength: 100_000));

    /// <summary>
    /// Quoted strings whose bodies are 20 to 2,000 printable ASCII characters other than <c>"</c> and <c>\</c>: long runs.
    /// </summary>
    public static string Strings =>
        _lazyStrings.Value;

    /// <summary>
    /// Lower-case words of 1 to 12 letters, each followed by 1 to 4 spaces: short runs.
    /// </summary>
    public static string Words =>
        _lazyWords.Value;

    private static string GenerateStrings(int seed, int targetLength)
    {
        var random = new Random(seed);
        var builder = new StringBuilder();

        while (builder.Length < targetLength)
        {
            builder.Append('"');

            for (var length = random.Next(20, 2_001); length > 0; length--)
            {
                char c;

                do
                {
                    c = (char)random.Next(' ', '~' + 1);
                }
                while (c is '"' or '\\');

                builder.Append(c);
            }

            builder.Append('"');
        }

        return builder.ToString();
    }

    private static string GenerateWords(int seed, int targetLength)
    {
        var random = new Random(seed);
        var builder = new StringBuilder();

        while (builder.Length < targetLength)
        {
            for (var length = random.Next(1, 13); length > 0; length--)
            {
                builder.Append((char)random.Next('a', 'z' + 1));
            }

            builder.Append(' ', random.Next(1, 5));
        }

        return builder.ToString();
    }
}
