using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

/// <summary>
/// The two lexers of the INT-06 prototypes, as Creole parsers: a JSON lexer of 12 rules and a C#-like lexer of 138.
/// Both wrap their numbers' fraction and exponent in <c>Atomic</c>, and the C#-like block comment uses an <c>Atomic</c> that contains a repetition,
/// so both lie outside fragment E and inside E′ (research note "Commit-aware automaton prototypes", findings 5 and 19).
/// </summary>
internal static class LexerSamples
{
    /// <summary>
    /// C#-like text for the C#-like lexer, with comments, literals, operators, an identifier that starts with a keyword, and characters no rule matches.
    /// </summary>
    public const string CSharpText = """
        /* header ** comment */ /**/ /***/ namespace Creole.Tests; // a line comment
        public static class iffy { int x = 0x1F + 12.5e-3f * 'c' - '\n' / 7.; string s = "a\"b\\";
        x <<= 2; y ??= z => q?.r; if (a <= b && c != d || !e) return @x #y $z; é 1e 1.e5 /* never closed
        """;

    /// <summary>
    /// JSON-like text for the JSON lexer, including numbers and escapes that fail part-way.
    /// </summary>
    public const string JsonText = """
        {"name": "Creole", "version": -1.25e+3, "tags": ["a\"b", "\u00e9\n", true, false, null],
         "nested": {"x": 0, "y": [1, 2.5, 3E-2]}, "bad": 01, "frac": 1., "exp": 1e, "neg": -, "u": "\u12G4"}
        """;

    private static readonly string[] _keywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
        "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long",
        "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected", "public",
        "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
        "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void",
        "volatile", "while",
    ];

    private static readonly string[] _operators =
    [
        "{", "}", "[", "]", "(", ")", ".", ",", ":", ";", "+", "-", "*", "/", "%", "&", "|", "^", "!", "~", "=", "<", ">", "?",
        "??", "::", "++", "--", "&&", "||", "->", "==", "!=", "<=", ">=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<",
        "<<=", ">>", ">>=", "=>", "??=", "..", "@", "#", "$",
    ];

    private static Parser<char, char> Digit09 => Range('0', '9');

    private static Parser<char, char> Hex => Of(CharSet.Range('0', '9').Union(CharSet.Range('a', 'f')).Union(CharSet.Range('A', 'F')));

    private static Parser<char, char> AnyOf(string chars) => Of(CharSet.Of(chars));

    private static Parser<char, T> Flat<T>(Parser<char, T>[] p) => p.Length switch
    {
        1 => p[0],
        2 => Grammar<char>.Longest(p[0], p[1]),
        3 => Grammar<char>.Longest(p[0], p[1], p[2]),
        4 => Grammar<char>.Longest(p[0], p[1], p[2], p[3]),
        5 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4]),
        6 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5]),
        7 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6]),
        8 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7]),
        9 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8]),
        10 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9]),
        11 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10]),
        12 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11]),
        13 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11], p[12]),
        14 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11], p[12], p[13]),
        15 => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11], p[12], p[13], p[14]),
        _ => Grammar<char>.Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11], p[12], p[13], p[14], p[15]),
    };

    private static Parser<char, char> Of(CharSet set) => new CharClassNode(set, null);

    /// <summary>
    /// The C#-like lexer: whitespace, comments, the 77 keywords, identifiers, numbers, strings, characters and 54 operators.
    /// </summary>
    public static Parser<char, Unit>[] CSharp()
    {
        var letter = CharSet.Range('a', 'z').Union(CharSet.Range('A', 'Z')).Union(CharSet.Of('_'));
        var identifier = Seq(Of(letter), Of(letter.Union(CharSet.Range('0', '9'))).SkipMany()).Ignore();
        var whitespace = AnyOf(" \t\n\r").SkipAtLeastOnce();
        var lineComment = Seq(String("//"), NoneOf('\n').SkipMany()).Ignore();
        var blockComment = Seq(
            String("/*"),
            NoneOf('*').Or(Atomic(Seq(Char('*').SkipAtLeastOnce(), NoneOf('*', '/')).Select(static (_, c) => c))).SkipMany(),
            Char('*').SkipAtLeastOnce(),
            Char('/')).Ignore();
        var number = Seq(
            Digit09.SkipAtLeastOnce(),
            Atomic(Seq(Char('.'), Digit09.SkipAtLeastOnce())).Optional(),
            Atomic(Seq(AnyOf("eE"), AnyOf("+-").Optional(), Digit09.SkipAtLeastOnce())).Optional(),
            AnyOf("fFdDmMuUlL").Optional()).Ignore();
        var hexNumber = Seq(String("0x"), Hex.SkipAtLeastOnce()).Ignore();
        var escapeOrPlain = NoneOf('"', '\\', '\n').Or(Seq(Char('\\'), NoneOf('\n')).Select(static (_, c) => c));
        var text = Seq(Char('"'), escapeOrPlain.SkipMany(), Char('"')).Ignore();
        var character = Seq(Char('\''), NoneOf('\'', '\\', '\n').Or(Seq(Char('\\'), NoneOf('\n')).Select(static (_, c) => c)), Char('\'')).Ignore();

        return
        [
            whitespace, lineComment, blockComment,
            .. _keywords.Select(static keyword => String(keyword).Ignore()),
            identifier, number, hexNumber, text, character,
            .. _operators.Select(static op => String(op).Ignore()),
        ];
    }

    /// <summary>
    /// The JSON lexer: whitespace, the six punctuation tokens, the three literals, numbers and strings.
    /// </summary>
    public static Parser<char, Unit>[] Json()
    {
        var whitespace = AnyOf(" \t\n\r").SkipAtLeastOnce();
        var number = Seq(
            Char('-').Optional(),
            Char('0').Or(Seq(Range('1', '9'), Digit09.SkipMany()).Select(static (c, _) => c)),
            Atomic(Seq(Char('.'), Digit09.SkipAtLeastOnce())).Optional(),
            Atomic(Seq(AnyOf("eE"), AnyOf("+-").Optional(), Digit09.SkipAtLeastOnce())).Optional()).Ignore();
        var plain = Of(CharSet.Of('"', '\\').Union(CharSet.Range('\0', '\x1f')).Complement());
        var escape = Seq(Char('\\'), AnyOf("\"\\/bfnrt").Or(Seq(Char('u'), Hex, Hex, Hex, Hex).Select(static (u, _, _, _, _) => u))).Select(static (_, c) => c);
        var text = Seq(Char('"'), plain.Or(escape).SkipMany(), Char('"')).Ignore();

        return
        [
            whitespace,
            .. "{}[]:,".Select(static c => Char(c).Ignore()),
            String("true").Ignore(), String("false").Ignore(), String("null").Ignore(),
            number, text,
        ];
    }

    /// <summary>
    /// <paramref name="count"/> distinct lowercase words of two to nine letters as <c>String</c> rules, in ordinal order, then an identifier rule.
    /// The words come from a fixed linear congruential sequence, so every run builds the same rules.
    /// </summary>
    public static Parser<char, Unit>[] Keywords(int count)
    {
        var words = new SortedSet<string>(StringComparer.Ordinal);
        var state = 1u;

        int Next(int bound)
        {
            state = (state * 1_664_525) + 1_013_904_223;

            return (int)((state >> 16) % (uint)bound);
        }

        while (words.Count < count)
        {
            var length = 2 + Next(8);

            words.Add(new string([.. Enumerable.Range(0, length).Select(_ => (char)('a' + Next(26)))]));
        }

        var letter = CharSet.Range('a', 'z').Union(CharSet.Range('A', 'Z')).Union(CharSet.Of('_'));
        var identifier = Seq(Of(letter), Of(letter.Union(CharSet.Range('0', '9'))).SkipMany()).Ignore();

        return [.. words.Select(static word => String(word).Ignore()), identifier];
    }

    /// <summary>
    /// <c>Longest</c> over any number of alternatives, nested 16 to a level as the docs advise beyond 16.
    /// </summary>
    public static Parser<char, T> Longest<T>(Parser<char, T>[] alternatives)
    {
        Parser<char, T> Level(int from) =>
            alternatives.Length - from <= 16 ? Flat(alternatives[from..]) : Flat([.. alternatives[from..(from + 15)], Level(from + 15)]);

        return Level(0);
    }
}
