using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Benchmarks;

/// <summary>
/// A JSON lexer of 12 rules and a C#-like lexer of 138, each one <c>Longest</c> whose value is the winning rule's index.
/// </summary>
/// <remarks>
/// The rules are those of <c>LexerSamples</c> in <c>Creole.Tests</c>, which checks that both lexers tokenize like the interpreter.
/// The C#-like lexer has more than 16 rules, so it nests <c>Longest</c> 16 to a level, as the docs advise.
/// </remarks>
public static class LongestLexers
{
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

    /// <summary>
    /// The C#-like lexer: whitespace, comments, the 77 keywords, identifiers, numbers, strings, characters and 54 operators.
    /// </summary>
    public static Parser<char, int> CSharp { get; } = Indexed(CSharpRules());

    /// <summary>
    /// The JSON lexer: whitespace, the six punctuation tokens, the three literals, numbers and strings.
    /// </summary>
    public static Parser<char, int> Json { get; } = Indexed(JsonRules());

    private static Parser<char, char> AnyOf(string chars) => Of(CharSet.Of(chars));

    private static Parser<char, Unit>[] CSharpRules()
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

    private static Parser<char, T> Flat<T>(Parser<char, T>[] p) => p.Length switch
    {
        1 => p[0],
        2 => Longest(p[0], p[1]),
        3 => Longest(p[0], p[1], p[2]),
        4 => Longest(p[0], p[1], p[2], p[3]),
        5 => Longest(p[0], p[1], p[2], p[3], p[4]),
        6 => Longest(p[0], p[1], p[2], p[3], p[4], p[5]),
        7 => Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6]),
        8 => Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7]),
        9 => Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8]),
        10 => Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9]),
        11 => Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10]),
        12 => Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11]),
        13 => Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11], p[12]),
        14 => Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11], p[12], p[13]),
        15 => Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11], p[12], p[13], p[14]),
        _ => Longest(p[0], p[1], p[2], p[3], p[4], p[5], p[6], p[7], p[8], p[9], p[10], p[11], p[12], p[13], p[14], p[15]),
    };

    private static Parser<char, int> Indexed(Parser<char, Unit>[] rules)
    {
        var indexed = rules.Select(static (rule, index) => rule.As(index)).ToArray();

        Parser<char, int> Level(int from) =>
            indexed.Length - from <= 16
                ? Flat(indexed[from..])
                : Flat([.. indexed[from..(from + 15)], Level(from + 15)]);

        return Level(0);
    }

    private static Parser<char, Unit>[] JsonRules()
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

    private static Parser<char, char> Of(CharSet set) => new CharClassNode(set, null);
}
