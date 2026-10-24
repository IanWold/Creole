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
}
