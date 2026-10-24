using System.Text;

namespace Creole.Benchmarks;

/// <summary>
/// Inputs for <see cref="LoweringBenchmarks"/>, each shaped to exercise one lowering rewrite.
/// </summary>
public static class LoweringInputs
{
    private static readonly string[] _csharpWords =
    [
        "abstract", "as", "base", "bool", "break", "case", "class", "const", "default", "else", "enum", "false", "for", "foreach",
        "if", "in", "int", "internal", "is", "namespace", "new", "null", "out", "override", "private", "public", "readonly",
        "ref", "return", "sealed", "static", "string", "struct", "switch", "this", "throw", "true", "using", "var", "void",
        "while", "iffy", "classes", "value", "result", "cursor", "Position", "_index", "item2", "TryParse",
    ];

    private static readonly string[] _csharpOperators =
    [
        "{", "}", "(", ")", "[", "]", ".", ",", ";", ":", "+", "-", "*", "/", "%", "=", "==", "!=", "<", "<=", "<<", "<<=", ">",
        ">=", ">>", "=>", "&&", "||", "!", "?", "??", "??=", "++", "--", "+=", "->", "::", "..", "@", "#",
    ];

    private static readonly Lazy<string> _lazyCSharp = new(() => GenerateCSharp(seed: 8, targetLength: 100_000));
    private static readonly Lazy<string> _lazyDigits = new(() => GenerateDigits(seed: 7, length: 100_000));
    private static readonly Lazy<JsonToken[]> _lazyJsonTokens = new(() => JsonTokenLexer.Lex(Json));
    private static readonly Lazy<string> _lazyStrings = new(() => GenerateStrings(seed: 6, targetLength: 100_000));
    private static readonly Lazy<string> _lazyWords = new(() => GenerateWords(seed: 5, targetLength: 100_000));

    /// <summary>
    /// C#-like text for <see cref="LongestLexers.CSharp"/>: keywords, identifiers, numbers, strings, characters, operators and comments,
    /// with a few characters that no rule matches.
    /// </summary>
    public static string CSharp =>
        _lazyCSharp.Value;

    /// <summary>
    /// Decimal digits.
    /// </summary>
    public static string Digits =>
        _lazyDigits.Value;

    /// <summary>
    /// The medium JSON document (<see cref="JsonInputs.Medium"/>, 104 KB).
    /// </summary>
    public static string Json =>
        JsonInputs.Get(JsonInputs.Medium);

    /// <summary>
    /// <see cref="Json"/> lexed to tokens.
    /// </summary>
    public static JsonToken[] JsonTokens =>
        _lazyJsonTokens.Value;

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

    private static string GenerateCSharp(int seed, int targetLength)
    {
        var random = new Random(seed);
        var builder = new StringBuilder();

        while (builder.Length < targetLength)
        {
            switch (random.Next(20))
            {
                case < 7:
                    builder.Append(_csharpWords[random.Next(_csharpWords.Length)]);
                    break;
                case < 12:
                    builder.Append(_csharpOperators[random.Next(_csharpOperators.Length)]);
                    break;
                case 12:
                    builder.Append(random.Next(1_000));
                    break;
                case 13:
                    builder.Append(random.Next(100)).Append('.').Append(random.Next(100)).Append(random.Next(2) == 0 ? "e-3f" : "");
                    break;
                case 14:
                    builder.Append("0x").Append(random.Next(0x10000).ToString("X", null));
                    break;
                case 15:
                    builder.Append('"').Append(_csharpWords[random.Next(_csharpWords.Length)]).Append(random.Next(2) == 0 ? "\\\"" : " ").Append('"');
                    break;
                case 16:
                    builder.Append(random.Next(2) == 0 ? "'c'" : "'\\n'");
                    break;
                case 17:
                    builder.Append("// ").Append(_csharpWords[random.Next(_csharpWords.Length)]).Append('\n');
                    break;
                case 18:
                    builder.Append("/* ").Append(_csharpWords[random.Next(_csharpWords.Length)]).Append(random.Next(2) == 0 ? " ** */" : " */");
                    break;
                default:
                    builder.Append(random.Next(4) == 0 ? "\u00e9" : "\t");
                    break;
            }

            builder.Append(random.Next(8) == 0 ? '\n' : ' ');
        }

        return builder.ToString();
    }

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
