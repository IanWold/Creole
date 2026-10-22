using System.Text;

namespace Creole.Benchmarks;

/// <summary>
/// Inputs for <see cref="LoweringBenchmarks"/>, each shaped to exercise one lowering rewrite.
/// </summary>
public static class LoweringInputs
{
    private static readonly Lazy<string> _lazyDigits = new(() => GenerateDigits(seed: 7, length: 100_000));
    private static readonly Lazy<string> _lazyStrings = new(() => GenerateStrings(seed: 6, targetLength: 100_000));
    private static readonly Lazy<string> _lazyWords = new(() => GenerateWords(seed: 5, targetLength: 100_000));

    /// <summary>
    /// Decimal digits.
    /// </summary>
    public static string Digits =>
        _lazyDigits.Value;

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

    private static string GenerateDigits(int seed, int length)
    {
        var random = new Random(seed);

        return string.Create(length, random, static (chars, random) =>
        {
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = (char)random.Next('0', '9' + 1);
            }
        });
    }

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
