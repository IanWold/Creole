using System.Collections.Immutable;
using System.Text;
using CsCheck;

namespace Creole.Tests;

file enum Open
{
    Brace,
    Substitution,
}

file enum TokenKind
{
    Identifier,
    OpenBrace,
    CloseBrace,
    EmptyBraces,
    NoSubstitution,
    Head,
    Middle,
    Tail,
}

file readonly record struct Lexeme(TokenKind Kind, string Text);

file abstract record Code;

file record Word(string Text) : Code;

file record Block(Code[] Items, bool Tight) : Code;

file record Template(string First, (Code[] Code, string Text)[] Parts) : Code;

/// <summary>
/// A lexer for identifiers, braces and template literals with substitutions, as JavaScript has, whose state is the stack of braces and substitutions still open.
/// </summary>
/// <remarks>
/// <para>
/// Whether a <c>}</c> closes a brace or a substitution depends on that stack, and so does whether what follows it is code or template text.
/// Template text may hold braces of its own, which neither open nor close anything.
/// </para>
/// <para>
/// The grammar changes the state where the parse goes back, on purpose, so the lexer is right only if backtracking puts the state back:
/// a <c>Longest</c> whose <c>{</c> alternative pushes a brace and then loses to <c>{}</c>, after the winner ran;
/// an <c>Atomic</c> that pops, finds a substitution rather than a brace, and rewinds;
/// and a <c>Lookahead</c>, a <c>Not</c> and an <c>Optional</c> whose parsers change the state.
/// </para>
/// </remarks>
file class TemplateLexer : CharGrammar
{
    private static readonly Parser<char, Lexeme> _close =
        Atomic(
            Char('}')
                .Scan<ImmutableStack<Open>, Open?>(static (_, open, _) => open.IsEmpty
                    ? (null, open)
                    : (open.Peek(), open.Pop())
                )
                .Where(static closed => closed == Open.Brace)
        )
            .As(new Lexeme(TokenKind.CloseBrace, "}"));

    private static readonly Parser<char, Lexeme> _emptyBraces = String("{}").As(new Lexeme(TokenKind.EmptyBraces, "{}"));

    private static readonly Parser<char, Lexeme> _identifier =
        Not(Letter.Scan<ImmutableStack<Open>, char>(static (letter, open, _) => (letter, open.Push(Open.Brace))).Then(Char('#')))
            .Then(SetState(ImmutableStack<Open>.Empty).Then(Char('#')).Optional())
            .Then(Letter.AtLeastOnce().Text())
            .Select(static text => new Lexeme(TokenKind.Identifier, text));

    private static readonly Parser<char, Lexeme> _open =
        Char('{').Scan<ImmutableStack<Open>, Lexeme>(static (_, open, _) => (new Lexeme(TokenKind.OpenBrace, "{"), open.Push(Open.Brace)));

    private static readonly Parser<char, string> _text = NoneOf('`', '$').SkipMany().Text();

    private static readonly Parser<char, Lexeme> _continuation =
        Seq(
            Char('}'),
            _text,
            OneOf(
                String("${").As(TokenKind.Middle),
                Char('`').Scan<ImmutableStack<Open>, TokenKind>(static (_, open, _) => (TokenKind.Tail, open.IsEmpty
                    ? open
                    : open.Pop())
                )
            )
        )
            .Select(static (_, text, kind) => new Lexeme(kind, kind == TokenKind.Middle
                ? $"}}{text}${{"
                : $"}}{text}`"));

    private static readonly Parser<char, TokenKind> _templateEnd = OneOf(
        Lookahead(Char('`').Scan<ImmutableStack<Open>, char>(static (tick, open, _) => (tick, open.Push(Open.Brace)))).Then(Char('`')).As(TokenKind.NoSubstitution),
        String("${").Scan<ImmutableStack<Open>, TokenKind>(static (_, open, _) => (TokenKind.Head, open.Push(Open.Substitution)))
    );

    private static readonly Parser<char, Lexeme> _template =
        Seq(Char('`'), _text, _templateEnd)
            .Select(static (_, text, kind) => new Lexeme(kind, kind == TokenKind.Head
                ? $"`{text}${{"
                : $"`{text}`")
            );

    public static readonly Layer<char, Lexeme> Lexer = Layer.Tokens(
        ImmutableStack<Open>.Empty,
        Longest(_identifier, _emptyBraces, _open, _close.Or(_continuation), _template),
        Whitespace.SkipMany(),
        name: "templates"
    );
}

/// <summary>
/// Programs as trees of words, blocks and templates, and their text.
/// </summary>
/// <remarks>
/// Writing a program out also gives the tokens it is made of and the stack of open braces and substitutions before each, which is what the lexer must produce and what each token's slot must record as the state before its step.
/// </remarks>
file static class Programs
{
    public static readonly Gen<Code[]> All = Generate(3).Array[0, 4];

    private static Gen<Code> Generate(int depth)
    {
        var word = Gen.Char["abc"].Array[1, 3].Select(static letters => (Code)new Word(new string(letters)));

        if (depth == 0)
        {
            return word;
        }

        var items = Generate(depth - 1).Array[0, 3];
        var text = Gen.Char["ab {}"].Array[0, 4].Select(static characters => new string(characters));
        var block = Gen.Select(items, Gen.Bool, static (inner, tight) => (Code)new Block(inner, tight));
        var template = Gen.Select(text, Gen.Select(items, text).Array[0, 3], static (first, parts) => (Code)new Template(first, parts));

        return Gen.Frequency((3, word), (1, block), (1, template));
    }

    public static string Describe(IEnumerable<(Lexeme Lexeme, ImmutableStack<Open> Before)> tokens) =>
        string.Join(" ", tokens.Select(static entry => $"{entry.Lexeme.Text}[{string.Join("", entry.Before.Select(static open => open == Open.Brace ? '{' : '$'))}]"));

    public static string? Difference(Code[] program, int gap, int segmentSize, bool recordFailures)
    {
        var (text, expected) = Render(program, gap);
        var actual = LayerRun.Items(TemplateLexer.Lexer, text, segmentSize, recordFailures: recordFailures).Select(static entry => (entry.Item, (ImmutableStack<Open>)entry.Slot.StateBefore!)).ToList();

        var agrees = actual.Count == expected.Count
            && actual.Zip(expected).All(static pair => pair.First.Item1 == pair.Second.Lexeme && pair.First.Item2.SequenceEqual(pair.Second.Before));

        return agrees
            ? null
            : $"on \"{text}\" in segments of {segmentSize} {(recordFailures ? "with" : "without")} a failure log: expected {Describe(expected)}, lexed {Describe(actual)}";
    }

    /// <summary>
    /// Writes <paramref name="program"/> out as text, with <paramref name="gap"/> spaces before each token of code, and gives the tokens it is made of with the stack before each.
    /// </summary>
    public static (string Text, List<(Lexeme Lexeme, ImmutableStack<Open> Before)> Tokens) Render(Code[] program, int gap)
    {
        var text = new StringBuilder();
        var tokens = new List<(Lexeme Lexeme, ImmutableStack<Open> Before)>();
        var stack = ImmutableStack<Open>.Empty;

        void Add(TokenKind kind, string written, ImmutableStack<Open> after)
        {
            tokens.Add((new Lexeme(kind, written), stack));
            text.Append(written);
            stack = after;
        }

        void Space() =>
            text.Append(' ', gap);

        void Codes(Code[] items)
        {
            foreach (var item in items)
            {
                Space();
                One(item);
            }
        }

        void One(Code code)
        {
            switch (code)
            {
                case Word word:
                    Add(TokenKind.Identifier, word.Text, stack);
                    break;

                case Block { Items.Length: 0, Tight: true }:
                    Add(TokenKind.EmptyBraces, "{}", stack);
                    break;

                case Block block:
                    Add(TokenKind.OpenBrace, "{", stack.Push(Open.Brace));
                    Codes(block.Items);
                    Space();
                    Add(TokenKind.CloseBrace, "}", stack.Pop());
                    break;

                case Template { Parts.Length: 0 } template:
                    Add(TokenKind.NoSubstitution, $"`{template.First}`", stack);
                    break;

                case Template template:
                    Add(TokenKind.Head, $"`{template.First}${{", stack.Push(Open.Substitution));

                    for (var index = 0; index < template.Parts.Length; index++)
                    {
                        var (inner, after) = template.Parts[index];

                        Codes(inner);
                        Space();

                        if (index < template.Parts.Length - 1)
                        {
                            Add(TokenKind.Middle, $"}}{after}${{", stack);
                        }
                        else
                        {
                            Add(TokenKind.Tail, $"}}{after}`", stack.Pop());
                        }
                    }

                    break;
            }
        }

        Codes(program);
        Space();

        return (text.ToString(), tokens);
    }
}

/// <summary>
/// LAY-04's criterion: a template-string brace-depth lexer backtracks correctly (§ 2. Layer-local state).
/// </summary>
public class TemplateLexerTests
{
    [Fact]
    public void TheLexerProducesTheTokensOfRandomProgramsWithTheStackBeforeEach() =>
        Gen.Select(Programs.All, Gen.Int[1, 2], Gen.OneOf(1, 2, 3, 1024), Gen.Bool).Sample(
            static (program, gap, segmentSize, recordFailures) => Programs.Difference(program, gap, segmentSize, recordFailures) is null,
            iter: 5_000,
            print: static sample => Programs.Difference(sample.Item1, sample.Item2, sample.Item3, sample.Item4) ?? "no difference"
        );

    [Theory]
    [InlineData("{}", "{}[]")]
    [InlineData("{ }", "{[] }[{]")]
    [InlineData("`a}b{c`", "`a}b{c`[]")]
    [InlineData("`a${ {} }b`", "`a${[] {}[$] }b`[$]")]
    [InlineData("`a${ { x } }b${ `c${ d }e` }f`", "`a${[] {[$] x[{$] }[{$] }b${[$] `c${[$] d[$$] }e`[$$] }f`[$]")]
    public void TheLexerReadsBracesInCodeAndTemplateTextByTheStack(string text, string tokens) =>
        Assert.Equal(tokens, Programs.Describe(LayerRun.Items(TemplateLexer.Lexer, text).Select(static entry => (entry.Item, (ImmutableStack<Open>)entry.Slot.StateBefore!))));
}
