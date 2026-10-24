using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

public class RegularTests
{
    private static readonly Parser<char, char> _a = Char('a');
    private static readonly Parser<char, char> _b = Char('b');
    private static readonly Parser<char, char> _c = Char('c');
    private static readonly Parser<char, Unit> _digits = Range('0', '9').SkipAtLeastOnce();

    private static readonly Dictionary<string, (bool Regular, Func<object?> Decide)> _cases = new()
    {
        ["char class"] = (true, static () => Regular.Of(_a)),
        ["Any"] = (true, static () => Regular.Of(Any)),
        ["String"] = (true, static () => Regular.Of(String("ab"))),
        ["CIString"] = (true, static () => Regular.Of(CIString("ab"))),
        ["Seq"] = (true, static () => Regular.Of(Seq(_a, _b, _c))),
        ["Or"] = (true, static () => Regular.Of(_a.Or(_b))),
        ["OneOf"] = (true, static () => Regular.Of(OneOf(_a, _b, _c))),
        ["Optional"] = (true, static () => Regular.Of(_a.Optional())),
        ["Many"] = (true, static () => Regular.Of(_a.Many())),
        ["AtLeastOnce"] = (true, static () => Regular.Of(_a.AtLeastOnce())),
        ["SkipMany"] = (true, static () => Regular.Of(_a.SkipMany())),
        ["SkipAtLeastOnce"] = (true, static () => Regular.Of(_a.SkipAtLeastOnce())),
        ["Atomic"] = (true, static () => Regular.Of(Atomic(Seq(_a, _b)))),
        ["Select, As and Ignore"] = (true, static () => Regular.Of(_a.Select(static c => c.ToString()).As(1).Ignore())),
        ["Slice and Text"] = (true, static () => Regular.Of(_a.Many().Slice().Then(_b.Text()))),
        ["Named and Context"] = (true, static () => Regular.Of(_a.Named("a").Context("in a"))),

        ["Parsec-style decimal"] = (true, static () => Regular.Of(Seq(_digits, Seq(Char('.'), _digits).Optional()))),
        ["Atomic fraction"] = (true, static () => Regular.Of(Seq(_digits, Atomic(Seq(Char('.'), _digits)).Optional()))),
        ["Atomic exponent"] = (true, static () => Regular.Of(Atomic(Seq(Char('e'), Char('+').Optional(), _digits)).Optional())),
        ["block comment, no Atomic"] = (true, static () => Regular.Of(Seq(String("/*"), NoneOf('*').Or(Seq(Char('*').SkipAtLeastOnce(), NoneOf('*', '/')).Select(static (_, c) => c)).SkipMany(), Char('*').SkipAtLeastOnce(), Char('/')))),
        ["block comment, Atomic"] = (true, static () => Regular.Of(Seq(String("/*"), NoneOf('*').Or(Atomic(Seq(Char('*').SkipAtLeastOnce(), NoneOf('*', '/')).Select(static (_, c) => c))).SkipMany(), Char('*').SkipAtLeastOnce(), Char('/')))),
        ["string with escapes"] = (true, static () => Regular.Of(Seq(Char('"'), NoneOf('"', '\\').Or(Seq(Char('\\'), Any).Select(static (_, c) => c)).SkipMany(), Char('"')))),

        ["finding 2's counterexample"] = (false, static () => Regular.Of(Atomic(Seq(new CharClassNode(CharSet.Of('a', 'c'), null), String("cbc")).SkipMany()).Or(Atomic(_b).SkipMany()))),
        ["first version's counterexample"] = (false, static () => Regular.Of(Atomic(Seq(Atomic(String("ba").SkipMany()), _a.Or(_b))).SkipMany())),
        ["its variant that agrees"] = (true, static () => Regular.Of(Atomic(Seq(Atomic(String("ba").SkipMany()), _a)).SkipMany())),
        ["overlapping choice inside Atomic"] = (false, static () => Regular.Of(Atomic(Seq(String("ab").Or(String("a")), _c)))),
        ["overlapping choice, nothing after it"] = (true, static () => Regular.Of(Atomic(String("ab").Or(String("a"))))),
        ["nullable second branch at the end of Atomic"] = (false, static () => Regular.Of(Atomic(Seq(_a, _b).Optional()))),
        ["nullable second branch, overlapping rest"] = (false, static () => Regular.Of(Atomic(Seq(_a.Optional(), Seq(_a, _b))))),

        ["Rec"] = (false, static () => Regular.Of(Rec(static () => _a))),
        ["Bind"] = (false, static () => Regular.Of(_a.Bind(static _ => _b))),
        ["Where"] = (false, static () => Regular.Of(_a.Where(static c => c == 'a'))),
        ["Lookahead"] = (false, static () => Regular.Of(Lookahead(_a))),
        ["Not"] = (false, static () => Regular.Of(Not(_a))),
        ["Cut"] = (false, static () => Regular.Of(_a.Then(Cut))),
        ["Satisfy"] = (false, static () => Regular.Of(Satisfy(char.IsDigit))),
        ["Return"] = (false, static () => Regular.Of(Return('a'))),
        ["Fail"] = (false, static () => Regular.Of(Fail<char>("never"))),
        ["End"] = (false, static () => Regular.Of(End)),
        ["Token"] = (false, static () => Regular.Of(Token('a'))),
        ["SepBy"] = (false, static () => Regular.Of(_a.SepBy(_b))),
        ["Until"] = (false, static () => Regular.Of(_a.Until(_b))),
        ["Repeat with a maximum"] = (false, static () => Regular.Of(_a.Repeat(1, 3))),
        ["Repeat with a minimum above 1"] = (false, static () => Regular.Of(_a.Repeat(2))),
        ["repetition of a nullable item"] = (false, static () => Regular.Of(_a.Optional().Many())),
        ["nested Longest"] = (false, static () => Regular.Of(Seq(Longest(_a, _b), _c))),
        ["one excluded node deep inside"] = (false, static () => Regular.Of(Seq(_a, _b.Or(Satisfy(char.IsDigit)).Many()))),
    };

    public static TheoryData<string> Cases => [.. _cases.Keys];

    [Fact]
    public void ACaseInsensitiveStringMatchesEveryCaseVariantOfEachCharacter()
    {
        var term = Assert.IsType<RegularTerm.Atomic>(Regular.Of(CIString("ka")));
        var first = Assert.IsType<RegularTerm.Chars>(Assert.IsType<RegularTerm.Sequence>(term.Item).First);

        Assert.True(first.Set.Contains('k'));
        Assert.True(first.Set.Contains('K'));
        Assert.True(first.Set.Contains('K'));
        Assert.False(first.Set.Contains('a'));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void DecidesTheKnownCases(string name)
    {
        var (regular, decide) = _cases[name];

        Assert.Equal(regular, decide() is not null);
    }

    [Fact]
    public void EveryRuleOfBothPrototypeLexersIsRegular()
    {
        Assert.All(LexerSamples.Json(), static rule => Assert.NotNull(Regular.Of(rule)));
        Assert.All(LexerSamples.CSharp(), static rule => Assert.NotNull(Regular.Of(rule)));
        Assert.Equal(12, LexerSamples.Json().Length);
        Assert.Equal(138, LexerSamples.CSharp().Length);
    }

    [Fact]
    public void OptionalAndAtLeastOnceBecomeTheirDefinitions()
    {
        Assert.Equal(new RegularTerm.Choice(new RegularTerm.Chars(CharSet.Of('a')), RegularTerm.Empty.Instance), Regular.Of(_a.Optional()));
        Assert.Equal(
            new RegularTerm.Sequence(new RegularTerm.Chars(CharSet.Of('a')), new RegularTerm.Many(new RegularTerm.Chars(CharSet.Of('a')))),
            Regular.Of(_a.AtLeastOnce())
        );
    }
}
